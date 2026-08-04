// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Extensions.Msal.Accessors
{
    internal static class FileWithPermissions
    {
        #region Unix specific

        /// <summary>
        /// Calls open(2) with caller-supplied flags and mode. Used instead of creat(2) so that
        /// O_NOFOLLOW can be included to atomically reject symlinks at the kernel level.
        /// See https://man7.org/linux/man-pages/man2/open.2.html
        /// </summary>
        [DllImport("libc", EntryPoint = "open", SetLastError = true)]
        private static extern int PosixOpen([MarshalAs(UnmanagedType.LPStr)] string pathname, int flags, int mode);

        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int PosixChmod([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);

        // open(2) flags — values differ between Linux and macOS.
        // Linux:  O_WRONLY=0x1, O_CREAT=0x40,  O_TRUNC=0x200,  O_NOFOLLOW=0x20000
        // macOS:  O_WRONLY=0x1, O_CREAT=0x200, O_TRUNC=0x400,  O_NOFOLLOW=0x100
        private static readonly int s_openFlags = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? 0x1 | 0x200 | 0x400 | 0x100   // macOS
            : 0x1 | 0x40  | 0x200 | 0x20000; // Linux

        // ELOOP errno — kernel returns this when O_NOFOLLOW encounters a symlink.
        // Linux: 40  macOS: 62
        private static readonly int s_eloop = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 62 : 40;

        #endregion

        /// <summary>
        /// Creates a new file with "600" permissions (i.e. read / write only by the owner) and writes some data to it.
        /// On Windows, file security is more complex, but an equivalent is achieved.
        /// </summary>
        /// <remarks>
        /// This logic will not work on Mono, see https://github.com/NuGet/NuGet.Client/commit/d62db666c710bf95121fe8f5c6a6cbe01985456f
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static void WriteToNewFileWithOwnerRWPermissions(string path, byte[] data)
        {

            if (SharedUtilities.IsWindowsPlatform())
            {
                WriteToNewFileWithOwnerRWPermissionsWindows(path, data);
            }
            else if (SharedUtilities.IsMacPlatform() || SharedUtilities.IsLinuxPlatform())
            {
                WriteToNewFileWithOwnerRWPermissionsUnix(path, data);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Based on https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core and on 
        /// https://github.com/NuGet/NuGet.Client/commit/d62db666c710bf95121fe8f5c6a6cbe01985456f
        /// </summary>
        /// <remarks>
        /// <paramref name="path"/> must not be a symbolic link. Two layers enforce this:
        /// <list type="number">
        ///   <item>The caller (<see cref="FileIOWithRetries.CreateAndWriteToFile"/> with
        ///   <c>setChmod600: true</c>) runs a pre-check via <c>lstat(2)</c> before entering the
        ///   retry loop, throwing <see cref="InvalidOperationException"/> with a clear message for
        ///   the non-adversarial case.</item>
        ///   <item><c>open(2)</c> is called with <c>O_NOFOLLOW</c> so the kernel atomically rejects
        ///   a symlink that appears in the race window between the pre-check and the write,
        ///   returning <c>ELOOP</c> which is also surfaced as <see cref="InvalidOperationException"/>.</item>
        /// </list>
        /// </remarks>
        private static void WriteToNewFileWithOwnerRWPermissionsUnix(string path, byte[] data)
        {
            int _0600 = Convert.ToInt32("600", 8);

            int fileDescriptor = PosixOpen(path, s_openFlags, _0600);

            if (fileDescriptor == -1)
            {
                int errno = Marshal.GetLastWin32Error();

                if (errno == s_eloop)
                {
                    throw new InvalidOperationException(
                        $"The cache file path '{path}' is a symbolic link. MSAL cache paths must not be symbolic links.");
                }

                // For any other error fall back to File.Create which will throw a meaningful exception.
                using (File.Create(path))
                {
                    // File.Create() should have thrown an exception with an appropriate error message
                }
                File.Delete(path);
                throw new InvalidOperationException($"libc open() failed with last error code {errno}, but File.Create did not");
            }

            var safeFileHandle = new SafeFileHandle((IntPtr)fileDescriptor, ownsHandle: true);
            using (var fileStream = new FileStream(safeFileHandle, FileAccess.ReadWrite))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Windows has a more complex file security system. "600" mode, i.e. read/write for owner translates to this in Windows.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        private static void WriteToNewFileWithOwnerRWPermissionsWindows(string filePath, byte[] data)
        {
            FileSecurity security = new FileSecurity();
            var rights = FileSystemRights.Read | FileSystemRights.Write;

            // https://stackoverflow.com/questions/39480255/c-sharp-how-to-grant-access-only-to-current-user-and-restrict-access-to-others
            security.AddAccessRule(
                new FileSystemAccessRule(
                        WindowsIdentity.GetCurrent().Name,
                        rights,
                        InheritanceFlags.None,
                        PropagationFlags.NoPropagateInherit,
                        AccessControlType.Allow));

            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            FileStream fs = null;

            try
            {
#if NET45_OR_GREATER
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                fs = File.Create(filePath, data.Length, FileOptions.None, security);
#else
                FileInfo info = new FileInfo(filePath);
                fs = info.Create(FileMode.Create, rights, FileShare.Read, data.Length, FileOptions.None, security);
#endif

                fs.Write(data, 0, data.Length);
            }
            finally
            {
                fs?.Dispose();
            }
        }

    }
}
