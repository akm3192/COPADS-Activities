using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComprehensiveReview;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== CSCI 251 Comprehensive Review ===\n");
        Console.WriteLine("Select a demo to run:");
        Console.WriteLine("1. False Sharing Demo (Part 1)");
        Console.WriteLine("2. Race Condition Demo (Part 1)");
        Console.WriteLine("3. PLINQ Ordering Demo (Part 5)");
        Console.WriteLine("4. Bug Hunt - MessageService (Part 6)");
        Console.WriteLine("5. Run All Demos");
        Console.WriteLine();
        Console.Write("Enter choice (1-5): ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                FalseSharingDemo.Run();
                break;
            case "2":
                RaceConditionDemo.Run();
                break;
            case "3":
                PlinqDemo.Run();
                break;
            case "4":
                BugHuntDemo.Run();
                break;
            case "5":
                FalseSharingDemo.Run();
                Console.WriteLine("\n" + new string('=', 50) + "\n");
                RaceConditionDemo.Run();
                Console.WriteLine("\n" + new string('=', 50) + "\n");
                PlinqDemo.Run();
                Console.WriteLine("\n" + new string('=', 50) + "\n");
                BugHuntDemo.Run();
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }
}

/// <summary>
/// Part 1, Q1.1: Demonstrates false sharing performance impact
/// </summary>
static class FalseSharingDemo
{
    // Without padding - counters on same cache line
    private static int[] countersNoPadding = new int[4];

    // With padding - each counter on its own cache line
    [StructLayout(LayoutKind.Sequential)]
    struct PaddedCounter
    {
        public int Value;
        private long _p1, _p2, _p3, _p4, _p5, _p6, _p7; // 56 bytes padding
    }
    private static PaddedCounter[] countersPadded = new PaddedCounter[4];

    public static void Run()
    {
        Console.WriteLine("=== FALSE SHARING DEMO ===");
        Console.WriteLine("Comparing parallel counter increments with and without padding.\n");

        const int iterations = 10_000_000;

        // Test without padding (false sharing)
        var sw = Stopwatch.StartNew();
        Parallel.For(0, 4, i =>
        {
            for (int j = 0; j < iterations; j++)
                countersNoPadding[i]++;
        });
        sw.Stop();
        var timeNoPadding = sw.ElapsedMilliseconds;

        // Test with padding (no false sharing)
        sw.Restart();
        Parallel.For(0, 4, i =>
        {
            for (int j = 0; j < iterations; j++)
                countersPadded[i].Value++;
        });
        sw.Stop();
        var timePadded = sw.ElapsedMilliseconds;

        Console.WriteLine($"Without padding (false sharing): {timeNoPadding} ms");
        Console.WriteLine($"With padding (no false sharing): {timePadded} ms");
        Console.WriteLine($"Speedup: {(double)timeNoPadding / timePadded:F2}x");

        Console.WriteLine("\n>> QUESTION: Why is the padded version faster?");
        Console.WriteLine(">> HINT: Think about cache lines (typically 64 bytes).");
    }
}

/// <summary>
/// Part 1, Q1.4: Demonstrates race condition with timeline
/// </summary>
static class RaceConditionDemo
{
    private static int balance = 1000;

    public static void Run()
    {
        Console.WriteLine("=== RACE CONDITION DEMO ===");
        Console.WriteLine("Two threads transferring money simultaneously.\n");

        Console.WriteLine("Running 10 trials with Thread A: -$100, Thread B: -$200");
        Console.WriteLine("Expected final balance: $700\n");

        for (int trial = 1; trial <= 10; trial++)
        {
            balance = 1000;

            var threadA = new Thread(() => Transfer(100, "A"));
            var threadB = new Thread(() => Transfer(200, "B"));

            threadA.Start();
            threadB.Start();

            threadA.Join();
            threadB.Join();

            string result = balance == 700 ? "CORRECT" : "RACE CONDITION!";
            Console.WriteLine($"Trial {trial,2}: Final balance = ${balance} ({result})");
        }

        Console.WriteLine("\n>> QUESTION: Draw a timeline showing how the race condition occurs.");
        Console.WriteLine(">> QUESTION: What synchronization would fix this?");
    }

    private static void Transfer(int amount, string threadName)
    {
        int current = balance;      // READ
        Thread.Sleep(10);           // Simulate processing delay
        balance = current - amount; // WRITE
    }
}

