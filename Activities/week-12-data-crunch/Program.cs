using System.Diagnostics;

Console.WriteLine("=== Week 12: Data Crunch ===");
Console.WriteLine("Parallel Data Processing with PLINQ\n");

Console.WriteLine($"System Info: {Environment.ProcessorCount} logical processors available\n");

Console.WriteLine(@"
Big Data needs parallel processing!

When you have millions of records, sequential processing is too slow.
PLINQ (Parallel LINQ) makes parallelizing data operations easy.

Key insight: Data parallelism is embarrassingly parallel
- Same operation on different data
- No dependencies between elements
- Perfect for parallel execution!
");

// ============================================
// PART 1: Introduction to PLINQ
// ============================================
Console.WriteLine("--- Part 1: Introduction to PLINQ ---\n");

Console.WriteLine(@"
TASK 1.2: Process 1 million numbers - calculate squares where n % 7 == 0
");

var numbers = Enumerable.Range(1, 1_000_000).ToList();
var sw = Stopwatch.StartNew();

// Sequential
sw.Restart();
var seqResults = numbers.Where(n => n % 7 == 0).Select(n => (long)n * n).ToList();
var seqTime = sw.ElapsedMilliseconds;

// Parallel
sw.Restart();
var parResults = numbers.AsParallel().Where(n => n % 7 == 0).Select(n => (long)n * n).ToList();
var parTime = sw.ElapsedMilliseconds;

Console.WriteLine("| Approach   | Time (ms) | Speedup |");
Console.WriteLine("|------------|-----------|---------|");
Console.WriteLine($"| Sequential | {seqTime,9} | 1x      |");
Console.WriteLine($"| Parallel   | {parTime,9} | {(seqTime > 0 ? (double)seqTime / parTime : 0):F1}x     |");

// ============================================
// PART 2: When PLINQ Helps vs Hurts
// ============================================
Console.WriteLine("\n--- Part 2: When PLINQ Helps vs Hurts ---\n");

Console.WriteLine("TASK 2.1: Test different sizes\n");
Console.WriteLine("| Size      | Sequential (ms) | Parallel (ms) | Faster?  |");
Console.WriteLine("|-----------|-----------------|---------------|----------|");

int[] sizes = { 100, 1000, 10000, 100000, 1000000 };
foreach (int size in sizes)
{
    var data = Enumerable.Range(1, size).ToList();

    sw.Restart();
    var s = data.Where(n => n % 7 == 0).Select(n => (long)n * n).Sum();
    var seqMs = sw.ElapsedMilliseconds;

    sw.Restart();
    var p = data.AsParallel().Where(n => n % 7 == 0).Select(n => (long)n * n).Sum();
    var parMs = sw.ElapsedMilliseconds;

    string faster = parMs < seqMs ? "Parallel" : (parMs > seqMs ? "Sequential" : "Same");
    Console.WriteLine($"| {size,9:N0} | {seqMs,15} | {parMs,13} | {faster,-8} |");
}

Console.WriteLine("\nQ2.2: Simple vs Complex operations - which benefits more from parallelism?\n");

// Generate sales test data
Console.WriteLine("Generating 100,000 sales records for analysis...\n");
sw.Restart();
var salesData = GenerateSalesData(100_000);
sw.Stop();
Console.WriteLine($"Generated in {sw.ElapsedMilliseconds}ms\n");

// ============================================
// PART 3: Sales Data Analysis
// ============================================
Console.WriteLine("--- Part 3: Sales Data Analysis ---\n");

Console.WriteLine("Task: Find total revenue for Electronics category\n");

// Sequential
sw.Restart();
decimal sequentialTotal = salesData
    .Where(s => s.Category == "Electronics")
    .Sum(s => s.Amount);
sw.Stop();
long sequentialMs = sw.ElapsedMilliseconds;
Console.WriteLine($"Sequential: ${sequentialTotal:N2} in {sequentialMs}ms");

// Parallel - just add .AsParallel()!
sw.Restart();
decimal parallelTotal = salesData
    .AsParallel()
    .Where(s => s.Category == "Electronics")
    .Sum(s => s.Amount);
sw.Stop();
long parallelMs = sw.ElapsedMilliseconds;
Console.WriteLine($"Parallel:   ${parallelTotal:N2} in {parallelMs}ms");

Console.WriteLine($"\nSpeedup: {(double)sequentialMs / parallelMs:F2}x");

// ============================================
// PART 3b: PLINQ Operations
// ============================================
Console.WriteLine("\n--- Part 3b: PLINQ Operations ---\n");

Console.WriteLine(@"
PLINQ supports all standard LINQ operations:

Query Syntax:
    var results = from item in data.AsParallel()
                  where item.Value > 100
                  select item.Name;

Method Syntax:
    var results = data.AsParallel()
                      .Where(x => x.Value > 100)
                      .Select(x => x.Name);

Useful options:
    .WithDegreeOfParallelism(4)  // Limit thread count
    .WithCancellation(token)     // Support cancellation
    .AsOrdered()                 // Preserve order (slower)
    .ForAll(action)              // Execute action on each
");

// Example: Complex aggregation
Console.WriteLine("Complex aggregation: Revenue by category\n");

sw.Restart();
var categoryTotals = salesData
    .AsParallel()
    .GroupBy(s => s.Category)
    .Select(g => new { Category = g.Key, Total = g.Sum(s => s.Amount) })
    .OrderByDescending(x => x.Total)
    .ToList();
sw.Stop();

Console.WriteLine("| Category      | Total Revenue    |");
Console.WriteLine("|---------------|------------------|");
foreach (var cat in categoryTotals)
    Console.WriteLine($"| {cat.Category,-13} | ${cat.Total,14:N2} |");
Console.WriteLine($"\nCompleted in {sw.ElapsedMilliseconds}ms");

// ============================================
// PART 3: When NOT to Use PLINQ
// ============================================
Console.WriteLine("\n--- Part 3: When NOT to Use PLINQ ---\n");

Console.WriteLine(@"
PLINQ is NOT always faster!

Bad cases:
1. Small datasets - parallelization overhead dominates
2. Simple operations - not enough work per element
3. I/O bound work - threads wait, don't compute
4. Operations that need ordering - overhead to maintain order

Good cases:
1. Large datasets (10,000+ elements)
2. CPU-intensive operations per element
3. Independent operations (no shared state)
");

// Demo: Small data is slower with PLINQ
var smallData = GenerateSalesData(1000);

sw.Restart();
var smallSeq = smallData.Where(s => s.Amount > 50).Sum(s => s.Amount);
var smallSeqMs = sw.ElapsedMilliseconds;

sw.Restart();
var smallPar = smallData.AsParallel().Where(s => s.Amount > 50).Sum(s => s.Amount);
var smallParMs = sw.ElapsedMilliseconds;

Console.WriteLine($"Small dataset (1,000 items):");
Console.WriteLine($"  Sequential: {smallSeqMs}ms, Parallel: {smallParMs}ms");
Console.WriteLine($"  (Parallel overhead may make it slower!)\n");

// ============================================
// PART 4: Parallel Aggregation Patterns
// ============================================
Console.WriteLine("--- Part 4: Parallel Aggregation Patterns ---\n");

Console.WriteLine(@"
Map-Reduce Pattern:
1. MAP: Transform each element independently (parallel)
2. REDUCE: Combine results into final answer (may be sequential)

PLINQ handles this automatically, but understanding it helps!
");

// Custom aggregation with Aggregate
sw.Restart();
var stats = salesData
    .AsParallel()
    .Aggregate(
        // Seed: initial value for each partition
        () => new SalesStats(),
        // Accumulator: update stats with each sale (per partition)
        (stats, sale) => stats.Add(sale),
        // Combiner: merge partition results
        (stats1, stats2) => stats1.Merge(stats2),
        // Finalizer: compute final result
        stats => stats
    );
sw.Stop();

Console.WriteLine($"Parallel Statistics (computed in {sw.ElapsedMilliseconds}ms):");
Console.WriteLine($"  Count: {stats.Count:N0}");
Console.WriteLine($"  Total: ${stats.Total:N2}");
Console.WriteLine($"  Average: ${stats.Average:N2}");
Console.WriteLine($"  Min: ${stats.Min:N2}");
Console.WriteLine($"  Max: ${stats.Max:N2}");

// ============================================
// PART 5: YOUR TASK
// ============================================
Console.WriteLine("\n--- Part 5: YOUR TASK ---\n");

Console.WriteLine(@"
Analyze the sales data using PLINQ:

Task 1: Top Products
--------------------
Find the top 10 products by total revenue.
Hint: GroupBy ProductId, Sum amounts, OrderByDescending, Take 10

Task 2: Daily Trends
--------------------
Calculate total sales per day, find the busiest day.
Hint: GroupBy Date property

Task 3: Customer Analysis
-------------------------
Find customers who spent more than $1000 total.
Count how many such 'VIP' customers exist.

Task 4: Performance Comparison
------------------------------
For each task, compare sequential vs parallel timing.
When is parallel faster? By how much?

Complete the methods below and print your findings!
");

// TODO: Implement these analyses
Console.WriteLine("Your implementations here...\n");

// Task 1: Top Products (TODO)
// var topProducts = salesData.AsParallel()...

// Task 2: Daily Trends (TODO)
// var dailyTrends = salesData.AsParallel()...

// Task 3: VIP Customers (TODO)
// var vipCustomers = salesData.AsParallel()...

// ============================================
// PART 6: Common Pitfalls
// ============================================
Console.WriteLine("--- Part 6: Common Pitfalls ---\n");

Console.WriteLine(@"
Q6.1: What's wrong with this code?

    int count = 0;
    data.AsParallel().ForAll(x => { if (x > 100) count++; });  // BUG!

Problem? _______________________________________________________________
Fix?     _______________________________________________________________

Q6.2: What's wrong with this code?

    var results = new List<int>();
    data.AsParallel().ForAll(x => { results.Add(x * 2); });  // BUG!

Problem? _______________________________________________________________
Fix?     _______________________________________________________________

Additional Questions:

1. Why does .AsOrdered() reduce parallel performance?

   _______________________________________________________________

2. What's the difference between ForAll() and ToList().ForEach()?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• PLINQ makes parallel data processing simple - just add .AsParallel()
• Great for CPU-bound operations on large datasets
• Not always faster - overhead can dominate for small/simple tasks
• Map-Reduce pattern: transform in parallel, combine results
• Use WithDegreeOfParallelism() to control resource usage
• Aggregate() allows custom parallel reductions
");

// ============================================
// Helper Classes and Methods
// ============================================

List<Sale> GenerateSalesData(int count)
{
    var categories = new[] { "Electronics", "Clothing", "Food", "Books", "Home" };
    var products = Enumerable.Range(1, 100).Select(i => $"Product{i}").ToArray();
    var customers = Enumerable.Range(1, 10000).Select(i => $"Customer{i}").ToArray();
    var baseDate = new DateTime(2024, 1, 1);

    // Fixed seed for reproducible results - students can compare their outputs
    var random = new Random(42);

    return Enumerable.Range(0, count)
        .Select(_ => new Sale
        {
            Category = categories[random.Next(categories.Length)],
            ProductId = products[random.Next(products.Length)],
            CustomerId = customers[random.Next(customers.Length)],
            Amount = Math.Round((decimal)(random.NextDouble() * 200), 2),
            Date = baseDate.AddDays(random.Next(365))
        })
        .ToList();
}

public record Sale
{
    public required string Category { get; init; }
    public required string ProductId { get; init; }
    public required string CustomerId { get; init; }
    public decimal Amount { get; init; }
    public DateTime Date { get; init; }
}

public class SalesStats
{
    public int Count { get; private set; }
    public decimal Total { get; private set; }
    public decimal Min { get; private set; } = decimal.MaxValue;
    public decimal Max { get; private set; } = decimal.MinValue;
    public decimal Average => Count > 0 ? Total / Count : 0;

    public SalesStats Add(Sale sale)
    {
        Count++;
        Total += sale.Amount;
        if (sale.Amount < Min) Min = sale.Amount;
        if (sale.Amount > Max) Max = sale.Amount;
        return this;
    }

    public SalesStats Merge(SalesStats other)
    {
        Count += other.Count;
        Total += other.Total;
        if (other.Min < Min) Min = other.Min;
        if (other.Max > Max) Max = other.Max;
        return this;
    }
}
