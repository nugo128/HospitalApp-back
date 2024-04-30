using Hospital.Models;
using HospitalApp.Migrations;

namespace HospitalApp.Models
{
    public class UserWithCategories
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public byte[] Image { get; set; }
        public byte[]? CV { get; set; }
        public string? Description {get;set;}
        public int Rating { get; set; }
        public string IDNumber { get; set; }
        public bool Pinned { get; set; }
        public List<Category> Categories { get; set; }
    }
}
