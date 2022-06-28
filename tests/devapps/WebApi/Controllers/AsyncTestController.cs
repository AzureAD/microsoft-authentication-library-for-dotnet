// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using WebApi.MockHttp;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AsyncTestController : Controller
    {
        private static readonly IConfidentialClientApplication s_app;
        private static readonly IEnumerable<string> s_scope = new[] { "scope1" };
        private static readonly MockHttpClientFactory s_mockHttpClientFactory = new MockHttpClientFactory(500);

        static AsyncTestController()
        {
            s_app = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithAuthority("https://login.microsoftonline.com/tenantId")
                    .WithHttpClientFactory(s_mockHttpClientFactory)
                    .WithClientSecret("secret")
                    .Build();
        }

        [HttpGet]
        public async Task<string> WithAsyncWithoutCache()
        {
            var result = await s_app.AcquireTokenForClient(s_scope)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);
            return result.AuthenticationResultMetadata.TokenSource.ToString();
        }

        [HttpGet]
        public string WithoutAsyncWithoutCache()
        {
            var result = s_app.AcquireTokenForClient(s_scope)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
            return result.AuthenticationResultMetadata.TokenSource.ToString();
        }
    }
}
