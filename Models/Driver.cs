using System;
using System.ComponentModel.DataAnnotations;

namespace adminPage.Models
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Please enter the driver's name")]
        public string? Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the driver's phone number")]
        public string? PhoneNumber { get; set; } = string.Empty;

        public string? Email { get; set; }

        [Required(ErrorMessage = "Please enter the driver's license number")]
        public string? LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the driver's experience")]
        [Range(0, 60, ErrorMessage = "Experience must be a whole number between 0 and 60")]
        public int Experience { get; set; }

        public DateTime LicenseExpiry { get; set; }

        public string? Address { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}