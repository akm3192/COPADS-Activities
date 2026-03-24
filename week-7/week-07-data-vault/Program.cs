using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

Console.WriteLine("=== Week 7: Data Vault ===");
Console.WriteLine("Building a Concurrent Key-Value Store\n");

Console.WriteLine(@"
Today we're building a key-value store server — like a mini Redis.

Unlike a chat server (streaming + broadcasting), this uses
request-response: clients send commands, server sends back results.

Commands:
  SET key value   → Store a value
  GET key         → Retrieve a value
  DEL key         → Delete a key
  EXISTS key      → Check if key exists
  KEYS            → List all keys
  COUNT           → Number of stored keys
  CLEAR           → Remove all keys
  APPEND key val  → Append to existing value
  INCR key        → Increment numeric value
  QUIT            → Disconnect
");

// ============================================
// PART 2: Single-Client Demo
// ============================================
Console.WriteLine("--- Part 2: Single-Client Demo ---\n");

int port = 5050;
var server = new KeyValueServer(port);
var serverTask = Task.Run(() => server.Start());

await Task.Delay(500);
Console.WriteLine($"Key-Value server started on port {port}\n");

// Simulate a single client session
Console.WriteLine("Simulating single client session...\n");
await SimulateSingleClient(port);

await Task.Delay(500);

// ============================================
// PART 3: Multi-Client Demo (AFTER you fix Part 3)
// ============================================
Console.WriteLine("\n--- Part 3: Multi-Client Demo ---\n");
Console.WriteLine("After implementing concurrent client handling (Part 3),");
Console.WriteLine("uncomment the code below to test with multiple clients.\n");

// TODO: Uncomment after implementing multi-client support in Part 3
await SimulateMultipleClients(port);

// ============================================
// PART 5: The Increment Race
// ============================================
Console.WriteLine("\n--- Part 5: The Increment Race ---\n");
Console.WriteLine("After implementing INCR, uncomment to see the race condition.\n");

// TODO: Uncomment after implementing INCR
// await SimulateIncrementRace(port);

await Task.Delay(500);
server.Stop();

Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
- Request-response: client sends command, waits for reply
- ConcurrentDictionary for thread-safe shared state
- Individual operations are safe, but read-modify-write needs AddOrUpdate
- Protocol design: consistent format makes parsing straightforward
");

// ============================================
// Simulation Methods
// ============================================

async Task SimulateSingleClient(int serverPort)
{
    try
    {
        using var client = new TcpClient("127.0.0.1", serverPort);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        // Read welcome message
        var welcome = await reader.ReadLineAsync();
        Console.WriteLine($"  Server: {welcome}");

        // Run some commands
        string[] commands = [
            "SET name Alice",
            "SET age 25",
            "SET city Pittsburgh",
            "GET name",
            "GET age",
            "GET nonexistent",
            "SET greeting Hello World",
            "GET greeting",
<<<<<<< HEAD
            "KEYS",
            "DEL name",
            "EXISTS name",
            "KEYS"
=======
            "COUNT",
            "CLEAR",
            "APPEND greeting World"
>>>>>>> 15473d7a6621d21c103d58037d1078f25fbaeddb
        ];

        foreach (var cmd in commands)
        {
            await writer.WriteLineAsync(cmd);
            var response = await reader.ReadLineAsync();
            Console.WriteLine($"  > {cmd}  →  {response}");
            await Task.Delay(100);
        }

        await writer.WriteLineAsync("QUIT");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Client error: {ex.Message}");
    }
}

#pragma warning disable CS8321
async Task SimulateMultipleClients(int serverPort)
{
    Console.WriteLine("Testing concurrent access from 3 clients...\n");

    var tasks = new List<Task>();

    // Client 1: Sets a value, then later reads it back
    tasks.Add(Task.Run(async () =>
    {
        await Task.Delay(100);
        using var client = new TcpClient("127.0.0.1", serverPort);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };
        await reader.ReadLineAsync(); // welcome

        // SET name Alice
        await writer.WriteLineAsync("SET name Alice");
        var resp = await reader.ReadLineAsync();
        Console.WriteLine($"  [Client 1] SET name Alice  →  {resp}");

        // Wait for Client 2 and Client 3 to finish
        await Task.Delay(1000);

        // GET name (should see Charlie's overwrite)
        await writer.WriteLineAsync("GET name");
        resp = await reader.ReadLineAsync();
        Console.WriteLine($"  [Client 1] GET name  →  {resp}");

        await writer.WriteLineAsync("QUIT");
    }));

    // Client 2: Reads value set by Client 1
    tasks.Add(Task.Run(async () =>
    {
        await Task.Delay(300); // Start after Client 1 has set the value
        using var client = new TcpClient("127.0.0.1", serverPort);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };
        await reader.ReadLineAsync(); // welcome

        await writer.WriteLineAsync("GET name");
        var resp = await reader.ReadLineAsync();
        Console.WriteLine($"  [Client 2] GET name  →  {resp}");

        await writer.WriteLineAsync("QUIT");
    }));

    // Client 3: Overwrites the value
    tasks.Add(Task.Run(async () =>
    {
        await Task.Delay(500); // After Client 2 reads
        using var client = new TcpClient("127.0.0.1", serverPort);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };
        await reader.ReadLineAsync(); // welcome

        await writer.WriteLineAsync("SET name Charlie");
        var resp = await reader.ReadLineAsync();
        Console.WriteLine($"  [Client 3] SET name Charlie  →  {resp}");

        await writer.WriteLineAsync("QUIT");
    }));

    await Task.WhenAll(tasks);
    Console.WriteLine("\n  Multi-client test complete!");
}

