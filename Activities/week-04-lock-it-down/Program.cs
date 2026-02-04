
using System.Runtime.CompilerServices;

Console.WriteLine("=== Week 4: Lock It Down ===");
Console.WriteLine("Synchronization with Locks\n");

Console.WriteLine(@"
Last week we saw race conditions break our bank. Today we FIX it!

The solution: LOCKS (also called mutexes - mutual exclusion)
Only one thread can hold a lock at a time. Others must wait.
");

// ============================================
// PART 1: The Broken Version (Review)
// ============================================
Console.WriteLine("--- Part 1: The Broken Version (Review) ---\n");

var brokenAccount = new BrokenBankAccount();
RunTest(brokenAccount, "Broken (no lock)");

// ============================================
// PART 2: The Fixed Version
// ============================================
Console.WriteLine("\n--- Part 2: The Fixed Version ---\n");

Console.WriteLine(@"
We fix it by wrapping the critical section in a lock:

    private readonly object _lock = new object();

    public void Deposit(decimal amount)
    {
        lock (_lock)  // Only one thread at a time!
        {
            decimal current = Balance;
            Thread.Sleep(1);
            Balance = current + amount;
        }
    }
");

var fixedAccount = new FixedBankAccount();
RunTest(fixedAccount, "Fixed (with lock)");

// ============================================
// PART 3: YOUR TASK - Fix the Counter
// ============================================
Console.WriteLine("\n--- Part 3: YOUR TASK - Fix the Counter ---\n");

Console.WriteLine(@"
Below is a broken counter class. Multiple threads increment it,
but the final value is wrong due to race conditions.

YOUR TASK: Modify the BrokenCounter class to use a lock.
");
 
var counter = new BrokenCounter();
var threads = new Thread[10];

var _lock = new object();

for (int i = 0; i < 10; i++)
{
    threads[i] = new Thread(() =>
    {
        
       lock(_lock){
            for (int j = 0; j < 10000; j++)
                counter.Increment();
       }
    });
}

foreach (var t in threads) t.Start();
foreach (var t in threads) t.Join();

Console.WriteLine($"Expected: 100,000");
Console.WriteLine($"Actual:   {counter.Value:N0}");
Console.WriteLine($"Status:   {(counter.Value == 100000 ? "CORRECT!" : "WRONG - Add a lock!")}");

// ============================================
// PART 4: Lock Performance
// ============================================
Console.WriteLine("\n--- Part 4: Lock Performance ---\n");

Console.WriteLine("Comparing lock overhead...\n");

var sw = System.Diagnostics.Stopwatch.StartNew();
var thread = new Thread[4];
var noLockCounter = 0;
for(int j =0; j<4;j++){
    thread[j] = new Thread(() =>{    
    for (int i = 0; i < 10_000_000; i++)
        noLockCounter++;
    });
}
foreach (var t in thread) t.Start();
foreach (var t in thread) t.Join();
sw.Stop();
Console.WriteLine($"No lock (4 thread): {sw.ElapsedMilliseconds}ms");

sw.Restart();
    var lockObj = new object();
    //thread = new Thread[4];
    var lockCounter = 0;
    for(int j =0; j<4;j++){
        thread[j] = new Thread(() =>{
        for (int i = 0; i < 10_000_000; i++)
        {
            lock (lockObj)
                lockCounter++;
        }
        });
    }
    foreach (var t in thread) t.Start();
    foreach (var t in thread) t.Join();
sw.Stop();
Console.WriteLine($"With lock (4 thread): {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"Lock overhead: {sw.ElapsedMilliseconds}ms for 10M operations");
sw.Restart();
    
    thread = new Thread[1];
    lockCounter = 0;
    for(int j =0; j<1;j++){
        thread[j] = new Thread(() =>{
        for (int i = 0; i < 10_000_000; i++)
        {
            
                Interlocked.Increment(ref lockCounter);
        }
        });
    }
    foreach (var t in thread) t.Start();
    foreach (var t in thread) t.Join();
sw.Stop();
Console.WriteLine($"With interlock (4 thread): {sw.ElapsedMilliseconds}ms");
// ============================================
// PART 5: Discussion Questions
// ============================================
Console.WriteLine("\n--- Part 5: Discussion Questions ---\n");

Console.WriteLine(@"
1. What does the 'lock' keyword do in C#?

   _______________________________________________________________

2. Why do we lock on a private object instead of 'this'?

   _______________________________________________________________

3. What happens if a thread tries to acquire a lock that's held?

   _______________________________________________________________

4. Could we have TOO MANY locks? What problems might that cause?

   _______________________________________________________________

5. What's the difference between 'lock' and 'Monitor.Enter/Exit'?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• lock() ensures only one thread executes the critical section at a time
• Always lock on a private readonly object
• Locks have overhead - don't lock more than necessary
• Keep critical sections SHORT to minimize contention
• Next week: What happens when locks go wrong? DEADLOCKS!
");

// Helper method
void RunTest(IBankAccount account, string name)
{
    const int ThreadCount = 100;
    const decimal DepositAmount = 100m;
    const int DepositsPerThread = 2;

    Console.WriteLine($"Testing: {name}");
    Console.WriteLine($"  {ThreadCount} threads × ${DepositAmount} deposits each\n");

    Console.WriteLine("| Run | Expected | Final Balance | Status |");
    Console.WriteLine("|-----|----------|---------------|--------|");

    decimal expected = ThreadCount * DepositAmount * DepositsPerThread;

    for (int run = 1; run <= 5; run++)
    {
        account.Reset();

        var testThreads = new Thread[ThreadCount];
        for (int t = 0; t < ThreadCount; t++)
        {
            testThreads[t] = new Thread(() =>
            {
                for (int i = 0; i < DepositsPerThread; i++)
                    account.Deposit(DepositAmount);
            });
        }

        foreach (var t in testThreads) t.Start();
        foreach (var t in testThreads) t.Join();

        var status = account.Balance == expected ? "OK" : "WRONG";
        Console.WriteLine($"|  {run}  | ${expected,8:N0} | ${account.Balance,13:N0} | {status,-6} |");
    }
}

// ============================================
// Classes
// ============================================

public interface IBankAccount
{
    decimal Balance { get; }
    void Deposit(decimal amount);
    void Withdraw(decimal amount);
    void Reset();
}

public class BrokenBankAccount : IBankAccount
{
    public decimal Balance { get; private set; } = 0m;

    public void Deposit(decimal amount)
    {
        decimal current = Balance;
        Thread.Sleep(1);
        Balance = current + amount;
    }

    public void Withdraw(decimal amount)
    {
        decimal current = Balance;
        Thread.Sleep(1);
        Balance = current - amount;
    }

    public void Reset() => Balance = 0m;
}

public class FixedBankAccount : IBankAccount
{
    private readonly object _lock = new object();
    public decimal Balance { get; private set; } = 0m;

    public void Deposit(decimal amount)
    {
        lock (_lock)
        {
            decimal current = Balance;
            Thread.Sleep(1);
            Balance = current + amount;
        }
    }

    public void Withdraw(decimal amount)
    {
        lock (_lock)
        {
            decimal current = Balance;
            Thread.Sleep(1);
            Balance = current - amount;
        }
    }

    public void Reset() { lock (_lock) { Balance = 0m; } }
}

// TODO: Fix this class by adding a lock!
public class BrokenCounter
{
    public int Value { get; private set; } = 0;

    public void Increment()
    {
        // TODO: Add lock here to fix the race condition
        int current = Value;
        Value = current + 1;
    }
}
