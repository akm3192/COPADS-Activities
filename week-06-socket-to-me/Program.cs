using System.Net;
using System.Net.Sockets;

Console.WriteLine("=== Week 6: Socket to Me ===");
Console.WriteLine("Introduction to Network Sockets\n");

Console.WriteLine(@"
Sockets are the foundation of network programming.
They provide an endpoint for sending/receiving data over a network.

Today we'll build a simple TCP echo server and client!

TCP vs UDP:
| Feature    | TCP                      | UDP                      |
|------------|--------------------------|--------------------------|
| Connection | Phone call (connected)   | Letters (connectionless) |
| Reliability| Guaranteed, in-order     | Best effort, may lose    |
| Speed      | Slower (overhead)        | Faster (no handshake)    |
| Use cases  | Web, email, files        | Streaming, gaming, DNS   |
");

// ============================================
// PART 1: Understanding Networks
// ============================================
Console.WriteLine("--- Part 1: Understanding Networks ---\n");

Console.WriteLine(@"
Q1.1: Choose TCP or UDP for each and explain:
- Online banking:           _____________
- Live video streaming:     _____________
- Multiplayer game updates: _____________
- Email server:             _____________

Q1.2: Why does TCP need the three-way handshake (SYN → SYN-ACK → ACK)?

      _______________________________________________________________
");

// ============================================
// PART 2: Client-Server Model
// ============================================
Console.WriteLine("--- Part 2: Client-Server Model ---\n");

Console.WriteLine(@"
The Connection Lifecycle:

SERVER                              CLIENT
1. Create TcpListener               1. Create TcpClient
2. Start listening                  2. Connect to server:port
3. AcceptTcpClient() [blocks]  <--- connection established
4. Get NetworkStream                3. Get NetworkStream
5. Read/Write data         <------> 4. Write/Read data
6. Close connection                 5. Close connection

Q2.1: Match concepts to descriptions:
| Concept       | Description                      |
|---------------|----------------------------------|
| Port          | A) Unique machine address        |
| IP Address    | B) Service number (0-65535)      |
| Socket        | C) Stream for reading/writing    |
| NetworkStream | D) IP + Port endpoint            |

Q2.2: Well-known ports: HTTP=___, HTTPS=___, SSH=___, FTP=___
");

// ============================================
// PART 3: Build an Echo Server
// ============================================
Console.WriteLine("--- Part 3: Build an Echo Server ---\n");

Console.WriteLine("Starting Echo Server on port 5000...\n");

int port = 5000;
var serverTask = Task.Run(() => RunServer(port));

// Give server time to start
await Task.Delay(500);
Console.WriteLine($"Server listening on port {port}\n");

// Test with a client
Console.WriteLine("Connecting test client...\n");
await TestClient(port);

