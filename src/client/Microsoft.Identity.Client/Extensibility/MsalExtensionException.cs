// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    ///
    /// </summary>
    public class MsalExtensionException : MsalException
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public MsalExtensionException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MsalExtensionException(string message, Exception innerException)
            : base(message, string.Empty, innerException)
        {
        }
    }
}
