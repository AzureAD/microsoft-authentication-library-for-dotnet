// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal enum ErrorType
    {
        Uninitialized,
        Scenario,
        Action,
        Other
    }

    internal enum ErrorSeverity
    {
        Warning,
        LibraryError
    }

    internal interface IErrorStore
    {
        void ReportError(string errorMessage, ErrorType errorType, ErrorSeverity errorSeverity);
        IEnumerable<IPropertyBag> GetEventsForUpload();
        void Append(IErrorStore errorStore);
        void Clear();
    }
}
