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

using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal enum AuthorizationStatus
    {
        Failed = -1,
        Success = 1,
    }

    [DataContract]
    internal class AuthorizationResult
    {
        internal AuthorizationResult(string code)
        {
            this.Status = AuthorizationStatus.Success;
            this.Code = code;
        }

        internal AuthorizationResult(string error, string errorDescription)
        {
            this.Status = AuthorizationStatus.Failed;
            this.Error = error;
            this.ErrorDescription = errorDescription;
        }

        public AuthorizationStatus Status { get; private set; }

        [DataMember]
        public string Code { get; private set; }

        [DataMember]
        public string Error { get; private set; }

        [DataMember]
        public string ErrorDescription { get; private set; }
    }
}
