//------------------------------------------------------------------------------
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

namespace Microsoft.Identity.Core.Telemetry
{
    internal class XmsCliTelemInfo
    {
        /// <summary>
        /// Monotonically increasing integer specifying 
        /// x-ms-cliteleminfo header version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Bundle id for server error.
        /// </summary>
        public string ServerErrorCode { get; set; }

        /// <summary>
        /// Bundle id for server suberror.
        /// </summary>
        public string ServerSubErrorCode { get; set; }

        /// <summary>
        /// Bundle id for refresh token age.
        /// Floating-point value with a unit of milliseconds 
        /// </summary>
        public string TokenAge { get; set; }

        /// <summary>
        /// Bundle id for spe_ring info. Indicates whether the request was executed 
        /// on a ring serving SPE traffic. An empty string indicates this occurred on 
        /// an outer ring, and the string "I" indicates the request occurred on the 
        /// inner ring
        /// </summary>
        public string SpeInfo { get; set; }
    }
}
