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
            if (await _dynamoDbService.UserExistsAsync(userRegistrationDto.Email))
            {
                return BadRequest("User already exists.");
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userRegistrationDto.UserName,
                Email = userRegistrationDto.Email,
                PasswordHash = HashPassword(userRegistrationDto.Password),
                //CreatedAt = DateTime.UtcNow.ToString("o")
                //CreatedAt = DateTime.UtcNow
            };

            await _dynamoDbService.CreateUserAsync(user);
            return Ok("User created successfully.");
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
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(120)
            });

            return Ok(new { Token = token });
        }

        // Logout User
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            if (Request.Cookies.ContainsKey("jwt_token"))
            {
                Response.Cookies.Delete("jwt_token");
            }

            return Ok("User logged out successfully.");
        }

        // Change User Name
        [HttpPut("users/change-username")]
        [Authorize]
        public async Task<IActionResult> ChangeUserNameAsync([FromBody] ChangeUserNameDto changeUserNameDto)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(changeUserNameDto.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            
            await _dynamoDbService.ChangeUserNameAsync(changeUserNameDto.Email, changeUserNameDto.NewUserName);
            return Ok($"User name successfully updated from {user.UserName} to {changeUserNameDto.NewUserName}.");
        }

        // Change User Email
        [HttpPut("users/change-email")]
        [Authorize]
        public async Task<IActionResult> ChangeUserEmailAsync([FromBody] ChangeUserEmailDto changeUserEmailDto)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(changeUserEmailDto.OldEmail);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            await _dynamoDbService.ChangeUserEmailAsync(changeUserEmailDto.OldEmail, changeUserEmailDto.NewEmail);
            return Ok("User email updated successfully.");
        }

        // Change Password
        [HttpPut("users/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto changePasswordDto)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(changePasswordDto.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash))
            {
                return BadRequest("Incorrect old password.");
            }

            await _dynamoDbService.ChangePasswordAsync(changePasswordDto.Email, HashPassword(changePasswordDto.NewPassword));
            return Ok("Password updated successfully.");
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
        private bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
