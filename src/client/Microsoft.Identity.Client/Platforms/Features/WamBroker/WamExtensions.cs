// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
#if NET5_WIN
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
    internal static class WamExtensions
    {
        public static bool IsSuccessStatus(this WebTokenRequestStatus status)
        {
            return status == WebTokenRequestStatus.Success ||
                status == WebTokenRequestStatus.AccountSwitch;
        }

        public static string ToLogString(this WebTokenRequest webTokenRequest, bool pii)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("=== WebTokenRequest ===");
            stringBuilder.AppendLine($"ClientId: {webTokenRequest?.ClientId}");            
            stringBuilder.AppendLine($"PromptType: {webTokenRequest?.PromptType}");
            stringBuilder.AppendLine($"Scope: {webTokenRequest?.Scope}");
            stringBuilder.AppendLine($"Properties.Count: {webTokenRequest?.Properties?.Count ?? 0}");

            if (pii)
            {
                foreach (var prop in webTokenRequest?.Properties)
                {
                    stringBuilder.AppendLine($"webTokenRequest.Property: {prop.Key}: {prop.Value}");
                }
            }

            stringBuilder.AppendLine($"WebAccountProvider: {webTokenRequest?.WebAccountProvider.ToLogString(pii)}");

            return stringBuilder.ToString();
        }

        public static string ToLogString(this WebAccountProvider webAccountProvider, bool pii)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"=== WebAccountProvider ===");
            stringBuilder.AppendLine($"Authority {webAccountProvider?.Authority}");
            stringBuilder.AppendLine($"DisplayName {webAccountProvider?.DisplayName}");
            stringBuilder.AppendLine($"Id {webAccountProvider?.Id}");

            return stringBuilder.ToString();
        }

        public static string ToLogString(this WebAccount webAccount, bool pii)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"=== WebAccount ===");

            stringBuilder.AppendLine($"Id {webAccount?.Id}");
            stringBuilder.AppendLine($"State {webAccount?.State}");
            if (pii)
            {
                stringBuilder.AppendLine($"UserName {webAccount?.UserName}");

                foreach (var prop in webAccount?.Properties)
                {
                    stringBuilder.AppendLine($"webAccount.Property: {prop.Key}: {prop.Value}");
                }                
            }

            stringBuilder.AppendLine($"WebAccountProvider: {webAccount?.WebAccountProvider.ToLogString(pii)}");

            return stringBuilder.ToString();
        }
    }
}
