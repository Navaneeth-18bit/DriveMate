using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using adminPage.Models;
using adminPage.Utilities;

namespace adminPage.Controllers;

public class HomeController : Controller
{
    private const string BookingDraftSessionKey = "BookingDraft";

    private readonly ILogger<HomeController> _logger;
    private readonly DriveMateDbContext _context;

    public HomeController(ILogger<HomeController> logger, DriveMateDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required.";
            return View();
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Email and password are required.";
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash ?? ""))
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.Error = "Your account is inactive. Please contact support.";
            return View();
        }

        // Store user session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserName", user.Name ?? string.Empty);
        HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);

        return RedirectToAction("Index");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    public IActionResult NewUser()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(User user, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("confirmPassword", "Passwords do not match.");
            return View("NewUser", user);
        }

        if (password?.Length < 6)
        {
            ModelState.AddModelError("password", "Password must be at least 6 characters long.");
            return View("NewUser", user);
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return View("NewUser", user);
        }

        if (!ModelState.IsValid)
        {
            return View("NewUser", user);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("password", "Password is required.");
            return View("NewUser", user);
        }

        user.PasswordHash = PasswordHasher.HashPassword(password);
        user.CreatedAt = DateTime.Now;
        user.IsActive = true;

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Store user session after successful registration
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.Name ?? string.Empty);
            HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);

            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            return View("NewUser", user);
        }
    }

    public IActionResult Booking()
    {
        if (!IsUserLoggedIn())
        {
            TempData["Message"] = "Please log in to book a driver.";
            return RedirectToAction(nameof(Login));
        }

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!int.TryParse(userIdStr, out var userId))
        {
            TempData["Message"] = "Please log in to book a driver.";
            return RedirectToAction(nameof(Login));
        }

        var currentUser = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (currentUser == null)
        {
            TempData["Message"] = "Your user profile could not be found. Please log in again.";
            return RedirectToAction(nameof(Login));
        }

        ViewBag.CurrentUser = currentUser;
        PopulateDropdowns();
        return View("Booking/Create", new Booking
        {
            StartTime = DateTime.Now,
            UserId = currentUser.Id,
            UserName = currentUser.Name ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateBooking(Booking booking)
    {
        if (!IsUserLoggedIn())
        {
            TempData["Message"] = "Please log in to book a driver.";
            return RedirectToAction(nameof(Login));
        }

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!int.TryParse(userIdStr, out var userId))
        {
            TempData["Message"] = "Please log in to book a driver.";
            return RedirectToAction(nameof(Login));
        }

        var currentUser = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (currentUser == null)
        {
            TempData["Message"] = "Your user profile could not be found. Please log in again.";
            return RedirectToAction(nameof(Login));
        }

        booking.UserId = currentUser.Id;

        if (ModelState.IsValid)
        {
            var availableDrivers = _context.Drivers
                .OrderBy(d => d.Name)
                .ToList();

            if (availableDrivers.Count == 0)
            {
                TempData["Message"] = "No drivers are registered yet.";
                return RedirectToAction("Booking");
            }

            StoreBookingDraft(booking);

            ViewBag.AvailableDrivers = availableDrivers;
            return View("Booking/Available_slots", booking);
        }

        ViewBag.CurrentUser = currentUser;
        PopulateDropdowns();
        return View("Booking/Create", booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]

    public IActionResult ConfirmBooking(int selectedDriverId, string bookingName, string bookingPickupLocation, DateTime bookingStartTime)
    {
        if (!IsUserLoggedIn())
        {
            TempData["Message"] = "Please log in to book a driver.";
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrWhiteSpace(bookingName))
        {
            TempData["Error"] = "Please enter your name before confirming the booking.";
            return RedirectToAction("Booking");
        }

        if (selectedDriverId == 0)
        {
            TempData["Error"] = "Please select a driver.";
            return RedirectToAction("Booking");
        }

        var selectedDriver = _context.Drivers.FirstOrDefault(driver => driver.DriverId == selectedDriverId);
        if (selectedDriver == null)
        {
            TempData["Error"] = "The selected driver could not be found.";
            return RedirectToAction("Booking");
        }

        if (!selectedDriver.IsAvailable)
        {
            TempData["Error"] = "The selected driver is no longer available. Please choose another driver.";
            return RedirectToAction("Booking");
        }

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!int.TryParse(userIdStr, out var userId))
        {
            TempData["Message"] = "Please log in to book a driver.";
            return RedirectToAction(nameof(Login));
        }

        var draft = GetBookingDraft();
        var finalName = !string.IsNullOrWhiteSpace(draft?.Name) ? draft!.Name : bookingName.Trim();
        var finalPickupLocation = !string.IsNullOrWhiteSpace(draft?.PickupLocation) ? draft!.PickupLocation : bookingPickupLocation;
        var finalStartTime = draft?.StartTime ?? bookingStartTime;

        if (string.IsNullOrWhiteSpace(finalName))
        {
            TempData["Error"] = "Please enter your name before confirming the booking.";
            return RedirectToAction("Booking");
        }

        var booking = new Booking
        {
            UserName = finalName.Trim(),
            UserId = userId,
            DriverId = selectedDriverId,
            PickupLocation = finalPickupLocation,
            StartTime = finalStartTime,
            DropLocation = null,
            EndTime = null,
            Status = "Pending",
            TotalAmount = 0
        };

        using var transaction = _context.Database.BeginTransaction();

        selectedDriver.IsAvailable = false;
        _context.Bookings.Add(booking);

        _context.SaveChanges();
        transaction.Commit();

        HttpContext.Session.Remove(BookingDraftSessionKey);

        TempData["Success"] = "Booking confirmed successfully!";
        return RedirectToAction("RecentBookings", "Booking", new { userId = userId });
    }

    private void PopulateDropdowns()
    {
        ViewBag.AvailableDrivers = _context.Drivers
            .OrderBy(d => d.Name)
            .ToList();
    }

    private void StoreBookingDraft(Booking booking)
    {
        var draft = new BookingDraft
        {
            Name = booking.UserName,
            PickupLocation = booking.PickupLocation ?? string.Empty,
            StartTime = booking.StartTime
        };

        HttpContext.Session.SetString(BookingDraftSessionKey, JsonSerializer.Serialize(draft));
    }

    private BookingDraft? GetBookingDraft()
    {
        var draftJson = HttpContext.Session.GetString(BookingDraftSessionKey);
        if (string.IsNullOrWhiteSpace(draftJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BookingDraft>(draftJson);
        }
        catch
        {
            return null;
        }
    }

    private bool IsUserLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
    }

    private sealed class BookingDraft
    {
        public string Name { get; set; } = string.Empty;

        public string PickupLocation { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
