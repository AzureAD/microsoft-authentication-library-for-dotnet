// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommonCache.Test.Common
{
    public static class CacheFileUtils
    {
        public static byte[] ReadFromFileIfExists(string path)
        {
            byte[] fileBytes = !string.IsNullOrEmpty(path) && File.Exists(path) ? File.ReadAllBytes(path) : null;
            //byte[] unprotectedBytes = protectedBytes != null
            //                              ? ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser)
            //                              : null;
            //return unprotectedBytes;

            if (fileBytes != null)
            {
                  Console.WriteLine("Reading cache from {0}", path);
            //    Console.WriteLine("< ----");
            //    Console.WriteLine(Encoding.UTF8.GetString(fileBytes));
            //    Console.WriteLine("---- >");
            //    Console.WriteLine();
            }

            return fileBytes;
        }

        public static void WriteToFileIfNotNull(string path, byte[] blob)
        {
            if (blob != null)
            {
                //byte[] protectedBytes = ProtectedData.Protect(blob, null, DataProtectionScope.CurrentUser);
                //File.WriteAllBytes(path, protectedBytes);

                Console.WriteLine("Writing cache to {0}", path);
                //Console.WriteLine("< ----");
                //Console.WriteLine(Encoding.UTF8.GetString(blob));
                //Console.WriteLine("---- >");
                //Console.WriteLine();

                File.WriteAllBytes(path, blob);
            }
            else
            {
                File.Delete(path);
            }
        }
    }
}
