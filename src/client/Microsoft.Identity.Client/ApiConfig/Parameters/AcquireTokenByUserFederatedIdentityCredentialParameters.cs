// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByUserFederatedIdentityCredentialParameters : IAcquireTokenParameters
    {
        public string Username { get; set; }
        public Guid? UserObjectId { get; set; }
        public string Assertion { get; set; }
        public bool? SendX5C { get; set; }
        public bool ForceRefresh { get; set; }

        /// <inheritdoc/>
        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                // PII-enabled message: includes actual Username and UserObjectId values
                var builder = new StringBuilder();
                builder.AppendLine("=== AcquireTokenByUserFederatedIdentityCredentialParameters ===");
                builder.AppendLine("SendX5C: " + SendX5C);
                builder.AppendLine("ForceRefresh: " + ForceRefresh);
                builder.AppendLine("Username: " + Username);
                builder.AppendLine("UserObjectId: " + UserObjectId);
                builder.AppendLine("Assertion set: " + !string.IsNullOrEmpty(Assertion));

                string messageWithPii = builder.ToString();

                // Non-PII message: redacts Username and UserObjectId to booleans
                builder = new StringBuilder();
                builder.AppendLine("=== AcquireTokenByUserFederatedIdentityCredentialParameters ===");
                builder.AppendLine("SendX5C: " + SendX5C);
                builder.AppendLine("ForceRefresh: " + ForceRefresh);
                builder.AppendLine("Username set: " + !string.IsNullOrEmpty(Username));
                builder.AppendLine("UserObjectId set: " + UserObjectId.HasValue);
                builder.AppendLine("Assertion set: " + !string.IsNullOrEmpty(Assertion));

                logger.InfoPii(messageWithPii, builder.ToString());
            }
        }
    }
}
