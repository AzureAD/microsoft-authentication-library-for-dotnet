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

using System.Collections.Generic;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal enum NavigateErrorStatusCode
    {
        HTTP_STATUS_BAD_REQUEST = 400, 
        HTTP_STATUS_DENIED = 401, 
        HTTP_STATUS_PAYMENT_REQ = 402, 
        HTTP_STATUS_FORBIDDEN = 403, 
        HTTP_STATUS_NOT_FOUND = 404, 
        HTTP_STATUS_BAD_METHOD = 405, 
        HTTP_STATUS_NONE_ACCEPTABLE = 406, 
        HTTP_STATUS_PROXY_AUTH_REQ = 407, 
        HTTP_STATUS_REQUEST_TIMEOUT = 408, 
        HTTP_STATUS_CONFLICT = 409, 
        HTTP_STATUS_GONE = 410, 
        HTTP_STATUS_LENGTH_REQUIRED = 411, 
        HTTP_STATUS_PRECOND_FAILED = 412, 
        HTTP_STATUS_REQUEST_TOO_LARGE = 413, 
        HTTP_STATUS_URI_TOO_LONG = 414, 
        HTTP_STATUS_UNSUPPORTED_MEDIA = 415, 
        HTTP_STATUS_RETRY_WITH = 449, 
        HTTP_STATUS_SERVER_ERROR = 500, 
        HTTP_STATUS_NOT_SUPPORTED = 501, 
        HTTP_STATUS_BAD_GATEWAY = 502, 
        HTTP_STATUS_SERVICE_UNAVAIL = 503, 
        HTTP_STATUS_GATEWAY_TIMEOUT = 504, 
        HTTP_STATUS_VERSION_NOT_SUP = 505, 
        INET_E_INVALID_URL = -2146697214, 
        INET_E_NO_SESSION = -2146697213, 
        INET_E_CANNOT_CONNECT = -2146697212, 
        INET_E_RESOURCE_NOT_FOUND = -2146697211, 
        INET_E_OBJECT_NOT_FOUND = -2146697210, 
        INET_E_DATA_NOT_AVAILABLE = -2146697209, 
        INET_E_DOWNLOAD_FAILURE = -2146697208, 
        INET_E_AUTHENTICATION_REQUIRED = -2146697207, 
        INET_E_NO_VALID_MEDIA = -2146697206, 
        INET_E_CONNECTION_TIMEOUT = -2146697205, 
        INET_E_INVALID_REQUEST = -2146697204, 
        INET_E_UNKNOWN_PROTOCOL = -2146697203, 
        INET_E_SECURITY_PROBLEM = -2146697202, 
        INET_E_CANNOT_LOAD_DATA = -2146697201, 
        INET_E_CANNOT_INSTANTIATE_OBJECT = -2146697200, 
        INET_E_REDIRECT_FAILED = -2146697196, 
        INET_E_REDIRECT_TO_DIR = -2146697195, 
        INET_E_CANNOT_LOCK_REQUEST = -2146697194, 
        INET_E_USE_EXTEND_BINDING = -2146697193, 
        INET_E_TERMINATED_BIND = -2146697192, 
        INET_E_INVALID_CERTIFICATE = -2146697191, 
        INET_E_CODE_DOWNLOAD_DECLINED = -2146696960, 
        INET_E_RESULT_DISPATCHED = -2146696704, 
        INET_E_CANNOT_REPLACE_SFP_FILE = -2146696448, 
        INET_E_CODE_INSTALL_BLOCKED_BY_HASH_POLICY = -2146695936, 
        INET_E_CODE_INSTALL_SUPPRESSED = -2146696192
    }

    internal class NavigateErrorStatus
    {
        public Dictionary<int, string> Messages;

        public NavigateErrorStatus()
        {
            this.Messages = new Dictionary<int, string>
                {
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_BAD_REQUEST, "The request could not be processed by the server due to invalid syntax." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_DENIED, "The requested resource requires user authentication." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_PAYMENT_REQ, "Not currently implemented in the HTTP protocol." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_FORBIDDEN, "The server understood the request, but is refusing to fulfill it." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_NOT_FOUND, "The server has not found anything matching the requested URI (Uniform Resource Identifier)." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_BAD_METHOD, "The HTTP verb used is not allowed." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_NONE_ACCEPTABLE, "No responses acceptable to the client were found." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_PROXY_AUTH_REQ, "Proxy authentication required." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_REQUEST_TIMEOUT, "The server timed out waiting for the request." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_CONFLICT, "The request could not be completed due to a conflict with the current state of the resource. The user should resubmit with more information." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_GONE, "The requested resource is no longer available at the server, and no forwarding address is known." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_LENGTH_REQUIRED, "The server refuses to accept the request without a defined content length." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_PRECOND_FAILED, "The precondition given in one or more of the request header fields evaluated to false when it was tested on the server." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_REQUEST_TOO_LARGE, "The server is refusing to process a request because the request entity is larger than the server is willing or able to process." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_URI_TOO_LONG, "The server is refusing to service the request because the request URI (Uniform Resource Identifier) is longer than the server is willing to interpret." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_UNSUPPORTED_MEDIA, "The server is refusing to service the request because the entity of the request is in a format not supported by the requested resource for the requested method." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_RETRY_WITH, "The request should be retried after doing the appropriate action." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_SERVER_ERROR, "The server encountered an unexpected condition that prevented it from fulfilling the request." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_NOT_SUPPORTED, "The server does not support the functionality required to fulfill the request." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_BAD_GATEWAY, "The server, while acting as a gateway or proxy, received an invalid response from the upstream server it accessed in attempting to fulfill the request." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_SERVICE_UNAVAIL, "The service is temporarily overloaded." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_GATEWAY_TIMEOUT, "The request was timed out waiting for a gateway." },
                    { (int)NavigateErrorStatusCode.HTTP_STATUS_VERSION_NOT_SUP, "The server does not support, or refuses to support, the HTTP protocol version that was used in the request message." },

                    { (int)NavigateErrorStatusCode.INET_E_AUTHENTICATION_REQUIRED, "Authentication is needed to access the object." },
                    { (int)NavigateErrorStatusCode.INET_E_CANNOT_CONNECT, "The attempt to connect to the Internet has failed." },
                    { (int)NavigateErrorStatusCode.INET_E_CANNOT_INSTANTIATE_OBJECT, "CoCreateInstance failed." },
                    { (int)NavigateErrorStatusCode.INET_E_CANNOT_LOAD_DATA, "The object could not be loaded." },
                    { (int)NavigateErrorStatusCode.INET_E_CANNOT_LOCK_REQUEST, "The requested resource could not be locked." },
                    { (int)NavigateErrorStatusCode.INET_E_CANNOT_REPLACE_SFP_FILE, "Cannot replace a file that is protected by SFP." },
                    { (int)NavigateErrorStatusCode.INET_E_CODE_DOWNLOAD_DECLINED, "The component download was declined by the user." },
                    { (int)NavigateErrorStatusCode.INET_E_CODE_INSTALL_SUPPRESSED, "Internet Explorer 6 for Windows XP SP2 and later. The Authenticode prompt for installing a ActiveX control was not shown because the page restricts the installation of the ActiveX controls. The usual cause is that the Information Bar is shown instead of the Authenticode prompt." },
                    { (int)NavigateErrorStatusCode.INET_E_CODE_INSTALL_BLOCKED_BY_HASH_POLICY, "Internet Explorer 6 for Windows XP SP2 and later. Installation of ActiveX control (as identified by cryptographic file hash) has been disallowed by registry key policy." },
                    { (int)NavigateErrorStatusCode.INET_E_CONNECTION_TIMEOUT, "The Internet connection has timed out." },
                    { (int)NavigateErrorStatusCode.INET_E_DATA_NOT_AVAILABLE, "An Internet connection was established, but the data cannot be retrieved." },
                    { (int)NavigateErrorStatusCode.INET_E_DOWNLOAD_FAILURE, "The download has failed (the connection was interrupted)." },
                    { (int)NavigateErrorStatusCode.INET_E_INVALID_CERTIFICATE, "The SSL certificate is invalid." }, 
                    { (int)NavigateErrorStatusCode.INET_E_INVALID_REQUEST, "The request was invalid." },
                    { (int)NavigateErrorStatusCode.INET_E_INVALID_URL, "The URL could not be parsed." },
                    { (int)NavigateErrorStatusCode.INET_E_NO_SESSION, "No Internet session was established." },
                    { (int)NavigateErrorStatusCode.INET_E_NO_VALID_MEDIA, "The object is not in one of the acceptable MIME types." },
                    { (int)NavigateErrorStatusCode.INET_E_OBJECT_NOT_FOUND, "The object was not found." },
                    { (int)NavigateErrorStatusCode.INET_E_REDIRECT_FAILED, "WinInet cannot redirect. This error code might also be returned by a custom protocol handler." },
                    { (int)NavigateErrorStatusCode.INET_E_REDIRECT_TO_DIR, "The request is being redirected to a directory." },
                    { (int)NavigateErrorStatusCode.INET_E_RESOURCE_NOT_FOUND, "The server or proxy was not found." },
                    { (int)NavigateErrorStatusCode.INET_E_RESULT_DISPATCHED, "The binding has already been completed and the result has been dispatched, so your abort call has been canceled." },
                    { (int)NavigateErrorStatusCode.INET_E_SECURITY_PROBLEM, "A security problem was encountered." }, 
                    { (int)NavigateErrorStatusCode.INET_E_TERMINATED_BIND, "Binding was terminated. (See IBinding::GetBindResult.)" },
                    { (int)NavigateErrorStatusCode.INET_E_UNKNOWN_PROTOCOL, "The protocol is not known and no pluggable protocols have been entered that match." },
                    { (int)NavigateErrorStatusCode.INET_E_USE_EXTEND_BINDING, "(Microsoft internal.) Reissue request with extended binding." } 
                };
        }
    }
}
