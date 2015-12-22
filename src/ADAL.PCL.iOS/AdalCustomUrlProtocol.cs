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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public class AdalCustomUrlProtocol : NSUrlProtocol
    {
        private NSUrlConnection connection;

        [Export("canInitWithRequest:")]
        public static bool canInitWithRequest(NSUrlRequest request)
        {
            if (request.Url.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
            {
                return GetProperty("ADURLProtocol", request) == null;
            }

            return false;
        }

        [Export("canonicalRequestForRequest:")]
        public new static NSUrlRequest GetCanonicalRequest(NSUrlRequest request)
        {
            return request;
        }

        [Export("initWithRequest:cachedResponse:client:")]
        public AdalCustomUrlProtocol(NSUrlRequest request, NSCachedUrlResponse cachedResponse,
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
            SetProperty(new NSString("YES"), "ADURLProtocol", mutableRequest);
            this.connection = new NSUrlConnection(mutableRequest, new AdalCustomConnectionDelegate(this), true);
        }

        public override void StopLoading()
        {
            this.connection.Cancel();
        }

        private class AdalCustomConnectionDelegate : NSUrlConnectionDataDelegate
        {
            private AdalCustomUrlProtocol handler;

            public AdalCustomConnectionDelegate(AdalCustomUrlProtocol handler)
            {
                this.handler = handler;
            }

            public override void ReceivedData(NSUrlConnection connection, NSData data)
            {
                handler.Client.DataLoaded(handler, data);
            }

            public override void FailedWithError(NSUrlConnection connection, NSError error)
            {
                handler.Client.FailedWithError(handler, error);
                connection.Cancel();
            }

            public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
            {
                handler.Client.ReceivedResponse(handler, response, NSUrlCacheStoragePolicy.NotAllowed);
            }

            public override NSUrlRequest WillSendRequest(NSUrlConnection connection, NSUrlRequest request,
                NSUrlResponse response)
            {
                NSMutableUrlRequest mutableRequest = (NSMutableUrlRequest) request.MutableCopy();
                if (response != null)
                {
                    RemoveProperty("ADURLProtocol", mutableRequest);
                    handler.Client.Redirected(handler, mutableRequest, response);
                    connection.Cancel();
                    if (!request.Headers.ContainsKey(new NSString("x-ms-PkeyAuth")))
                    {
                        mutableRequest[BrokerConstants.ChallengeHeaderKey] = BrokerConstants.ChallengeHeaderValue;
                    }
                    return mutableRequest;
                }

                if (!request.Headers.ContainsKey(new NSString(BrokerConstants.ChallengeHeaderKey)))
                {
                    mutableRequest[BrokerConstants.ChallengeHeaderKey] = BrokerConstants.ChallengeHeaderValue;
                }

                return mutableRequest;
            }

            public override void FinishedLoading(NSUrlConnection connection)
            {
                handler.Client.FinishedLoading(handler);
                connection.Cancel();
            }
        }
    }
}
