using StackExchange.Redis;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

// Configuración de Redis
//builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(builder.Configuration["RedisCacheSettings:ConnectionString"]));
// Obtener la configuración de la conexión Redis desde appsettings.json
var redisConfiguration = builder.Configuration["RedisCacheSettings:ConnectionString"];

// Configuración de Redis con modo administrador
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfiguration));

builder.Services.AddSingleton<RedisCacheService>();
builder.Services.AddSingleton<RedisClientService>();

// Añadir y configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Redis API", Version = "v1" });
});

var app = builder.Build();

// Habilitar middleware para servir Swagger generado como un endpoint JSON
app.UseSwagger();

// Habilitar middleware para servir la interfaz de usuario de Swagger, especificando el endpoint JSON de Swagger
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Redis API V1");
});

// Simulación de base de datos en memoria, simple
var database = new List<string> { "data1", "data2", "data3" };

///Example redis others data formats
///
var listData = new List<string> { "item1", "item2", "item3" };
var setData = new HashSet<string> { "unique1", "unique2", "unique3" };
var sortedSetData = new SortedDictionary<string, double>
{
    { "member1", 1.0 },
    { "member2", 2.0 },
    { "member3", 3.0 }
};


// Endpoint para obtener datos, con caché de Redis
app.MapGet("/data", async (RedisCacheService cacheService) =>
{
    string key = "myDataKey";
    var cachedData = await cacheService.GetAsync<List<string>>(key);
    Console.WriteLine("Obteniendo datos...");

    // Verifica si el caché está vacío o si los datos son diferentes de los de la base de datos.
    if (cachedData == null || !cachedData.SequenceEqual(database))
    {
        Console.WriteLine("Datos no encontrados en caché o desactualizados, actualizando caché...");
        await cacheService.SetAsync(key, database, TimeSpan.FromSeconds(40));  // TTL de 10 minutos
        cachedData = database;  // Actualizar la variable local después de actualizar el caché
    }
    else
    {
        Console.WriteLine("Datos recuperados del caché.");
    }

    return Results.Ok(cachedData);
}).WithTags("Redis Operations").WithName("GetData");

// Endpoint para añadir datos a la base de datos en memoria y actualizar el caché
app.MapPost("/data", async (RedisCacheService cacheService, string newData) =>
{
    Console.WriteLine("Insertando nuevo dato...");
    database.Add(newData);
    string key = "myDataKey";

    // Actualizar el caché después de modificar la base de datos
    Console.WriteLine("Actualizando caché con nuevos datos...");
    await cacheService.SetAsync(key, database, TimeSpan.FromSeconds(20));  // TTL de 10 minutos

    return Results.Ok(new { Message = "Dato añadido y caché actualizado.", newData });
}).WithTags("Redis Operations").WithName("AddData");

///Other types
///
// Endpoint para Listas
app.MapGet("/lists/{key}", async (RedisCacheService cacheService, string key) =>
{
    var result = await cacheService.GetListAsync(key);
    if (result.Count == 0)
    {
        Console.WriteLine("Lista vacía o no encontrada, inicializando con datos predeterminados...");
        await cacheService.SetListAsync(key, listData, TimeSpan.FromSeconds(120));
        result = listData;
    }
    return Results.Ok(result);
});



app.MapPost("/lists/{key}", async (RedisCacheService cacheService, string key, List<string> values) =>
{
    Console.WriteLine("Actualizando lista...");
    await cacheService.SetListAsync(key, values, TimeSpan.FromSeconds(120));
    return Results.Ok("Lista actualizada.");
});

app.MapPost("/lists2/{key}", async (RedisCacheService cacheService, string key, List<string> newValues) =>
{
    Console.WriteLine("Añadiendo nuevos elementos a la lista...");
    var existingValues = await cacheService.GetListAsync(key);
    if (existingValues == null) existingValues = new List<string>();
    existingValues.AddRange(newValues);
    await cacheService.SetListAsync(key, existingValues, TimeSpan.FromSeconds(120));
    return Results.Ok("Elementos añadidos a la lista.");
});


