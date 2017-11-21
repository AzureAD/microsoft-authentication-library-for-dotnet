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
using System.Globalization;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class Extensions
    {
        internal static string GetPiiScrubbedDetails(this Exception ex)
        {
            string res = null;
            if (ex != null)
            {
                var sb = new StringBuilder();

                sb.Append(string.Format(CultureInfo.CurrentCulture, "Exception type: {0}", ex.GetType()));

                if (ex is AdalException)
                {
                    var adalException = (AdalException) ex;
                    sb.Append(string.Format(CultureInfo.CurrentCulture, ", ErrorCode: {0}", adalException.ErrorCode));
                }

                if (ex is AdalServiceException)
                {
                    var adalServiceException  = (AdalServiceException) ex; 
                    sb.Append(string.Format(CultureInfo.CurrentCulture, ", StatusCode: {0}", adalServiceException.StatusCode));
                }

                if (ex.InnerException != null)
                {
                    sb.Append(" ---> " + GetPiiScrubbedDetails(ex.InnerException) + Environment.NewLine +
                              "--- End of inner exception stack trace ---");
                }
                if (ex.StackTrace != null)
                {
                    sb.Append(Environment.NewLine + ex.StackTrace);
                }

                res = sb.ToString();
            }

            return res;
        }
    }
}