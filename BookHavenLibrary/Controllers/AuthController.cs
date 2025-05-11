using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BookHavenLibrary.DTO;
using BookHavenLibrary.Models;
using BookHavenLibrary.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace BookHavenLibrary.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            SignInManager<User> signInManager,
            AppDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { success = false, message = "Email already in use" });

            var user = new User
            {
                UserName = model.Email.Split("@")[0],
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                DateJoined = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { success = false, errors = result.Errors });

            await _userManager.AddToRoleAsync(user, "member");

            // Send registration email
            var loginUrl = "https://localhost:5500/index.html";
            var emailBody = $"Hello {user.UserName},<br><br>Your account has been successfully registered!<br><a href='{loginUrl}'>Login Here</a>";

            var smtpSettings = _configuration.GetSection("SmtpSettings");
            try
            {
                using var smtpClient = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]!))
                {
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"]!)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["Username"]),
                    Subject = "Account Registered and Ready to Login",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(user.Email!);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error sending email: " + ex.Message });
            }

            return Ok(new { success = true, message = "Registration successful", role = "Member" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { success = false, message = "Invalid email or password" });

            var token = await GenerateJwtToken(user);

            var roles = await _userManager.GetRolesAsync(user);
             

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token,
                userId = user.Id,
                username = user.UserName,
                email = user.Email,
                roles
            });
        }


        [Authorize]
        [HttpGet("protected")]
        public IActionResult ProtectedRoute()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            return Ok(new
            {
                message = "✅ You are authorized!",
                userId,
                username,
                roles
            });
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = jwtSettings["Key"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expiryMinutes = int.TryParse(jwtSettings["ExpiryInMinutes"], out int expiry) ? expiry : 60;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }







        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();

            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.PhoneNumber,
                    user.FirstName,
                    user.LastName,
                    user.Address,
                    user.DateJoined,
                    user.IsActive,
                    Role = roles.FirstOrDefault() ?? "N/A"
                });
            }

            return Ok(userList);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.FirstName,
                user.LastName,
                user.Address,
                user.DateJoined,
                user.IsActive,
                Role = roles.FirstOrDefault() ?? "N/A"
            };

            return Ok(userDto);
        }


        [Authorize(Roles = "admin")]
        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> Delete(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return NotFound("User not found.");

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == user.Id.ToString())
                    return BadRequest("You cannot delete your own account.");

                // Delete related entities manually
                var cartItems = _context.ShoppingCarts.Where(c => c.UserId == user.Id);
                _context.ShoppingCarts.RemoveRange(cartItems);


                // Add other dependent removals here

                await _context.SaveChangesAsync(); // Save before deleting user

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return BadRequest("Failed to delete user.");

                return Ok("User deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }




        [Authorize(Roles = "admin")]
        [HttpPut("Update-role/{userId}")]
        public async Task<IActionResult> UpdateRole(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return NotFound("User not found.");

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("admin"))
                    return BadRequest("Cannot change role of an admin.");

                // Ensure 'staff' role exists
                if (!await _roleManager.RoleExistsAsync("staff"))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<int>("staff"));
                    if (!createRoleResult.Succeeded)
                        return BadRequest("Failed to create 'staff' role.");
                }

                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Remove existing roles
                    foreach (var role in userRoles)
                    {
                        var removeResult = await _userManager.RemoveFromRoleAsync(user, role);
                        if (!removeResult.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Failed to remove role: {role}");
                        }
                    }

                    // Add staff role
                    var addResult = await _userManager.AddToRoleAsync(user, "staff");
                    if (!addResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Failed to assign 'staff' role.");
                    }

                        await transaction.CommitAsync();
                        return Ok(new { success = true, message = $"User {user.Email} role updated to 'staff'" });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"An error occurred: {ex.Message}");
                    }
    }


        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In JWT, logout is handled on client side by removing the token
            return Ok(new { success = true, message = "Logged out successfully" });
        }


    }
}