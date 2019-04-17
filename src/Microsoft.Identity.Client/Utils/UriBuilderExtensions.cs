// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Utils
{
    internal static class UriBuilderExtensions
    {
        public static void AppendQueryParameters(this UriBuilder builder, string queryParams)
        {
            if (builder == null || string.IsNullOrEmpty(queryParams))
            {
                return;
            }

            if (builder.Query.Length > 1)
            {
                builder.Query = builder.Query.Substring(1) + "&" + queryParams;
            }
            else
            {
                builder.Query = queryParams;
            }
        }

        public static void AppendQueryParameters(this UriBuilder builder, IDictionary<string, string> queryParams)
        {
            var list = new List<string>();
            foreach (var kvp in queryParams)
            {
                list.Add($"{kvp.Key}={kvp.Value}");
            }
            AppendQueryParameters(builder, string.Join("&", list));
        }
    }
}
