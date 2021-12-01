﻿using System;
namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This exception is thrown when Intune requires App protection policy.
    /// Properties in it can be used by app to obtain the required enrollmentID from MAM SDK
    /// </summary>
    public class IntuneAppProtectionPolicyRequiredException : MsalServiceException
    {
        /// <summary>
        /// UPN of the user
        /// </summary>
        public string Upn { get; internal set; }

        /// <summary>
        /// Local account id
        /// </summary>
        public string AccountUserId { get; internal set; }

        /// <summary>
        /// Tenant ID of the App
        /// </summary>
        public string TenantId { get; internal set; }

        /// <summary>
        /// AUthority URL
        /// </summary>
        public string AuthorityUrl { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code and error message.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by the client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        public IntuneAppProtectionPolicyRequiredException(string errorCode, string errorMessage) :
            base(errorCode, errorMessage, null)
        {
        }
    }
}
