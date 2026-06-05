using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using adminPage.Models;
using adminPage.Filters;
using adminPage.Utilities;

[AdminSessionAuthorize]
public class UserController : Controller
{
    private readonly DriveMateDbContext _context;

    public UserController(DriveMateDbContext context)
    {
        _context = context;
    }

    // 🔹 PUBLIC: GET Register (User self-registration)
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    // 🔹 PUBLIC: POST Register (User self-registration)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(User user, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("confirmPassword", "Passwords do not match.");
            return View(user);
        }

        if (password?.Length < 6)
        {
            ModelState.AddModelError("password", "Password must be at least 6 characters long.");
            return View(user);
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return View(user);
        }

        if (!ModelState.IsValid)
        {
            return View(user);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("password", "Password is required.");
            return View(user);
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

            return RedirectToAction("Index", "Home");
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            return View(user);
        }
    }

    // 🔹 PUBLIC: GET Login
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> Profile(int? id = null)
    {
        int? userId = id;

        if (!userId.HasValue)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var sessionUserId))
            {
                return RedirectToAction("Login");
            }

            userId = sessionUserId;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        return View("~/Views/Home/User/profile.cshtml", user);
    }

    // 🔹 PUBLIC: POST Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]


    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash ?? ""))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account is inactive. Please contact support.");
            return View();
        }

        // Store user session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserName", user.Name ?? string.Empty);
        HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);

        return RedirectToAction("Index", "Home");
    }

    // 🔹 PUBLIC: Logout
    [AllowAnonymous]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // 🔹 ADMIN: GET User (List all users)
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    // 🔹 ADMIN: GET Edit
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // 🔹 ADMIN: GET Create
    public IActionResult Create()
    {
        return View();
    }

    // 🔹 ADMIN: POST Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        if (ModelState.IsValid)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    // 🔹 ADMIN: POST Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    // 🔹 ADMIN: GET Delete
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(m => m.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // 🔹 ADMIN: POST Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}