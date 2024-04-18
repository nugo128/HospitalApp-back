using HospitalApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

namespace Hospital.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string LastName { get; set; } = "";

        public string Role { get; set; } = "user";

        public int Rating { get; set; } = 0;
        public string IDNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public byte[]? ProfilePicture { get; set; }
        public byte[]? CV {  get; set; }
        public byte[] PasswordHash { get; set; } = new byte[32];

        public byte[] PasswordSalt { get; set; } = new byte[32];

        public bool IsActive { get; set; } = false;

        public string? VerificationToken { get; set; } = "";
        public DateTime? VerifiedAt { get; set; }
        public string? PasswordResetToken { get; set; } = "";
        public DateTime? PasswordResetExpiration { get; set; }
        public bool TwoStepActive { get; set; } = false;
        public string? TwoStepToken { get; set; } = "";
        public DateTime? TwoStepExpiration { get; set; }

        public DateTime? ActivationCodeExpiration { get; set; }

        public ICollection<CategoryUser>? CategoryUsers { get; set; }


        public ICollection<Booking>? Bookings { get; set; }
    }
}