// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    ///     Options object with string values loadable from JSON configuration (as in an asp.net configuration scenario)
    /// </summary>
    public abstract class ApplicationOptions
    {
        /// <summary>
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Can be domain, can be guid tenant, can be meta-tenant (e.g. consumers).
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Mutually exclusive with TenantId...
        /// </summary>
        public AadAuthorityAudience AadAuthorityAudience { get; set; } = AadAuthorityAudience.None;

        /// <summary>
        /// For compat with AzureAdOptions...
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Mutually exclusive with Instance, allows users to use the enum instead of the explicit url.
        /// </summary>
        public AzureCloudInstance AzureCloudInstance { get; set; } = AzureCloudInstance.None;

        /// <summary>
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// </summary>
        public bool EnablePiiLogging { get; set; }

        /// <summary>
        /// </summary>
        public bool IsDefaultPlatformLoggingEnabled { get; set; }

        /// <summary>
        /// </summary>
        public string SliceParameters { get; set; }

        /// <summary>
        ///     TODO: do we have a better / more descriptive name for this?
        /// </summary>
        public string Component { get; set; }
    }
}