// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using MSIHelperService.Helper;

namespace MSIHelperService.Controllers
{
    /// <summary>
    /// GetEnvironmentVariablesController
    /// Gets All Environment Variables
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentVariablesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        /// <summary>
        /// GetEnvironmentVariablesController ctor
        /// Inject Logger and IHttpClientFactory instance 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpClientFactory"></param>
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
        /// Returns the environment variables of an azure resource.
        /// 400 - Returns the error object for any validation failures
        /// 500 - Returns the error object for any Server Errors
        /// </returns>
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

                MSIHelper.AzureResource.AzureArc => await MSIHelper.GetAzureArcEnvironmentVariables(
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.ServiceFabric => throw new NotImplementedException(),

                MSIHelper.AzureResource.CloudShell => throw new NotImplementedException(),

                _ => throw new Exception("Provided Option does not exist"),
            };

            _logger.LogInformation("GetEnvironmentVariablesController returned a response.");

            return response;
        }
    }
}
