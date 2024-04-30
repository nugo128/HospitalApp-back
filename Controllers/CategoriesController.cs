using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hospital.Models;
using HospitalApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace HospitalApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly UserContext _context;

        public CategoriesController(UserContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categoriesWithCount = await _context.Categories
               .Select(c => new CategoryWithUserCount
               {
                   Id = c.Id,
                   Name = c.Name,
                   UserCount = c.CategoryUsers.Count()
               })
               .ToListAsync();

            return Ok(categoriesWithCount);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // PUT: api/Categories/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, string name)
        {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound("Category not found.");
                }

                if (!string.IsNullOrEmpty(name))
                {
                    category.Name = name;
                }

                _context.Entry(category).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok("Category updated successfully.");
            
        }

        // POST: api/Categories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCategory", new { id = category.Id }, category);
        }

        // DELETE: api/Categories/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            var categoryHasUser = await _context.CategoryUsers.FirstOrDefaultAsync(u => u.CategoryId == id);
            if (categoryHasUser != null)
            {
                return BadRequest(new { message = "You can't delete category that is used" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
