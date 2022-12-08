// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using MSIHelperService.Helper;
using Swashbuckle.AspNetCore.Annotations;

namespace MSIHelperService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [SwaggerTag(description: "Get Environment Variable")]
    public class GetEnvironmentVariableController : ControllerBase
    {
        private readonly ILogger _logger;

        /// <summary>
        /// GetEnvironmentVariableController ctor
        /// </summary>
        /// <param name="logger"></param>
        public GetEnvironmentVariableController(ILogger<GetEnvironmentVariableController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the Environment Variable Name of the Azure Resource
        /// </summary>
        /// <param name="variableName">Environment Variable Name Requested for</param>
        /// <param name="resource">Allowed Values : "webapp", "function", "vm", "azurearc", "servicefabric"</param>
        /// <returns>
        /// Returns the environment variable of an azure resource based on the value passed on the query string 
        /// </returns>
        [SwaggerResponse(200, "Returns Azure Web App Environment Variables", Type = typeof(string))]
        [SwaggerResponse(400, "Returns the error object for any validation failures", Type = typeof(string))]
        [SwaggerResponse(500, "Returns the error object for any Server Errors", Type = typeof(string))]
        [HttpGet]
        public string? GetEnvValue(
            [FromQuery(Name = "variablename")] string variableName,
            [FromQuery(Name = "resource")] string resource = MSIHelper.DefaultAzureResource)
        {
            _logger.LogInformation("GetEnvironmentVariablesController called.");

            string? response = Enum.Parse<MSIHelper.AzureResource>(resource) switch
            {
                MSIHelper.AzureResource.webapp => MSIHelper.GetWebAppEnvironmentVariable(
                    variableName,
                    _logger),

                MSIHelper.AzureResource.function => MSIHelper.GetFunctionAppEnvironmentVariable(
                    variableName,
                    _logger),

                MSIHelper.AzureResource.vm => throw new NotImplementedException(),

                MSIHelper.AzureResource.azurearc => throw new NotImplementedException(),

                MSIHelper.AzureResource.servicefabric => throw new NotImplementedException(),

                MSIHelper.AzureResource.cloudshell => throw new NotImplementedException(),

                _ => null,
            };

            _logger.LogInformation("GetEnvironmentVariableController returned a response.");

            return response;
        }
    }
}
