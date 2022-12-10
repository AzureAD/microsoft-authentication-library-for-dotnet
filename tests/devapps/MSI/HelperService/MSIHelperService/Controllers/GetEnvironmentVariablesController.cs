// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using MSIHelperService.Helper;
using Swashbuckle.AspNetCore.Annotations;

namespace MSIHelperService.Controllers
{
    /// <summary>
    /// GetEnvironmentVariablesController
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [SwaggerTag(description: "Gets All Environment Variables")]
    public class GetEnvironmentVariablesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        /// <summary>
        /// GetEnvironmentVariablesController ctor
        /// Inject Logger and IHttpClientFactory instance 
        /// </summary>
        /// <param name="logger"></param>
        public GetEnvironmentVariablesController(
            ILogger<GetEnvironmentVariablesController> logger, 
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets all the Environment Variables of the Azure Resource
        /// </summary>
        /// <param name="resource">Allowed Values : "webapp", "function", "vm", "azurearc", "servicefabric"</param>
        /// <returns>
        /// Returns the environment variables of an azure resource
        /// </returns>
        [SwaggerResponse(200, "Returns Azure Web App Environment Variables", Type = typeof(string))]
        [SwaggerResponse(400, "Returns the error object for any validation failures", Type = typeof(string))]
        [SwaggerResponse(500, "Returns the error object for any Server Errors", Type = typeof(string))]
        [HttpGet]
        public Dictionary<string, string>? GetEnvValues([FromQuery(Name = "resource")]
        string resource = MSIHelper.DefaultAzureResource)
        {
            _logger.LogInformation("GetEnvironmentVariablesController called.");

            //create an HttpClient using IHttpClientFactory
            HttpClient httpClient = _httpClientFactory.CreateClient();

            //Call the MSIHelper method based on the resource
            Dictionary<string, string>? response = Enum.Parse<MSIHelper.AzureResource>(resource) switch
            {
                MSIHelper.AzureResource.webapp => MSIHelper.GetWebAppEnvironmentVariables(
                    _logger),

                MSIHelper.AzureResource.function => MSIHelper.GetFunctionAppEnvironmentVariables(httpClient,
                    _logger),

                MSIHelper.AzureResource.vm => throw new NotImplementedException(),

                MSIHelper.AzureResource.azurearc => throw new NotImplementedException(),

                MSIHelper.AzureResource.servicefabric => throw new NotImplementedException(),

                MSIHelper.AzureResource.cloudshell => throw new NotImplementedException(),

                _ => null,
            };

            _logger.LogInformation("GetEnvironmentVariablesController returned a response.");

            return response;
        }
    }
}
