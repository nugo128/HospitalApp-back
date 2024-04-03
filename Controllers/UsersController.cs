using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hospital.Models;
using Microsoft.AspNetCore.Identity;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using HospitalApp.Models;
using HospitalApp.Services;

namespace Hospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        public UsersController(UserContext context, IEmailService emailService, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserWithCategories>>> GetAllUsersWithCategories()
        {
            var usersWithCategories = await _context.Users
                .Include(u => u.CategoryUsers)
                .ThenInclude(uc => uc.Category)
                .Select(u => new UserWithCategories
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    LastName = u.LastName,
                    Role = u.Role,
                    Rating = u.Rating,
                    Categories = u.CategoryUsers.Select(uc => uc.Category).ToList(),
                    Image = u.ProfilePicture
                })
                .ToListAsync();

            return Ok(usersWithCategories);
        }


        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser([FromForm] UserRegisterRequest request)
        {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            byte[] imageData = null;
            if (request.Image != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await request.Image.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }
            }

            var user = new User
            {
                Name = request.Name,
                LastName = request.LastName,
                IDNumber = request.IDNumber,
                Email = request.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                VerificationToken = CreateRandomToken(),
                ProfilePicture = imageData
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var verificationUrl = "http://localhost:4200/register/verify?token=" + user.VerificationToken;
            await _emailService.SendEmailAsync(user.Email, "Verify your account", verificationUrl);

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<UserWithCategories>> GetUserWithCategories(int id)
        {
            var userWithCategories = await _context.Users
                .Where(u => u.Id == id)
                .Include(u => u.CategoryUsers)
                .ThenInclude(uc => uc.Category)
                .Select(u => new UserWithCategories
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    LastName = u.LastName,
                    Role = u.Role,
                    Rating = u.Rating,
                    Categories = u.CategoryUsers.Select(uc => uc.Category).ToList(),
                    Image = u.ProfilePicture
                })
                .FirstOrDefaultAsync();

            if (userWithCategories == null)
            {
                return NotFound();
            }

            return Ok(userWithCategories);
        }


        [HttpPost("login")]
        public async Task<ActionResult<User>> Login(UserLoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Password is incorrect");
            }
            if (user.VerifiedAt == null)
            {
                return BadRequest("Not verified!");
            }

            string token = CreateToken(user);

            return Ok(new { token, user });
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.Email, user.Email),
            };
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var cred = new SigningCredentials(Key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], null, expires: DateTime.Now.AddDays(1), signingCredentials: cred);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
        [HttpPost("verify")]
        public async Task<ActionResult<User>> Verify(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid token" });
            }
            user.IsActive = true;
            user.VerifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User verified" });
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                return computedHash.SequenceEqual(passwordHash);
            }

        }
        private string CreateRandomToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }

        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}