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


using System;
using CoreFoundation;
using Foundation;
#if MAC
using INSUrlProtocolClient = Foundation.NSUrlProtocolClient;
#endif

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AdalCustomUrlProtocol : NSUrlProtocol
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
            private INSUrlProtocolClient client;

            public AdalCustomConnectionDelegate(AdalCustomUrlProtocol handler)
            {
                this.handler = handler;
#if MAC
                client = (INSUrlProtocolClient)handler.WeakClient;
#else
                client = handler.Client;
#endif
            }

            public override void ReceivedData(NSUrlConnection connection, NSData data)
            {
                client.DataLoaded(handler, data);
            }

            public override void FailedWithError(NSUrlConnection connection, NSError error)
            {
                client.FailedWithError(handler, error);
                connection.Cancel();
            }

            public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
            {
                client.ReceivedResponse(handler, response, NSUrlCacheStoragePolicy.NotAllowed);
            }

            public override NSUrlRequest WillSendRequest(NSUrlConnection connection, NSUrlRequest request,
                NSUrlResponse response)
            {
                NSMutableUrlRequest mutableRequest = (NSMutableUrlRequest) request.MutableCopy();
                if (response != null)
                {
                    RemoveProperty("ADURLProtocol", mutableRequest);
                    client.Redirected(handler, mutableRequest, response);
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
                client.FinishedLoading(handler);
                connection.Cancel();
            }
        }
    }
}