// Endpoint para Conjuntos
app.MapGet("/sets/{key}", async (RedisCacheService cacheService, string key) =>
{
    var result = await cacheService.GetSetAsync(key);
    if (result.Count == 0)
    {
        Console.WriteLine("Conjunto vacío o no encontrado, inicializando con datos predeterminados...");
        await cacheService.SetSetAsync(key, setData, TimeSpan.FromSeconds(180));
        result = setData;
    }
    return Results.Ok(result);
});

app.MapPost("/sets/{key}", async (RedisCacheService cacheService, string key, HashSet<string> values) =>
{
    Console.WriteLine("Actualizando conjunto...");
    await cacheService.SetSetAsync(key, values, TimeSpan.FromSeconds(180));
    return Results.Ok("Conjunto actualizado.");
});

app.MapPost("/sets2/{key}", async (RedisCacheService cacheService, string key, HashSet<string> newValues) =>
{
    Console.WriteLine("Añadiendo nuevos elementos al conjunto...");
    var existingValues = await cacheService.GetSetAsync(key);
    if (existingValues == null) existingValues = new HashSet<string>();
    foreach (var value in newValues)
    {
        existingValues.Add(value); // HashSet automáticamente maneja duplicados
    }
    await cacheService.SetSetAsync(key, existingValues, TimeSpan.FromSeconds(180));
    return Results.Ok("Elementos añadidos al conjunto.");
});


// Endpoint para Conjuntos Ordenados
app.MapGet("/sortedsets/{key}", async (RedisCacheService cacheService, string key) =>
{
    var result = await cacheService.GetSortedSetAsync(key);
    if (result.Count == 0)
    {
        Console.WriteLine("Conjunto ordenado vacío o no encontrado, inicializando con datos predeterminados...");
        await cacheService.SetSortedSetAsync(key, sortedSetData, TimeSpan.FromSeconds(240));
        result = sortedSetData;
    }
    return Results.Ok(result);
});

app.MapPost("/sortedsets/{key}", async (RedisCacheService cacheService, string key, SortedDictionary<string, double> values) =>
{
    Console.WriteLine("Actualizando conjunto ordenado...");
    await cacheService.SetSortedSetAsync(key, values, TimeSpan.FromSeconds(240));
    return Results.Ok("Conjunto ordenado actualizado.");
});

app.MapPost("/sortedsets2/{key}", async (RedisCacheService cacheService, string key, SortedDictionary<string, double> newValues) =>
{
    Console.WriteLine("Actualizando/Añadiendo elementos en el conjunto ordenado...");
    var existingValues = await cacheService.GetSortedSetAsync(key);
    if (existingValues == null) existingValues = new SortedDictionary<string, double>();
    foreach (var value in newValues)
    {
        existingValues[value.Key] = value.Value; // Añade o actualiza la puntuación del elemento
    }
    await cacheService.SetSortedSetAsync(key, existingValues, TimeSpan.FromSeconds(240));
    return Results.Ok("Conjunto ordenado actualizado.");
});

app.MapGet("/getClient/{clientId}", async (RedisCacheService clientService, string clientId) =>
{
    var name = await clientService.GetClientNameAsync(clientId);
    return name != null ? Results.Ok(name) : Results.NotFound("Cliente no encontrado.");
});

///Clients  bigdata
///
app.MapPost("/loadClients", async ([FromServices] RedisClientService clientService) =>
{
    await clientService.LoadClientsAsync();
    return Results.Ok("Todos los clientes han sido cargados en Redis.");
});

app.MapGet("/clients", async ([FromServices] RedisClientService clientService, [FromQuery] int page = 1, [FromQuery] int pageSize = 100) =>
{
    var clients = await clientService.GetClientsPaginatedAsync(page, pageSize);
    return Results.Ok(clients);
});

//deleta all
app.MapPost("/flushDatabase", async ([FromServices] RedisClientService clientService) =>
{
    await clientService.FlushDatabaseAsync();
    return Results.Ok("La base de datos de Redis ha sido borrada completamente.");
});

app.Run();




