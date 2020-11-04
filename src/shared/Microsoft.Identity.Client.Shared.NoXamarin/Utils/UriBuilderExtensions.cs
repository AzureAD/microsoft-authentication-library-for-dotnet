// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Identity.Client.Utils
{
    internal static class UriBuilderExtensions
    {
        private const int DefaultHttpsPort = 443;

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

        public static string GetHttpsUriWithOptionalPort(string host, string tenant, string path, int port)
        {
            var builder = new UriBuilder("https", host);
            builder.Path = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", tenant, path);

            //No need to set port if it equals 443 as it is the default https port
            if (port != DefaultHttpsPort)
            {
                builder.Port = port;
            }

            return builder.Uri.AbsoluteUri;
        }

        public static string GetHttpsUriWithOptionalPort(string uri, int port)
        {
            //No need to set port if it equals 443 as it is the default https port
            if (port != DefaultHttpsPort)
            {
                var builder = new UriBuilder(uri);
                builder.Port = port;
                return builder.Uri.AbsoluteUri;
            }

            return uri;
        }
    }
}
