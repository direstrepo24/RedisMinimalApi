using RedisMinimalApi.Domains.Models;
using StackExchange.Redis;

public class RedisClientService
{
    private readonly IDatabase _database;
      private const int PageSize = 10000; // Tamaño fijo de página para simplificar


    public RedisClientService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

     // Método para cargar clientes en Redis
    public async Task LoadClientsAsync()
    {
        var batch = _database.CreateBatch();
        List<Task> tasks = new List<Task>();

        for (int i = 1; i <= 1000000; i++)
        {
            var clientId = $"client:{i}";
            var client = new Client { Id = clientId, Name = $"Cliente {i}" };
            var hashEntries = new HashEntry[]
            {
                new HashEntry("Id", client.Id),
                new HashEntry("Name", client.Name)
            };

            tasks.Add(batch.HashSetAsync(clientId, hashEntries));
            tasks.Add(batch.ListRightPushAsync("clientList", clientId));
        }

        batch.Execute();
        await Task.WhenAll(tasks);

        // Establece un TTL para la lista de clientes
        await _database.KeyExpireAsync("clientList", TimeSpan.FromHours(24)); // TTL de 24 horas
    }

public async Task<List<Client>> GetClientsPaginatedAsync2(int page)
    {
        int startIndex = (page - 1) * PageSize;
        int endIndex = startIndex + PageSize - 1;
        var clientListKey = "clientList";

        // Verificar si los datos necesarios están en Redis
        long storedDataCount = await _database.ListLengthAsync(clientListKey);
        if (storedDataCount < endIndex + 1)
        {
            // Cargar más datos desde la fuente de datos hasta alcanzar endIndex
            await LoadMoreClientsAsync(storedDataCount, endIndex);
        }

        // Recuperar los datos de la página solicitada
        var clientIds = await _database.ListRangeAsync(clientListKey, startIndex, endIndex);
        var clients = new List<Client>();
        foreach (var clientId in clientIds)
        {
            var hashEntries = await _database.HashGetAllAsync((string)clientId);
            clients.Add(new Client
            {
                Id = hashEntries.FirstOrDefault(x => x.Name.ToString() == "Id").Value,
                Name = hashEntries.FirstOrDefault(x => x.Name.ToString() == "Name").Value
            });
        }

        return clients;
    }

    private async Task LoadMoreClientsAsync(long startIndex, int endIndex)
    {
        var batch = _database.CreateBatch();
        List<Task> tasks = new List<Task>();

        for (int i = (int)startIndex + 1; i <= endIndex + 1; i++)
        {
            var clientId = $"client:{i}";
            var client = new Client { Id = clientId, Name = $"Cliente {i}" };
            var hashEntries = new HashEntry[]
            {
                new HashEntry("Id", client.Id),
                new HashEntry("Name", client.Name)
            };

            tasks.Add(batch.HashSetAsync(clientId, hashEntries));
            tasks.Add(batch.ListRightPushAsync("clientList", clientId));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }


    public async Task FlushDatabaseAsync()
   {
    var endpoints = _database.Multiplexer.GetEndPoints();
    var server = _database.Multiplexer.GetServer(endpoints.First());
    await server.FlushDatabaseAsync(_database.Database);
   }





   public async Task<List<Client>> GetClientsPaginatedAsync(int page, int pageSize)
{
    var start = (page - 1) * pageSize;
    var end = start + pageSize - 1;
    var clientIds = await _database.ListRangeAsync("clientList", start, end);
    var clients = new List<Client>();

    foreach (var clientId in clientIds)
    {
        var hashEntries = await _database.HashGetAllAsync((string)clientId);
        var client = new Client
        {
            Id = hashEntries.FirstOrDefault(x => x.Name.ToString() == "Id").Value,
            Name = hashEntries.FirstOrDefault(x => x.Name.ToString() == "Name").Value
        };
        clients.Add(client);
    }

    return clients;
}

}
