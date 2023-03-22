using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAppWithBroker.MSALClient
{
    internal static class AppConstants
    {
        // ClientID of the application in (ms sample testing)
        internal const string ClientId = "858b4a09-dc31-45d3-83a7-2b5f024f99cd"; // TODO - Replace with your client Id. And also replace in the AndroidManifest.xml

        // TenantID of the organization (ms sample testing)
        internal const string TenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab"; // TODO - Replace with your TenantID. And also replace in the AndroidManifest.xml

        /// <summary>
        /// Scopes defining what app can access in the graph
        /// </summary>
        internal static string[] Scopes = { "User.Read" };
    }
}
