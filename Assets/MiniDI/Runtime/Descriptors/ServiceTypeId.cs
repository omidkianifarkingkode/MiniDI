using System.Threading;

namespace MiniDI
{
    internal static class ServiceTypeId
    {
        private static int _nextId = -1;
        public static int Next() => Interlocked.Increment(ref _nextId);
    }
}