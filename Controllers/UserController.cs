using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserController> _logger;
    private readonly DynamoDbService _dynamoDbService;

    public UserController(
        IConfiguration configuration, 
        ILogger<UserController> logger,
        DynamoDbService dynamoDbService)
    {
        _configuration = configuration;
        _logger = logger;
        _dynamoDbService = dynamoDbService;
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateNewUser([FromBody] UserRequest userRequest)
    {
        // Check if user exists
        var existingUser = await _dynamoDbService.GetUserByEmailAsync(userRequest.Email);
        if (existingUser != null)
        {
            return BadRequest("User already exists.");
        }

        // Create new instance of user
        var user = new Dictionary<string, AttributeValue>
        {
            { "Id", new AttributeValue { S = Guid.NewGuid().ToString() } },
            { "Name", new AttributeValue { S = userRequest.Name } },
            { "Email", new AttributeValue { S = userRequest.Email } },
            { "Password", new AttributeValue { S = HashPassword(userRequest.Password) } }
        };

        // Write to DynamoDB
        await _dynamoDbService.SaveUserAsync(user);

        return Ok("User created successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        // Check if user exists
        var user = await _dynamoDbService.GetUserByEmailAsync(loginRequest.Email);
        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Verify password
        if (!VerifyPassword(loginRequest.Password, user["Password"].S))
        {
            return Unauthorized("Invalid email or password.");
        }

        // Generate JWT token
        var token = GenerateJwtToken(user["Email"].S);

        return Ok(new { Token = token });
    }

    [HttpGet("users/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        // Retrieve user by email
        var user = await _dynamoDbService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return StringComparer.OrdinalIgnoreCase.Compare(hashOfInput, hashedPassword) == 0;
    }

    private string GenerateJwtToken(string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Models for requests
public class UserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
