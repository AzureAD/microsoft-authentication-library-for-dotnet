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
using Foundation;

namespace Microsoft.Identity.Client
{
    internal class MsalCustomUrlProtocol : NSUrlProtocol
    {
        private NSUrlConnection connection;

        [Export("initWithRequest:cachedResponse:client:")]
        public MsalCustomUrlProtocol(NSUrlRequest request, NSCachedUrlResponse cachedResponse,
            INSUrlProtocolClient client)
            : base(request, cachedResponse, client)
        {
        }

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