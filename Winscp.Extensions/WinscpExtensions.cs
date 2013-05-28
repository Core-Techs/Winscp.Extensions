using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using WinSCP;
using System.Linq;

namespace CTX.WinscpExtensions
{
    public static class WinscpExtensions
    {
        static WinscpExtensions()
        {
            if (AppSettings.AutoCreateExe)
                EnsureExecutableExists();
        }

        public static void EnsureExecutableExists()
        {
            const string exeName = "WinSCP.exe";
            var exeInfo = new FileInfo(exeName);
            if (!exeInfo.Exists || !DllAndExeVersionsMatch(exeInfo))
                ExtractWinscpExe(new FileInfo(exeName));
        }

        private static bool DllAndExeVersionsMatch(FileInfo exeInfo)
        {
            var exeVersion = FileVersionInfo.GetVersionInfo(exeInfo.FullName).ProductVersion;
            var dllVersion = FileVersionInfo.GetVersionInfo(typeof(Session).Assembly.Location).ProductVersion;
            return exeVersion == dllVersion;
        }

        private static void ExtractWinscpExe(FileInfo destination)
        {
            using (var read = new MemoryStream(Resources.WinSCP_exe_gz))
            using (var write = destination.OpenWrite())
            using (var gz = new GZipStream(read, CompressionMode.Decompress))
                gz.CopyTo(write);
        }

        /// <summary>
        /// Creates and opens a Winscp session based on a connection string.
        /// </summary>
        /// <param name="connectionStringOrName">The connection string or the name of a connection string.</param>
        public static Session Open(this Session session, string connectionStringOrName)
        {
            var csi = new ConnStringInfo(connectionStringOrName);

            if (string.IsNullOrWhiteSpace(session.ExecutablePath))
            {
                var exe = csi["exe"] ?? csi["exepath"];
                if (!string.IsNullOrWhiteSpace(exe))
                    session.ExecutablePath = exe;
            }

            var o = new SessionOptions
            {
                HostName = csi["host"] ?? csi["hostname"],
                UserName = csi["username"] ?? csi["user"],
                Password = csi["password"] ?? csi["pw"],
                SshHostKeyFingerprint = csi["hostkey"] ?? csi["HostKeyFingerprint"],
            };

            var port = csi["port"] ?? csi["portnumber"];
            if (!string.IsNullOrWhiteSpace(port))
                o.PortNumber = int.Parse(port);

            session.Open(o);
            return session;
        }

        /// <summary>
        /// Uploads a file to the remote server.
        /// Improves upon Session.PutFiles by ensuring all directories in the remoteDirectoryPath exist,
        /// uses correct remote path delimiters, providing a method interface that is more  explicit, 
        /// and automatically checking for transfer errors. 
        /// </summary>
        /// <param name="session">An open Winscp session.</param>
        /// <param name="localFilePath">The local file to upload.</param>
        /// <param name="remoteFileName">The new name of the file on the remote server. Defaults to the local filename.</param>
        /// <param name="remoteDirectoryPath">The remote directory path to place the uploaded file in. Will be automatically created if it doesn't exist.</param>
        /// <param name="transferOptions">Winscp specific transfer options.</param>
        public static TransferOperationResult UploadFile(this Session session, string localFilePath,
                                                         string remoteFileName = null, string remoteDirectoryPath = null,
                                                         TransferOptions transferOptions = null,
            bool ensureRemoteDirectoryStructureExists = true,
                                                         CancellationToken cancellationToken =
                                                             default(CancellationToken))
        {
            return session.UploadFile(new FileInfo(localFilePath), remoteFileName, remoteDirectoryPath, transferOptions,
                                      ensureRemoteDirectoryStructureExists, cancellationToken);
        }

        /// <summary>
        /// Uploads a file to the remote server.
        /// Improves upon Session.PutFiles by ensuring all directories in the remoteDirectoryPath exist,
        /// uses correct remote path delimiters, providing a method interface that is more  explicit, 
        /// and automatically checking for transfer errors. 
        /// </summary>
        /// <param name="session">An open Winscp session.</param>
        /// <param name="localFile">The local file to upload.</param>
        /// <param name="remoteFileName">The new name of the file on the remote server. Defaults to the local filename.</param>
        /// <param name="remoteDirectoryPath">The remote directory path to place the uploaded file in. Will be automatically created if it doesn't exist.</param>
        /// <param name="transferOptions">Winscp specific transfer options.</param>
        public static TransferOperationResult UploadFile(this Session session, FileInfo localFile,
                                                         string remoteFileName = null, string remoteDirectoryPath = null,
                                                         TransferOptions transferOptions = null, bool ensureRemoteDirectoryStructureExists = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            var remoteDirs = new string[0];
            if (!string.IsNullOrWhiteSpace(remoteDirectoryPath))
            {
                remoteDirs = SplitPath(remoteDirectoryPath);

                if (ensureRemoteDirectoryStructureExists)
                    EnsureRemoteDirectoryStructureInternal(session, remoteDirs, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(remoteFileName))
                remoteFileName = localFile.Name;

            var remotePath = remoteDirs.Length < 1
                                 ? remoteFileName
                                 : "/" + string.Join("/", remoteDirs) + "/" + remoteFileName;

            return session.DoWork(s =>
                {
                    var result = s.PutFiles(localFile.FullName, remotePath, options: transferOptions);
                    result.Check();
                    return result;
                }, cancellationToken);
        }

        /// <summary>
        /// Creates all of the remote directories in the remotePath if they do not already exist.
        /// </summary>
        public static void EnsureRemoteDirectoryStructure(this Session session, string remotePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var dirs = SplitPath(remotePath);
            EnsureRemoteDirectoryStructureInternal(session, dirs, cancellationToken);
        }

        private static void EnsureRemoteDirectoryStructureInternal(Session session, ICollection<string> dirs, CancellationToken cancellationToken = default(CancellationToken))
        {
            session.DoWork(s =>
                {
                    for (var i = 0; i < dirs.Count; i++)
                    {
                        var dir = string.Join("/", dirs.Take(i + 1).ToArray());
                        if (!s.FileExists(dir))
                            s.CreateDirectory(dir);
                    }
                }, cancellationToken);
        }

        internal static string[] SplitPath(string remotePath)
        {
            var dirs = new Stack<string>();
            while (!string.IsNullOrWhiteSpace(remotePath))
            {
                dirs.Push(Path.GetFileName(remotePath));
                remotePath = Path.GetDirectoryName(remotePath);
            }

            return dirs.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }

        /// <summary>
        /// Invokes a delegate that performs some work with the session and returns a result.
        /// The session will be aborted if cancellation is requested.
        /// </summary>
        /// <param name="session">The Winscp session.</param>
        /// <param name="work">The delgate that performs the work.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        public static T DoWork<T>(this Session session, Func<Session, T> work, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(session.Abort))
            {
                try
                {
                    return work(session);
                }
                catch (SessionException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        /// <summary>
        /// Invokes a delegate that performs some work with the session.
        /// The session will be aborted if cancellation is requested.
        /// </summary>
        /// <param name="session">The Winscp session.</param>
        /// <param name="work">The delgate that performs the work.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        public static void DoWork(this Session session, Action<Session> work, CancellationToken cancellationToken)
        {
            session.DoWork(s =>
                {
                    work(s);
                    return (object)null;
                }, cancellationToken);
        }
    }
}