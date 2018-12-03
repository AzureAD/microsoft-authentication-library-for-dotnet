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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Common.Core.Helpers;
#if NETFRAMEWORK
using Microsoft.Identity.Core.Platforms.net45.CacheV2;
#endif

namespace Test.MSAL.NET.Unit.net45.CacheV2Tests
{
#if NETFRAMEWORK
    [TestClass]
    public class FileIOTests
    {
        private const string FileName = "FileIOTests.bin";
        private const string TestFolderBase = "FileIOTestsFolder";
        private const string TestFolder = "FileIOTestsFolder\\MoreFileIOTestsFolder";
        private byte[] _data;
        private WindowsFileSystemCacheKeyStorage _io;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCleanup();
            _io = new WindowsFileSystemCacheKeyStorage(Path.Combine(AssemblyUtils.GetExecutingAssemblyDirectory(), TestFolder));
            _data = RandomDataUtils.GetRandomData(1024);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(TestFolderBase))
            {
                Directory.Delete(TestFolderBase, true);
            }

            if (File.Exists(TestFolderBase))
            {
                File.Delete(TestFolderBase);
            }
        }

        [TestMethod]
        public void CurrentPathFormat()
        {
            Debug.WriteLine("TEST START: CurrentPathFormat");
            Assert.IsTrue(Path.IsPathRooted(AssemblyUtils.GetExecutingAssemblyDirectory()));
        }

        [TestMethod]
        public void DirectoryIteratorBehaviorRelativePath()
        {
            Debug.WriteLine("TEST START: DirectoryIteratorBehaviorRelativePath");
            _io.Write("a.txt", _data);
            _io.Write("b.txt", _data);
            _io.Write("x/y/c.txt", _data);

            var expectedEntriesPaths = new HashSet<string>
            {
                Path.Combine(TestFolder, "a.txt"),
                Path.Combine(TestFolder, "b.txt"),
                Path.Combine(TestFolder, "x")
            };

            var actualEntriesPaths = new HashSet<string>(Directory.EnumerateFileSystemEntries(TestFolder));

            // When iterating over a relative path, it produces paths relative to the current_path, not to the path it received
            // as an input
            Assert.IsTrue(HashSetUtil.AreEqual(actualEntriesPaths, expectedEntriesPaths));
        }

        [TestMethod]
        public void DirectoryIteratorBehaviorAbsolutePath()
        {
            Debug.WriteLine("TEST START: DirectoryIteratorBehaviorAbsolutePath");
            _io.Write("a.txt", _data);
            _io.Write("b.txt", _data);
            _io.Write("x/y/c.txt", _data);

            var expectedEntriesPaths = new HashSet<string>
            {
                Path.Combine(AssemblyUtils.GetExecutingAssemblyDirectory(), TestFolder, "a.txt"),
                Path.Combine(AssemblyUtils.GetExecutingAssemblyDirectory(), TestFolder, "b.txt"),
                Path.Combine(AssemblyUtils.GetExecutingAssemblyDirectory(), TestFolder, "x")
            };

            var actualEntriesPaths = new HashSet<string>(
                Directory.EnumerateFileSystemEntries(Path.Combine(AssemblyUtils.GetExecutingAssemblyDirectory(), TestFolder)));

            // When iterating over an absolute path, it produces absolute paths
            Assert.IsTrue(HashSetUtil.AreEqual(actualEntriesPaths, expectedEntriesPaths));
        }

        // Tests filesystem::path::parent_path() behavior for absolute paths
        [TestMethod]
        public void ParentAbsolutePath()
        {
            Debug.WriteLine("TEST START: ParentAbsolutePath");

            string p = "C:\\root\\..\\root\\file.bin";

            p = _io.GetParentPath(p);
            Assert.AreEqual("C:\\root\\..\\root", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual("C:\\root\\..", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual("C:\\root", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual("C:\\", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual(string.Empty, p); // todo: this varies from c++, in c++ it's c:\\
        }

        [TestMethod]
        public void ParentRelativePath()
        {
            Debug.WriteLine("TEST START: ParentRelativePath");

            // string p = "root\\subroot\\..\\subroot\\file.bin";  // todo: why will we ever do ../ representation here anyway?
            string p = "root\\subroot\\subroot\\file.bin";

            p = _io.GetParentPath(p);
            Assert.AreEqual("root\\subroot\\subroot", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual("root\\subroot", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual("root", p);

            p = _io.GetParentPath(p);
            Assert.AreEqual(string.Empty, p);

            p = _io.GetParentPath(p);
            Assert.AreEqual(string.Empty, p);
        }

        // Tests MakeMutexName() functionality, which converts a file path into a mutex name
        [TestMethod]
        public void TestMakeMutexName()
        {
            Debug.WriteLine("TEST START: TestMakeMutexName");

            const string namePrefix = FileMutex.MutexNamePrefix;

            // This is why the relative path should be in its normal form
            // Calling Write("root/subroot/file.bin") and Write("root/subroot/../subroot/file.bin") from two different threads
            // will result in acquiring two different mutexes and therefore possible race conditions
            Assert.AreEqual(FileMutex.MakeMutexName("root/subroot/file.bin"), namePrefix + "root/subroot/file.bin");
            Assert.AreEqual(
                FileMutex.MakeMutexName("root/subroot/../subroot/file.bin"),
                namePrefix + "root/subroot/../subroot/file.bin");

            // All path formats get converted to the generic form (which uses '/' as a delimiter)
            Assert.AreEqual(FileMutex.MakeMutexName("root\\subroot\\file.bin"), namePrefix + "root/subroot/file.bin");
            Assert.AreEqual(
                FileMutex.MakeMutexName("root\\subroot\\..\\subroot\\file.bin"),
                namePrefix + "root/subroot/../subroot/file.bin");
        }

        // Tests FileIO::GetFullPath() functionality, which returns the absolute path of a file to operate on
        [TestMethod]
        public void GetFullPath()
        {
            Debug.WriteLine("TEST START: GetFullPath");

            var io = new WindowsFileSystemCacheKeyStorage("C:\\root");

            // You're supposed to provide a base path in a constructor and a relative path when doing IO operations
            Assert.AreEqual("C:/root/subroot/file.bin", io.GetFullPath("subroot\\file.bin"));
            Assert.AreEqual("C:/root/subroot/../subroot/file.bin", io.GetFullPath("subroot\\..\\subroot\\file.bin"));

            // It's okay if the relative path is empty, which means you're referring to folder specified by the base bath
            Assert.AreEqual("C:/root", io.GetFullPath(string.Empty));

            // Do not pass an absolute path as a relative path
            Assert.ThrowsException<ArgumentException>(() => io.GetFullPath("C:\\root\\subroot\\file.bin"));

            // Base path may not be empty or in a relative format
            Assert.ThrowsException<ArgumentException>(() => new WindowsFileSystemCacheKeyStorage(string.Empty));
            Assert.ThrowsException<ArgumentException>(() => new WindowsFileSystemCacheKeyStorage("subroot\\file.bin"));
        }

        // Tests that FileIO::LockFile indeed locks the file and returns a locked mutex and its guard
        [TestMethod]
        public void LockFile()
        {
            Debug.WriteLine("TEST START: LockFile");

            using (_io.LockFile(FileName))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () =>
                    {
                        // Another thread should not be able to acquire mutex with this name because it's being used by fileMutex
                        Assert.ThrowsException<InvalidOperationException>(() => new FileMutex(FileName));
                    });
            }

            ThreadTestUtils.RunActionOnThreadAndJoin(
                () =>
                {
                    // Now this thread should be able to acquire mutex with this name because fileMutex went out of scope
                    Assert.ThrowsException<InvalidOperationException>(() => new FileMutex(FileName));
                });
        }

        // Tests FileIO::CreateDirectoriesLockParent which creates a directory and all its nonexistent parents up to and including
        // the one specified by the basePath
        [TestMethod]
        public void CreateDirectoriesLockParent()
        {
            Debug.WriteLine("TEST START: CreateDirectoriesLockParent");

            // The basePath doesn't exist (and its parent doesn't exist either), but the path gets created
            // Only basePath and its descendants get locked, but not its ancestors (FileIO never locks anything above the
            // basePath)
            using (_io.CreateDirectoriesLockParent("Level1/Level2/Level3"))
            {
            }

            string actual = _io.GetFullPath("Level1/Level2/Level3");
            Assert.IsTrue(Directory.Exists(actual));
        }

        // Tests FileIO::CreateDirectoriesLockParent with an empty input
        [TestMethod]
        public void CreateDirectoriesEmptyPath()
        {
            Debug.WriteLine("TEST START: CreateDirectoriesEmptyPath");

            // Creates the basePath
            using (_io.CreateDirectoriesLockParent(string.Empty))
            {
            }

            Assert.IsTrue(Directory.Exists(_io.BasePath));
        }

        // Tests FileIO::CreateDirectoriesLockParent concurrently
        [TestMethod]
        public void CreateDirectoriesConcurrently()
        {
            Debug.WriteLine("TEST START: CreateDirectoriesConcurrently");

            ThreadTestUtils.ParallelExecute(
                () =>
                {
                    Thread.Sleep(new Random().Next() % 10);
                    using (_io.CreateDirectoriesLockParent("Level1/Level2/Level3"))
                    {
                    }
                });

            string actual = _io.GetFullPath("Level1/Level2/Level3");
            Assert.IsTrue(Directory.Exists(actual));
        }

        // Verifies that multiple threads trying to read the same file concurrently will get the same output
        [TestMethod]
        public void ReadFileConcurrently()
        {
            Debug.WriteLine("TEST START: ReadFileConcurrently");

            _io.Write(FileName, _data);

            ThreadTestUtils.ParallelExecute(
                () =>
                {
                    Thread.Sleep(new Random().Next() % 10);
                    byte[] actual = _io.Read(FileName);
                    CollectionAssert.AreEqual(_data, actual);
                });
        }

        // Verifies that multiple threads trying to write into the same file concurrently will do it in order and the file will
        // not get corrupted
        [TestMethod]
        public void WriteFileConcurrently()
        {
            Debug.WriteLine("TEST START: WriteFileConcurrently");

            ThreadTestUtils.ParallelExecute(
                () =>
                {
                    Thread.Sleep(new Random().Next() % 10);
                    _io.Write(FileName, _data);
                });

            CollectionAssert.AreEqual(_data, _io.Read(FileName));

            // The file is in the right location
            Assert.IsTrue(File.Exists(_io.GetFullPath(FileName)));
        }

        // Tests file creation and deletion
        [TestMethod]
        public void TestDeleteFile()
        {
            Debug.WriteLine("TEST START: TestDeleteFile");

            _io.Write(FileName, _data);
            Assert.IsTrue(File.Exists(_io.GetFullPath(FileName)));

            _io.DeleteFile(FileName);
            Assert.IsFalse(File.Exists(_io.GetFullPath(FileName)));
        }

        // Tests FileIO::DeleteFile() and FileIO::Write() concurrently
        [TestMethod]
        public void WriteDeleteConcurrently()
        {
            Debug.WriteLine("TEST START: WriteDeleteConcurrently");

            var actions = new List<Action>();
            for (int i = 0; i < 100; i++)
            {
                actions.Add(
                    () =>
                    {
                        Thread.Sleep(new Random().Next() % 10);
                        _io.Write(FileName, _data);
                    });
                actions.Add(
                    () =>
                    {
                        Thread.Sleep(new Random().Next() % 10);
                        _io.DeleteFile(FileName);
                    });
            }

            Parallel.Invoke(
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8
                },
                actions.ToArray());

            // Either the file is in a valid state or doesn't exist
            if (File.Exists(_io.GetFullPath(FileName)))
            {
                CollectionAssert.AreEqual(_data, _io.Read(FileName));
            }
        }

        // Tests FileIO::Read(), FileIO::Write(), and FileIO::DeleteFile() concurrently
        [TestMethod]
        public void ReadWriteDeleteConcurrently()
        {
            Debug.WriteLine("TEST START: ReadWriteDeleteConcurrently");

            Directory.CreateDirectory(_io.GetFullPath(string.Empty));

            int successfulReadsCount = 0;

            var actions = new List<Action>();
            for (int i = 0; i < 100; i++)
            {
                actions.Add(
                    () =>
                    {
                        Thread.Sleep(new Random().Next() % 10);
                        byte[] actualData = _io.Read(FileName);

                        // Either the file doesn't exist
                        if (!actualData.Any())
                        {
                        }
                        else
                        {
                            CollectionAssert.AreEqual(_data, actualData);
                            ++successfulReadsCount;
                        }

                        _io.Write(FileName, _data);
                    });
                actions.Add(
                    () =>
                    {
                        Thread.Sleep(new Random().Next() % 10);
                        _io.Write(FileName, _data);
                    });
                actions.Add(
                    () =>
                    {
                        Thread.Sleep(new Random().Next() % 10);
                        _io.DeleteFile(FileName);
                    });
            }

            Parallel.Invoke(
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8
                },
                actions.ToArray());

            Assert.IsTrue(successfulReadsCount > 0);

            // Either the file is in a valid state or doesn't exist
            if (File.Exists(_io.GetFullPath(FileName)))
            {
                CollectionAssert.AreEqual(_data, _io.Read(FileName));
            }
        }

        // Writes random data of various length into a file, reads it, and verifies that the input matches the output
        [TestMethod]
        public void WriteReadDataFuzz()
        {
            Debug.WriteLine("TEST START: WriteReadDataFuzz");

            for (int dataSize = 1; dataSize <= 1 << 18; dataSize <<= 1)
            {
                byte[] localData = RandomDataUtils.GetRandomData(dataSize);

                _io.Write(FileName, localData);
                byte[] actual = _io.Read(FileName);
                CollectionAssert.AreEqual(localData, actual);
            }
        }

        // Verifies that FileIO::DeleteFile throws when trying to delete a file locked by the filesystem
        [TestMethod]
        public void DeleteLockedFileThrows()
        {
            Debug.WriteLine("TEST START: DeleteLockedFileThrows");

            // Create the file and the folders above it
            _io.Write(FileName, _data);

            // Lock the file by the filesystem
            using (File.Open(_io.GetFullPath(FileName), FileMode.Append, FileAccess.Write))
            {
                Assert.ThrowsException<IOException>(() => _io.DeleteFile(FileName));
            }
        }

        // Verifies that FileIO::Write throws when trying to write into a readonly file
        [TestMethod]
        public void WriteReadonlyFileThrows()
        {
            Debug.WriteLine("TEST START: WriteReadonlyFileThrows");

            // Create the file and the folders above it
            _io.Write(FileName, _data);

            // Make the file read-only
            try
            {
                File.SetAttributes(_io.GetFullPath(FileName), FileAttributes.ReadOnly);
                Assert.ThrowsException<UnauthorizedAccessException>(() => _io.Write(FileName, _data));
            }
            finally
            {
                File.SetAttributes(_io.GetFullPath(FileName), FileAttributes.Normal);
            }
        }

        // Verifies that trying to read a file which doesn't exist returns 'false' and doesn't
        // throw
        [TestMethod]
        public void ReadNonexistentFileReturnsEmpty()
        {
            Debug.WriteLine("TEST START: ReadNonexistentFileReturnsEmpty");

            Assert.IsFalse(_io.Read(FileName).Any());
        }

        // Verifies that trying to create an invalid directory throws an exception
        [TestMethod]
        public void CreateInvalidDirectoryThrows()
        {
            Debug.WriteLine("TEST START: CreateInvalidDirectoryThrows");

            // Create a file called TestFolderBase
            File.WriteAllText(TestFolderBase, "here's some content");

            // This will try to create directory called TestFolderBase
            Assert.ThrowsException<IOException>(() => _io.Write(FileName, _data));
        }

        // Tests that FileIO::Read can't read a file which is locked
        [TestMethod]
        public void ReadLocksFile()
        {
            Debug.WriteLine("TEST START: ReadLocksFile");

            using (new FileMutex(FileName))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () => Assert.ThrowsException<InvalidOperationException>(() => _io.Read(FileName)));
            }
        }

        // Tests that FileIO::Write can't write in to a file which is locked
        [TestMethod]
        public void WriteLocksFile()
        {
            Debug.WriteLine("TEST START: WriteLocksFile");

            using (new FileMutex(FileName))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () => Assert.ThrowsException<InvalidOperationException>(() => _io.Write(FileName, _data)));
            }
        }

        // Tests that FileIO::DeleteFile can't delete a file which is locked
        [TestMethod]
        public void DeleteFileLocksFile()
        {
            Debug.WriteLine("TEST START: DeleteFileLocksFile");

            using (new FileMutex(FileName))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () => { Assert.ThrowsException<InvalidOperationException>(() => _io.DeleteFile(FileName)); });
            }
        }

        // Tests that FileIO::Write can't create a file whose parent is locked
        [TestMethod]
        public void CreateFileLocksParentDirectory()
        {
            Debug.WriteLine("TEST START: CreateFileLocksParentDirectory");

            // Mutex name for the root directory, which is the parent directory for the file we're about to try to write to
            using (new FileMutex(string.Empty))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () => Assert.ThrowsException<InvalidOperationException>(() => _io.Write(FileName, _data)));
            }
        }

        // Tests that FileIO::Write doesn't lock a parent directory of a file which already exists
        [TestMethod]
        public void WriteDoesNotLockParentDirectory()
        {
            Debug.WriteLine("TEST START: WriteDoesNotLockParentDirectory");

            // Mutex name for the root directory, which is the parent directory for the file we're about to write to
            // Create the file
            _io.Write(FileName, _data);

            using (new FileMutex(string.Empty))
            {
                // Writing into the file should work, because the file already exists and FileIO::Write doesn't need to lock the
                // parent directory
                ThreadTestUtils.RunActionOnThreadAndJoin(() => _io.Write(FileName, _data));
            }
        }

        // Tests that FileIO::DeleteFile can't delete a file whose parent is locked
        [TestMethod]
        public void DeleteFileLocksParentDirectory()
        {
            Debug.WriteLine("TEST START: DeleteFileLocksParentDirectory");

            // Mutex name for the root directory, which is the parent directory for the file we're about to try to delete
            using (new FileMutex(string.Empty))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () => Assert.ThrowsException<InvalidOperationException>(() => { _io.DeleteFile(FileName); }));
            }
        }

        // Tests that FileIO::Read can read a file whose parent is locked
        [TestMethod]
        public void ReadFileDoesNotLockParentDirectory()
        {
            Debug.WriteLine("TEST START: ReadFileDoesNotLockParentDirectory");

            _io.Write(FileName, _data);

            // Mutex name for the root directory, which is the parent directory for the file we're about to read
            using (new FileMutex(string.Empty))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(() => { Assert.AreEqual(_data, _io.Read(FileName)); });
            }
        }

        // Tests FileIO::ReadModifyWrite on an existing file
        [TestMethod]
        public void ReadModifyWrite()
        {
            Debug.WriteLine("TEST START: ReadModifyWrite");

            const string noRegrets = "No regrets";
            const string noRagrats = "No ragrats";

            _io.Write(FileName, Encoding.UTF8.GetBytes(noRegrets));

            _io.ReadModifyWrite(
                FileName,
                bytes =>
                {
                    var outval = new byte[bytes.Length];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (bytes[i] == (byte)'e')
                        {
                            outval[i] = (byte)'a';
                        }
                        else
                        {
                            outval[i] = bytes[i];
                        }
                    }

                    return outval;
                });

            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(noRagrats), _io.Read(FileName));
        }

        // Tests FileIO::ReadModifyWrite on a non-existing file
        [TestMethod]
        public void ReadModifyWriteNewFile()
        {
            Debug.WriteLine("TEST START: ReadModifyWriteNewFile");

            const string NewFile = "New file!";
            _io.ReadModifyWrite(FileName, bytes => bytes.Any() ? bytes : Encoding.UTF8.GetBytes(NewFile));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(NewFile), _io.Read(FileName));
        }

        // Tests FileIO::ListContent
        [TestMethod]
        public void ListContent()
        {
            Debug.WriteLine("TEST START: ListContent");

            _io.Write("x/a.txt", _data);
            _io.Write("x/b.txt", _data);
            _io.Write("x/c/d.txt", _data);
            _io.Write("x/e/f.txt", _data);
            _io.Write("x/e/g.txt", _data);
            _io.Write("z.txt", _data);

            List<string> actual = _io.ListContent("x").ToList();
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "x/a.txt",
                    "x/b.txt",
                    "x/c",
                    "x/e"
                },
                actual);

            actual = _io.ListContent("x/e").ToList();
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "x/e/f.txt",
                    "x/e/g.txt"
                },
                actual);

            actual = _io.ListContent("x/c").ToList();
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "x/c/d.txt"
                },
                actual);

            actual = _io.ListContent(string.Empty).ToList();
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "x",
                    "z.txt"
                },
                actual);

            Assert.IsFalse(_io.ListContent("doesnt_exist").Any());
            Assert.IsFalse(_io.ListContent("x/doesnt_exist").Any());
            Assert.IsFalse(_io.ListContent("doesnt_exist/doesnt_exist").Any());
            Assert.IsFalse(_io.ListContent("doesnt_exist/doesnt_exist/doesnt_exist").Any());
            Assert.IsFalse(_io.ListContent("x/a").Any());
            Assert.IsFalse(_io.ListContent("x/e/doesnt_exist").Any());

            Assert.IsFalse(_io.ListContent("x/a.txt").Any());

            _io.DeleteFile("x/e/f.txt");
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "x/e/g.txt"
                },
                _io.ListContent("x/e").ToList());

            _io.DeleteFile("x/e/g.txt");
            Assert.IsFalse(_io.ListContent("x/e").Any());
        }

        // Tests FileIO::DeleteContent
        [TestMethod]
        public void DeleteContent()
        {
            Debug.WriteLine("TEST START: DeleteContent");

            _io.Write("x/a.txt", _data);
            _io.Write("x/b.txt", _data);
            _io.Write("x/c/d.txt", _data);
            _io.Write("x/e/f.txt", _data);
            _io.Write("y/zz.txt", _data);
            _io.Write("z.txt", _data);

            _io.DeleteContent("x");

            var expectedContent = new List<string>
            {
                "y",
                "z.txt"
            };

            List<string> actualContent = _io.ListContent(string.Empty).ToList();

            CollectionAssert.AreEqual(expectedContent, actualContent);
        }

        // Tests that FileIO::DeleteContent locks the parent directory of the file/directory it's deleting
        [TestMethod]
        public void DeleteContentLocksParentDirectory()
        {
            Debug.WriteLine("TEST START: DeleteContentLocksParentDirectory");

            _io.Write("x/a.txt", _data);

            // Mutex name for the root directory, which is the parent directory for the directory we're about to try to delete
            using (new FileMutex(string.Empty))
            {
                ThreadTestUtils.RunActionOnThreadAndJoin(
                    () => Assert.ThrowsException<InvalidOperationException>(() => _io.DeleteContent("x")));
            }
        }
    }
#endif
}