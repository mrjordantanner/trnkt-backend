using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Trnkt.Models;
using Trnkt.Dtos;
using Trnkt.Configuration;


namespace Trnkt.Services
{
    public class DynamoDbService
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly AppConfig _settings;
        private readonly ILogger<DynamoDbService> _logger;

        public DynamoDbService(
            IAmazonDynamoDB dynamoDbClient, 
            IOptions<AppConfig> settings, 
            ILogger<DynamoDbService> logger)
        {
            _dynamoDbClient = dynamoDbClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public string GenerateJwtToken(string email)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _settings.JwtIssuer,
                audience: _settings.JwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Users
        public async Task<bool> UserExistsAsync(string email)
        {
            var request = new QueryRequest
            {
                TableName = _settings.UsersTableName,
                KeyConditionExpression = "Email = :v_email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_email", new AttributeValue { S = email } }
                }
            };

            var response = await _dynamoDbClient.QueryAsync(request);
            return response.Items.Count > 0;
        }

        public async Task CreateUserAsync(User user)
        {
            var request = new PutItemRequest
            {
                TableName = _settings.UsersTableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Email", new AttributeValue { S = user.Email } },
                    { "UserName", new AttributeValue { S = user.UserName } },
                    { "Password", new AttributeValue { S = user.PasswordHash } },
                    { "CreatedAt", new AttributeValue { S = user.CreatedAt } },
                    { "UserId", new AttributeValue { S = user.UserId } }
                }
            };

            await _dynamoDbClient.PutItemAsync(request);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var request = new QueryRequest
            {
                TableName = _settings.UsersTableName,
                KeyConditionExpression = "Email = :v_email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v_email", new AttributeValue { S = email } }
                }
            };

            var response = await _dynamoDbClient.QueryAsync(request);
            if (response.Items.Count == 0)
            {
                return null;
            }

            var item = response.Items[0];
            return new User
            {
                Email = item["Email"].S,
                UserId = item["UserId"].S,
                UserName = item["UserName"].S,
                PasswordHash = item["Password"].S,
                CreatedAt = item["CreatedAt"].S,
                //CreatedAt = DateTime.Parse(item["CreatedAt"].S)
            };
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var request = new ScanRequest
            {
                TableName = _settings.UsersTableName
            };

            var response = await _dynamoDbClient.ScanAsync(request);
            var users = new List<User>();

            foreach (var item in response.Items)
            {
                var user = new User
                {
                    Email = item["Email"].S,
                    UserId = item["UserId"].S,
                    UserName = item["UserName"].S,
                    PasswordHash = item["Password"].S,
                    CreatedAt = item.ContainsKey("CreatedAt") ? item["CreatedAt"].S : null,
                };
                users.Add(user);
            }

            return users;
        }

        public async Task<User> UpdateUserInfoAsync(UpdateUserDto updateUserDto)
        {
            var user = await GetUserByEmailAsync(updateUserDto.Email);
            if (user == null)
            {
                return null;
            }

            var updateItem = new Dictionary<string, AttributeValueUpdate>();

            if (!string.IsNullOrEmpty(updateUserDto.NewEmail))
            {
                updateItem["Email"] = new AttributeValueUpdate
                {
                    Action = AttributeAction.PUT,
                    Value = new AttributeValue { S = updateUserDto.NewEmail }
                };
                user.Email = updateUserDto.NewEmail;
            }

            if (!string.IsNullOrEmpty(updateUserDto.NewUserName))
            {
                updateItem["UserName"] = new AttributeValueUpdate
                {
                    Action = AttributeAction.PUT,
                    Value = new AttributeValue { S = updateUserDto.NewUserName }
                };
                user.UserName = updateUserDto.NewUserName;
            }

            if (!string.IsNullOrEmpty(updateUserDto.NewPasswordHash))
            {
                updateItem["PasswordHash"] = new AttributeValueUpdate
                {
                    Action = AttributeAction.PUT,
                    Value = new AttributeValue { S = updateUserDto.NewPasswordHash }
                };
                user.PasswordHash = updateUserDto.NewPasswordHash;
            }

            var request = new UpdateItemRequest
            {
                TableName = _settings.UsersTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Email", new AttributeValue { S = updateUserDto.Email } }
                },
                AttributeUpdates = updateItem
            };

            var response = await _dynamoDbClient.UpdateItemAsync(request);
            Console.WriteLine("User successfully updated");
            return user;
        }

        public async Task DeleteUserAsync(string email)
        {
            var request = new DeleteItemRequest
            {
                TableName = _settings.UsersTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Email", new AttributeValue { S = email } }
                }
            };

            await _dynamoDbClient.DeleteItemAsync(request);
        }


    }
}