Console.WriteLine(@"
Q3.2: Analyze this server code:

    var listener = new TcpListener(IPAddress.Any, 5000);
    listener.Start();
    var client = await listener.AcceptTcpClientAsync();
    using var stream = client.GetStream();
    using var reader = new StreamReader(stream);
    using var writer = new StreamWriter(stream) { AutoFlush = true };

    while (true)
    {
        string? message = await reader.ReadLineAsync();
        if (message == null) break;
        await writer.WriteLineAsync($""Echo: {message}"");
    }

- What does IPAddress.Any mean?      ________________________________
- Why does AcceptTcpClient() block?  ________________________________
- What causes ReadLineAsync() null?  ________________________________
- Why set AutoFlush = true?          ________________________________
");

// ============================================
// PART 4: Enhance the Server
// ============================================
Console.WriteLine("--- Part 4: Enhance the Server ---\n");

Console.WriteLine(@"
YOUR TASKS - Modify the HandleClientAsync method to:

Q4.1: Echo in UPPERCASE
      Input: 'hello' → Output: 'HELLO'

Q4.2: Add timestamps
      Input: 'hello' → Output: '[14:32:15] HELLO'

Q4.3: Implement commands:
      | Command | Response              |
      |---------|-----------------------|
      | /time   | Current server time   |
      | /date   | Current date          |
      | /help   | List commands         |
      | /quit   | Disconnect client     |

Look for the TODO comments in HandleClientAsync!
");

// ============================================
// PART 5: Error Handling
// ============================================
Console.WriteLine("--- Part 5: Error Handling ---\n");

Console.WriteLine(@"
Q5.1: List 5 things that could go wrong with network communication:
      1. _______________________________________________________________
      2. _______________________________________________________________
      3. _______________________________________________________________
      4. _______________________________________________________________
      5. _______________________________________________________________

Q5.2: What exception type when connecting to non-running server?

      _______________________________________________________________

Q5.3: How does server detect: graceful close vs crash vs network drop?

      _______________________________________________________________
");

// ============================================
// PART 6: Multiple Clients
// ============================================
Console.WriteLine("--- Part 6: Multiple Clients ---\n");

Console.WriteLine(@"
Q6.1: Why can a simple server only handle ONE client at a time?

      _______________________________________________________________

Q6.2: Analyze multi-client pattern:

    while (true) {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleClientAsync(client));
    }

- Why Task.Run()?  _______________________________________________
- What does _ = mean?  ___________________________________________

Q6.3: Test with 3 clients. Do others continue when one disconnects?
      (Open multiple terminals and use: nc localhost 5000)
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• TCP is connection-oriented and reliable
• TcpListener accepts; TcpClient initiates
• Data sent as bytes—encode strings properly (or use StreamReader/Writer)
• Handle errors and cleanup resources
• Use async/tasks for multiple clients
");

Console.WriteLine("\nServer is still running. Press Ctrl+C to exit.");
Console.WriteLine("Try connecting with: nc localhost 5000\n");

// Keep server running for manual testing
await Task.Delay(Timeout.Infinite);

// ============================================
// Server Method (uses StreamReader/StreamWriter as shown in writeup)
// ============================================
async Task RunServer(int serverPort)
{
    var listener = new TcpListener(IPAddress.Any, serverPort);
    listener.Start();

    try
    {
        while (true)
        {
            var tcpClient = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"[Server] Client connected from {tcpClient.Client.RemoteEndPoint}");

            // Handle each client in a separate task (Part 6 pattern)
            _ = Task.Run(() => HandleClientAsync(tcpClient));
        }
    }
    catch (SocketException)
    {
        // Server stopped
    }
}

async Task HandleClientAsync(TcpClient tcpClient)
{
    try
    {
        using var stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        await writer.WriteLineAsync("Welcome to the Echo Server! Type /help for commands.");

        while (true)
        {
            string? message = await reader.ReadLineAsync();
            if (message == null) break; // Client disconnected

            Console.WriteLine($"[Server] Received: '{message}'");

            // TODO (Q4.3): Implement commands
            // Handle messages starting with "/" as commands
            // Supported commands:
            //   /time - respond with current server time (HH:mm:ss format)
            //   /date - respond with current date (yyyy-MM-dd format)
            //   /help - respond with list of available commands
            //   /quit - send "Goodbye!" and return to disconnect the client
            //   unknown commands - respond with "Unknown command: {message}"
            if (message.StartsWith("/"))
            {
                // TODO: Implement the command switch statement here
                // Use message.ToLower() to make commands case-insensitive
                await writer.WriteLineAsync($"Command not implemented: {message}");
            }
            else
            {
                // TODO: Q4.1 - Convert to UPPERCASE
                // TODO: Q4.2 - Add timestamp prefix [HH:mm:ss]
                string response = message; // Change this!
                await writer.WriteLineAsync($"Echo: {response}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Server] Error: {ex.Message}");
    }
    finally
    {
        tcpClient.Close();
        Console.WriteLine("[Server] Client disconnected");
    }
}

async Task TestClient(int serverPort)
{
    try
    {
        using var client = new TcpClient("127.0.0.1", serverPort);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        // Read welcome message
        string? welcome = await reader.ReadLineAsync();
        Console.WriteLine($"Server: {welcome}\n");

        // Test messages
        string[] messages = { "Hello, Server!", "/time", "/help", "Goodbye!" };

        foreach (var msg in messages)
        {
            await writer.WriteLineAsync(msg);
            Console.WriteLine($"Sent:     '{msg}'");

            string? response = await reader.ReadLineAsync();
            Console.WriteLine($"Received: '{response}'\n");

            await Task.Delay(300);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Client error: {ex.Message}");
    }
}
