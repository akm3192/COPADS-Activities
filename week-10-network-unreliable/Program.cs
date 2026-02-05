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

Console.WriteLine("Attempting to send with exponential backoff (baseDelay = 1000ms)...\n");

int maxRetries = 5;
int baseDelayMs = 1000;

for (int attempt = 0; attempt <= maxRetries; attempt++)
{
    var result = await betterNetwork.SendAsync("Important message");

    if (result.Count > 0)
    {
        Console.WriteLine($"  Attempt {attempt + 1}: SUCCESS!");
        break;
    }
    else
    {
        int delayMs = baseDelayMs * (1 << attempt); // 2^attempt
        int jitter = Random.Shared.Next(0, delayMs / 4);
        int totalDelay = delayMs + jitter;

        Console.WriteLine($"  Attempt {attempt + 1}: FAILED, waiting {totalDelay}ms before retry...");

        if (attempt < maxRetries)
            await Task.Delay(totalDelay);
        else
            Console.WriteLine("  Max retries exceeded. Giving up.");
    }
}

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

var bank = new IdempotentBankService();

string requestId = Guid.NewGuid().ToString();
Console.WriteLine($"Request ID: {requestId}");
Console.WriteLine();

// Simulate duplicate requests (network retried)
for (int i = 1; i <= 3; i++)
{
    decimal result = bank.ProcessDeposit(requestId, 100m);
    Console.WriteLine($"Deposit attempt {i}: Balance = ${result}");
}

Console.WriteLine("\nNotice: Balance only increased once despite 3 'requests'!");

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

public class IdempotentBankService
{
    private decimal _balance = 1000m;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, decimal> _processedRequests = new();

    public decimal ProcessDeposit(string requestId, decimal amount)
    {
        // Check if we've already processed this request
        if (_processedRequests.TryGetValue(requestId, out decimal cachedResult))
        {
            Console.WriteLine($"  [Server] Duplicate request {requestId[..8]}... returning cached result");
            return cachedResult;
        }

        // Process the deposit
        _balance += amount;

        // Cache the result
        _processedRequests[requestId] = _balance;
        Console.WriteLine($"  [Server] Processed new request {requestId[..8]}...");

        return _balance;
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
    public async Task<(bool success, string? response)> SendReliablyAsync(string message)
    {
        // 1. Generate request ID
        // 2. Attempt to send with retries
        // 3. Use exponential backoff between retries
        // 4. Return success/failure

        return (false, null);  // TODO: Implement
    }

    // TODO: Implement send with timeout
    public async Task<(bool success, string? response)> SendWithTimeoutAsync(string message, int timeoutMs)
    {
        // Use CancellationTokenSource to implement timeout
        // Distinguish between "failed" and "timed out" (unknown status)

        return (false, null);  // TODO: Implement
    }
}
