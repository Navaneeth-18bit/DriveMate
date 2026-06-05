using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using adminPage.Models;

public class AdminController : Controller
{
    private readonly DriveMateDbContext _context;

    public AdminController(DriveMateDbContext context)
    {
        _context = context;
    }
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("AdminLoggedIn") == "true")
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password)
    {
        if (username == "admin" && password == "1234")
        {
            HttpContext.Session.SetString("AdminLoggedIn", "true");
            HttpContext.Session.SetString("AdminUser", username);
            return RedirectToAction(nameof(Dashboard));
        }

        ViewBag.Error = "Invalid username or password.";
        return View();
    }

    public IActionResult Dashboard(string? userSearch, string? userStatus, string? bookingSearch, string? bookingStatus, int userPage = 1, int bookingPage = 1, int driverPage = 1)
    {
        if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
        {
            return RedirectToAction(nameof(Login));
        }

        const int pageSize = 5;
        userPage = Math.Max(1, userPage);
        bookingPage = Math.Max(1, bookingPage);
        driverPage = Math.Max(1, driverPage);

        var usersQuery = _context.Users.AsQueryable();
        var bookingsQuery = _context.Bookings
            .Include(booking => booking.User)
            .Include(booking => booking.Driver)
            .AsQueryable();
        var driversQuery = _context.Drivers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userSearch))
        {
            var normalizedUserSearch = userSearch.Trim();
            usersQuery = usersQuery.Where(user =>
                (user.Name ?? string.Empty).Contains(normalizedUserSearch) ||
                (user.Email ?? string.Empty).Contains(normalizedUserSearch) ||
                (user.PhoneNumber ?? string.Empty).Contains(normalizedUserSearch));
        }

        if (string.Equals(userStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            usersQuery = usersQuery.Where(user => user.IsActive);
        }
        else if (string.Equals(userStatus, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            usersQuery = usersQuery.Where(user => !user.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(bookingSearch))
        {
            var normalizedBookingSearch = bookingSearch.Trim();
            bookingsQuery = bookingsQuery.Where(booking =>
                booking.BookingId.ToString().Contains(normalizedBookingSearch) ||
                (booking.UserName ?? string.Empty).Contains(normalizedBookingSearch) ||
                (booking.User != null && (
                    (booking.User.Name ?? string.Empty).Contains(normalizedBookingSearch) ||
                    (booking.User.Email ?? string.Empty).Contains(normalizedBookingSearch))) ||
                (booking.Driver != null && (
                    (booking.Driver.Name ?? string.Empty).Contains(normalizedBookingSearch) ||
                    (booking.Driver.PhoneNumber ?? string.Empty).Contains(normalizedBookingSearch))));
        }

        if (!string.IsNullOrWhiteSpace(bookingStatus))
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.Status == bookingStatus);
        }

        var totalUsers = usersQuery.Count();
        var totalBookings = bookingsQuery.Count();
        var totalDrivers = driversQuery.Count();

        var totalUserPages = Math.Max(1, (int)Math.Ceiling(totalUsers / (double)pageSize));
        var totalBookingPages = Math.Max(1, (int)Math.Ceiling(totalBookings / (double)pageSize));
        var totalDriverPages = Math.Max(1, (int)Math.Ceiling(totalDrivers / (double)pageSize));

        userPage = Math.Min(userPage, totalUserPages);
        bookingPage = Math.Min(bookingPage, totalBookingPages);
        driverPage = Math.Min(driverPage, totalDriverPages);

        var viewModel = new AdminDashboardViewModel
        {
            Users = usersQuery
                .OrderByDescending(user => user.CreatedAt)
                .Skip((userPage - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            Bookings = bookingsQuery
                .OrderByDescending(booking => booking.StartTime)
                .Skip((bookingPage - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            Drivers = driversQuery
                .OrderBy(driver => driver.Name)
                .Skip((driverPage - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            NewDriver = new Driver
            {
                LicenseExpiry = DateTime.Now,
                IsAvailable = true
            },
            UserPage = userPage,
            BookingPage = bookingPage,
            DriverPage = driverPage,
            PageSize = pageSize,
            TotalUsers = totalUsers,
            TotalBookings = totalBookings,
            TotalDrivers = totalDrivers,
            TotalUserPages = totalUserPages,
            TotalBookingPages = totalBookingPages,
            TotalDriverPages = totalDriverPages,
            UserSearch = userSearch,
            UserStatus = userStatus,
            BookingSearch = bookingSearch,
            BookingStatus = bookingStatus
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateDriver(AdminDashboardViewModel model)
    {
        if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
        {
            return RedirectToAction(nameof(Login));
        }

        model.Drivers = _context.Drivers.OrderBy(driver => driver.Name).Take(model.PageSize > 0 ? model.PageSize : 5).ToList();
        model.Users = _context.Users.OrderByDescending(user => user.CreatedAt).Take(model.PageSize > 0 ? model.PageSize : 5).ToList();
        model.Bookings = _context.Bookings.Include(booking => booking.User).Include(booking => booking.Driver).OrderByDescending(booking => booking.StartTime).Take(model.PageSize > 0 ? model.PageSize : 5).ToList();

        if (model.NewDriver == null)
        {
            ModelState.AddModelError(string.Empty, "Driver details are required.");
            model.NewDriver = new Driver { LicenseExpiry = DateTime.Now, IsAvailable = true };
            return View("Dashboard", model);
        }

        if (!ModelState.IsValid)
        {
            model.NewDriver.LicenseExpiry = model.NewDriver.LicenseExpiry == default ? DateTime.Now : model.NewDriver.LicenseExpiry;
            model.NewDriver.IsAvailable = true;
            return View("Dashboard", model);
        }

        model.NewDriver.IsAvailable = true;
        model.NewDriver.CreatedAt = DateTime.Now;

        _context.Drivers.Add(model.NewDriver);
        _context.SaveChanges();

        TempData["DriverCreated"] = $"Driver '{model.NewDriver.Name}' inserted successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}