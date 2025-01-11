# Explanation of Each Scenario

## 1. Scenario1_DeadlockUsingTaskResult

Demonstrates a potential deadlock scenario where you call Task.Run(...).Result.
In many UI frameworks (WinForms, WPF) or ASP.NET, this will deadlock because the async method is trying to resume on the main thread (or the synchronization context), which is blocked by .Result.

## 2. Scenario2_ThreadPoolStarvation

Shows how blocking tasks (i.e. using .Result in many queued tasks) can exhaust or starve the .NET Thread Pool, causing tasks to stall.

## 3. Scenario3_DeadlockWithLock

A classic deadlock scenario using two locks (lockA and lockB).
Each task acquires one lock and then waits for the other. If both locks are acquired in opposing order, a deadlock occurs.

## 4. Scenario4_RaceCondition

Demonstrates a race condition by incrementing a shared counter without synchronization (sharedCounter++).
The final value is typically less than expected because increments can be lost when multiple threads interleave the read–modify–write cycle.

## 5. Scenario5_AsyncDeadlockInSynchronousContext

Similar to Scenario 1, but in a slightly different pattern that mimics real-world usage (calling an async method and then immediately doing .Result).
In a UI or web environment, the synchronization context tries to marshal the continuation back to the blocked thread, resulting in deadlock.
