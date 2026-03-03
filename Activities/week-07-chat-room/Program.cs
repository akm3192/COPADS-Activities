using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("=== Week 7: Chat Room ===");
Console.WriteLine("Building a Multi-Client Chat Server\n");

Console.WriteLine(@"
Last week we built a simple echo server. Now let's scale up!

A chat server must:
1. Accept multiple client connections simultaneously
2. Broadcast messages from one client to ALL others
3. Handle clients joining and leaving gracefully
4. Track connected clients

Key concepts:
- Concurrent collections for thread-safe client tracking
- Async/await for non-blocking I/O
- Broadcasting patterns
");

// ============================================
// PART 1: The Chat Server Architecture
// ============================================
Console.WriteLine("--- Part 1: Chat Server Architecture ---\n");

Console.WriteLine(@"
Architecture Overview:

    ┌─────────────────────────────────────────┐
    │              Chat Server                │
    │  ┌─────────────────────────────────┐   │
    │  │   ConcurrentDictionary          │   │
    │  │   <clientId, TcpClient>         │   │
    │  └─────────────────────────────────┘   │
    │           │         │         │        │
    │        Client1   Client2   Client3     │
    └─────────────────────────────────────────┘

When Client1 sends 'Hello':
1. Server receives message from Client1
2. Server broadcasts to Client2, Client3 (not back to Client1)
3. Each client sees: '[Client1]: Hello'
");

// ============================================
// PART 2: Run the Demo
// ============================================
Console.WriteLine("--- Part 2: Running the Chat Demo ---\n");

int port = 5001;
var server = new ChatServer(port);
var serverTask = Task.Run(() => server.Start());

await Task.Delay(500);
Console.WriteLine($"Chat server started on port {port}\n");

// Simulate multiple clients
Console.WriteLine("Simulating 3 chat clients...\n");

var client1Task = SimulateClient("Alice", port, ["Hello everyone!", "How's it going?"]);
var client2Task = SimulateClient("Bob", port, ["Hey Alice!", "Pretty good, you?"]);
var client3Task = SimulateClient("Charlie", port, ["Hi all!", "Just joined!"]);

await Task.WhenAll(client1Task, client2Task, client3Task);

await Task.Delay(1000);
server.Stop();

// ============================================
// PART 3: Understanding the Code
// ============================================
Console.WriteLine("\n--- Part 3: Understanding the Code ---\n");

Console.WriteLine(@"
Key Components:

1. ConcurrentDictionary for client tracking:
   - Thread-safe add/remove of clients
   - Each client has unique ID
   - Safe iteration during broadcast

2. Async client handling:
   async Task HandleClient(TcpClient client)
   {
       // Each client runs in its own async context
       // Non-blocking reads allow handling many clients
   }

3. Broadcast pattern:
   void Broadcast(string message, string? excludeId)
   {
       foreach (var client in _clients)
           if (client.Key != excludeId)
               SendToClient(client.Value, message);
   }
");

// ============================================
// PART 4: YOUR TASK
// ============================================
Console.WriteLine("--- Part 4: YOUR TASK ---\n");

Console.WriteLine(@"
Enhance the ChatServer class with these features:

1. Private Messages: Allow '@username message' syntax
   - Parse messages starting with @
   - Find the target client by name
   - Send only to that client

2. User List Command: '/who' shows all connected users
   - Client sends '/who'
   - Server responds with list of usernames

3. Nickname Change: '/nick newname' changes display name
   - Validate new name isn't taken
   - Broadcast 'OldName is now known as NewName'

4. BONUS: Add '/quit' command for graceful disconnect

Look for the TODO comments in the ChatServer class!
");

// ============================================
// PART 5: Discussion Questions
// ============================================
Console.WriteLine("--- Part 5: Discussion Questions ---\n");

Console.WriteLine(@"
1. Why do we use ConcurrentDictionary instead of Dictionary?

   _______________________________________________________________

2. What would happen if we used a regular List for clients?

   _______________________________________________________________

3. Why do we exclude the sender when broadcasting?

   _______________________________________________________________

4. How could we handle a client that disconnects without warning?

   _______________________________________________________________

5. What are the security concerns with a chat server like this?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• Use concurrent collections for thread-safe shared state
• Async/await enables efficient handling of many connections
• Broadcasting requires iterating over all clients safely
• Always handle client disconnection gracefully
• Consider security: authentication, message validation, rate limiting
");

// ============================================
// Helper Methods
// ============================================

async Task SimulateClient(string name, int serverPort, string[] messages)
{
    try
    {
        await Task.Delay(Random.Shared.Next(100, 300));

        using var client = new TcpClient("127.0.0.1", serverPort);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        // Send name
        await writer.WriteLineAsync(name);

        // Start receiving in background
        var receiveTask = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;
                    Console.WriteLine($"  [{name} sees]: {line}");
                }
            }
            catch { }
        });

        // Send messages
        foreach (var msg in messages)
        {
            await Task.Delay(Random.Shared.Next(200, 500));
            await writer.WriteLineAsync(msg);
            Console.WriteLine($"  [{name} sends]: {msg}");
        }

        await Task.Delay(500);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{name}] Error: {ex.Message}");
    }
}

