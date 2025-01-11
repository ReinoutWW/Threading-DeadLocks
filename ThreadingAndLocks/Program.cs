// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Scenario1_DeadlockUsingTaskResult();
Scenario2_ThreadPoolStarvation();
Scenario3_DeadlockWithLock();
Scenario4_RaceCondition();
Scenario5_AsyncDeadlockInSynchronousContext();




/// <summary>
/// SCENARIO 1:
/// A common deadlock scenario when using Task.Run().Result in the same thread context.
/// - We start a task that tries to return an async result synchronously,
///   causing a deadlock if the async code awaits something that needs the same thread context.
/// - In console apps, this sometimes won't deadlock, but in UI or ASP.NET contexts, it often will.
///   We simulate the scenario for demonstration.
/// </summary>
static void Scenario1_DeadlockUsingTaskResult()
{
    Console.WriteLine("SCENARIO 1: Deadlock with Task.Run + .Result");

    // This will sometimes hang in a UI or ASP.NET context because the async call
    // is blocking a thread that is required to complete the async operation.
    // In a plain console app, it may *not* deadlock, but it's still a bad practice.
    Console.WriteLine("Starting...");
    try
    {
        var result = Task.Run(() => AsyncMethod()).Result;  // Potential deadlock scenario
        Console.WriteLine("Result: " + result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception occurred: {ex.Message}");
    }
}

static async Task<string> AsyncMethod()
{
    await Task.Delay(1000);  // Simulate some asynchronous work
    return "Hello from async!";
}

/// <summary>
/// SCENARIO 2:
/// Demonstrates thread pool starvation or blocking threads.
/// - We queue up a bunch of tasks that block on .Result or .Wait,
///   causing the thread pool to get stuck waiting.
/// </summary>
static void Scenario2_ThreadPoolStarvation()
{
    Console.WriteLine("SCENARIO 2: ThreadPool Starvation / Blocking Tasks");

    // We will create many tasks that block on .Result.
    // This can lead to thread pool exhaustion if the tasks
    // never free up threads for each other.
    for (int i = 0; i < 20; i++)
    {
        Task.Run(() =>
        {
            // Each of these tasks blocks on another async call.
            // If the thread pool doesn't have enough threads, or if they get stuck,
            // tasks can be starved or delayed significantly.
            Console.WriteLine($"Starting blocking task {Task.CurrentId}");
            var data = GetDataAsync().Result;  // blocking call
            Console.WriteLine($"Task {Task.CurrentId} got result: {data}");
        });
    }

    // Some breathing room for the tasks to start
    Thread.Sleep(5000);
    Console.WriteLine("Finished spawning tasks. Check if the tasks completed or got stuck.");
}

static async Task<int> GetDataAsync()
{
    await Task.Delay(3000); // Simulate I/O or network call
    return Thread.CurrentThread.ManagedThreadId; // Return thread ID
}

/// <summary>
/// SCENARIO 3:
/// Demonstrate a classic deadlock scenario using 'lock'.
/// - Two resources lock each other in opposite order.
///   If both tasks acquire the first lock before requesting the second, they block forever.
/// </summary>
static void Scenario3_DeadlockWithLock()
{
    Console.WriteLine("SCENARIO 3: Classic lock-based deadlock");

    object lockA = new object();
    object lockB = new object();

    var task1 = Task.Run(() =>
    {
        lock (lockA)
        {
            Console.WriteLine("Task1 locked A");
            // Simulate some work
            Thread.Sleep(500);

            // Now tries to lock B
            Console.WriteLine("Task1 waiting for B...");
            lock (lockB)
            {
                Console.WriteLine("Task1 locked B");
            }
        }
    });

    var task2 = Task.Run(() =>
    {
        lock (lockB)
        {
            Console.WriteLine("Task2 locked B");
            // Simulate some work
            Thread.Sleep(500);

            // Now tries to lock A
            Console.WriteLine("Task2 waiting for A...");
            lock (lockA)
            {
                Console.WriteLine("Task2 locked A");
            }
        }
    });

    // Both tasks will run and get stuck if they acquire the locks in the opposite order.
    // In many cases, you'll see the output stop after "Task1 locked A" and "Task2 locked B",
    // indicating a deadlock.
    Task.WaitAll(task1, task2);
    Console.WriteLine("If you see this line, somehow the deadlock was avoided (unlikely).");
}

/// <summary>
/// SCENARIO 4:
/// Demonstrate a race condition by sharing a variable across multiple threads.
/// - We increment a shared counter from multiple threads without proper synchronization.
///   The final count won't match the expected value.
/// </summary>
static void Scenario4_RaceCondition()
{
    Console.WriteLine("SCENARIO 4: Race condition on shared counter");

    int sharedCounter = 0;
    int incrementCount = 100000;
    int taskCount = 10;

    Task[] tasks = new Task[taskCount];
    for (int i = 0; i < taskCount; i++)
    {
        tasks[i] = Task.Run(() =>
        {
            for (int j = 0; j < incrementCount; j++)
            {
                // Not thread safe!
                // This can cause missed increments and corrupted data
                sharedCounter++;
            }
        });
    }

    Task.WaitAll(tasks);
    // The expected value is taskCount * incrementCount = 1,000,000
    Console.WriteLine($"Expected counter: {taskCount * incrementCount}, Actual: {sharedCounter}");
}

/// <summary>
/// SCENARIO 5:
/// Demonstrate an async deadlock in a synchronous context (similar to scenario 1),
/// but in a more "realistic" usage pattern where an async method calls await on itself.
/// In a UI or ASP.NET context, calling .Result or .Wait can cause a deadlock.
/// </summary>
static void Scenario5_AsyncDeadlockInSynchronousContext()
{
    Console.WriteLine("SCENARIO 5: Async deadlock in synchronous context");

    try
    {
        // This method calls an async method, then blocks on .Result.
        var result = CallAsyncMethodAndBlock();
        Console.WriteLine($"Result: {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
    }
}

static string CallAsyncMethodAndBlock()
{
    // Potentially blocking: we do an async call, then do .Result
    return SomeAsyncMethod().Result;
}

static async Task<string> SomeAsyncMethod()
{
    await Task.Delay(1000);
    return "Hello from SomeAsyncMethod!";
}