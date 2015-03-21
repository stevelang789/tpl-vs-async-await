# Exploration of TPL vs async/await

This sample WPF application performs a long-running calculation in the background whilst allowing the UI to still be responsive.

To launch the solution from Visual Studio, set WpfApp as the startup project.

## Background

I've been using the Task Parallel Library (TPL) for some time - essentially `Task.Factory.StartNew()` and `.ContinueWith()` - but I've never had the opportunity to use async/await. I wrote this sample app as a means to have a side-by-side comparison between TPL and async/await.

## Observations

The program flow is as follows: when the user clicks a button, run a calculation in a background thread, and update the UI once done.

Using TPL, the code to update the UI is in `.ContinueWith()`, which runs in a new thread. Exception handling in `.ContinueWith()` is rather contrived as `task.Exception` is of type AggregateException, and needs to be flattened first.

Using async/await, the code to update the UI is right after the `await` - no need for delegates or anonymous methods. It's a huge boon to readability. The code after the await doesn't run in a new thread.

## Key Facts

1. The await keyword serves as a "bookmark": don't wait for this to complete; go back where you came from, and I'll signal you once this is done so that you can continue where you left off. Somewhat like the `yield` keyword.
2. The signature and the return type of an async method are not the same! For example, if the signature is `async Task<int> CalculateAsync()`, the return statement would return an int. If there is no return statement, the signature is `async Task DoSomething()`.
3. To handle cancellations, instead of checking `task.IsCanceled`, catch `OperationCanceledException`.

## Conclusion

Async/await doesn't actually replace `Task.Factory.StartNew()` - for compute-bound operations, `Task.Factory.StartNew()` (now shortened to `Task.Run()`) is still the way to go. Async/await is a mechanism to manage callbacks and promises, with big improvements in readability and exception handling. Nifty!
