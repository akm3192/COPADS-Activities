using System.Threading.Tasks;

Console.WriteLine("=== Week 5: Deadlock Escape Room ===");
Console.WriteLine("Understanding and Preventing Deadlocks\n");

Console.WriteLine(@"
The Dining Philosophers Problem:
================================
Five philosophers sit at a round table. Between each pair is a fork.
To eat, a philosopher needs BOTH the fork on their left AND right.

        [P0]
     F4      F0
   [P4]      [P1]
     F3      F1
      [P3]  [P2]
         F2

The naive solution: pick up left fork, then right fork.
This DEADLOCKS when everyone picks up their left fork simultaneously!
");

Console.WriteLine("Press ENTER to start the deadlock simulation...");
Console.ReadLine();

// ============================================
// PART 1: Observe the Deadlock
// ============================================
Console.WriteLine("--- Part 1: Observe the Deadlock ---\n");

var forks = new object[5];
for (int i = 0; i < 5; i++)
    forks[i] = new object();

var mealsEaten = new int[5];
var cts = new CancellationTokenSource();

Console.WriteLine("Starting 5 philosophers with naive fork acquisition...\n");

var philosophers = new Thread[5];
for (int i = 0; i < 5; i++)
{
    int id = i;
    philosophers[i] = new Thread(() => NaivePhilosopher(id, forks, mealsEaten, cts.Token));
    philosophers[i].Start();
}

// Wait and check for deadlock
for (int second = 0; second < 10; second++)
{
    Thread.Sleep(1000);
    int totalMeals = mealsEaten.Sum();
    Console.WriteLine($"Time {second + 1}s: Total meals eaten = {totalMeals}");

    if (second > 2 && totalMeals == mealsEaten.Sum())
    {
        Console.WriteLine("\n*** DEADLOCK DETECTED! No progress for 1 second. ***");
        break;
    }
}

cts.Cancel();
Thread.Sleep(500);

// ============================================
// PART 2: The Four Conditions for Deadlock
// ============================================
Console.WriteLine("\n--- Part 2: The Four Conditions for Deadlock ---\n");

Console.WriteLine(@"
ALL FOUR conditions must be present for deadlock:

| Condition          | Present? | How it appears here                     |
|--------------------|----------|-----------------------------------------|
| Mutual Exclusion   | Yes      | Only one philosopher can hold a fork    |
| Hold and Wait      | Yes      | Hold left fork, wait for right fork     |
| No Preemption      | Yes      | Can't force a philosopher to drop fork  |
| Circular Wait      | Yes      | P0→F0→P1→F1→P2→F2→P3→F3→P4→F4→P0       |

Break ANY ONE condition to prevent deadlock!
");

// ============================================
// PART 3: YOUR TASK - Implement a Fix
// ============================================
Console.WriteLine("--- Part 3: YOUR TASK - Implement a Fix ---\n");

Console.WriteLine(@"
Choose ONE strategy and implement it:

Strategy A: Resource Ordering (Break Circular Wait)
--------------------------------------------------
Always acquire forks in consistent order (lower ID first).
If left fork ID > right fork ID, acquire right first.



Strategy B: Timeout with Retry (Break Hold and Wait)
----------------------------------------------------
Use Monitor.TryEnter with a timeout instead of lock.
If you can't get both forks, release what you have and retry.

  Reminder: lock (obj) { ... } is shorthand for:
      Monitor.Enter(obj);
      try { ... }
      finally { Monitor.Exit(obj); }

  Monitor.TryEnter(obj, TimeSpan) returns true if the lock was acquired,
  false if the timeout expired. You must call Monitor.Exit if it returns true.

Strategy C: Limit Diners (Break Circular Wait)
---------------------------------------------
Use a SemaphoreSlim to only allow 4 of 5 philosophers to try eating.
This guarantees at least one can always get both forks.

Implement your chosen strategy in the FixedPhilosopher method below!
");

philosophers = new Thread[5];
for (int i = 0; i < 5; i++)
{
    int id = i;
    philosophers[i] = new Thread(() => NaivePhilosopher(id, forks, mealsEaten, cts.Token));
    philosophers[i].Start();
}
Console.WriteLine("Press ENTER to test your fix...");
Console.ReadLine();

// Reset and test fixed version
Array.Clear(mealsEaten);
for (int i = 0; i < 5; i++)
    forks[i] = new object();
cts = new CancellationTokenSource();

Console.WriteLine("Testing fixed version...\n");

for (int i = 0; i < 5; i++)
{
    int id = i;
    philosophers[i] = new Thread(() => FixedPhilosopher(id, forks, mealsEaten, cts.Token));
    philosophers[i].Start();
}

for (int second = 0; second < 10; second++)
{
    Thread.Sleep(1000);
    int totalMeals = mealsEaten.Sum();
    Console.WriteLine($"Time {second + 1}s: Total meals = {totalMeals} | Per philosopher: [{string.Join(", ", mealsEaten)}]");
}

cts.Cancel();
Console.WriteLine("\nNo deadlock! Philosophers are eating successfully.");

// ============================================
// PART 4: Interlocked - Avoiding Locks Entirely
// ============================================
Console.WriteLine("\n--- Part 4: Interlocked - Avoiding Locks Entirely ---\n");

Console.WriteLine(@"
For simple operations like incrementing a counter, you can avoid locks
(and deadlock risk) entirely using Interlocked:

    // Instead of this (requires lock):
    lock (_lock) { _count++; }

    // Use this (atomic, no lock needed):
    Interlocked.Increment(ref _count);

Common Interlocked operations:
- Interlocked.Increment(ref x)     // Add 1
- Interlocked.Decrement(ref x)     // Subtract 1
- Interlocked.Add(ref x, value)    // Add value
- Interlocked.Exchange(ref x, new) // Set and return old
");

Console.WriteLine("Comparing counter implementations (10 threads × 100,000 increments):\n");

const int ThreadCount = 10;
const int IncrementsPerThread = 100_000;
const int Expected = ThreadCount * IncrementsPerThread;

// Broken (no synchronization)
int brokenCounter = 0;
var sw = System.Diagnostics.Stopwatch.StartNew();
var brokenThreads = new Thread[ThreadCount];
for (int i = 0; i < ThreadCount; i++)
{
    brokenThreads[i] = new Thread(() => {
        for (int j = 0; j < IncrementsPerThread; j++)
            brokenCounter++;
    });
}
foreach (var t in brokenThreads) t.Start();
foreach (var t in brokenThreads) t.Join();
sw.Stop();
Console.WriteLine($"No lock:      {brokenCounter,10:N0}  (expected {Expected:N0}) - {sw.ElapsedMilliseconds}ms - {(brokenCounter == Expected ? "OK" : "WRONG")}");

// With lock
int lockCounter = 0;
object lockObj = new object();
sw.Restart();
var lockThreads = new Thread[ThreadCount];
for (int i = 0; i < ThreadCount; i++)
{
    lockThreads[i] = new Thread(() => {
        for (int j = 0; j < IncrementsPerThread; j++)
            lock (lockObj) { lockCounter++; }
    });
}
foreach (var t in lockThreads) t.Start();
foreach (var t in lockThreads) t.Join();
sw.Stop();
Console.WriteLine($"With lock:    {lockCounter,10:N0}  (expected {Expected:N0}) - {sw.ElapsedMilliseconds}ms - {(lockCounter == Expected ? "OK" : "WRONG")}");

// With Interlocked
int interlockedCounter = 0;
sw.Restart();
var interlockedThreads = new Thread[ThreadCount];
for (int i = 0; i < ThreadCount; i++)
{
    interlockedThreads[i] = new Thread(() => {
        for (int j = 0; j < IncrementsPerThread; j++)
            Interlocked.Increment(ref interlockedCounter);
    });
}
foreach (var t in interlockedThreads) t.Start();
foreach (var t in interlockedThreads) t.Join();
sw.Stop();
Console.WriteLine($"Interlocked:  {interlockedCounter,10:N0}  (expected {Expected:N0}) - {sw.ElapsedMilliseconds}ms - {(interlockedCounter == Expected ? "OK" : "WRONG")}");

Console.WriteLine(@"
When to use each:
- Interlocked: Simple atomic operations (counters, flags)
- lock: Complex operations, multiple variables, or method calls
");

// ============================================
// PART 5: Discussion Questions
// ============================================
Console.WriteLine("\n--- Part 5: Discussion Questions ---\n");

Console.WriteLine(@"
1. Which deadlock condition does Resource Ordering break?

   _______________________________________________________________

2. Which condition does Timeout with Retry break?

   _______________________________________________________________

3. Why does limiting to 4 diners prevent deadlock with 5 forks?

   _______________________________________________________________

4. What's the tradeoff of each strategy?
   - Resource Ordering:  _________________________________________
   - Timeout/Retry:      _________________________________________
   - Limit Diners:       _________________________________________

5. Can you think of a real-world system that might deadlock?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• Deadlock requires ALL FOUR conditions (break any one to prevent)
• Resource ordering is simple and effective
• Timeouts add resilience but can cause livelock
• Semaphores can limit concurrency to prevent circular wait
• Interlocked avoids locks entirely for simple atomic operations
• Always think about lock ordering in concurrent code!
");

// ============================================
// Philosopher Methods
// ============================================

void NaivePhilosopher(int id, object[] forkArray, int[] meals, CancellationToken token)
{
    // Left fork is shared with previous philosopher, right fork with next
    // P0: left=F4, right=F0 | P1: left=F0, right=F1 | etc.
    int leftId = (id + 4) % 5;
    int rightId = id;
    var leftFork = forkArray[leftId];
    var rightFork = forkArray[rightId];

    while (!token.IsCancellationRequested)
    {
        // Think
        Thread.Sleep(Random.Shared.Next(10, 50));

        // Try to eat (DEADLOCK PRONE!)
        lock (leftFork)
        {
            Console.WriteLine($"P{id} picked up left fork");
            Thread.Sleep(10); // Widen the deadlock window

            lock (rightFork)
            {
                Console.WriteLine($"P{id} eating...");
                Thread.Sleep(Random.Shared.Next(10, 30));
                meals[id]++;
            }
        }
    }
}

async Task FixedPhilosopher(int id, object[] forkArray, int[] meals, CancellationToken token)
{
    // TODO: Implement your chosen strategy here!
    //
    // Setup (same for all strategies):
    int leftId = (id + 4) % 5;
    int rightId = id;
    var leftFork = forkArray[leftId];
    var rightFork = forkArray[rightId];

    while (!token.IsCancellationRequested)
    {
        // Think
        Thread.Sleep(Random.Shared.Next(10, 50));

        // TODO: Replace the naive fork acquisition below with your fix.
        //
        // Strategy A: Resource Ordering
        //   Acquire the lower-numbered fork first.
        //   Hint: use Math.Min/Math.Max on leftId and rightId
        //
        // Strategy B: Timeout with Retry
        //   Use Monitor.TryEnter(fork, TimeSpan.FromMilliseconds(100))
        //   instead of lock. If it returns false, the timeout expired.
        //   If it returns true, you MUST call Monitor.Exit in a finally block.
        //   If you can't get both forks, release and retry after a random delay.
        //
        // Strategy C: Limit Diners
        //   Use a SemaphoreSlim(4) to only allow 4 philosophers to try at once.
        //   (You'll need to create the semaphore outside this method.)

        // --- Naive version (DEADLOCKS - replace this!) ---
        
       var limit= new SemaphoreSlim(4);
        await limit.WaitAsync();

        try
        {
            lock (leftFork)
            {
                lock (rightFork)
                {
                    //eat
                    Console.WriteLine($"P{id} eating...");
                Thread.Sleep(Random.Shared.Next(10, 30));
                meals[id]++;
                }
            }
        }
        finally
        {
            limit.Release();
        }

    }
}
