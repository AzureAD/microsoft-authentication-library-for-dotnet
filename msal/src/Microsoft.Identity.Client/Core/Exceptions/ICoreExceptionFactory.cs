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

using Microsoft.Identity.Core.Http;
using System;

namespace Microsoft.Identity.Core
{
    /// <summary>
    /// Factory for creating ADAL or MSAL exceptions.
    /// ErrorCodes should be made public constants for users to reference them
    /// </summary>
    internal interface ICoreExceptionFactory
    {
        /// <summary>
        /// Create a client exception, arising from logic within this library.
        /// </summary>
        Exception GetClientException(
            string errorCode,
            string errorMessage,
            Exception innerException = null);

        /// <summary>
        /// Create a service exception, arising from logic external to the library, e.g. a failing web request. 
        /// </summary>
        /// <remarks>Prefer using the constructor taking in an <see cref="IHttpWebResponse"/> for http service errors</remarks>
        Exception GetServiceException(
           string errorCode,
           string errorMessage,
           ExceptionDetail exceptionDetail);

        /// <summary>
        /// Create a service exception, aristing from a failed http request.
        /// </summary>
        Exception GetServiceException(
          string errorCode,
          string errorMessage,
          IHttpWebResponse httpResponse);

        /// <summary>
        /// Create a service exception that wraps another exception. 
        /// </summary>
        Exception GetServiceException(
           string errorCode,
           string errorMessage,
           Exception innerException,
           ExceptionDetail exceptionDetail);

        Exception GetUiRequiredException(
           string errorCode,
           string errorMessage,
           Exception innerException,
           ExceptionDetail exceptionDetail);

        string GetPiiScrubbedDetails(Exception exception);
    }
}
