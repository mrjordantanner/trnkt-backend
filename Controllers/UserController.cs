using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Trnkt.Dtos;
using Trnkt.Models;
using Trnkt.Services;

namespace Trnkt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DynamoDbService _dynamoDbService;
        private readonly ILogger<UserController> _logger;

        private readonly int _tokenLifetimeMinutes = 360;

        public UserController(DynamoDbService dynamoDbService, ILogger<UserController> logger)
        {
            _dynamoDbService = dynamoDbService;
            _logger = logger;
        }

        // Register New User
        [HttpPost("users")]
        public async Task<IActionResult> CreateNewUser([FromBody] UserRegistrationDto userRegistrationDto)
        {
            string logMessage;
            if (await _dynamoDbService.UserExistsAsync(userRegistrationDto.Email))
            {
                logMessage = $"Register: Error -- User email {userRegistrationDto.Email} already exists.";
                _logger.LogError(logMessage);
                return BadRequest(logMessage);
            }

            var user = new User
            {
                UserId = userRegistrationDto.UserId ?? Guid.NewGuid().ToString(),
                UserName = userRegistrationDto.UserName,
                Email = userRegistrationDto.Email,
                PasswordHash = HashPassword(userRegistrationDto.Password),
                CreatedAt = DateTime.UtcNow.ToString("o"),
            };

            await _dynamoDbService.CreateUserAsync(user);

            logMessage = $"Register: User {user.UserName} / {user.UserId} created successfully.";
            _logger.LogInformation(logMessage);
            return Ok(logMessage);
        }

        // Get User by Email
        [HttpGet("users/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }

        // Get all Users
        [HttpGet("users/all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _dynamoDbService.GetAllUsersAsync();
            return Ok(users);
        }

        // Login User
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(userLoginDto.Email);
            if (user == null || !VerifyPassword(userLoginDto.Password, user.PasswordHash))
            {
                var message = "Login: Invalid email or password.";
                _logger.LogError(message);
                return Unauthorized(message);
            }

            var token = _dynamoDbService.GenerateJwtToken(user.Email);

            // Set the token in a cookie
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(_tokenLifetimeMinutes)
            });

            _logger.LogInformation("Login: JWT token generated. User {name} / {id} logged in successfully.", user.UserName, user.UserId);
            return Ok(new { User = user, Token = token });
        }

        // Logout User
        [HttpPost("logout")]
        //[Authorize]
        public IActionResult Logout()
        {
            if (Request.Cookies.ContainsKey("jwt_token"))
            {
                Response.Cookies.Delete("jwt_token");
                _logger.LogInformation("Logout: JWT token was found and deleted.");
            }

            var message = "Logout: User logged out successfully.";
            _logger.LogInformation(message);
            return Ok(message);
        }

        // Update User Info
        [HttpPatch("users/update")]
        [Authorize]
        public async Task<IActionResult> UpdateUserAsync([FromBody] UpdateUserDto updateUserDto)
        {
            var user = await _dynamoDbService.UpdateUserInfoAsync(updateUserDto);
            if (user == null)
            {
                var errorMessage = "Update: UpdateUserAsync returned null User.";
                _logger.LogError(errorMessage);
                return NotFound(errorMessage);
            }

            var message = $"Update: User {user.UserName} / {user.UserId} updated successfully.";
            _logger.LogInformation(message);
            return Ok(user);
        }

        // Delete User
        [HttpDelete("users/{email}")]
        [Authorize]
        public async Task<IActionResult> DeleteUserAsync(string email)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            await _dynamoDbService.DeleteUserAsync(email);

            var message = $"Delete: User {user.UserName} / {user.UserId} deleted successfully.";
            _logger.LogInformation(message);
            return Ok(message);
        }

        // Password utilities
        private static bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}
