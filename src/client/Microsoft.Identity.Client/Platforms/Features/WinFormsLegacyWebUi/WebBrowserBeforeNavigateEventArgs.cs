// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    /// <summary>
    /// Event arguments for <c>BeforeNavigate</c> event
    /// </summary>
    public class WebBrowserBeforeNavigateEventArgs : CancelEventArgs
    {
        /// <summary>
        /// </summary>
        public WebBrowserBeforeNavigateEventArgs(string url, byte[] postData, string headers, int flags,
            string targetFrameName, object webBrowserActiveXInstance)
        {
            Url = url;
            PostData = postData;
            Headers = headers;
            Flags = flags;
            TargetFrameName = targetFrameName;
            WebBrowserActiveXInstance = webBrowserActiveXInstance;
        }

        /// <summary>
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// </summary>
        public byte[] PostData { get; }

        /// <summary>
        /// </summary>
        public string Headers { get; }

        /// <summary>
        /// </summary>
        public int Flags { get; }

        /// <summary>
        /// </summary>
        public string TargetFrameName { get; }

        /// <summary>
        /// </summary>
        public object WebBrowserActiveXInstance { get; }
    }
}
