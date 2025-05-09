using BookHavenLibrary.Data;
using BookHavenLibrary.DTO;
using BookHavenLibrary.Models;
using BookHavenLibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace BookHavenLibrary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager ,SignInManager<User> signInManager, AppDbContext context, ITokenService tokenService ,IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _tokenService = tokenService;
            _configuration = configuration;
        }



        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                // Validate phone number (should contain exactly 10 digits)
                var phoneNumberRegex = new Regex(@"^\d{10}$");
                if (!phoneNumberRegex.IsMatch(dto.PhoneNumber))
                    return BadRequest(new { success = false, message = "Phone number must be exactly 10 digits." });

                // Validate email (should end with @gmail.com)
                if (!dto.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, message = "Email must be a Gmail address (e.g., user@gmail.com)." });

                // Check if the email already exists
                var emailExists = await _userManager.FindByEmailAsync(dto.Email);
                if (emailExists != null)
                    return BadRequest(new { success = false, message = "Email already registered." });

                // Check if the phone number already exists
                var phoneExists = _userManager.Users.Any(u => u.PhoneNumber == dto.PhoneNumber);
                if (phoneExists)
                    return BadRequest(new { success = false, message = "Phone number already registered." });

                // Determine role based on email
                var role = dto.Email.ToLower() == "admin@gmail.com" ? "admin" : "member";

                // Ensure the role exists before adding to the user
                var roleExists = await _roleManager.RoleExistsAsync(role);
                if (!roleExists)
                {
                    var newRole = new IdentityRole<int>(role); // Create the role if it doesn't exist
                    var roleCreationResult = await _roleManager.CreateAsync(newRole);
                    if (!roleCreationResult.Succeeded)
                        return BadRequest(new { success = true, message = "Failed to create role." });
                }

                // Create the user using UserManager (Identity framework handles user object creation)  
                var user = new User
                {
                    UserName = dto.Email!.Split("@")[0],
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Address = dto.Address,
                    DateJoined = DateTime.UtcNow,
                    IsActive = true
                };

                // Create the user in the database
                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);

                // Add user to the role (only after creation succeeds)
                var addRoleResult = await _userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                    return BadRequest(new { success = false, message = "Failed to assign role to user." });

                return Ok(new {success = true, message = "User registered successfully."});
            }
            catch (Exception ex)
            {
                // Log the exception (if you have a logger) and return a generic error message
                return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
            }
        }




        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                return Unauthorized("Invalid credentials or inactive user.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Invalid credentials.");

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateToken(user, roles);

            return Ok(new
            {
                message = $"Login successful. Role: {roles.FirstOrDefault()}",
                token
            });
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



    }
}
