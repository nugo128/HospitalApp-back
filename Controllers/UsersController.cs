using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hospital.Models;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using HospitalApp.Models;
using HospitalApp.Services;
using Microsoft.AspNetCore.Identity.Data;
using NuGet.Common;

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
        public async Task<IActionResult> EditUser(int id, [FromForm] UserEditModel userEditModel)
        {
            try
            {
                byte[] imageData = null;

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                if (userEditModel.Image != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await userEditModel.Image.CopyToAsync(memoryStream);
                        imageData = memoryStream.ToArray();

                    }
                    user.ProfilePicture = imageData;
                    
                }

                if (userEditModel.IdNumber != null)
                {
                    user.IDNumber = userEditModel.IdNumber;
                }

                if (userEditModel.Email != null)
                {
                    user.Email = userEditModel.Email;
                }
                if (userEditModel.Password != null)
                {
                    if (userEditModel.Password == null || string.IsNullOrEmpty(userEditModel.Password) || string.IsNullOrEmpty(userEditModel.RepeatPassword))
                    {
                        return BadRequest(new { message = "Password and repeat password are required" });
                    }

                    if (userEditModel.Password != userEditModel.RepeatPassword)
                    {
                        return BadRequest(new { message = "Passwords do not match" });
                    }

                    CreatePasswordHash(userEditModel.Password, out byte[] passwordHash, out byte[] passwordSalt);
                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;
                    await _context.SaveChangesAsync();

                    
                }

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error.");
            }
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser([FromForm] UserRegisterRequest request)
        {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            byte[] imageData = null;
            byte[] CV = null;
            int categoryId = 0;
            string role = "user";
            if (request.Image != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await request.Image.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                    
                }
                using (var memoryStream = new MemoryStream())
                {
                    await request.CV.CopyToAsync(memoryStream);
                    CV = memoryStream.ToArray();

                }
                var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == request.Category);
                if (existingCategory != null)
                {
                    categoryId = existingCategory.Id;
                }
                else { 
                var category = new Category { Name = request.Category };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                categoryId = category.Id;
                
                }

                role = "doctor";
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
                ProfilePicture = imageData,
                Role = role,
                CV = CV,
            };


            user.ActivationCodeExpiration = DateTime.Now.AddMinutes(30);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            if(request.Image != null)
            {
                var categoryUser = new CategoryUser { CategoryId = categoryId, UserId = user.Id, Category = null, User = null };
                _context.CategoryUsers.Add(categoryUser);
                await _context.SaveChangesAsync();
            }

            var verificationUrl = "http://localhost:4200/register/verify?token=" + user.VerificationToken;
            await _emailService.SendEmailAsync(user.Email, "Verify your account", verificationUrl);

            return Ok(user);
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
                    Image = u.ProfilePicture,
                    IDNumber = u.IDNumber
                })
                .FirstOrDefaultAsync();

            if (userWithCategories == null)
            {
                return NotFound();
            }

            return Ok(userWithCategories);
        }


        [HttpPost("login")]
        public async Task<ActionResult> Login(UserLoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Password is incorrect.");
            }

            if (user.VerifiedAt == null)
            {
                return BadRequest("Not verified!");
            }

            if (user.TwoStepActive)
            {
                if (user.TwoStepExpiration.HasValue && (DateTime.UtcNow - user.TwoStepExpiration.Value).TotalMinutes < -4)
                {
                    return BadRequest("Please wait one minute");
                }

                user.TwoStepToken = CreateRandomCode();
                user.TwoStepExpiration = DateTime.UtcNow.AddMinutes(5);


                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(user.Email, "Verify your account", user.TwoStepToken);

                return Ok(new { Message = "Code send" });
            }


            string token = CreateToken(user);

            return Ok(new { Token = token, User = user });
        }

        [HttpPost("verify-two-step")]
        public async Task<ActionResult> VerifyTwoStepCode(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return BadRequest("User not found.");
            if (!user.TwoStepActive || string.IsNullOrEmpty(user.TwoStepToken)) return BadRequest("Two-step verification is not enabled or no verification code is pending.");


            if (!user.TwoStepExpiration.HasValue || user.TwoStepExpiration < DateTime.UtcNow)
            {
                return BadRequest("The verification code is expired. You can resend code in one minute ");
            }

            if (user.TwoStepToken != code)
            {
                return BadRequest("Invalid verification code.");
            }

            user.TwoStepToken = null;
            user.TwoStepExpiration = null;
            await _context.SaveChangesAsync();

            var token = CreateToken(user);
            return Ok(new { Token = token, User = user });
        }
        [Authorize] 
        [HttpGet("loggedInUser")]
        public async Task<ActionResult<User>> GetLoggedInUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return BadRequest("User not found in token.");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var cred = new SigningCredentials(Key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims, expires: DateTime.Now.AddDays(1), signingCredentials: cred);
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

            if (user.ActivationCodeExpiration.HasValue && user.ActivationCodeExpiration <= DateTime.Now)
            {
                return BadRequest(new { message = "Activation code expired" });
            }
            user.IsActive = true;
            user.VerifiedAt = DateTime.Now;
            user.VerificationToken = null; 
            await _context.SaveChangesAsync();

            return Ok(new { message = "User verified" });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return Ok(new { message = "Password reset email sent if the email exists in our records" });
            }

            var resetToken = CreateRandomCode();
            user.PasswordResetToken = resetToken;
            user.PasswordResetExpiration = DateTime.Now.AddMinutes(5);
            await _context.SaveChangesAsync();

            var resetUrl = $"Here is your reset code: {resetToken}";
            await _emailService.SendEmailAsync(email, "Reset your password", resetUrl);

            return Ok(new { message = "Password reset email sent" });
        }
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword(string token, HospitalApp.Models.ResetPasswordRequest resetPasswordRequest)
        {
            if (resetPasswordRequest == null || string.IsNullOrEmpty(resetPasswordRequest.Password) || string.IsNullOrEmpty(resetPasswordRequest.RepeatPassword))
            {
                return BadRequest(new { message = "Password and repeat password are required" });
            }

            if (resetPasswordRequest.Password != resetPasswordRequest.RepeatPassword)
            {
                return BadRequest(new { message = "Passwords do not match" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
            if (user == null || IsTokenExpired(user.PasswordResetExpiration))
            {
                return BadRequest(new { message = "Invalid or expired token" });
            }

            CreatePasswordHash(resetPasswordRequest.Password, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.PasswordResetToken = null;
            user.PasswordResetExpiration = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }




        private bool IsTokenExpired(DateTime? expiration)
        {
            return expiration.HasValue && expiration <= DateTime.Now;
        }
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                return computedHash.SequenceEqual(passwordHash);
            }

        }
        private string CreateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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

        [HttpPost("change-email")]
        public async Task<ActionResult> ChangeEmail(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return Ok(new { message = "email sent if the user exists in our records" });
            }

            var resetToken = CreateRandomCode();
            user.ChangeEmailToken = resetToken;
            await _context.SaveChangesAsync();

            var resetUrl = $"Here is your reset code: {resetToken}";
            await _emailService.SendEmailAsync(user.Email, "change your email", resetUrl);

            return Ok(new { message = "Email sent" });
        }
        [HttpPost("change-email-verify")]
        public async Task<ActionResult> ChangeEmailVerify(int id, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user.ChangeEmailToken != code)
            {
                return BadRequest(new { message = "Invalid or expired token" });
            }

            user.ChangeEmailToken = null;
            user.ChangeEmailTokenVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "code verified, you can now set new email" });
        }
        [HttpPost("set-email")]
        public async Task<ActionResult> SetNewEmail(int id, string newEmail)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return BadRequest(new { message = "user not found" });
            }

            if (user.ChangeEmailTokenVerified == false) {
                return BadRequest(new { message = "current email not verified" });
            }

            var resetToken = CreateRandomCode();
            user.NewEmailToken = resetToken;
            await _context.SaveChangesAsync();

            var resetUrl = $"Here is your reset code: {resetToken}";
            await _emailService.SendEmailAsync(newEmail, "change your email", resetUrl);

            return Ok(new { message = "Email sent" });
        }
        [HttpPost("new-email-verify")]
        public async Task<ActionResult> NewEmailVerify(int id, string code, string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user.NewEmailToken != code)
            {
                return BadRequest(new { message = "Invalid or expired token" });
            }

            user.Email = email;
            user.NewEmailTokenVerified = true;
            user.NewEmailToken = null;
            user.ChangeEmailTokenVerified = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "email changed!" });
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