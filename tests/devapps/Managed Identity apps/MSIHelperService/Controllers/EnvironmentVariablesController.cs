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
    public class EnvironmentVariablesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        /// <summary>
        /// GetEnvironmentVariablesController ctor
        /// Inject Logger and IHttpClientFactory instance 
        /// </summary>
        /// <param name="logger"></param>
        public EnvironmentVariablesController(
            ILogger<EnvironmentVariablesController> logger, 
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets all the Environment Variables of the Azure Resource
        /// </summary>
        /// <param name="resource">Allowed Values :  "WebApp", "Function", "VM", "AzureArc", "ServiceFabric"</param>
        /// <returns>
        /// Returns the environment variables of an azure resource
        /// </returns>
        [SwaggerOperation(Summary = "Gets the Environment Variables from an Azure Resource")]
        [SwaggerResponse(200, "Returns Azure Web App Environment Variables", Type = typeof(string))]
        [SwaggerResponse(400, "Returns the error object for any validation failures", Type = typeof(string))]
        [SwaggerResponse(500, "Returns the error object for any Server Errors", Type = typeof(string))]
        [HttpGet]
        public async Task<Dictionary<string, string>?> EnvValues([FromQuery(Name = "resource")]
        string resource = MSIHelper.DefaultAzureResource)
        {
            _logger.LogInformation("GetEnvironmentVariablesController called.");

            //create an HttpClient using IHttpClientFactory
            HttpClient httpClient = _httpClientFactory.CreateClient();

            //Call the MSIHelper method based on the resource
            Dictionary<string, string>? response = Enum.Parse<MSIHelper.AzureResource>(resource) switch
            {
                MSIHelper.AzureResource.WebApp => await MSIHelper.GetWebAppEnvironmentVariablesAsync(
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.Function => await MSIHelper.GetFunctionAppEnvironmentVariablesAsync(httpClient,
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.VM => await MSIHelper.GetVirtualMachineEnvironmentVariables(
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.AzureArc => throw new NotImplementedException(),

                MSIHelper.AzureResource.ServiceFabric => throw new NotImplementedException(),

                MSIHelper.AzureResource.CloudShell => throw new NotImplementedException(),

                _ => throw new Exception("Provided Option does not exist"),
            };

            _logger.LogInformation("GetEnvironmentVariablesController returned a response.");

            return response;
        }
    }
}
