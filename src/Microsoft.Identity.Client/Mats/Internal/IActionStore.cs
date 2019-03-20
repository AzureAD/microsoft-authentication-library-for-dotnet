// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

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
        Wam
    }

    internal interface IActionStore
    {
        /** ADAL action */
        AdalAction StartAdalAction(Scenario scenario, string correlationId, string resource);

        void ProcessAdalTelemetryBlob(IDictionary<string, string> blob);

        void EndAdalAction(AdalAction action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription);

        IEnumerable<IPropertyBag> GetEventsForUpload();
    }
}
