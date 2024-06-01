using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

public class DynamoDbService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly DynamoDBContext _context;

    public DynamoDbService(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
        _context = new DynamoDBContext(dynamoDbClient);
    }

    public async Task SaveItemAsync<T>(T item)
    {
        await _context.SaveAsync(item);
    }
}
