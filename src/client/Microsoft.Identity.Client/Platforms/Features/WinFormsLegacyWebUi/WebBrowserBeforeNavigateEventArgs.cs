// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    /// <summary>
    /// Event arguments for <c>BeforeNavigate</c> event.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <c>WebBrowserBeforeNavigateEventArgs</c>.
    /// </remarks>
    public class WebBrowserBeforeNavigateEventArgs(
        string url,
        byte[] postData,
        string headers,
        int flags,
        string targetFrameName,
        object webBrowserActiveXInstance) : CancelEventArgs
    {

        /// <summary>
        /// The URL to be navigated to.
        /// </summary>
        public string Url { get; } = url;

        /// <summary>
        /// The data to send to the server, if the HTTP POST transaction is used.
        /// </summary>
        public byte[] PostData { get; } = postData;

        /// <summary>
        /// Additional HTTP headers to send to the server
        /// </summary>
        public string Headers { get; } = headers;

        /// <summary>
        /// The following flag, or zero.
        /// beforeNavigateExternalFrameTarget (H0001)
        /// Internet Explorer 7 or later. This navigation is the result of 
        /// an external window or tab that targets this browser.
        /// </summary>
        public int Flags { get; } = flags;

        /// <summary>
        /// The name of the frame in which to display the resource,
        /// or null if no named frame is targeted for the resource.
        /// </summary>
        public string TargetFrameName { get; } = targetFrameName;

        /// <summary>
        /// A pointer to the IDispatch interface for the WebBrowserControl object that represents the window or frame.
        /// </summary>
        public object WebBrowserActiveXInstance { get; } = webBrowserActiveXInstance;
    }
}
