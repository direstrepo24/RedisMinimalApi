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
 // Método para cargar clientes en Redis por lotes
    public async Task LoadClientsAsync()
    {
        const int batchSize = 10000; // Define el tamaño del lote
        int totalClients = 1000000;
        for (int batchStart = 0; batchStart < totalClients; batchStart += batchSize)
        {
            var batch = _database.CreateBatch();
            List<Task> tasks = new List<Task>();
            int batchEnd = Math.Min(batchStart + batchSize, totalClients);

            for (int i = batchStart; i < batchEnd; i++)
            {
                var clientId = $"client:{i + 1}";
                var client = new Client { Id = clientId, Name = $"Cliente {i + 1}" };
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

        // Opcionalmente, establece un TTL para la lista de clientes
        await _database.KeyExpireAsync("clientList", TimeSpan.FromSeconds(30));
    }

    // Método para obtener clientes paginados y cargar a demanda
    public async Task<List<Client>> GetClientsPaginatedAsync(int page, int pageSize)
    {
        int start = (page - 1) * pageSize;
        int end = start + pageSize - 1;
        long storedDataCount = await _database.ListLengthAsync("clientList");

        if (storedDataCount < end + 1)
        {
            await LoadClientsAsync(); // Carga más clientes si no están disponibles
        }

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


    public async Task FlushDatabaseAsync()
   {
    var endpoints = _database.Multiplexer.GetEndPoints();
    var server = _database.Multiplexer.GetServer(endpoints.First());
    await server.FlushDatabaseAsync(_database.Database);
   }


}
