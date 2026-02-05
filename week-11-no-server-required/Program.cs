using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

Console.WriteLine("=== Week 11: No Server Required ===");
Console.WriteLine("Peer-to-Peer Networking\n");

Console.WriteLine(@"
So far we've built client-server systems:
- Centralized server handles all coordination
- Single point of failure
- Server can become bottleneck

What if we eliminate the server entirely?
Welcome to Peer-to-Peer (P2P) networking!

Examples: BitTorrent, Bitcoin, IPFS, many games
");

// ============================================
// PART 1: Client-Server vs P2P
// ============================================
Console.WriteLine("--- Part 1: Client-Server vs P2P ---\n");

Console.WriteLine(@"
CLIENT-SERVER:                    PEER-TO-PEER:
    ┌────────┐                      ┌────────┐
    │ Server │                      │ Peer A │
    └────────┘                      └────────┘
    /    |    \                       /    \
   /     |     \                     /      \
  ▼      ▼      ▼                   ▼        ▼
┌──┐   ┌──┐   ┌──┐             ┌────────┐  ┌────────┐
│C1│   │C2│   │C3│             │ Peer B │──│ Peer C │
└──┘   └──┘   └──┘             └────────┘  └────────┘

Pros of P2P:                     Cons of P2P:
+ No single point of failure     - Discovery is harder
+ Scales naturally               - No central authority
+ Reduced central costs          - More complex protocols
+ Censorship resistant           - NAT traversal issues
");

// ============================================
// PART 2: Peer Discovery
// ============================================
Console.WriteLine("--- Part 2: Peer Discovery ---\n");

Console.WriteLine(@"
How do peers find each other?

1. Bootstrap nodes: Well-known initial peers
2. DHT (Distributed Hash Table): Decentralized lookup
3. Local broadcast: Find peers on same network
4. Peer exchange: Ask known peers for more peers

We'll implement local UDP broadcast discovery!
");

int discoveryPort = 5002;
var localPeers = new ConcurrentBag<IPEndPoint>();
var cts = new CancellationTokenSource();

// Start listening for discovery broadcasts
var listenerTask = Task.Run(() => ListenForPeers(discoveryPort, localPeers, cts.Token));

// Broadcast our presence
Console.WriteLine("Broadcasting presence on local network...\n");
await BroadcastPresence(discoveryPort);

await Task.Delay(2000);
cts.Cancel();

Console.WriteLine($"Discovered {localPeers.Count} peer(s) on local network.");
foreach (var peer in localPeers)
    Console.WriteLine($"  - {peer}");

// ============================================
// PART 3: A Simple P2P Protocol
// ============================================
Console.WriteLine("\n--- Part 3: Simple P2P Message Passing ---\n");

Console.WriteLine(@"
Let's build a simple P2P message passing system.

Each peer:
1. Listens for incoming connections
2. Can connect to other peers
3. Maintains a list of connected peers
4. Forwards messages to all connected peers (gossip)

This is the foundation of gossip protocols!
");

var node1 = new P2PNode(5003, "Node1");
var node2 = new P2PNode(5004, "Node2");
var node3 = new P2PNode(5005, "Node3");

// Start all nodes
await Task.WhenAll(
    node1.StartAsync(),
    node2.StartAsync(),
    node3.StartAsync()
);

// Connect nodes in a line: Node1 -- Node2 -- Node3
await node1.ConnectToPeerAsync("127.0.0.1", 5004);
await node2.ConnectToPeerAsync("127.0.0.1", 5005);

await Task.Delay(500);

// Node1 sends a message - it should reach Node3 via Node2
Console.WriteLine("\nNode1 broadcasts 'Hello P2P World!'");
await node1.BroadcastAsync("Hello P2P World!");

await Task.Delay(1000);

Console.WriteLine($"\nMessages received:");
Console.WriteLine($"  Node1: {node1.ReceivedMessages.Count} messages");
Console.WriteLine($"  Node2: {node2.ReceivedMessages.Count} messages");
Console.WriteLine($"  Node3: {node3.ReceivedMessages.Count} messages");

await node1.StopAsync();
await node2.StopAsync();
await node3.StopAsync();

// ============================================
// PART 4: Gossip Protocols
// ============================================
Console.WriteLine("\n--- Part 4: Gossip Protocols ---\n");

Console.WriteLine(@"
Gossip (Epidemic) Protocols spread information like rumors:

1. Node receives new message
2. Picks random subset of peers
3. Sends message to those peers
4. Repeat until everyone knows

Properties:
- Eventually consistent (all nodes get message eventually)
- Fault tolerant (works even if some nodes fail)
- Scalable (O(log n) rounds to reach all nodes)

Used by: Cassandra, Consul, Bitcoin, many more
");

// ============================================
// PART 5: YOUR TASK
// ============================================
Console.WriteLine("--- Part 5: YOUR TASK ---\n");

Console.WriteLine(@"
Enhance the P2P system with these features:

Task 1: Deduplication
---------------------
Currently, messages can loop forever in cycles.
Add message IDs and track seen messages to prevent this.

Task 2: Peer List Exchange
--------------------------
When connecting to a peer, exchange known peer lists.
This helps new nodes discover the network.

Task 3: TTL (Time To Live)
--------------------------
Add a hop counter that decrements with each forward.
Messages die after N hops to prevent flooding.

Task 4: BONUS - Simple Consensus
--------------------------------
Implement a simple voting protocol where peers
agree on a value (e.g., 'What time is it?')

Look for TODO comments in the P2PNode class!
");

// ============================================
// PART 6: Discussion Questions
// ============================================
Console.WriteLine("--- Part 6: Discussion Questions ---\n");

Console.WriteLine(@"
1. What makes NAT (Network Address Translation) a challenge for P2P?

   _______________________________________________________________

2. How does BitTorrent handle the 'free rider' problem?

   _______________________________________________________________

3. Why do gossip protocols use randomization?

   _______________________________________________________________

4. What's the difference between flooding and gossip?

   _______________________________________________________________

5. How does Bitcoin achieve consensus without a central server?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• P2P removes the single point of failure
• Peer discovery can use broadcast, DHT, or bootstrapping
• Gossip protocols spread information reliably
• Message deduplication and TTL prevent infinite loops
• NAT traversal is a real-world challenge
• Consensus in P2P requires special protocols (Paxos, Raft, PoW)
");

// ============================================
// Helper Methods
// ============================================

async Task BroadcastPresence(int port)
{
    using var udp = new UdpClient();
    udp.EnableBroadcast = true;

    byte[] message = Encoding.UTF8.GetBytes($"PEER_HELLO:{Environment.MachineName}");
    await udp.SendAsync(message, new IPEndPoint(IPAddress.Broadcast, port));
}

async Task ListenForPeers(int port, ConcurrentBag<IPEndPoint> peers, CancellationToken token)
{
    using var udp = new UdpClient(port);

    try
    {
        while (!token.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(token);
            string message = Encoding.UTF8.GetString(result.Buffer);

            if (message.StartsWith("PEER_HELLO:"))
            {
                peers.Add(result.RemoteEndPoint);
                Console.WriteLine($"  Discovered peer: {result.RemoteEndPoint}");
            }
        }
    }
    catch (OperationCanceledException) { }
}

// ============================================
// P2P Node Class
// ============================================

public class P2PNode
{
    private readonly int _port;
    private readonly string _name;
    private TcpListener? _listener;
    private readonly ConcurrentDictionary<string, TcpClient> _peers = new();
    private readonly ConcurrentBag<string> _receivedMessages = new();
    private readonly HashSet<string> _seenMessageIds = new();  // For deduplication
    private bool _running;

    public IReadOnlyCollection<string> ReceivedMessages => _receivedMessages.ToArray();

    public P2PNode(int port, string name)
    {
        _port = port;
        _name = name;
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _running = true;

        Console.WriteLine($"[{_name}] Listening on port {_port}");

        _ = Task.Run(AcceptConnectionsAsync);
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _running = false;
        _listener?.Stop();

        foreach (var peer in _peers.Values)
            peer.Close();

        _peers.Clear();
        await Task.CompletedTask;
    }

    public async Task ConnectToPeerAsync(string host, int port)
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port);

            string peerId = $"{host}:{port}";
            _peers[peerId] = client;

            Console.WriteLine($"[{_name}] Connected to {peerId}");

            _ = Task.Run(() => HandlePeerAsync(client, peerId));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_name}] Failed to connect: {ex.Message}");
        }
    }

    public async Task BroadcastAsync(string message, int ttl = 5)
    {
        // TODO: Add message ID for deduplication and TTL
        // Message format: "{messageId}|{ttl}|{sender}|{content}"
        // Example: "abc123|5|Node1|Hello World"
        string messageId = Guid.NewGuid().ToString("N")[..8];
        string fullMessage = $"{messageId}|{ttl}|{_name}|{message}";
        byte[] data = Encoding.UTF8.GetBytes(fullMessage + "\n");

        foreach (var peer in _peers.Values)
        {
            try
            {
                await peer.GetStream().WriteAsync(data);
            }
            catch { /* Peer disconnected */ }
        }
    }

    private async Task AcceptConnectionsAsync()
    {
        while (_running)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync();
                string peerId = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                _peers[peerId] = client;

                Console.WriteLine($"[{_name}] Accepted connection from {peerId}");

                _ = Task.Run(() => HandlePeerAsync(client, peerId));
            }
            catch when (!_running) { }
        }
    }

    private async Task HandlePeerAsync(TcpClient client, string peerId)
    {
        try
        {
            using var reader = new StreamReader(client.GetStream());

            while (_running && client.Connected)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null) break;

                // TODO: Parse message format: "{messageId}|{ttl}|{sender}|{content}"
                // var parts = line.Split('|', 4);
                // string messageId = parts[0];
                // int ttl = int.Parse(parts[1]);
                // string sender = parts[2];
                // string content = parts[3];
                //
                // Deduplication: if (_seenMessageIds.Contains(messageId)) continue;
                // _seenMessageIds.Add(messageId);
                //
                // TTL: if (ttl <= 0) continue; // Don't forward expired messages
                // When forwarding, decrement TTL: $"{messageId}|{ttl-1}|{sender}|{content}"

                _receivedMessages.Add(line);
                Console.WriteLine($"[{_name}] Received: {line}");

                // TODO: Forward to other peers (gossip)
                // Don't send back to sender!
            }
        }
        catch { }
        finally
        {
            _peers.TryRemove(peerId, out _);
        }
    }
}
