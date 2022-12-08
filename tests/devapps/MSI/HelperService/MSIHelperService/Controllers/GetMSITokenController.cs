// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using MSIHelperService.Helper;
using Swashbuckle.AspNetCore.Annotations;

namespace MSIHelperService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [SwaggerTag(description: "Gets MSI Token")]
    public class GetMSITokenController : ControllerBase
    {
        private readonly ILogger _logger;

        /// <summary>
        /// GetMSITokenController ctor
        /// </summary>
        /// <param name="logger"></param>
        public GetMSITokenController(ILogger<GetMSITokenController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the Managed Service Identity Token of an Azure Resource
        /// </summary>
        /// <param name="identityHeader">IDENTITY_HEADER of the MSI Endpoint</param>
        /// <param name="uri">URI of the MSI Endpoint</param>
        /// <param name="azureResource">Resource for which you need the MSI Token</param>
        /// <returns>
        /// Returns the MSI token for an azure resource
        /// </returns>
        [ProducesResponseType(typeof(ManagedIdentityResponse), (int)MSIHelper.HTTPErrorResponseCode.Status200OK)]
        [HttpGet]
        [SwaggerResponse(200, "Returns an Azure Resource MSI Token Response", Type = typeof(ManagedIdentityResponse))]
        [SwaggerResponse(400, "Returns the error object for any validation failures", Type = typeof(ManagedIdentityResponse))]
        [SwaggerResponse(500, "Returns the error object for any Server Errors", Type = typeof(ManagedIdentityResponse))]
        public async Task<ActionResult?> GetRemoteHttpResponse(
            [FromHeader(Name = "IDENTITY_HEADER")] string identityHeader,
            [FromQuery(Name = "uri")] string uri,
            [FromQuery(Name = "azureResource")] string azureResource = MSIHelper.DefaultAzureResource)
        {
            _logger.LogInformation("GetMSITokenController called.");

            ActionResult? msiEndpointResult = Enum.Parse<MSIHelper.AzureResource>(azureResource) switch
            {
                MSIHelper.AzureResource.webapp => await MSIHelper.GetWebAppMSIToken(
                    identityHeader,
                    uri,
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.function => await MSIHelper.GetFunctionAppMSIToken(
                    identityHeader,
                    uri,
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.vm => throw new NotImplementedException(),

                MSIHelper.AzureResource.azurearc => throw new NotImplementedException(),

                MSIHelper.AzureResource.servicefabric => throw new NotImplementedException(),

                MSIHelper.AzureResource.cloudshell => throw new NotImplementedException(),

                _ => null,
            };

            _logger.LogInformation("GetMSITokenController returned a response.");

            return msiEndpointResult;
        }
    }
}