#pragma warning disable CS8321
async Task SimulateIncrementRace(int serverPort)
{
    Console.WriteLine("Racing 10 clients, each incrementing 'visits' 100 times...\n");

    // Reset the counter
    using (var setup = new TcpClient("127.0.0.1", serverPort))
    using (var s = setup.GetStream())
    using (var r = new StreamReader(s))
    using (var w = new StreamWriter(s) { AutoFlush = true })
    {
        await r.ReadLineAsync(); // welcome
        await w.WriteLineAsync("SET visits 0");
        await r.ReadLineAsync();
        await w.WriteLineAsync("QUIT");
    }

    // Race!
    var tasks = new List<Task>();
    int clientCount = 10;
    int incrementsPerClient = 100;

    for (int i = 0; i < clientCount; i++)
    {
        int clientNum = i + 1;
        tasks.Add(Task.Run(async () =>
        {
            using var client = new TcpClient("127.0.0.1", serverPort);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await reader.ReadLineAsync(); // welcome

            for (int j = 0; j < incrementsPerClient; j++)
            {
                await writer.WriteLineAsync("INCR visits");
                await reader.ReadLineAsync();
            }
            await writer.WriteLineAsync("QUIT");
        }));
    }

    await Task.WhenAll(tasks);

    // Check final value
    using (var check = new TcpClient("127.0.0.1", serverPort))
    using (var s = check.GetStream())
    using (var r = new StreamReader(s))
    using (var w = new StreamWriter(s) { AutoFlush = true })
    {
        await r.ReadLineAsync(); // welcome
        await w.WriteLineAsync("GET visits");
        var result = await r.ReadLineAsync();
        int expected = clientCount * incrementsPerClient;
        Console.WriteLine($"  Expected: {expected}");
        Console.WriteLine($"  Actual:   {result}");
        Console.WriteLine($"  {(result == expected.ToString() ? "CORRECT!" : "RACE CONDITION — values were lost!")}");
        await w.WriteLineAsync("QUIT");
    }
}

// ============================================
// Key-Value Server
// ============================================

public class KeyValueServer
{
    private readonly int _port;
    private TcpListener? _listener;
    private bool _running;
    private int _clientCount;

    // TODO (Part 3): Change Dictionary to ConcurrentDictionary for thread safety
    private readonly ConcurrentDictionary<string, string> _store = new();

    public KeyValueServer(int port) => _port = port;

    public async Task Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _running = true;

        Console.WriteLine($"[Server] Key-Value store listening on port {_port}");

