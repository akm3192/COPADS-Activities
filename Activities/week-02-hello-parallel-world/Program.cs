// Week 2: Hello Parallel World - Instructor Demo
// CSCI 251 - Concepts of Parallel and Distributed Systems
//
// This is an optional demonstration for the instructor to show
// sequential vs parallel execution. Students complete the paper
// worksheet for this activity (no coding required).

using System.Diagnostics;

Console.WriteLine("=== Week 2: Hello Parallel World ===");
Console.WriteLine("Sequential vs Parallel Execution Demo\n");

Console.WriteLine($"This machine has {Environment.ProcessorCount} logical processors.\n");

// Simulated workload
const int TaskCount = 100;
const int WorkTimeMs = 50;

Console.WriteLine($"Processing {TaskCount} items, each taking {WorkTimeMs}ms...\n");

// Sequential processing
Console.WriteLine("Sequential Processing:");
var stopwatch = Stopwatch.StartNew();

for (int i = 0; i < TaskCount; i++)
{
    Thread.Sleep(WorkTimeMs); // Simulate work
}

stopwatch.Stop();
var sequentialTime = stopwatch.ElapsedMilliseconds;
Console.WriteLine($"  Time: {sequentialTime}ms\n");

// Parallel processing
Console.WriteLine("Parallel Processing:");
stopwatch.Restart();

Parallel.For(0, TaskCount, i =>
{
    Thread.Sleep(WorkTimeMs); // Simulate work
});

stopwatch.Stop();
var parallelTime = stopwatch.ElapsedMilliseconds;
Console.WriteLine($"  Time: {parallelTime}ms");
Console.WriteLine($"  Speedup: {(double)sequentialTime / parallelTime:F2}x\n");

// Show effect of different parallelism levels
Console.WriteLine("Effect of MaxDegreeOfParallelism:");
Console.WriteLine("| Threads |  Time (ms) | Speedup |");
Console.WriteLine("|---------|------------|---------|");

foreach (var degree in new[] { 1, 2, 4, 8, Environment.ProcessorCount })
{
    var options = new ParallelOptions { MaxDegreeOfParallelism = degree };

    stopwatch.Restart();
    Parallel.For(0, TaskCount, options, i => Thread.Sleep(WorkTimeMs));
    stopwatch.Stop();

    var time = stopwatch.ElapsedMilliseconds;
    var speedup = (double)sequentialTime / time;
    Console.WriteLine($"| {degree,7} | {time,10} | {speedup,6:F2}x |");
}

Console.WriteLine("\nThis demonstrates why Amdahl's Law matters!");
Console.WriteLine("See your worksheet for the calculations.");
