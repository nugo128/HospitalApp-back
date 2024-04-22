using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.Models
{
    public class UserEditModel
    {
        [FromForm(Name = "image")]
        public IFormFile? Image { get; set; }
        public string? IdNumber { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? RepeatPassword { get; set; }
    }
}
