Console.WriteLine("=== Week 10: The Network is Unreliable ===");
Console.WriteLine("Distributed Systems Fundamentals\n");

Console.WriteLine(@"
Welcome to Distributed Systems!

The First Rule: The network is unreliable.
The Second Rule: The network is REALLY unreliable.

Things that can (and will) go wrong:
- Messages get lost
- Messages arrive out of order
- Messages get duplicated
- Servers crash and restart
- Network partitions split your system
- Latency varies wildly (1ms to 10 seconds!)
");

// ============================================
// PART 1: Simulating an Unreliable Network
// ============================================
Console.WriteLine("--- Part 1: The Unreliable Network ---\n");

var network = new UnreliableNetwork(
    packetLossRate: 0.3,     // 30% of messages lost
    duplicationRate: 0.1,    // 10% of messages duplicated
    maxDelayMs: 500          // Up to 500ms delay
);

Console.WriteLine("Sending 10 messages over unreliable network...\n");

for (int i = 1; i <= 10; i++)
{
    string message = $"Message {i}";
    Console.WriteLine($"Sending: {message}");

    var responses = await network.SendAsync(message);

    if (responses.Count == 0)
        Console.WriteLine($"  -> LOST!");
    else if (responses.Count > 1)
        Console.WriteLine($"  -> DUPLICATED! Received {responses.Count} times");
    else
        Console.WriteLine($"  -> Delivered after {responses[0].delayMs}ms");
}

Console.WriteLine("\nThis is why we need strategies to handle failures!");

// ============================================
// PART 2: Retry with Exponential Backoff
// ============================================
Console.WriteLine("\n--- Part 2: Retry with Exponential Backoff ---\n");

Console.WriteLine(@"
When a request fails, retry! But be smart about it:

Simple Retry:     Try, fail, try, fail, try, fail... (hammers server)
Exponential:      Try, wait 1s, try, wait 2s, try, wait 4s... (gentle)

Formula: delay = baseDelay * (2 ^ attemptNumber) + random jitter
");

var betterNetwork = new UnreliableNetwork(packetLossRate: 0.5, duplicationRate: 0, maxDelayMs: 100);

Console.WriteLine(@"
TASK 2.1: Calculate delays (baseDelay = 1000ms):
| Attempt | Delay (no jitter) | With jitter (0-25%) |
|---------|-------------------|---------------------|
| 0       | 1000ms            | 1000-1250ms         |
| 1       |       ms          |                     |
| 2       |       ms          |                     |
| 3       |       ms          |                     |
| 4       |       ms          |                     |
Total max wait after 5 attempts: _____ ms
");

Console.WriteLine("Attempting to send with exponential backoff...\n");
Console.WriteLine("Using ReliableClient (see Part 5) to send with retry + backoff:\n");

var reliableClient = new ReliableClient(betterNetwork, maxRetries: 5, baseDelayMs: 1000);
var (success, response) = await reliableClient.SendReliablyAsync("Important message");

if (success)
    Console.WriteLine($"\n  Result: Delivered successfully!");
else
    Console.WriteLine($"\n  Result: Failed after all retries.");

// ============================================
// PART 3: Idempotency - Safe Retries
// ============================================
Console.WriteLine("\n--- Part 3: Idempotency ---\n");

Console.WriteLine(@"
The Problem with Retries:
- You send: 'Transfer $100 to Bob'
- Network drops the ACK (but server processed it!)
- You retry: 'Transfer $100 to Bob'
- Bob now has $200!

Solution: IDEMPOTENCY
An operation is idempotent if doing it multiple times = doing it once.

Examples:
✓ SET balance = 500        (idempotent - same result each time)
✗ ADD 100 to balance       (NOT idempotent - keeps adding)
✓ SET balance = balance + 100 WHERE version = 5 (idempotent!)

Key technique: Request IDs
- Client assigns unique ID to each logical request
- Server tracks processed IDs
- Duplicate requests return cached response
");

var bank = new IdempotentService();

string requestId = Guid.NewGuid().ToString();
Console.WriteLine($"Request ID: {requestId}");
Console.WriteLine();

// Simulate duplicate requests (network retried the same logical request)
// Once you implement IdempotentService.ProcessRequest, the balance
// should only increase ONCE despite 3 calls with the same request ID.
var depositRequest = new Request("deposit", 100m);
for (int i = 1; i <= 3; i++)
{
    var result = bank.ProcessRequest(requestId, depositRequest);
    Console.WriteLine($"Deposit attempt {i}: Balance = ${result.Balance}");
}

// Now try a NEW request (different ID) — this one SHOULD increase the balance
string requestId2 = Guid.NewGuid().ToString();
var result2 = bank.ProcessRequest(requestId2, new Request("deposit", 50m));
Console.WriteLine($"\nNew request (different ID): Balance = ${result2.Balance}");
Console.WriteLine("Duplicates rejected, new requests processed — that's idempotency!");

// ============================================
// PART 4: Timeouts and Partial Failures
// ============================================
Console.WriteLine("\n--- Part 4: Timeouts and Partial Failures ---\n");

Console.WriteLine(@"
The Hardest Problem in Distributed Systems:
You send a request and get no response. What happened?

Possibilities:
1. Request was lost -> Server never processed it
2. Server crashed before processing -> Never processed
3. Server processed it, then crashed -> Processed!
4. Response was lost -> Processed!
5. Server is just slow -> Will be processed

YOU CAN'T TELL THE DIFFERENCE!

This is why:
- We need idempotent operations
- We need transaction logs
- We need consensus protocols (Paxos, Raft)
- Distributed systems are hard!
");

var slowNetwork = new UnreliableNetwork(packetLossRate: 0.3, duplicationRate: 0, maxDelayMs: 3000);
var timeoutClient = new ReliableClient(slowNetwork);

Console.WriteLine("Sending with 1-second timeout over a slow network (up to 3s delay)...\n");

for (int i = 1; i <= 5; i++)
{
    var (ok, msg) = await timeoutClient.SendWithTimeoutAsync($"Request {i}", timeoutMs: 1000);
    if (ok)
        Console.WriteLine($"  Request {i}: SUCCESS");
    else
        Console.WriteLine($"  Request {i}: TIMEOUT — did the server process it? We don't know!");
}

Console.WriteLine("\nThis ambiguity is why idempotency matters — it makes retries safe.");

// ============================================
// PART 5: YOUR TASK
// ============================================
Console.WriteLine("--- Part 5: YOUR TASK ---\n");

Console.WriteLine(@"
Implement a reliable message delivery system:

Task 1: ReliableClient class
-----------------------------
- Implement SendWithRetry() with exponential backoff
- Include request ID for idempotency
- Return success/failure after max retries

Task 2: Handle duplicates on server
-----------------------------------
- Track processed request IDs
- Return cached response for duplicates
- Clean up old IDs after some time

Task 3: Implement a simple timeout
----------------------------------
- Cancel request after timeout period
- Distinguish between 'definitely failed' and 'unknown'

BONUS: Implement at-most-once and at-least-once semantics
- at-most-once: Never retry (may lose messages)
- at-least-once: Always retry (may duplicate)
- exactly-once: Idempotent retries (what we want!)

Look for TODO comments in the classes below!
");

// ============================================
// PART 6: Discussion Questions
// ============================================
Console.WriteLine("--- Part 6: Discussion Questions ---\n");

Console.WriteLine(@"
1. Why use exponential backoff instead of fixed delays?

   _______________________________________________________________

2. What's the difference between 'at-least-once' and 'exactly-once'?

   _______________________________________________________________

3. Why can't you use wall-clock time for ordering in distributed systems?

   _______________________________________________________________

4. What is the CAP theorem and what does it mean?

   _______________________________________________________________

5. How do real systems like Kafka or databases handle these problems?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• The network WILL fail - design for it
• Retry with exponential backoff and jitter
• Use idempotency keys for safe retries
• Timeouts can't tell you if something succeeded
• CAP theorem: pick 2 of Consistency, Availability, Partition tolerance
• Distributed systems require careful thought about failure modes
");

// ============================================
// Helper Classes
// ============================================

public class UnreliableNetwork
{
    private readonly double _packetLossRate;
    private readonly double _duplicationRate;
    private readonly int _maxDelayMs;

    public UnreliableNetwork(double packetLossRate, double duplicationRate, int maxDelayMs)
    {
        _packetLossRate = packetLossRate;
        _duplicationRate = duplicationRate;
        _maxDelayMs = maxDelayMs;
    }

    public async Task<List<(string message, int delayMs)>> SendAsync(string message)
    {
        var results = new List<(string, int)>();

        // Simulate packet loss
        if (Random.Shared.NextDouble() < _packetLossRate)
            return results;

        // Simulate delay
        int delay = Random.Shared.Next(10, _maxDelayMs);
        await Task.Delay(delay);

        results.Add((message, delay));

        // Simulate duplication
        if (Random.Shared.NextDouble() < _duplicationRate)
        {
            int extraDelay = Random.Shared.Next(10, _maxDelayMs);
            await Task.Delay(extraDelay);
            results.Add((message, delay + extraDelay));
        }

        return results;
    }
}

public record Request(string Operation, decimal Amount);
public record Response(decimal Balance, bool WasDuplicate);

// TODO: Implement this class
// Part 3 calls ProcessRequest — it won't behave correctly until you implement it.
public class IdempotentService
{
    private decimal _balance = 1000m;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Response> _processedRequests = new();

    public Response ProcessRequest(string requestId, Request request)
    {
        if (_processedRequests.ContainsKey(requestId))
        {
            return _processedRequests[requestId];
        }

        else
        {
            _balance += request.Amount;
            Console.WriteLine($"  [Server] Processed request {requestId[..8]}...");
            _processedRequests[requestId] = new Response(_balance, WasDuplicate: false);
            return new Response(_balance, WasDuplicate: false);
        }
        
        // TODO: Make this idempotent using the requestId!
        // 1. Check if requestId is already in _processedRequests
        //    - If yes: print "Duplicate request", return the cached Response
        //    - If no: process the deposit (add request.Amount to _balance),
        //      cache a new Response in _processedRequests, and return it
        //
        // Without idempotency, every call blindly adds to the balance:
        _balance += request.Amount;
        Console.WriteLine($"  [Server] Processed request {requestId[..8]}...");
        return new Response(_balance, WasDuplicate: false);
    }
}

// TODO: Implement this class
public class ReliableClient
{
    private readonly UnreliableNetwork _network;
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;

    public ReliableClient(UnreliableNetwork network, int maxRetries = 5, int baseDelayMs = 1000)
    {
        _network = network;
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
    }

    // TODO: Implement reliable send with exponential backoff
    // This is where the retry loop with backoff goes!
    // Part 2 calls this method — it won't work until you implement it.
    public async Task<(bool success, string? response)> SendReliablyAsync(string message)
    {
        // 1. Generate a request ID (for idempotency — see Part 3)
        // 2. Loop from attempt = 0 to _maxRetries:
        //    a. Try sending via _network.SendAsync(message)
        //    b. If result.Count > 0, return (true, result[0].message)
        //    c. If failed and attempts remain:
        //       - Calculate delay: _baseDelayMs * (2 ^ attempt)
        //       - Add jitter: random 0-25% of delay
        //       - await Task.Delay(delay + jitter)
        //       - Print the attempt number and wait time
        // 3. If all retries exhausted, return (false, null)

        return (false, null);  // Replace this with your implementation
    }

    // TODO: Implement send with timeout
    // Part 4 calls this method — it won't work until you implement it.
    public async Task<(bool success, string? response)> SendWithTimeoutAsync(string message, int timeoutMs)
    {
        // 1. Create a CancellationTokenSource with the timeout:
        //    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        // 2. Try sending via _network.SendAsync(message) — but race it against the timeout.
        //    Hint: use Task.WhenAny with _network.SendAsync and Task.Delay(timeoutMs, cts.Token)
        //    OR wrap the send in a try/catch for OperationCanceledException
        // 3. If the send completes before timeout and has results, return (true, result[0].message)
        // 4. If timeout fires first, return (false, null)
        //    Note: the server may STILL process the request — we just don't know!

        return (false, null);  // Replace this with your implementation
    }
}
