using System.IO;
using CTX.WinscpExtensions;
using NUnit.Framework;
using WinSCP;

namespace Winscp.Extensions.Tests
{
    public class SessionTests
    {
        [Test]
        public void TestConnStringInfo()
        {
            const string connString = @"host=localhost;user=bob;pw=goatman;anything=everything";
            var info = new ConnStringInfo(connString);

            Assert.AreEqual("localhost", info["host"]);
            Assert.AreEqual("bob", info["user"]);
            Assert.AreEqual("goatman", info["pw"]);
            Assert.AreEqual("everything", info["anything"]);
        }

        [Test]
        public void TestSessionOpen()
        {
            using (var s = new Session())
                s.Open("sftp"); // uses conn string called sftp in app.config
        }

        [Test]
        public void TestExecutableExtraction()
        {
            var exe = new FileInfo("winscp.exe");
            if (exe.Exists)
                exe.Delete();

            WinscpExtensions.EnsureExecutableExists();

            exe.Refresh();
            Assert.True(exe.Exists);
        }
    }
}