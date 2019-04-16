// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.UIAutomation.Infrastructure
{
#pragma warning disable 1032  // add constructor for exception that takes message
    [Serializable]
    public class ResultVerificationFailureException : Exception
    {
        public VerificationError Error { get; private set; }

        public ResultVerificationFailureException(VerificationError error)
        {
            Error = error;
        }
    }
#pragma warning restore 1032

    public enum VerificationError
    {
        ResultNotFound,
        ResultIndicatesFailure
    }
}
