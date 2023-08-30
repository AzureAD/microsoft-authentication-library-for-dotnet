// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// Exception that results when trying to persist data to the underlying OS mechanism (KeyRing, KeyChain, DPAPI)
    /// Inspect inner exception for details.
    /// </summary>
    public class MsalCachePersistenceException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public MsalCachePersistenceException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public MsalCachePersistenceException(string message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MsalCachePersistenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MsalCachePersistenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
