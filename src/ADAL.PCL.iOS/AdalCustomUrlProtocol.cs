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


using Foundation;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public class AdalCustomUrlProtocol : NSUrlProtocol
    {
        [Export("canInitWithRequest:")]
        public static bool canInitWithRequest(NSUrlRequest request)
        {
            if (null != request.Headers && request.Headers.ContainsKey(NSObject.FromObject("PKeyAuth")))
            {
                return false; // request has already been handled
            }
            return true;
        }

        [Export("canonicalRequestForRequest:")]
        public new static NSUrlRequest GetCanonicalRequest(NSUrlRequest request)
        {
            return request;
        }

        [Export("initWithRequest:cachedResponse:client:")]
        public AdalCustomUrlProtocol(NSUrlRequest request, NSCachedUrlResponse cachedResponse, INSUrlProtocolClient client)
            : base(request, cachedResponse, client)
        {
        }

        public override NSUrlRequest Request
        {
            get
            {
                // inject the HTTP header
                NSMutableDictionary headers = null;
                if (null == base.Request.Headers)
                {
                    headers = new NSMutableDictionary();
                }
                else
                {
                    headers = new NSMutableDictionary(base.Request.Headers);
                }
                headers.Add(NSObject.FromObject("PKeyAuth"), NSObject.FromObject("1.0"));
                NSMutableUrlRequest newRequest = (NSMutableUrlRequest) base.Request.MutableCopy();
                newRequest.Headers = headers;
                return newRequest;
            }
        }

        public override void StartLoading()
        {
            new NSUrlConnection(Request, new AdalCustomConnectionDelegate(this), true);
        }

        public override void StopLoading()
        {
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

            public override void FinishedLoading(NSUrlConnection connection)
            {
                handler.Client.FinishedLoading(handler);
                connection.Cancel();
            }
        }
    }
}
