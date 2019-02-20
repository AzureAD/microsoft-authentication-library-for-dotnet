//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabApiConstants
    {
        // constants for Lab api
        public const string MobileDeviceManagementWithConditionalAccess = "mdmca";
        public const string MobileAppManagementWithConditionalAccess = "mamca";
        public const string MobileAppManagement = "mam";
        public const string MultiFactorAuthentication = "mfa";
        public const string License = "license";
        public const string FederationProvider = "federationProvider";
        public const string FederatedUser = "isFederated";
        public const string UserType = "usertype";
        public const string External = "external";
        public const string B2CProvider = "b2cProvider";
        public const string B2CLocal = "local";
        public const string B2CFacebook = "facebook";
        public const string B2CGoogle = "google";
        public const string UserContains = "usercontains";
        public const string AppName = "AppName";
        public const string UserSearchQuery = "usercontains";
        public const string MSAOutlookAccount = "MSIDLAB4_Outlook";
        public const string MSAOutlookAccountClientID = "9668f2bd-6103-4292-9024-84fa2d1b6fb2";

        public const string True = "true";
        public const string False = "false";

        public const string BetaEndpoint = "http://api.msidlab.com/api/userbeta";
        public const string LabEndpoint = "http://api.msidlab.com/api/user";
    }
}