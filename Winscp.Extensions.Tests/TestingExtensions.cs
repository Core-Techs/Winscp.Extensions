using System;
using System.Collections.Generic;

namespace Winscp.Extensions.Tests
{
    public static class TestingExtensions
    {
        public static void Dispose(this IEnumerable<IDisposable> disposables)
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}