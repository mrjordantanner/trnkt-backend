using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Trnkt.Models;

namespace Trnkt.Services
{
    public class DynamoDbService
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly DynamoDBSettings _settings;

        public DynamoDbService(IAmazonDynamoDB dynamoDbClient, IOptions<DynamoDBSettings> settings)
        {
            _dynamoDbClient = dynamoDbClient;
            _settings = settings.Value;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var request = new QueryRequest
            {
                TableName = _settings.TableName,
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
                TableName = _settings.TableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Email", new AttributeValue { S = user.Email } },
                    { "UserName", new AttributeValue { S = user.UserName } },
                    { "Password", new AttributeValue { S = user.PasswordHash } },
                    // { "Favorites", new AttributeValue { S = user.Favorites } },
                    // { "CreatedAt", new AttributeValue { S = user.CreatedAt } },
                    { "Id", new AttributeValue { S = user.Id } },
                }
            };

            await _dynamoDbClient.PutItemAsync(request);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var request = new QueryRequest
            {
                TableName = _settings.TableName,
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
                Id = item["Id"].S,
                UserName = item["UserName"].S,
                PasswordHash = item["Password"].S,
                CreatedAt = DateTime.Parse(item["CreatedAt"].S),
                // Favorites = item["Favorites"].S
            };
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
    }
}
