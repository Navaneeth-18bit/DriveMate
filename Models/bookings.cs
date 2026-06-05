using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace adminPage.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [Column("userName")]
        public string UserName { get; set; } = string.Empty;

        // User who books
        [Required]
        public int UserId { get; set; }

        // Driver assigned
        [Required]
        public int DriverId { get; set; }

        // Pickup & Drop
        [Required]
        public string? PickupLocation { get; set; }

        public string? DropLocation { get; set; }

        // Time details
        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        // Status (Pending, Accepted, Completed, Cancelled)
        public string Status { get; set; } = "Pending";

        // Price
        public decimal? TotalAmount { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }
    }

}