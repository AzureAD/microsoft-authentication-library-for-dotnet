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

using Owin;

namespace Test.ADAL.Common
{
    internal class RelyingParty
    {
        public void Configuration(IAppBuilder app)
        {
            app.Run(ctx =>
            {
                var response = ctx.Response;
                response.StatusCode = 401;
                response.Headers.Add("WWW-authenticate", 
                    new string[] { @" Bearer     authorization_uri  =   ""https://login.windows.net/aaltests.onmicrosoft.com/oauth2/authorize""   ,    Resource_id  =  ""test_resource, test_resource2""  " });
                
                return response.WriteAsync("dummy");
            });
        }
    }
}
