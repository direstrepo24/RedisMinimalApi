using StackExchange.Redis;
using System.Text.Json;
using System.Linq;

public class RedisCacheService
{
    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    /// <summary>
    /// Retrieve a generic object from Redis cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to retrieve.</typeparam>
    /// <param name="key">The key associated with the object.</param>
    /// <returns>The object of type T, if it exists and deserialization is successful; otherwise, default(T).</returns>
    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            return default(T);
        }
        return JsonSerializer.Deserialize<T>(value);
    }

    /// <summary>
    /// Store a generic object in Redis cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to store.</typeparam>
    /// <param name="key">The key under which the object is stored.</param>
    /// <param name="value">The object to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiry);
    }

    /// <summary>
    /// Retrieve a list from Redis.
    /// </summary>
    /// <param name="key">The key of the list.</param>
    /// <returns>A List of strings if the list exists; otherwise, an empty list.</returns>
    public async Task<List<string>> GetListAsync(string key)
    {
        var values = await _database.ListRangeAsync(key);
        return values.Select(value => (string)value).ToList();
    }

    /// <summary>
    /// Store or update a list in Redis.
    /// </summary>
    /// <param name="key">The key of the list.</param>
    /// <param name="values">The list of strings to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    public async Task SetListAsync(string key, List<string> values, TimeSpan? expiry = null)
    {
        await _database.KeyDeleteAsync(key); // Clear the existing list
        await _database.ListRightPushAsync(key, values.Select(v => (RedisValue)v).ToArray());
        if (expiry.HasValue)
        {
            await _database.KeyExpireAsync(key, expiry);
        }
    }

    /// <summary>
    /// Retrieve a set from Redis.
    /// </summary>
    /// <param name="key">The key of the set.</param>
    /// <returns>A HashSet of strings if the set exists; otherwise, an empty HashSet.</returns>
    public async Task<HashSet<string>> GetSetAsync(string key)
    {
        var values = await _database.SetMembersAsync(key);
        return values.Select(value => (string)value).ToHashSet();
    }

    /// <summary>
    /// Store or update a set in Redis.
    /// </summary>
    /// <param name="key">The key of the set.</param>
    /// <param name="values">The set of strings to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    public async Task SetSetAsync(string key, HashSet<string> values, TimeSpan? expiry = null)
    {
        await _database.KeyDeleteAsync(key); // Remove the existing set
        foreach (var value in values)
        {
            await _database.SetAddAsync(key, value);
        }
        if (expiry.HasValue)
        {
            await _database.KeyExpireAsync(key, expiry);
        }
    }

    /// <summary>
    /// Retrieve a sorted set from Redis.
    /// </summary>
    /// <param name="key">The key of the sorted set.</param>
    /// <returns>A SortedDictionary representing the sorted set if it exists; otherwise, an empty SortedDictionary.</returns>
    public async Task<SortedDictionary<string, double>> GetSortedSetAsync(string key)
    {
        var range = await _database.SortedSetRangeByRankWithScoresAsync(key);
        var sortedSet = new SortedDictionary<string, double>();
        foreach (var entry in range)
        {
            sortedSet[(string)entry.Element] = entry.Score;
        }
        return sortedSet;
    }

    /// <summary>
    /// Store or update a sorted set in Redis.
    /// </summary>
    /// <param name="key">The key of the sorted set.</param>
    /// <param name="values">A SortedDictionary representing the sorted set to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    public async Task SetSortedSetAsync(string key, SortedDictionary<string, double> values, TimeSpan? expiry = null)
    {
        await _database.KeyDeleteAsync(key); // Remove the existing sorted set
        foreach (var value in values)
        {
            await _database.SortedSetAddAsync(key, value.Key, value.Value);
        }
        if (expiry.HasValue)
        {
            await _database.KeyExpireAsync(key, expiry);
        }
    }

    public async Task<bool> SetClientAsync(string clientId, Dictionary<string, string> clientData)
    {
        var hashEntries = clientData.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
        await _database.HashSetAsync(clientId, hashEntries);
        return true;
    }

    public async Task<string> GetClientNameAsync(string clientId)
{
   string redisKey = $"client:{clientId}";  // Clave adecuada usando solo el ID del cliente
        var name = await _database.HashGetAsync(redisKey, "name");
        if (!string.IsNullOrEmpty(name))
        {
            Console.WriteLine("Retrieved from Redis");
            return name; // Retorna el nombre si está en Redis
        }

        // Si no está en Redis, carga desde la FakeDatabase
        var clientData = FakeDatabase.GetClientData(redisKey);
        if (clientData != null)
        {
            // Almacena los datos en Redis usando un hash para almacenar múltiples atributos si es necesario
            await _database.HashSetAsync(redisKey, new HashEntry[] { new HashEntry("name", clientData["name"]) });
            Console.WriteLine("Retrieved from Fake Database and saved to Redis");
            return clientData["name"];
        }
        return null;
}


    // public async Task<string> GetClientNameAsynWithString(string clientId)
    // {
    //     string name = await _database.StringGetAsync(clientId);
    //     if (!string.IsNullOrEmpty(name))
    //     {
    //         Console.WriteLine("Retrieved from Redis");
    //         return name;
    //     }

    //     Console.WriteLine("Retrieved from Fake Database");
    //     name = FakeDatabase.GetClientData(clientId);
    //     if (name != null)
    //     {
    //         await _database.StringSetAsync(clientId, name, TimeSpan.FromMinutes(10)); // Cache for 10 minutes
    //     }
    //     return name;
    // }

}
