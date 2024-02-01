// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//#r "Newtonsoft.Json" //uncomment this on the function app 
//This is the Azure Function App Code - not related to this solution 
//The contents of this file is copied to the Azure Function 

using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace MSIHelperService.AzureFunction // Name space is not required while updating this code in the function app
{
    /// <summary>
    /// Gets the environment variables from the function app 
    /// </summary>
    public class GetEnvironmentVariables
    {
        private static Dictionary<string, string> _envVariables => new Dictionary<string, string>();

        /// <summary>
        /// Gets the environment variables
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string? name = req.Query["variableName"];

            log.LogInformation("Querystring value for variableName is : ", name);

            if (string.IsNullOrEmpty(name))
            {
                Dictionary<string, string?> _envVariables = new Dictionary<string, string?>();
                _envVariables.Add("IDENTITY_HEADER", Environment.GetEnvironmentVariable("IDENTITY_HEADER"));
                _envVariables.Add("IDENTITY_ENDPOINT", Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"));
                _envVariables.Add("IDENTITY_API_VERSION", Environment.GetEnvironmentVariable("IDENTITY_API_VERSION"));

                log.LogInformation("Returning All Environment Variables");
                return new OkObjectResult(_envVariables);
            }
            else
            {
                string? responseMessage = Environment.GetEnvironmentVariable(name);
                log.LogInformation("Returning Environment Variable Based on the variable name : " , name);
                return new OkObjectResult(responseMessage);
            }
        }
    }
}
