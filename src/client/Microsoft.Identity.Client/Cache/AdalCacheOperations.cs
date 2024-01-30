// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Cache
{
    internal static class AdalCacheOperations
    {
        private const int SchemaVersion = 3;
        private const string Delimiter = ":::";

        public static byte[] Serialize(ILoggerAdapter logger, IDictionary<AdalTokenCacheKey, AdalResultWrapper> tokenCacheDictionary)
        {
            using (Stream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(SchemaVersion);
                logger.Info(() => $"[AdalCacheOperations] Serializing token cache with {tokenCacheDictionary.Count} items. ");

                writer.Write(tokenCacheDictionary.Count);
                foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in tokenCacheDictionary)
                {
                    var key = kvp.Key;
                    writer.Write($"{key.Authority}{Delimiter}{key.Resource}{Delimiter}{key.ClientId}{Delimiter}{(int)key.TokenSubjectType}");
                    writer.Write(kvp.Value.Serialize());
                }

                int length = (int)stream.Position;
                stream.Position = 0;
                BinaryReader reader = new BinaryReader(stream);
                return reader.ReadBytes(length);
            }
        }

        public static IDictionary<AdalTokenCacheKey, AdalResultWrapper> Deserialize(ILoggerAdapter logger, byte[] state)
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                new Dictionary<AdalTokenCacheKey, AdalResultWrapper>();
            if (state == null || state.Length == 0)
            {
                return dictionary;
            }

            using (Stream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(state);
                writer.Flush();
                stream.Position = 0;

                BinaryReader reader = new BinaryReader(stream);
                int blobSchemaVersion = reader.ReadInt32();
                if (blobSchemaVersion != SchemaVersion)
                {
                    logger.Warning("[AdalCacheOperations] The version of the persistent state of the cache does not match the current schema, so skipping deserialization. ");
                    return dictionary;
                }

                int count = reader.ReadInt32();
                for (int n = 0; n < count; n++)
                {
                    string keyString = reader.ReadString();

                    string[] kvpElements = keyString.Split(new[] { Delimiter }, StringSplitOptions.None);
                    AdalResultWrapper resultEx = AdalResultWrapper.Deserialize(reader.ReadString());
                    AdalTokenCacheKey key = new AdalTokenCacheKey(kvpElements[0], kvpElements[1], kvpElements[2],
                        (TokenSubjectType)int.Parse(kvpElements[3], CultureInfo.CurrentCulture),
                        resultEx.Result.UserInfo);

                    dictionary[key] = resultEx;
                }

                logger.Info(() => $"[AdalCacheOperations] Deserialized {dictionary.Count} items to ADAL token cache. ");
            }

            return dictionary;
        }
    }
}
