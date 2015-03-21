using System.Threading;
using System.Threading.Tasks;
using SteveLang.TplVsAsyncAwait.Model;
using System;

namespace SteveLang.TplVsAsyncAwait.Domain
{
    public class Calculator
    {
        public CalculationResultAndThreadId Increment(
            int from,
            int to,
            int numberOfTimesToRepeat,
            int divisor,
            Action onCancel = null)
        {
            // The following is to allow a DivideByZero exception to be triggered
            var temp = 1 / divisor;

            ulong result = 0;

            for (var i = 0; i < numberOfTimesToRepeat; i++)
            {
                for (var j = from; j <= to; j++)
                {
                    if (onCancel != null) onCancel();
                    result++;
                }
            }

            var threadId = Thread.CurrentThread.ManagedThreadId;
            return new CalculationResultAndThreadId(result, threadId);
        }

        public Task<CalculationResultAndThreadId> IncrementAsync(
            int from, int to, int numberOfTimesToRepeat, int divisor, CancellationToken ct)
        {
            return Task.Run(
                () => Increment(from, to, numberOfTimesToRepeat, divisor, ct.ThrowIfCancellationRequested),
                ct);
        }
    }
}
