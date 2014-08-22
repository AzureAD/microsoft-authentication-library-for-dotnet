//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal partial class AdalTests
    {
        private static bool positiveCalled;
        private static bool negativeCalled;

        public static void AcquireTokenWithCallbackTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority)
                          {
                              AuthenticationContextDelegate = AuthenticationContextPositiveDelegate
                          };

            positiveCalled = false;
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            Verify.IsTrue(positiveCalled);
            VerifySuccessResult(sts, result);

            context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority)
            {
                AuthenticationContextDelegate = AuthenticationContextNegativeDelegate
            };

            negativeCalled = false;
            result = context.AcquireToken(sts.ValidResource, sts.InvalidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            Verify.IsTrue(negativeCalled);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
        }

        private static void AuthenticationContextPositiveDelegate(AuthenticationResult result)
        {
            Verify.IsNotNullOrEmptyString(result.AccessToken);
            Verify.IsNullOrEmptyString(result.Error);
            positiveCalled = true;
        }

        private static void AuthenticationContextNegativeDelegate(AuthenticationResult result)
        {
            Verify.IsNullOrEmptyString(result.AccessToken);
            Verify.IsNotNullOrEmptyString(result.Error);
            negativeCalled = true;
        }
    }
}
