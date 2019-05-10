// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.CacheV2.Impl.InMemory;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    [TestClass]
    public class InMemoryCacheKeyStorageTests
    {
        private const string FileName = "FileIOTests.bin";
        private const string TestFolderBase = "FileIOTestsFolder";
        private byte[] _data;
        private InMemoryCachePathStorage _io;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _io = new InMemoryCachePathStorage();
            _data = RandomDataUtils.GetRandomData(1024);
        }

        // Verifies that multiple threads trying to read the same file concurrently will get the same output
        [TestMethod]
        public void ReadFileConcurrently()
        {
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
            ThreadTestUtils.ParallelExecute(
                () =>
                {
                    Thread.Sleep(new Random().Next() % 10);
                    _io.Write(FileName, _data);
                });

            CollectionAssert.AreEqual(_data, _io.Read(FileName));

            // The file is in the right location
            Assert.IsTrue(_io.RootDirectory.FileExists(FileName));
        }

        // Tests file creation and deletion
        [TestMethod]
        public void TestDeleteFile()
        {
            _io.Write(FileName, _data);
            Assert.IsTrue(_io.RootDirectory.FileExists(FileName));

            _io.DeleteFile(FileName);
            Assert.IsFalse(_io.RootDirectory.FileExists(FileName));
        }

        // Tests FileIO::DeleteFile() and FileIO::Write() concurrently
        [TestMethod]
        public void WriteDeleteConcurrently()
        {
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
            if (_io.RootDirectory.FileExists(FileName))
            {
                CollectionAssert.AreEqual(_data, _io.Read(FileName));
            }
        }

        // Tests FileIO::Read(), FileIO::Write(), and FileIO::DeleteFile() concurrently
        [TestMethod]
        [Ignore]
        public void ReadWriteDeleteConcurrently()
        {
            int successfulReadsCount = 0;

            var actions = new List<Action>();
            for (int i = 0; i < 100; i++)
            {
                actions.Add(
                    () =>
                    {
                        Thread.Sleep(new Random().Next() % 10);
                        byte[] actualData = _io.Read(FileName);


                        if (actualData.Any())
                        {
                            // We successfully read the data
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
            if (_io.RootDirectory.FileExists(FileName))
            {
                CollectionAssert.AreEqual(_data, _io.Read(FileName));
            }
        }

        // Writes random data of various length into a file, reads it, and verifies that the input matches the output
        [TestMethod]
        public void WriteReadDataFuzz()
        {
            for (int dataSize = 1; dataSize <= 1 << 18; dataSize <<= 1)
            {
                byte[] localData = RandomDataUtils.GetRandomData(dataSize);

                _io.Write(FileName, localData);
                byte[] actual = _io.Read(FileName);
                CollectionAssert.AreEqual(localData, actual);
            }
        }

        // Verifies that trying to read a file which doesn't exist returns 'false' and doesn't
        // throw
        [TestMethod]
        public void ReadNonexistentFileReturnsEmpty()
        {
            Assert.IsFalse(_io.Read(FileName).Any());
        }

        // Verifies that trying to create an invalid directory throws an exception
        [TestMethod]
        public void CreateInvalidDirectoryThrows()
        {
            // Create a file called TestFolderBase
            _io.RootDirectory.CreateFile(TestFolderBase, Encoding.UTF8.GetBytes("here's some content"));

            // This will try to create directory called TestFolderBase
            Assert.ThrowsException<InvalidOperationException>(() => _io.Write(TestFolderBase + "/" + FileName, _data));
        }

        // Tests FileIO::ReadModifyWrite on an existing file
        [TestMethod]
        public void ReadModifyWrite()
        {
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
            const string NewFile = "New file!";
            _io.ReadModifyWrite(FileName, bytes => bytes.Any() ? bytes : Encoding.UTF8.GetBytes(NewFile));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(NewFile), _io.Read(FileName));
        }

        // Tests FileIO::ListContent
        [TestMethod]
        public void ListContent()
        {
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
    }
}
