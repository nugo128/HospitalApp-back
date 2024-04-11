using Hospital.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace HospitalApp.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to link with the User
        public int UserId { get; set; }

        // Navigation property to access the associated user
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int DoctorId { get; set; }

        public DateTime BookingDate { get; set; }

        public string Time { get; set; } = string.Empty;
        public string description {  get; set; } = string.Empty; 
    }
}
