using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Hospital.Models
{
    public class UserRegisterRequest
    {
        [Required, EmailAddress,]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required, Length(11, 11)]
        public string IDNumber { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;
        [FromForm(Name = "image")]
        public IFormFile Image { get; set; }

    }
}