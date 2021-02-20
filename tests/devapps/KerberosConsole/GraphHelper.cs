// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Flurl.Http;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;

namespace KerberosConsole
{
    /// <summary>
    /// Helper class to access Microsoft Graph.
    /// </summary>
    internal class GraphHelper
    {
        internal static void ShowUserProfile(string accessToken)
        {
            try
            {
                string profileJson = "https://graph.microsoft.com/v1.0/me"
                    .WithOAuthBearerToken(accessToken)
                    .GetStringAsync()
                    .GetAwaiter()
                    .GetResult();
                Console.WriteLine(profileJson);
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        internal static void GetApplicationListAsync(string tenantId, string accessToken)
        {
            string uri = String.Format(CultureInfo.CurrentCulture,
                "https://graph.microsoft.com/v1.0/{0}/applications",
                tenantId);

            List<string> applications = new List<string>();
            bool done = false;

            try
            {
                while (!done)
                {
                    string json = uri
                        .WithOAuthBearerToken(accessToken)
                        .GetStringAsync()
                        .GetAwaiter()
                        .GetResult();

                    Page page = JsonConvert.DeserializeObject<Page>(json);
                    foreach (object obj in page.Results)
                    {
                        applications.Add(obj.ToString());
                    }

                    uri = page.NextLink;
                    done = string.IsNullOrEmpty(uri);
                }
            }
            catch (FlurlHttpException ex)
            {
                Console.WriteLine("Exception: " + ex);
            }

            Console.WriteLine("# of searched applications: " + applications.Count);
        }
    }
}
