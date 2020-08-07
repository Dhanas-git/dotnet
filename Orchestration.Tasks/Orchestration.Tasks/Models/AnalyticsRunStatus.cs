
namespace Orchestration.Tasks.Models 
{
    public enum AnalyticsRunStatus : int
    {
        // Summary:
        // The task has been initialized but has not yet been scheduled.
        //Created = 0,

        // Summary:
        // The task is waiting to be activated and scheduled internally by the .NET
        // Framework infrastructure.
        //WaitingForActivation = 1,

        // Summary:
        // The task has been scheduled for execution but has not yet begun executing.
        //WaitingToRun = 2,

        // Summary:
        // The task is running but has not yet completed.
        Running = 3,

        // Summary:
        // The task has finished executing and is implicitly waiting for attached child
        // tasks to complete.
        //WaitingForChildrenToComplete = 4, not used in orchestration

        // Summary:
        // The task completed execution successfully.
        RanToCompletion = 5,

        // Summary:
        //  The task acknowledged cancellation by throwing an OperationCanceledException
        //  with its own CancellationToken while the token was in signaled state, or
        //  the task's CancellationToken was already signaled before the task started
        //  executing. For more information, see Task Cancellation.
        Canceled = 6,

        // Summary:
        // The task completed due to an unhandled exception.
        //Faulted = 8, -- do not use this for orchestration -- we need intermediate failures

        // Summary:
        // The task with Cancel Pending
        //CancelPending = 9, 

        // Summary:
        // The task with an intermediate failure.
        RunningNeedsAttention = 10

    }
}
