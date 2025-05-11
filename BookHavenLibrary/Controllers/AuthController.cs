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



        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> Delete(int userId)
        {
            try
            {
                // Find the user by ID
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return NotFound("User not found.");

                // Ensure the user is not trying to delete themselves (optional, based on your business rules)
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get current logged-in user ID
                if (currentUserId == user.Id.ToString())
                    return BadRequest("You cannot delete your own account.");

                // Delete the user from the database
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return BadRequest("Failed to delete user.");

                return Ok("User deleted successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (if you have a logger) and return a generic error message
                return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
            }
        }


        [Authorize(Roles = "admin")]  // Ensures only Admin can access
        [HttpPut("Update-role/{userId}")]
        public async Task<IActionResult> UpdateRole(int userId)
        {
            try
            {
                // Find the user by ID
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return NotFound("User not found.");

                // Get the user's roles
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("member"))
                    return BadRequest("User is not a member or already has a different role.");

                // Ensure the 'staff' role exists
                var staffRole = await _roleManager.FindByNameAsync("staff");
                if (staffRole == null)
                {
                    // Create the 'staff' role if it doesn't exist
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<int> { Name = "staff" });
                    if (!createRoleResult.Succeeded)
                        return BadRequest("Failed to create 'staff' role.");
                }

                // Start a transaction
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Remove 'member' role
                        var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, "member");
                        if (!removeRoleResult.Succeeded)
                            return BadRequest("Failed to remove 'member' role.");

                        // Assign 'staff' role
                        var addRoleResult = await _userManager.AddToRoleAsync(user, "staff");
                        if (!addRoleResult.Succeeded)
                            return BadRequest("Failed to assign 'staff' role.");

                        // Commit the transaction
                        await transaction.CommitAsync();

                        return Ok($"User {user.Email} role updated to 'staff'.");
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction if something fails
                        await transaction.RollbackAsync();
                        return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
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