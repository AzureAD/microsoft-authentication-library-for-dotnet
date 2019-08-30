// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Foundation;

namespace Microsoft.Identity.Client.Platforms.Shared.Apple
{
    internal class CoreCustomUrlProtocol : NSUrlProtocol
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
        public CoreCustomUrlProtocol(
            NSUrlRequest request,
            NSCachedUrlResponse cachedResponse,
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
            this.connection = new NSUrlConnection(mutableRequest, new CoreCustomConnectionDelegate(this), true);
        }

        public override void StopLoading()
        {
            this.connection.Cancel();
        }

        private class CoreCustomConnectionDelegate : NSUrlConnectionDataDelegate
        {
            private readonly CoreCustomUrlProtocol handler;
            private readonly INSUrlProtocolClient client;

            public CoreCustomConnectionDelegate(CoreCustomUrlProtocol handler)
            {
                this.handler = handler;
                client = handler.Client;
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
