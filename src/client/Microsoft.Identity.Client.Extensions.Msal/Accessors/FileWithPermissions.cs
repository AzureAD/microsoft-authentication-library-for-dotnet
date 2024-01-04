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
        /// Equivalent to calling open() with flags  O_CREAT|O_WRONLY|O_TRUNC. O_TRUNC will truncate the file. 
        /// See https://man7.org/linux/man-pages/man2/open.2.html
        /// </summary>
        [DllImport("libc", EntryPoint = "creat", SetLastError = true)]
        private static extern int PosixCreate([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);

        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int PosixChmod([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);

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
        private static void WriteToNewFileWithOwnerRWPermissionsUnix(string path, byte[] data)
        {
            int _0600 = Convert.ToInt32("600", 8);

            int fileDescriptor =  PosixCreate(path, _0600);

            // if creat() fails, then try to use File.Create because it will throw a meaningful exception.
            if (fileDescriptor == -1)
            {
                int posixCreateError = Marshal.GetLastWin32Error();
                using (File.Create(path))
                {
                    // File.Create() should have thrown an exception with an appropriate error message
                }
                File.Delete(path);
                throw new InvalidOperationException($"libc creat() failed with last error code {posixCreateError}, but File.Create did not");
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
