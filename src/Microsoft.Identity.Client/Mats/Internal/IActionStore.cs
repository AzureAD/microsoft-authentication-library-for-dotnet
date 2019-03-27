// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal enum WamIdentityService
    {
        Msa,
        Aad
    }

    internal enum WamApi
    {
        RequestToken,
        GetTokenSilently,
        Other
    }

    internal enum ActionType
    {
        Adal,
        CustomInteractive,
        MsaInteractive,
        MsaNonInteractive,
        Wam,
        Msal
    }

    internal interface IActionStore
    {
        MatsAction StartMsalAction(MatsScenario scenario, string correlationId, IEnumerable<string> scopes);
        void ProcessMsalTelemetryBlob(IDictionary<string, string> blob);
        void EndMsalAction(MatsAction action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription);
        IEnumerable<IPropertyBag> GetEventsForUpload();
    }
}
