using System;
using System.Collections.Generic;
using System.IO;

namespace Winscp.Extensions.Tests
{
    public class DummyFile : IDisposable
    {
        public readonly FileInfo FileInfo;

        public DummyFile(int? sizeInBytes = null)
        {
            if (sizeInBytes == null)
                sizeInBytes = RandomHelper.Instance.Next(1024, 1048577);

            FileInfo = new FileInfo(Path.GetTempFileName());

            var bytes = new byte[sizeInBytes.Value];
            RandomHelper.Instance.NextBytes(bytes);

            using (var w = FileInfo.OpenWrite())
                w.Write(bytes, 0, bytes.Length);
        }

        public static IEnumerable<DummyFile> Generate(int? sizeInBytes = null)
        {
            while (true)
                yield return new DummyFile(sizeInBytes);
        }

        public void Dispose()
        {
            FileInfo.Delete();
        }
    }
}