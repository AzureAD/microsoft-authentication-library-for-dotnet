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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;

namespace Test.ADAL.WinPhone.Unit
{
    internal class ReplayerWebAuthenticationBrokerContinuationEventArgs : IWebAuthenticationBrokerContinuationEventArgs
    {
        public AuthorizationResult AuthorizationResult { get; set; }

        public WebAuthenticationResult WebAuthenticationResult
        {
            get { throw new NotImplementedException(); }
        }

        public ValueSet ContinuationData { get; set; }

        public ActivationKind Kind
        {
            get { throw new NotImplementedException(); }
        }

        public ApplicationExecutionState PreviousExecutionState
        {
            get { throw new NotImplementedException(); }
        }

        public SplashScreen SplashScreen
        {
            get { throw new NotImplementedException(); }
        }
    }

}
