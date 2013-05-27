using System;
using System.Threading;

namespace Winscp.Extensions.Tests
{
    /// <thanks to="Jon Skeet">http://stackoverflow.com/a/1785821/64334</thanks>
    public static class RandomHelper
    {
        private static int _seedCounter = new Random().Next();

        [ThreadStatic]
        private static Random _rng;

        public static Random Instance
        {
            get { return _rng ?? (_rng = new Random(Interlocked.Increment(ref _seedCounter))); }
        }
    }
}