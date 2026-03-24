using System.Diagnostics;

Console.WriteLine("=== Week 13: Chaos Monkey ===");
Console.WriteLine("Building Resilient Systems\n");

Console.WriteLine(@"
'Everything fails, all the time.' - Werner Vogels, AWS CTO

Netflix's Chaos Monkey randomly terminates production servers.
Why? To ensure their system can handle failures gracefully.

Today we learn: How to build systems that survive failures!
");

// ============================================
// PART 1: The Fragile System
// ============================================
Console.WriteLine("--- Part 1: The Fragile System ---\n");

Console.WriteLine(@"
Scenario: Payment processing with multiple services

    ┌─────────┐     ┌──────────┐     ┌─────────┐
    │ Payment │ ──> │ Fraud    │ ──> │ Bank    │
    │ Service │     │ Check    │     │ Gateway │
    └─────────┘     └──────────┘     └─────────┘

If ANY service fails, the whole transaction fails.
Let's see what happens...
");

var fragilePayment = new FragilePaymentService();

Console.WriteLine("Processing 10 payments with fragile service:\n");
int fragileSuccess = 0;
for (int i = 1; i <= 10; i++)
{
    try
    {
        await fragilePayment.ProcessPaymentAsync(100m);
        Console.WriteLine($"Payment {i}: SUCCESS");
        fragileSuccess++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Payment {i}: FAILED - {ex.Message}");
    }
}
Console.WriteLine($"\nSuccess rate: {fragileSuccess}/10 ({fragileSuccess * 10}%)\n");

// ============================================
// PART 2: Retry Pattern
// ============================================
Console.WriteLine("--- Part 2: Retry Pattern ---\n");

Console.WriteLine(@"
First defense: Retry transient failures!

    Request ──> [FAIL] ──> Wait ──> Retry ──> [SUCCESS]

Key considerations:
- Only retry transient errors (network, timeout), not permanent ones
- Use exponential backoff to avoid overwhelming services
- Set a maximum retry count
");

var retryService = new RetryPaymentService(fragilePayment, maxRetries: 3);

Console.WriteLine("Processing 10 payments with retry:\n");
int retrySuccess = 0;
for (int i = 1; i <= 10; i++)
{
    try
    {
        await retryService.ProcessPaymentAsync(100m);
        Console.WriteLine($"Payment {i}: SUCCESS");
        retrySuccess++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Payment {i}: FAILED after retries - {ex.Message}");
    }
}
Console.WriteLine($"\nSuccess rate: {retrySuccess}/10 ({retrySuccess * 10}%)\n");

// ============================================
// PART 3: Circuit Breaker Pattern
// ============================================
Console.WriteLine("--- Part 3: Circuit Breaker Pattern ---\n");

Console.WriteLine(@"
Problem: Retrying a dead service wastes resources and time.

Solution: Circuit Breaker - like an electrical circuit breaker!

States:
┌────────┐  failures     ┌────────┐  timeout    ┌───────────┐
│ CLOSED │ ──────────────> OPEN   │ ───────────> HALF-OPEN │
│ (OK)   │ <────success──│ (FAIL) │ <──success──│ (TESTING) │
└────────┘               └────────┘             └───────────┘

- CLOSED: Requests flow normally
- OPEN: All requests fail immediately (don't even try)
- HALF-OPEN: Allow ONE request to test if service recovered
");

var circuitBreaker = new CircuitBreaker(failureThreshold: 3, resetTimeoutMs: 2000);

Console.WriteLine("Testing circuit breaker (service fails 80% of time):\n");

for (int i = 1; i <= 15; i++)
{
    try
    {
        await circuitBreaker.ExecuteAsync(async () =>
        {
            // Simulate 80% failure rate
            if (Random.Shared.NextDouble() < 0.8)
                throw new Exception("Service unavailable");
            return true;
        });
        Console.WriteLine($"Request {i}: SUCCESS (Circuit: {circuitBreaker.State})");
    }
    catch (CircuitBreakerOpenException)
    {
        Console.WriteLine($"Request {i}: REJECTED (Circuit: OPEN - not even trying)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Request {i}: FAILED (Circuit: {circuitBreaker.State}) - {ex.Message}");
    }

    await Task.Delay(300);
}

// ============================================
// PART 4: Timeout Pattern
// ============================================
Console.WriteLine("\n--- Part 4: Timeout Pattern ---\n");

Console.WriteLine(@"
Never wait forever! Set timeouts on all external calls.

    Request ──> [Waiting...] ──> TIMEOUT! ──> Handle gracefully

Without timeouts:
- Threads get stuck waiting
- Resources exhausted
- Cascading failures

With timeouts:
- Fail fast, retry or fallback
- Keep resources available
- System stays responsive
");

var slowService = new SlowService();

Console.WriteLine("Calling slow service WITH timeout (2s)...");
var sw = Stopwatch.StartNew();
try
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    await slowService.DoWorkAsync(cts.Token);
    Console.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");
}
catch (OperationCanceledException)
{
    Console.WriteLine($"TIMEOUT after {sw.ElapsedMilliseconds}ms - didn't wait forever!");
}

// ============================================
// PART 5: Fallback Pattern
// ============================================
Console.WriteLine("\n--- Part 5: Fallback Pattern ---\n");

Console.WriteLine(@"
When the primary service fails, use a fallback!

Fallback strategies:
1. Cache: Return cached data (may be stale)
2. Default: Return a sensible default value
3. Alternative: Use a backup service
4. Graceful degradation: Disable non-essential features

Example:
    try { return await GetRecommendations(); }
    catch { return GetDefaultRecommendations(); }  // Fallback
");

var resilientService = new ResilientService();

Console.WriteLine("Testing fallback behavior:\n");

for (int i = 1; i <= 5; i++)
{
    var result = await resilientService.GetDataAsync();
    Console.WriteLine($"Request {i}: {result}");
}

// ============================================
// PART 6: YOUR TASK
// ============================================
Console.WriteLine("\n--- Part 6: YOUR TASK ---\n");

Console.WriteLine(@"
Build a resilient payment system with ALL patterns:

Task 1: Combine Patterns
------------------------
Create a ResilientPaymentService that uses:
- Timeout (2 seconds max)
- Retry with exponential backoff (3 attempts)
- Circuit breaker (opens after 5 failures)
- Fallback to queue for later processing

Task 2: Test Under Chaos
------------------------
Create a ChaosMonkey class that randomly:
- Adds latency (10-5000ms)
- Throws exceptions (30% of the time)
- Simulates partial failures

Task 3: Measure Resilience
--------------------------
Run 100 payments through your system.
Measure:
- Success rate
- Average latency
- Circuit breaker trips
- Fallback usage

Look for TODO comments in the classes below!
");

// ============================================
// PART 7: Discussion Questions
// ============================================
Console.WriteLine("--- Part 7: Discussion Questions ---\n");

Console.WriteLine(@"
1. Why use exponential backoff instead of fixed retry delays?

   _______________________________________________________________

2. When should you NOT retry a failed request?

   _______________________________________________________________

3. What's the danger of setting timeouts too short? Too long?

   _______________________________________________________________

4. How do circuit breakers prevent cascading failures?

   _______________________________________________________________

5. What's the tradeoff of using cached data as a fallback?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• Assume everything will fail - design for it
• Retry: Handle transient failures automatically
• Circuit Breaker: Stop hammering dead services
• Timeout: Never wait forever
• Fallback: Degrade gracefully instead of failing completely
• Combine patterns for defense in depth
• Test with chaos engineering to verify resilience
");

// ============================================
// Helper Classes
// ============================================

public class FragilePaymentService
{
    public async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        // Simulate 50% failure rate
        await Task.Delay(Random.Shared.Next(50, 200));
        if (Random.Shared.NextDouble() < 0.5)
            throw new Exception("Service temporarily unavailable");
        return true;
    }
}

public class RetryPaymentService
{
    private readonly FragilePaymentService _inner;
    private readonly int _maxRetries;

    public RetryPaymentService(FragilePaymentService inner, int maxRetries)
    {
        _inner = inner;
        _maxRetries = maxRetries;
    }

    public async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await _inner.ProcessPaymentAsync(amount);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < _maxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 100;
                    await Task.Delay(delayMs);
                }
            }
        }

        throw lastException!;
    }
}

public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly int _resetTimeoutMs;
    private int _failureCount;
    private DateTime _lastFailureTime;

    public CircuitBreakerState State { get; private set; } = CircuitBreakerState.Closed;

    public CircuitBreaker(int failureThreshold, int resetTimeoutMs)
    {
        _failureThreshold = failureThreshold;
        _resetTimeoutMs = resetTimeoutMs;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (State == CircuitBreakerState.Open)
        {
            if ((DateTime.UtcNow - _lastFailureTime).TotalMilliseconds >= _resetTimeoutMs)
            {
                State = CircuitBreakerState.HalfOpen;
            }
            else
            {
                throw new CircuitBreakerOpenException();
            }
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (CircuitBreakerOpenException)
        {
            throw;
        }
        catch (Exception)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        State = CircuitBreakerState.Closed;
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
            State = CircuitBreakerState.Open;
    }
}

public enum CircuitBreakerState { Closed, Open, HalfOpen }

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException() : base("Circuit breaker is open") { }
}

public class SlowService
{
    public async Task DoWorkAsync(CancellationToken token)
    {
        // Simulates a service that takes 5 seconds
        await Task.Delay(5000, token);
    }
}

public class ResilientService
{
    private string? _cachedData = "Cached result from yesterday";

    public async Task<string> GetDataAsync()
    {
        try
        {
            // Simulate 60% failure rate
            await Task.Delay(100);
            if (Random.Shared.NextDouble() < 0.6)
                throw new Exception("Primary service failed");

            _cachedData = $"Fresh data at {DateTime.Now:HH:mm:ss}";
            return _cachedData;
        }
        catch
        {
            // Fallback to cached data
            return $"[FALLBACK] {_cachedData}";
        }
    }
}

// TODO: Implement this class
public class ResilientPaymentService
{
    // Combine: Timeout + Retry + Circuit Breaker + Fallback
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount)
    {
        // TODO: Implement with all resilience patterns
        await Task.CompletedTask;
        return new PaymentResult { Success = false, Message = "Not implemented" };
    }
}

// TODO: Implement this class
public class ChaosMonkey
{
    // Inject random failures and latency
    public async Task<T> WrapAsync<T>(Func<Task<T>> action)
    {
        // TODO: Add random latency
        // TODO: Randomly throw exceptions
        return await action();
    }
}

public record PaymentResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
}
