using HospitalApp.Models;
using System.ComponentModel.DataAnnotations;

namespace Hospital.Models
{
    public class CategoryUser
    {
        public int UserId { get; set; }
        public int CategoryId { get; set; }

        public User User { get; set; }
        public Category Category { get; set; }
    }
}
