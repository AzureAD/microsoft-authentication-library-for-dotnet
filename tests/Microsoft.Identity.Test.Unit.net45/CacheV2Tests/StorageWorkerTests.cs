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

using System.Collections.Generic;
using System.IO;
using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    [TestClass]
    public class StorageWorkerTests
    {
        private const string _mockPath = "mock/relative/path.txt";
        public const string SampleJsonString1 = "{'life': 42, 'cat': '=^^='}";
        public const string SampleJsonString2 = "{'cat': '=^^='}";
        private FileSystemCredentialPathManager _credentialPathManager;
        private MockFileIO _mockFileIO;
        private PathStorageWorker _storageWorker;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _credentialPathManager =
                new FileSystemCredentialPathManager(TestCommon.CreateDefaultServiceBundle().PlatformProxy.CryptographyManager);
            _mockFileIO = new MockFileIO();
            _storageWorker = new PathStorageWorker(_mockFileIO, _credentialPathManager);
        }

        [TestMethod]
        public void SplitAndNormalizeScopes()
        {
            Assert.IsTrue(
                HashSetUtil.AreEqual(
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c",
                        "d",
                        "e\n\fe"
                    },
                    _storageWorker.SplitAndNormalizeScopes(" \r\f A\v\n\t  \vB  \n\t \r  \nC\n   D  E\n\fE\t  \t")));
            Assert.IsTrue(
                HashSetUtil.AreEqual(
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c"
                    },
                    _storageWorker.SplitAndNormalizeScopes("A B C")));
            Assert.IsTrue(
                HashSetUtil.AreEqual(
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    _storageWorker.SplitAndNormalizeScopes(" \r\f a \v\n\t\r\f b \v\n\t ")));
        }

        [TestMethod]
        public void GetAccessTokenPath()
        {
            string path = "UD";

            path = Path.Combine(path, "u_" + _credentialPathManager.ToSafeFilename(MockTestConstants.GetHomeAccountId()));
            path = Path.Combine(path, "e_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Environment));
            path = Path.Combine(path, "AT");
            path = Path.Combine(path, "r_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Realm));
            path = Path.Combine(path, "c_" + _credentialPathManager.ToSafeFilename(MockTestConstants.ClientId) + ".bin");
            path = PathUtils.Normalize(path);

            Assert.AreEqual(path, _storageWorker.GetCredentialPath(MockTestConstants.GetAccessToken()));
        }

        [TestMethod]
        public void GetRefreshTokenPath()
        {
            string path = "UD";
            path = Path.Combine(path, "u_" + _credentialPathManager.ToSafeFilename(MockTestConstants.GetHomeAccountId()));
            path = Path.Combine(path, "e_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Environment));
            path = Path.Combine(path, "RT");
            path = Path.Combine(path, "c_" + _credentialPathManager.ToSafeFilename(MockTestConstants.ClientId) + ".bin");
            path = PathUtils.Normalize(path);

            Assert.AreEqual(path, _storageWorker.GetCredentialPath(MockTestConstants.GetRefreshToken()));
        }

        [TestMethod]
        public void GetFamilyRefreshTokenPath()
        {
            string path = "UD";
            path = Path.Combine(path, "u_" + _credentialPathManager.ToSafeFilename(MockTestConstants.GetHomeAccountId()));
            path = Path.Combine(path, "e_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Environment));
            path = Path.Combine(path, "FRT");
            path = Path.Combine(path, "f_" + _credentialPathManager.ToSafeFilename(MockTestConstants.FamilyId) + ".bin");
            path = PathUtils.Normalize(path);

            Assert.AreEqual(path, _storageWorker.GetCredentialPath(MockTestConstants.GetFamilyRefreshToken()));
        }

        [TestMethod]
        public void GetIdTokenPath()
        {
            string path = "UD";
            path = Path.Combine(path, "u_" + _credentialPathManager.ToSafeFilename(MockTestConstants.GetHomeAccountId()));
            path = Path.Combine(path, "e_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Environment));
            path = Path.Combine(path, "ID");
            path = Path.Combine(path, "r_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Realm));
            path = Path.Combine(path, "c_" + _credentialPathManager.ToSafeFilename(MockTestConstants.ClientId) + ".bin");
            path = PathUtils.Normalize(path);

            Assert.AreEqual(path, _storageWorker.GetCredentialPath(MockTestConstants.GetIdToken()));
        }

        [TestMethod]
        public void GetAccountPath()
        {
            string path = "UD";

            path = Path.Combine(path, "u_" + _credentialPathManager.ToSafeFilename(MockTestConstants.GetHomeAccountId()));
            path = Path.Combine(path, "e_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Environment));
            path = Path.Combine(path, "Accounts");
            path = Path.Combine(path, "r_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Realm) + ".bin");
            path = PathUtils.Normalize(path);

            Assert.AreEqual(path, _credentialPathManager.GetAccountPath(MockTestConstants.GetAccount()));
        }

        [TestMethod]
        public void GetAppMetadataPath()
        {
            string path = "AppMetadata";
            path = Path.Combine(path, "e_" + _credentialPathManager.ToSafeFilename(MockTestConstants.Environment));
            path = Path.Combine(path, "c_" + _credentialPathManager.ToSafeFilename(MockTestConstants.ClientId) + ".bin");
            path = PathUtils.Normalize(path);

            Assert.AreEqual(path, _credentialPathManager.GetAppMetadataPath(MockTestConstants.GetAppMetadata()));
        }

        // Tests StorageWorker::Read on a non-existing file
        [TestMethod]
        public void ReadNonExisting()
        {
            var json = _storageWorker.Read(_mockPath);
            Assert.IsTrue(json.IsEmpty());
        }

        // Tests StorageWorker::Read on an existing file
        [TestMethod]
        public void ReadExisting()
        {
            var expectedJson = JObject.Parse(SampleJsonString1);

            _storageWorker.ReadModifyWrite(_mockPath, content => expectedJson);

            var actualJson = _storageWorker.Read(_mockPath);
            JObjectAssert.AreEqual(expectedJson, actualJson);
        }

        // Tests that StorageWorker::Read treats files with corrupted content as if they're empty
        [TestMethod]
        public void ReadBadContent()
        {
            // Write good content to "disk"
            _storageWorker.ReadModifyWrite(_mockPath, content => JObject.Parse(SampleJsonString2));

            // Corrupt the data - flip a bit
            byte[] data = _mockFileIO._fileSystem[_mockPath];
            data[data.Length / 2] ^= 0x01;

            Assert.IsTrue(_storageWorker.Read(_mockPath).IsEmpty());
        }

        // Tests StorageWorker::Read on an existing file which content is not true JSON
        [TestMethod]
        public void ReadBadJson()
        {
            byte[] encryptedContent = _storageWorker.Encrypt("{\"bad\": \"json\"");

            _mockFileIO.Write(_mockPath, encryptedContent);

            Assert.IsTrue(_storageWorker.Read(_mockPath).IsEmpty());
        }

        // Tests StorageWorker::ReadModifyWrite on a non existing file.
        [TestMethod]
        public void ReadModifyWriteNonExisting()
        {
            var expectedJson = JObject.Parse(SampleJsonString1);

            _storageWorker.ReadModifyWrite(
                _mockPath,
                content =>
                {
                    Assert.IsTrue(content.IsEmpty());
                    return expectedJson;
                });

            var actualJson = _storageWorker.Read(_mockPath);
            JObjectAssert.AreEqual(expectedJson, actualJson);
        }

        // Tests StorageWorker::ReadModifyWrite on an existing file.
        [TestMethod]
        public void ReadModifyWriteExisting()
        {
            var expectedJson = JObject.Parse(SampleJsonString1);

            _storageWorker.ReadModifyWrite(_mockPath, content => expectedJson);

            _storageWorker.ReadModifyWrite(
                _mockPath,
                content =>
                {
                    content["dog"] = "(^ .(I). ^)";
                    return content;
                });

            expectedJson["dog"] = "(^ .(I). ^)";

            var actualJson = _storageWorker.Read(_mockPath);
            JObjectAssert.AreEqual(expectedJson, actualJson);
        }

        // Tests StorageWorker::ReadModifyWrite on an existing file which content is corrupted.
        // It should be treated as an empty file
        [TestMethod]
        public void ReadModifyWriteBadContent()
        {
            // Write good content to "disk"
            _storageWorker.ReadModifyWrite(_mockPath, content => JObject.Parse(SampleJsonString2));

            //// Corrupt the data - flip a bit
            // vector<uint8_t>& data = _mockFileIO->_filesystem[_mockPath.u8string()];
            // data[data.size() / 2] ^= 0x01;

            // bool wasCalled = false;
            // ASSERT_NO_THROW(_storageWorker.ReadModifyWrite(_mockPath, content =>  {
            //    ASSERT_TRUE(content.empty());
            //    wasCalled = true;
            // }));
            // Assert.IsTrue(wasCalled);
        }

        // Tests StorageWorker::ReadModifyWrite on an existing file which content is not true JSON.
        // It should be treated as an empty file
        [TestMethod]
        public void ReadModifyWriteBadJson()
        {
            byte[] encryptedContent = _storageWorker.Encrypt("{\"bad\": \"json\"");

            _mockFileIO.Write(_mockPath, encryptedContent);

            // JObject expectedJson;
            bool wasCalled = false;
            // ASSERT_NO_THROW(
            _storageWorker.ReadModifyWrite(
                _mockPath,
                content =>
                {
                    Assert.IsTrue(content.IsEmpty());
                    wasCalled = true;
                    return content;
                });
            // );
            Assert.IsTrue(wasCalled);
        }

        private Credential EmplaceAccessToken(JObject j, HashSet<string> scopes)
        {
            string target = string.Join(" ", scopes);
            var token = MockTestConstants.GetAccessToken();
            token.Target = target;
            j[target] = StorageJsonUtils.CredentialToJson(token);
            return token;
        }

        private Credential GetAccessTokenWithScopes(HashSet<string> scopes)
        {
            string target = string.Join(" ", scopes);
            var token = MockTestConstants.GetAccessToken();
            token.Target = target;
            return token;
        }

        private JObject GetAccessTokenJsonWithScopes(HashSet<string> scopes)
        {
            return StorageJsonUtils.CredentialToJson(GetAccessTokenWithScopes(scopes));
        }

        [TestMethod]
        public void FindAccessTokenWithScopes()
        {
            {
                var j = new JObject();

                // ASSERT_MSAL_THROW(
                //    _storageWorker.FindAccessTokenWithScopes(j, ""),
                //    MsalStorageException,
                //    StorageErrorCodes::NO_ACCESS_TOKEN_SCOPES_REQUESTED);
                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "a"));
                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "a b c"));
            }

            {
                var j = new JObject();

                var tokenABC = EmplaceAccessToken(
                    j,
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c"
                    });
                var tokenDEFG = EmplaceAccessToken(
                    j,
                    new HashSet<string>
                    {
                        "d",
                        "e",
                        "f",
                        "g"
                    });

                Credential actualToken;

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "a");
                Assert.AreEqual(tokenABC, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "b");
                Assert.AreEqual(tokenABC, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "c");
                Assert.AreEqual(tokenABC, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "a b");
                Assert.AreEqual(tokenABC, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "b a");
                Assert.AreEqual(tokenABC, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "a b c");
                Assert.AreEqual(tokenABC, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "b c a");
                Assert.AreEqual(tokenABC, actualToken);

                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "a b c d"));
                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "a b d"));
                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "a d"));
                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "z"));
                Assert.IsNull(_storageWorker.FindAccessTokenWithScopes(j, "x z"));

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "d");
                Assert.AreEqual(tokenDEFG, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "e g");
                Assert.AreEqual(tokenDEFG, actualToken);

                actualToken = _storageWorker.FindAccessTokenWithScopes(j, "g e d");
                Assert.AreEqual(tokenDEFG, actualToken);
            }
        }

        [TestMethod]
        public void AddAccessTokenWithScopes()
        {
            void CreateAddVerify(
                List<HashSet<string>> scopesBefore,
                HashSet<string> scopesToAdd,
                List<HashSet<string>> scopesAfter)
            {
                var accessTokens = new JObject();
                foreach (HashSet<string> s in scopesBefore)
                {
                    EmplaceAccessToken(accessTokens, s);
                }

                _storageWorker.AddAccessTokenWithScopes(accessTokens, GetAccessTokenJsonWithScopes(scopesToAdd));

                Assert.AreEqual(accessTokens.Count, scopesAfter.Count);

                var keysToRemove = new List<string>();

                foreach (HashSet<string> s in scopesAfter)
                {
                    foreach (KeyValuePair<string, JToken> kvp in accessTokens)
                    {
                        if (HashSetUtil.AreEqual(s, ScopeUtils.SplitScopes(kvp.Key)))
                        {
                            keysToRemove.Add(kvp.Key);
                            break;
                        }
                    }
                }

                foreach (string keyToRemove in keysToRemove)
                {
                    accessTokens.Remove(keyToRemove);
                }

                Assert.IsTrue(accessTokens.IsEmpty());
            }

            CreateAddVerify(
                new List<HashSet<string>>(),
                new HashSet<string>
                {
                    "a",
                    "b"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    }
                });
            CreateAddVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    }
                },
                new HashSet<string>
                {
                    "c",
                    "d"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                });
            CreateAddVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b",
                    "c",
                    "d",
                    "Z"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c",
                        "d",
                        "Z"
                    }
                });
            CreateAddVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b",
                    "c",
                    "Z"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c",
                        "Z"
                    }
                });
            CreateAddVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    },
                    new HashSet<string>
                    {
                        "e",
                        "f"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "c",
                    "e"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "c",
                        "e"
                    }
                });
            CreateAddVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    },
                    new HashSet<string>
                    {
                        "e",
                        "f"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b",
                    "c",
                    "d",
                    "e",
                    "f"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c",
                        "d",
                        "e",
                        "f"
                    }
                });
            CreateAddVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    },
                    new HashSet<string>
                    {
                        "e",
                        "f"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b",
                    "c",
                    "Z"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b",
                        "c",
                        "Z"
                    },
                    new HashSet<string>
                    {
                        "e",
                        "f"
                    }
                });

            // ASSERT_MSAL_THROW(createAddVerify({}, {}, {}), MsalStorageException, StorageErrorCodes::ACCESS_TOKEN_HAS_NO_SCOPES);
        }

        [TestMethod]
        public void RemoveAccessTokenWithScopes()
        {
            void CreateRemoveVerify(
                List<HashSet<string>> scopesBefore,
                HashSet<string> scopesToRemove,
                List<HashSet<string>> scopesAfter)
            {
                var accessTokens = new JObject();
                foreach (HashSet<string> s in scopesBefore)
                {
                    EmplaceAccessToken(accessTokens, s);
                }

                _storageWorker.RemoveAccessTokenWithScopes(accessTokens, JoinScopes(scopesToRemove));

                Assert.AreEqual(accessTokens.Count, scopesAfter.Count);

                foreach (HashSet<string> s in scopesAfter)
                {
                    foreach (KeyValuePair<string, JToken> kvp in accessTokens)
                    {
                        if (HashSetUtil.AreEqual(s, ScopeUtils.SplitScopes(kvp.Key)))
                        {
                            accessTokens.Remove(kvp.Key);
                            break;
                        }
                    }
                }

                Assert.IsTrue(accessTokens.IsEmpty());
            }

            CreateRemoveVerify(
                new List<HashSet<string>>(),
                new HashSet<string>
                {
                    "a",
                    "b"
                },
                new List<HashSet<string>>());
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    }
                },
                new HashSet<string>
                {
                    "c",
                    "d"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    }
                });
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b",
                    "c",
                    "d",
                    "Z"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                });
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "Z"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                });
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "a"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                });
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                });
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                },
                new HashSet<string>
                {
                    "a",
                    "b",
                    "c"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    }
                });
            CreateRemoveVerify(
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "c",
                        "d"
                    },
                    new HashSet<string>
                    {
                        "e",
                        "f"
                    }
                },
                new HashSet<string>
                {
                    "c"
                },
                new List<HashSet<string>>
                {
                    new HashSet<string>
                    {
                        "a",
                        "b"
                    },
                    new HashSet<string>
                    {
                        "e",
                        "f"
                    }
                });

            // ASSERT_MSAL_THROW(
            //    CreateRemoveVerify({}, {}, {}), MsalStorageException, StorageErrorCodes::NO_ACCESS_TOKEN_SCOPES_REQUESTED);
            // ASSERT_MSAL_THROW(
            //    CreateRemoveVerify({{"a"}}, {}, {}), MsalStorageException, StorageErrorCodes::NO_ACCESS_TOKEN_SCOPES_REQUESTED);
        }

        private string JoinScopes(HashSet<string> scopes)
        {
            return string.Join(" ", scopes);
        }
    }
}
