
namespace SteveLang.TplVsAsyncAwait.Model
{
    public struct CalculationResultAndThreadId
    {
        public CalculationResultAndThreadId(ulong result, int threadId) : this()
        {
            Result = result;
            ThreadId = threadId;
        }

        public ulong Result { get; set; }
        public int ThreadId { get; set; }
    }
}
