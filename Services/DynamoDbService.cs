using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;

public class DynamoDbService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;

    public DynamoDbService(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"];
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "Email = :v_email",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":v_email", new AttributeValue { S = email } }
            }
        };

        var response = await _dynamoDbClient.QueryAsync(request);
        return response.Items.Count > 0;
    }

    public async Task CreateUserAsync(string userName, string email, string password)
    {
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "UserName", new AttributeValue { S = userName } },
                { "Email", new AttributeValue { S = email } },
                { "Password", new AttributeValue { S = password } },
                { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
            }
        };

        await _dynamoDbClient.PutItemAsync(request);
    }

    public async Task<Dictionary<string, AttributeValue>> GetUserByEmailAsync(string email)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "Email = :v_email",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":v_email", new AttributeValue { S = email } }
            }
        };

        var response = await _dynamoDbClient.QueryAsync(request);
        return response.Items.Count > 0 ? response.Items[0] : null;
    }

    public async Task SaveUserAsync(Dictionary<string, AttributeValue> user)
    {
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = user
        };

        await _dynamoDbClient.PutItemAsync(request);
    }
}
