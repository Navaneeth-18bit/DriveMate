using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using adminPage.Models;
using adminPage.Filters;

namespace adminPage.Controllers
{
    [AdminSessionAuthorize]
    public class DriverController : Controller
    {
        private readonly DriveMateDbContext _context;

        public DriverController(DriveMateDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers.ToListAsync();
            return View(drivers);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        [AllowAnonymous]
        public IActionResult Create()
        {
            return View("~/Views/Home/Driver/create.cshtml", new Driver { LicenseExpiry = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver driver)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Driver/create.cshtml", driver);
            }

            driver.IsAvailable = true;
            driver.CreatedAt = DateTime.Now;

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            TempData["DriverCreated"] = "Created a driver account";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Driver driver)
        {
            if (id != driver.DriverId)
            {
                return NotFound();
            }

            var existingDriver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.DriverId == id);
            if (existingDriver == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(driver);
            }

            try
            {
                _context.Update(driver);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Drivers.Any(e => e.DriverId == driver.DriverId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(m => m.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);

            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}