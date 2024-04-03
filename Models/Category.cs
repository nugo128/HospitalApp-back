using System.ComponentModel.DataAnnotations;

namespace Hospital.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public ICollection<CategoryUser>? CategoryUsers { get; set; }
    }
}
