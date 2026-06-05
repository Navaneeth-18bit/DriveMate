using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using adminPage.Models;
using adminPage.Filters;


namespace adminPage.Controllers
{
    [AdminSessionAuthorize]
    public class BookingController : Controller
    {
        private readonly DriveMateDbContext _context;

        public BookingController(DriveMateDbContext context)
        {
            _context = context;
        }

        // View Bookings
        public IActionResult Index()
        {
            var bookings = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Driver)
                .OrderByDescending(b => b.StartTime)
                .ToList();

            return View(bookings);
        }

        [AllowAnonymous]
        public IActionResult RecentBookings(int? userId = null)
        {
            var adminLoggedIn = HttpContext.Session.GetString("AdminLoggedIn") == "true";
            var userIdFromSession = HttpContext.Session.GetString("UserId");

            if (!adminLoggedIn && string.IsNullOrEmpty(userIdFromSession))
            {
                return RedirectToAction("Login", "Home");
            }

            if (int.TryParse(userIdFromSession, out var sessionUserId))
            {
                if (!adminLoggedIn)
                {
                    userId = sessionUserId;
                }
                else if (!userId.HasValue)
                {
                    userId = sessionUserId;
                }
            }

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Home");
            }

            var selectedUser = _context.Users.FirstOrDefault(u => u.Id == userId.Value);
            if (selectedUser == null)
            {
                return NotFound();
            }

            var bookings = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Driver)
                .Where(b => b.UserId == userId.Value)
                .OrderByDescending(b => b.StartTime)
                .ToList();

            ViewBag.SelectedUser = selectedUser;
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult FinishRide(int bookingId, string dropLocation, DateTime? endTime)
        {
            var adminLoggedIn = HttpContext.Session.GetString("AdminLoggedIn") == "true";
            var userIdFromSession = HttpContext.Session.GetString("UserId");

            var booking = _context.Bookings
                .Include(b => b.Driver)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (!adminLoggedIn)
            {
                if (string.IsNullOrEmpty(userIdFromSession) || !int.TryParse(userIdFromSession, out var sessionUserId) || booking.UserId != sessionUserId)
                {
                    return RedirectToAction("Login", "Home");
                }
            }

            if (booking.Status == "Completed" || booking.Status == "Cancelled")
            {
                return RedirectToAction(nameof(RecentBookings), new { userId = booking.UserId });
            }

            // Drop location is optional for this driver-hire platform.
            // If provided, trim and store; otherwise set to null.
            booking.DropLocation = string.IsNullOrWhiteSpace(dropLocation) ? null : dropLocation.Trim();
            booking.EndTime = endTime ?? DateTime.Now;
            booking.Status = "Completed";

            if (booking.DriverId > 0)
            {
                var driver = _context.Drivers.FirstOrDefault(d => d.DriverId == booking.DriverId);
                if (driver != null)
                {
                    driver.IsAvailable = true;
                }
            }

            _context.SaveChanges();

            TempData["Success"] = "Ride marked as finished and the driver is now available.";
            return RedirectToAction(nameof(RecentBookings), new { userId = booking.UserId });
        }


        [AllowAnonymous]
        public IActionResult Details(int id)
        {
            var adminLoggedIn = HttpContext.Session.GetString("AdminLoggedIn") == "true";
            var userIdFromSession = HttpContext.Session.GetString("UserId");

            var booking = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Driver)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (!adminLoggedIn)
            {
                if (string.IsNullOrEmpty(userIdFromSession) || !int.TryParse(userIdFromSession, out var sessionUserId) || booking.UserId != sessionUserId)
                {
                    return RedirectToAction("Login", "Home");
                }
            }

            return View(booking);
        }
        // Create Booking
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new Booking { StartTime = DateTime.Now, Status = "Pending" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Booking booking)
        {
            if (!_context.Users.Any(u => u.Id == booking.UserId))
            {
                ModelState.AddModelError(nameof(booking.UserId), "Please choose a valid user.");
            }

            if (!_context.Drivers.Any(d => d.DriverId == booking.DriverId))
            {
                ModelState.AddModelError(nameof(booking.DriverId), "Please choose a valid driver.");
            }

            if (ModelState.IsValid)
            {
                _context.Bookings.Add(booking);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns();
            return View(booking);
        }

        private void PopulateDropdowns()
        {
            var users = _context.Users
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, Display = u.Name + " (" + u.Email + ")" })
                .ToList();

            var drivers = _context.Drivers
                .OrderBy(d => d.Name)
                .Select(d => new { Id = d.DriverId, Display = d.Name + " (" + d.PhoneNumber + ")" })
                .ToList();

            ViewBag.Users = new SelectList(users, "Id", "Display");
            ViewBag.Drivers = new SelectList(drivers, "Id", "Display");
        }
    }
}