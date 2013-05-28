using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTX.WinscpExtensions;
using NUnit.Framework;
using WinSCP;

namespace Winscp.Extensions.Tests
{
    public class UploadTests
    {
        [Test]
        public void TestDirSplit()
        {
            const string path = "\\a/b\\c/d\\";
            var parts = WinscpExtensions.SplitPath(path);
            Assert.True(parts.SequenceEqual(new[] { "a", "b", "c", "d" }));
        }

        [Test]
        public void TestDirSplit2()
        {
            const string path = "a";
            var parts = WinscpExtensions.SplitPath(path);
            Assert.That(parts.SequenceEqual(new[] { "a" }), Is.True);
        }

        [Test]
        public void CanEnsureDirectoryStructure()
        {
            using (var session = OpenSession())
            {
                session.EnsureRemoteDirectoryStructure("a/b/c");
                Assert.True(session.FileExists("/a/b/c/"));
            }
        }

        [Test]
        public void CanUploadFiles()
        {
            using (var session = OpenSession())
            {
                var localFiles = DummyFile.Generate().Take(3).ToArray();

                foreach (var localFile in localFiles)
                {
                    session.UploadFile(localFile.FileInfo);
                    Assert.True(session.FileExists(localFile.FileInfo.Name));
                }
                localFiles.Dispose();
            }
        }

        [Test]
        public void CanUploadFilesDifferentNames()
        {
            using (var session = OpenSession())
            {
                var localFiles = DummyFile.Generate().Take(1).ToArray();

                foreach (var localFile in localFiles)
                {
                    var remoteFileName = "goober.dat";
                    session.UploadFile(localFile.FileInfo, remoteFileName);
                    Assert.True(session.FileExists(remoteFileName));
                }
                localFiles.Dispose();
            }
        }

        [Test]
        public void CanUploadFilesIntoSubDirectory()
        {

            using (var session = OpenSession())
            {
                var localFiles = DummyFile.Generate().Take(3).ToArray();

                foreach (var localFile in localFiles)
                {
                    session.UploadFile(localFile.FileInfo, remoteDirectoryPath: "desktop");
                    Assert.True(session.FileExists("/desktop/" + localFile.FileInfo.Name));
                }

                localFiles.Dispose();
            }
        }

        [Test]
        public void CanUploadFilesIntoSubDirectoryWithAnyPrePostSlashes()
        {

            using (var session = OpenSession())
            {
                var localFiles = DummyFile.Generate().Take(3).ToArray();

                foreach (var localFile in localFiles)
                {
                    session.UploadFile(localFile.FileInfo, remoteDirectoryPath: "/desktop\\");
                    Assert.True(session.FileExists(string.Format("/desktop/{0}", localFile.FileInfo.Name)));
                }

                localFiles.Dispose();
            }
        }

        [Test]
        public void CanUploadFilesIntoDeepSubDirectories()
        {
            using (var session = OpenSession())
            {
                var localFiles = DummyFile.Generate().Take(3).ToArray();

                foreach (var localFile in localFiles)
                {
                    session.UploadFile(localFile.FileInfo, remoteDirectoryPath: "cheech/chong/marvin/gaye/");
                    Assert.True(session.FileExists(string.Format("/cheech/chong/marvin/gaye/{0}", localFile.FileInfo.Name)));
                }
                localFiles.Dispose();
            }
        }

        [Test]
        public void CanAbortUploadWithCancellationToken()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                Task.Run(() =>
                     {
                         using (var session = OpenSession())
                         using (var localFile = new DummyFile(52428800 /* 50mb */))
                         {
                             try
                             {
                                 session.UploadFile(localFile.FileInfo, cancellationToken: cts.Token);
                             }
                             catch (AggregateException ex)
                             {
                                 ex.Handle(x => x is OperationCanceledException);
                             }
                         }
                     }, cts.Token).Wait();
            }
            catch (AggregateException ex)
            {
                ex.Flatten().Handle(x => x is OperationCanceledException);
            }
        }

        [Test]
        [ExpectedException(typeof (SessionLocalException))]
        public void CallingAbortOnUploadThrows()
        {
            using (var session = OpenSession())
            {
                Task.Delay(TimeSpan.FromSeconds(5))
                    .ContinueWith(t => session.Abort());

                using (var localFile = new DummyFile(52428800 /* 50mb */))
                    session.UploadFile(localFile.FileInfo);
            }
        }

        private static Session OpenSession()
        {
            return new Session().Open("sftp");
        }
    }
}

