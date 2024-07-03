using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Trnkt.Services;
using Trnkt.Models;
using Trnkt.Dtos;

namespace Trnkt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DynamoDbService _dynamoDbService;
        private readonly ILogger<UserController> _logger;

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
                // TODO add more logging
                logMessage = "User already exists.";
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

            logMessage = "User created successfully.";
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
                return Unauthorized("Invalid email or password.");
            }

            var token = _dynamoDbService.GenerateJwtToken(user.Email);

            // Set the token in a cookie
            // TODO are we using cookies?
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(120)
            });

            return Ok(new { User = user, Token = token });
        }

        // Logout User
        [HttpPost("logout")]
        //[Authorize]
        public IActionResult Logout()
        {
            // TODO use cookies or not
            if (Request.Cookies.ContainsKey("jwt_token"))
            {
                Response.Cookies.Delete("jwt_token");
            }

            return Ok("User logged out successfully.");
        }

        // Update User Info
        [HttpPatch("users/update")]
        [Authorize]
        public async Task<IActionResult> UpdateUserAsync([FromBody] UpdateUserDto updateUserDto)
        {
            var user = await _dynamoDbService.UpdateUserInfoAsync(updateUserDto);
            if (user == null)
            {
                return NotFound("UpdateUserAsync returned null User.");
            }

            // TODO make dynamo return user so don't have to fetch via email
            // var updatedUser = await _dynamoDbService.GetUserByEmailAsync(updateUserDto.NewEmail ?? updateUserDto.Email);
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
            return Ok("User deleted successfully.");
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
