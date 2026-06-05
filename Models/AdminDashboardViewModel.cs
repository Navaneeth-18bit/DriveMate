using System.Collections.Generic;

namespace adminPage.Models
{
    public class AdminDashboardViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();

        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();

        public IEnumerable<Driver> Drivers { get; set; } = new List<Driver>();

        public int UserPage { get; set; } = 1;

        public int BookingPage { get; set; } = 1;

        public int DriverPage { get; set; } = 1;

        public int PageSize { get; set; } = 5;

        public int TotalUsers { get; set; }

        public int TotalBookings { get; set; }

        public int TotalDrivers { get; set; }

        public int TotalUserPages { get; set; }

        public int TotalBookingPages { get; set; }

        public int TotalDriverPages { get; set; }

        public Driver NewDriver { get; set; } = new Driver();

        public string? UserSearch { get; set; }

        public string? UserStatus { get; set; }

        public string? BookingSearch { get; set; }

        public string? BookingStatus { get; set; }
    }
}