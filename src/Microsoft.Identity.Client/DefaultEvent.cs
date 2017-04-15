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

using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class DefaultEvent : EventBase
    {
        public DefaultEvent(string clientId) : base((string) (EventBase.EventNamePrefix + "default_event"))
        {
            this["client_id"] = clientId;
            this["sdk_platform"] = PlatformPlugin.PlatformInformation.GetProductName();
            this["sdk_version"] = MsalIdHelper.GetMsalVersion();
            // TODO: The following implementation will be used after the 3 helpers being implemented (in a separated PR)
            // this["application_name"] = MsalIdHelper.GetApplicationName();  // Not yet implemented
            // this["application_version"] = MsalIdHelper.GetApplicationVersion();  // Not yet implemented
            // this["device_id"] = MsalIdHelper.GetDeviceId();  // Not yet implemented
        }
    }
}