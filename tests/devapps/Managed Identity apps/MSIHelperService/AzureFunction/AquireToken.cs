// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//#r "Newtonsoft.Json" //uncomment this line in the function app
//This is the Azure Function App Code - not related to this solution 
//The contents of this file is copied to the Azure Function 

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace MSIHelperService.AzureFunction
{
    /// <summary>
    /// Gets the function app token
    /// </summary>
    public class AquireToken
    {
        private static readonly HttpClient s_httpClient = new HttpClient();

        /// <summary>
        /// Function app token request
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
        {
            string uri = req.Query["uri"];
            string identity_header = req.Query["header"];
            var managedIdentityAuthenticationHeader = "X-IDENTITY-HEADER";

            try
            {
                log.LogInformation("uri : ", uri);

                //set the http get method and the required headers for a web app
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                requestMessage.Headers.Add(managedIdentityAuthenticationHeader, identity_header);

                //send the request
                HttpResponseMessage result = await s_httpClient.SendAsync(requestMessage).ConfigureAwait(false);

                string body = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                return new OkObjectResult(body);
            }
            catch (Exception ex)
            {
                string errorText = string.Format("{0} \n\n{1}", ex.Message, ex.InnerException != null ?
                                        ex.InnerException.Message :
                                        "Acquire token failed");

                return new BadRequestObjectResult(errorText);
            }

        }
    }
}
