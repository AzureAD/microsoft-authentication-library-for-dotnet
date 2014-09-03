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
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class AcquireTokenInteractiveHandler
    {
        protected override Task PreTokenRequest()
        {
            base.PreTokenRequest();

            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            this.AcquireAuthorization();
            this.VerifyAuthorizationResult();

            return CompletedTask;
        }

        internal void AcquireAuthorization()
        {
            var sendAuthorizeRequest = new Action(
                delegate
                {
                    Uri authorizationUri = this.CreateAuthorizationUri(IncludeFormsAuthParams());
                    string resultUri = this.webUi.Authenticate(authorizationUri, this.redirectUri);
                    this.authorizationResult = OAuth2Response.ParseAuthorizeResponse(resultUri, this.CallState);
                });

            // If the thread is MTA, it cannot create or communicate with WebBrowser which is a COM control.
            // In this case, we have to create the browser in an STA thread via StaTaskScheduler object.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                using (var staTaskScheduler = new StaTaskScheduler(1))
                {
                    Task.Factory.StartNew(sendAuthorizeRequest, CancellationToken.None, TaskCreationOptions.None, staTaskScheduler).Wait();
                }
            }
            else
            {
                sendAuthorizeRequest();
            }
        }

        internal static bool IncludeFormsAuthParams()
        {
            return IsUserLocal() && IsDomainJoined();
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(Guid correlationId)
        {
            this.CallState.CorrelationId = correlationId;
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState);
            return this.CreateAuthorizationUri(false);
        }

        private static bool IsDomainJoined()
        {
            bool returnValue = false;
            IntPtr pDomain = IntPtr.Zero;
            try
            {
                NativeMethods.NetJoinStatus status = NativeMethods.NetJoinStatus.NetSetupUnknownStatus;
                int result = NativeMethods.NetGetJoinInformation(null, out pDomain, out status);
                if (pDomain != IntPtr.Zero)
                {
                    NativeMethods.NetApiBufferFree(pDomain);
                }

                returnValue = result == NativeMethods.ErrorSuccess &&
                              status == NativeMethods.NetJoinStatus.NetSetupDomainName;
            }
            catch (Exception)
            {
                // ignore the exception as the result is already set to false;
            }
            finally
            {
                pDomain = IntPtr.Zero;
            }
            return returnValue;
        }

        private static bool IsUserLocal()
        {
            string prefix = WindowsIdentity.GetCurrent().Name.Split('\\')[0].ToUpperInvariant();
            return prefix.Equals(Environment.MachineName.ToUpperInvariant());
        }

        private void SetRedirectUriRequestParameter()
        {
            this.redirectUriRequestParameter = redirectUri.AbsoluteUri;
        }

        private static class NativeMethods
        {
            public const int ErrorSuccess = 0;

            [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

            [DllImport("Netapi32.dll")]
            public static extern int NetApiBufferFree(IntPtr Buffer);

            public enum NetJoinStatus
            {
                NetSetupUnknownStatus = 0,
                NetSetupUnjoined,
                NetSetupWorkgroupName,
                NetSetupDomainName
            }
        }
    }
}
