using System.ComponentModel.DataAnnotations;

namespace Hospital.Models
{
    public class UserLoginRequest
    {
        [Required, EmailAddress,]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

    }
}
