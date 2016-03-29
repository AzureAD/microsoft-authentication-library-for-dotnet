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
using CoreFoundation;
using Foundation;

namespace Microsoft.Identity.Client
{
    internal class MsalCustomUrlProtocol : NSUrlProtocol
    {
        private NSUrlConnection connection;

        [Export("canInitWithRequest:")]
        public new static bool CanInitWithRequest(NSUrlRequest request)
        {
            if (request.Url.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
            {
                return GetProperty("MsalCustomUrlProtocol", request) == null;
            }

            return false;
        }

        [Export("canonicalRequestForRequest:")]
        public new static NSUrlRequest GetCanonicalRequest(NSUrlRequest request)
        {
            return request;
        }

        [Export("initWithRequest:cachedResponse:client:")]
        public MsalCustomUrlProtocol(NSUrlRequest request, NSCachedUrlResponse cachedResponse,
            INSUrlProtocolClient client)
            : base(request, cachedResponse, client)
        {
        }

        public override void StartLoading()
        {
            if (this.Request == null)
            {
                return;
            }

            NSMutableUrlRequest mutableRequest = (NSMutableUrlRequest) this.Request.MutableCopy();
            SetProperty(new NSString("YES"), "MsalCustomUrlProtocol", mutableRequest);
            this.connection = new NSUrlConnection(mutableRequest, new MsalCustomConnectionDelegate(this), true);
        }

        public override void StopLoading()
        {
            this.connection.Cancel();
        }

        private class MsalCustomConnectionDelegate : NSUrlConnectionDataDelegate
        {
            private readonly MsalCustomUrlProtocol _handler;

            public MsalCustomConnectionDelegate(MsalCustomUrlProtocol handler)
            {
                this._handler = handler;
            }

            public override void ReceivedData(NSUrlConnection connection, NSData data)
            {
                _handler.Client.DataLoaded(_handler, data);
            }

            public override void FailedWithError(NSUrlConnection connection, NSError error)
            {
                _handler.Client.FailedWithError(_handler, error);
                connection.Cancel();
            }

            public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
            {
                _handler.Client.ReceivedResponse(_handler, response, NSUrlCacheStoragePolicy.NotAllowed);
            }

            public override NSUrlRequest WillSendRequest(NSUrlConnection connection, NSUrlRequest request,
                NSUrlResponse response)
            {
                NSMutableUrlRequest mutableRequest = (NSMutableUrlRequest) request.MutableCopy();
                CustomHeaderHandler.ApplyHeadersTo(mutableRequest);

                if (response != null)
                {
                    RemoveProperty("MsalCustomUrlProtocol", mutableRequest);
                    _handler.Client.Redirected(_handler, mutableRequest, response);
                }

                return mutableRequest;
            }

            public override void FinishedLoading(NSUrlConnection connection)
            {
                _handler.Client.FinishedLoading(_handler);
                connection.Cancel();
            }
        }
    }
}
