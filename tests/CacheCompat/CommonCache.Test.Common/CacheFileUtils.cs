// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

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
