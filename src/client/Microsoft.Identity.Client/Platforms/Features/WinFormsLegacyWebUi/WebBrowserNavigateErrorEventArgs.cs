// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class WebBrowserNavigateErrorEventArgs(string url, string targetFrameName, int statusCode,
        object webBrowserActiveXInstance) : CancelEventArgs
    {

        /// <summary>
        /// </summary>
        public string TargetFrameName { get; } = targetFrameName;

        // URL as a string, as in case of error it could be invalid URL
        /// <summary>
        /// </summary>
        public string Url { get; } = url;

        /// <summary>
        /// </summary>
        public object WebBrowserActiveXInstance { get; } = webBrowserActiveXInstance;

        /// <summary>
        /// </summary>
        public int StatusCode { get; } = statusCode;
    }
}
