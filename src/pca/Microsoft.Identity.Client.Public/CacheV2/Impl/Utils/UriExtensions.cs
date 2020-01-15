// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.CacheV2.Impl.Utils
{
    internal static class UriExtensions
    {
        public static string GetEnvironment(this Uri uri)
        {
            return uri.Host;
        }

        public static string GetRealm(this Uri uri)
        {
            string path = uri.GetPath();
            string[] parts = path.Split(
                new[]
                {
                    '/'
                },
                StringSplitOptions.RemoveEmptyEntries);
            return parts[0];
            // return uri.GetPath().Split('/')[0]; // todo: verify this
        }

        public static string GetPath(this Uri uri)
        {
            return uri.AbsolutePath; // todo: verify this
        }
    }
}