        try
        {
            while (_running)
            {
                var client = await _listener.AcceptTcpClientAsync();

                // BUG: This only handles ONE client at a time!
                // TODO (Part 3): Make this handle multiple clients concurrently
                _= HandleClient(client);
            }
        }
        catch (SocketException) when (!_running)
        {
            // Normal shutdown
        }
    }

    public void Stop()
    {
        _running = false;
        _listener?.Stop();
        Console.WriteLine("[Server] Stopped");
    }

    private async Task HandleClient(TcpClient tcpClient)
    {
        _clientCount++;
        int clientNum = _clientCount;

        try
        {
            using var stream = tcpClient.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            await writer.WriteLineAsync("CONNECTED to DataVault");

            while (_running)
            {
                var message = await reader.ReadLineAsync();
                if (message == null) break;

                var response = ProcessCommand(message);
                if (response == null) break; // QUIT

                await writer.WriteLineAsync(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Client {clientNum} error: {ex.Message}");
        }
        finally
        {
            tcpClient.Close();
        }
    }

    private string? ProcessCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "ERROR: Empty command";

        var parts = message.Split(' ', 3);
        var command = parts[0].ToUpper();

        try
        {
            return command switch
            {
                "SET" when parts.Length >= 3 => Set(parts[1], parts[2]),
                "SET" => "ERROR: Usage: SET key value",
                "GET" when parts.Length >= 2 => Get(parts[1]),
                "GET" => "ERROR: Usage: GET key",
                "QUIT" => null,
<<<<<<< HEAD
                "DEL" when parts.Length >= 2 => Del(parts[1]),
                "EXISTS" when parts.Length >= 2 => Exists(parts[1]),
                "KEYS" => Keys(),
=======
                

>>>>>>> 15473d7a6621d21c103d58037d1078f25fbaeddb
                // TODO (Part 4): Implement these commands
                // "DEL" when parts.Length >= 2 => Del(parts[1]),
                // "EXISTS" when parts.Length >= 2 => Exists(parts[1]),
                // "KEYS" => Keys(),
                "COUNT" => Count(),
                "CLEAR" => Clear(),
                "APPEND" when parts.Length >= 3 => Append(parts[1], parts[2]),

                // TODO (Part 5): Implement INCR
                // "INCR" when parts.Length >= 2 => Incr(parts[1]),

                _ => $"ERROR: Unknown command '{command}'"
            };
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private string Set(string key, string value)
    {
        _store[key] = value;
        return "OK";
    }

    private string Get(string key)
    {
        return _store.TryGetValue(key, out var value) ? value : "(nil)";
    }

    // TODO (Part 4): Implement these methods

    private string Del(string key)
    {
        return _store.TryRemove(key, out _) ? "(1)": "(0)";
        
    }

    private string Exists(string key)
    {
        String value;
        if(_store.TryGetValue(key, out value))
        {
            return "(true)";
        }// Remove key, return (1) if removed, (0) if not found
        return "(false)";
        // Return (true) or (false)
    }

    private string Keys()
    {
        if (_store.IsEmpty)
        {
            return "(empty)";
        }
        else
        {
            List<String> keys = _store.Keys.ToList();
            return string.Join("\n", keys);;
        }
        
        // Return all keys, one per line, or "(empty)" if none
    }

    private string Count()
    {
        // Return number of stored keys
        return _store.Count().ToString();    
    }

    private string Clear()
    {
        // Remove all keys, return OK
        _store.Clear();
        return "OK";
    }

    private string Append(string key, string value)
    {
        // Append value to existing key (or create if new)
        // Return the new string length
        _store.AddOrUpdate(key, value, (oldKey, oldValue) => oldValue + value);
        return Get(key).Length.ToString();
    }

    // TODO (Part 5): Implement INCR
    // WARNING: The naive approach has a race condition!
    //
    // private string Incr(string key)
    // {
    //     if (_store.TryGetValue(key, out var current))
    //     {
    //         int num = int.Parse(current);
    //         _store[key] = (num + 1).ToString();
    //         return (num + 1).ToString();
    //     }
    //     _store[key] = "1";
    //     return "1";
    // }
    //
    // Can you fix this using ConcurrentDictionary.AddOrUpdate()?
}