/// <summary>
/// Part 5, Q5.3: Demonstrates PLINQ ordering behavior
/// </summary>
static class PlinqDemo
{
    public static void Run()
    {
        Console.WriteLine("=== PLINQ ORDERING DEMO ===\n");

        Console.WriteLine("1. Regular LINQ (sequential, ordered):");
        Enumerable.Range(1, 10)
            .ToList()
            .ForEach(n => Console.Write($"{n} "));
        Console.WriteLine("\n");

        Console.WriteLine("2. PLINQ without ordering (parallel, unordered):");
        Console.WriteLine("Run 1: ");
        Enumerable.Range(1, 10)
            .AsParallel()
            .ForAll(n => Console.Write($"{n} "));
        Console.WriteLine();

        Console.WriteLine("Run 2: ");
        Enumerable.Range(1, 10)
            .AsParallel()
            .ForAll(n => Console.Write($"{n} "));
        Console.WriteLine("\n");

        Console.WriteLine("3. PLINQ with AsOrdered():");
        var ordered = Enumerable.Range(1, 10)
            .AsParallel()
            .AsOrdered()
            .Select(n => n)
            .ToList();
        Console.WriteLine(string.Join(" ", ordered));

        Console.WriteLine("\n>> QUESTION: Why is the unordered output different each time?");
        Console.WriteLine(">> QUESTION: When would you use AsOrdered()? What's the tradeoff?");
    }
}

/// <summary>
/// Part 6, Q6.2: Bug Hunt - Find the bugs in this code!
/// </summary>
static class BugHuntDemo
{
    public static void Run()
    {
        Console.WriteLine("=== BUG HUNT: MessageService ===");
        Console.WriteLine("The code below has MULTIPLE bugs. Can you find them?\n");

        Console.WriteLine("```csharp");
        Console.WriteLine(@"public class MessageService
{
    private List<string> messages = new();
    private TcpClient client;

    public void Connect(string host)
    {
        client = new TcpClient(host, 8080);
    }

    public void SendMessage(string msg)
    {
        messages.Add(msg);
        byte[] data = Encoding.UTF8.GetBytes(msg);
        client.GetStream().Write(data);
    }

    public void Disconnect()
    {
        client.Close();
    }
}");
        Console.WriteLine("```\n");

        Console.WriteLine("BUGS TO FIND (at least 3):");
        Console.WriteLine("─────────────────────────────────────────");
        Console.WriteLine("1. ________________________________________");
        Console.WriteLine("2. ________________________________________");
        Console.WriteLine("3. ________________________________________");
        Console.WriteLine("4. ________________________________________");
        Console.WriteLine();

        Console.WriteLine("HINTS:");
        Console.WriteLine("- What happens if SendMessage is called before Connect?");
        Console.WriteLine("- Is List<T> thread-safe?");
        Console.WriteLine("- What happens if a network error occurs?");
        Console.WriteLine("- Are resources properly disposed?");
        Console.WriteLine("- How does the receiver know where one message ends?");

        Console.WriteLine("\n>> Try to identify all the bugs, then check the answer key!");
        Console.WriteLine("\n--- ANSWER KEY (scroll down after attempting) ---\n\n\n\n\n\n\n\n\n\n");
        Console.WriteLine("BUG HUNT ANSWERS:");
        Console.WriteLine("─────────────────────────────────────────");
        Console.WriteLine("1. NULL REFERENCE: client is null if SendMessage() is called before Connect().");
        Console.WriteLine("   Fix: Check if client is connected, or throw InvalidOperationException.");
        Console.WriteLine();
        Console.WriteLine("2. NOT THREAD-SAFE: List<string> is not thread-safe for concurrent access.");
        Console.WriteLine("   Fix: Use ConcurrentBag<string>, ConcurrentQueue<string>, or lock around access.");
        Console.WriteLine();
        Console.WriteLine("3. NO EXCEPTION HANDLING: Network operations can throw (connection refused, timeout, etc.).");
        Console.WriteLine("   Fix: Wrap in try-catch, handle SocketException, IOException appropriately.");
        Console.WriteLine();
        Console.WriteLine("4. RESOURCE LEAK: TcpClient implements IDisposable but is never disposed.");
        Console.WriteLine("   Fix: Implement IDisposable pattern, use 'using' statement, or call Dispose() in Disconnect().");
        Console.WriteLine();
        Console.WriteLine("5. NO MESSAGE FRAMING: Receiver cannot tell where one message ends and next begins.");
        Console.WriteLine("   Fix: Add length prefix, use delimiter, or implement a proper protocol.");
        Console.WriteLine();
        Console.WriteLine("6. STREAM NOT DISPOSED: GetStream() returns a NetworkStream that should be properly managed.");
        Console.WriteLine("   Fix: Store stream reference and dispose it, or use 'using' for each operation.");
    }
}
