// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class RandomDataUtils
    {
        private static readonly Random Random = new Random();

        public static byte[] GetRandomData(int size)
        {
            var data = new byte[size];
            Random.NextBytes(data);
            return data;
        }
    }
}
