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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
#if ADAL_NET
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The exception type thrown when a token cannot be acquired silently.
    /// </summary>
    [Serializable]
    public class AdalSilentTokenAcquisitionException : AdalException
#else
    class AdalSilentTokenAcquisitionException : AdalException
#endif
    {
        /// <summary>
        ///  Initializes a new instance of the exception class.
        /// </summary>
        public AdalSilentTokenAcquisitionException()
            : base(AdalError.FailedToAcquireTokenSilently, AdalErrorMessage.FailedToAcquireTokenSilently)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception class.
        /// </summary>
        /// <param name="exception">inner exception</param>
        public AdalSilentTokenAcquisitionException(Exception exception) : base(AdalError.FailedToAcquireTokenSilently, AdalErrorMessage.FailedToRefreshToken, exception)
        {
        }

#if ADAL_NET
        /// <summary>
        /// Initializes a new instance of the exception class with serialized data.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or destination.</param>
        protected AdalSilentTokenAcquisitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Sets the System.Runtime.Serialization.SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            base.GetObjectData(info, context);
        }
#endif
    }
}
