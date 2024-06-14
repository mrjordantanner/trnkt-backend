using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
                //Favorites = new List<FavoritesList>(),
                //CreatedAt = DateTime.UtcNow.ToString("o")
            };

            await _dynamoDbService.CreateUserAsync(user);
            return Ok("User created successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        {
            var user = await _dynamoDbService.GetUserByEmailAsync(userLoginDto.Email);
            if (user == null || !VerifyPassword(userLoginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = _dynamoDbService.GenerateJwtToken(user.Email);
            return Ok(new { Token = token });
        }

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
