public static class FakeDatabase
{
    public static Dictionary<string, Dictionary<string, string>> Clients = new Dictionary<string, Dictionary<string, string>>();

    static FakeDatabase()
    {
        for (int i = 1; i <= 1000000; i++)
        {
            Clients.Add($"client:{i}", new Dictionary<string, string>
            {
                {"name", $"Cliente {i}"}
            });
        }
    }

    public static Dictionary<string, string> GetClientData(string clientId)
    {
        if (Clients.TryGetValue(clientId, out var clientData))
        {
            return clientData;
        }
        return null;
    }
}

