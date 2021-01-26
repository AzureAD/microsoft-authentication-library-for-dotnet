using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal static class WamExtensions
    {
        public static bool IsSuccessStatus(this WebTokenRequestStatus status)
        {
            return status == WebTokenRequestStatus.Success ||
                status == WebTokenRequestStatus.AccountSwitch;
        }

        public static string ToLogString(this WebTokenRequest webTokenRequest)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("=== WebTokenRequest ===");
            stringBuilder.AppendLine($"ClientId: {webTokenRequest?.ClientId}");
            stringBuilder.AppendLine($"CorrelationId: {webTokenRequest?.CorrelationId}");
            stringBuilder.AppendLine($"PromptType: {webTokenRequest?.PromptType}");
            stringBuilder.AppendLine($"Scope: {webTokenRequest?.Scope}");
            stringBuilder.AppendLine($"Properties.Count: {webTokenRequest?.Properties?.Count ?? 0}");
            foreach (var prop in webTokenRequest?.Properties)
            {
                stringBuilder.AppendLine($"webTokenRequest.Property: {prop.Key}: {prop.Value}");
            }

            stringBuilder.AppendLine($"WebAccountProvider: {webTokenRequest?.WebAccountProvider.ToLogString()}");


            return stringBuilder.ToString();
        }

        public static string ToLogString(this WebAccountProvider webAccountProvider)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"=== WebAccountProvider ===");
            stringBuilder.AppendLine($"Authority {webAccountProvider?.Authority}");
            stringBuilder.AppendLine($"DisplayName {webAccountProvider?.DisplayName}");
            stringBuilder.AppendLine($"Id {webAccountProvider?.Id}");

            return stringBuilder.ToString();
        }

        public static string ToLogString(this WebAccount webAccount)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"=== WebAccount ===");

            stringBuilder.AppendLine($"Id {webAccount?.Id}");
            stringBuilder.AppendLine($"UserName {webAccount?.UserName}");
            stringBuilder.AppendLine($"State {webAccount?.State}");
            foreach (var prop in webAccount?.Properties)
            {
                stringBuilder.AppendLine($"webAccount.Property: {prop.Key}: {prop.Value}");
            }

            stringBuilder.AppendLine($"WebAccountProvider: {webAccount?.WebAccountProvider.ToLogString()}");

            return stringBuilder.ToString();
        }
    }
}