// ============================================
// Chat Server Class
// ============================================

public class ChatServer
{
    private readonly int _port;
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
    private TcpListener? _listener;
    private bool _running;

    public ChatServer(int port) => _port = port;

    public async Task Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _running = true;

        Console.WriteLine($"[Server] Listening on port {_port}");

        try
        {
            while (_running)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClient(client);
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

        foreach (var client in _clients.Values)
        {
            client.TcpClient.Close();
        }
        _clients.Clear();

        Console.WriteLine("[Server] Stopped");
    }

    private async Task HandleClient(TcpClient tcpClient)
    {
        string clientId = Guid.NewGuid().ToString("N")[..8];
        string? clientName = null;

        try
        {
            using var stream = tcpClient.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            // Prompt for nickname and read it
            await writer.WriteLineAsync("Enter your nickname:");
            clientName = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(clientName))
            {
                tcpClient.Close();
                return;
            }

            var clientInfo = new ClientInfo(clientId, clientName, tcpClient, writer);
            _clients[clientId] = clientInfo;

            Console.WriteLine($"[Server] {clientName} joined ({_clients.Count} clients)");
            Broadcast($"{clientName} joined the chat!", clientId);

            // Read messages
            while (_running)
            {
                var message = await reader.ReadLineAsync();
                if (message == null) break;

                Console.WriteLine($"[Server] {clientName}: {message}");

                // TODO: Add command handling here
                // - Check if message starts with '/' for commands
                // - Check if message starts with '@' for private messages

                // Broadcast to all other clients
                Broadcast($"[{clientName}]: {message}", clientId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Error with {clientName ?? clientId}: {ex.Message}");
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            tcpClient.Close();

            if (clientName != null)
            {
                Console.WriteLine($"[Server] {clientName} left ({_clients.Count} clients)");
                Broadcast($"{clientName} left the chat.", clientId);
            }
        }
    }

    private void Broadcast(string message, string? excludeClientId = null)
    {
        foreach (var kvp in _clients)
        {
            if (kvp.Key == excludeClientId) continue;

            try
            {
                kvp.Value.Writer.WriteLine(message);
            }
            catch
            {
                // Client disconnected, will be cleaned up
            }
        }
    }

    // TODO: Implement private messaging
    private void SendPrivateMessage(string fromName, string toName, string message)
    {
        // Find the target client by name
        // Send message only to them
        // Format: "[PM from {fromName}]: {message}"
    }

    // TODO: Implement user list
    private void SendUserList(ClientInfo requestingClient)
    {
        // Get all connected usernames
        // Send list to requesting client
    }
}

public record ClientInfo(string Id, string Name, TcpClient TcpClient, StreamWriter Writer);
