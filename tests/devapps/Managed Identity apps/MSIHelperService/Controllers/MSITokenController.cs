// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using MSIHelperService.Helper;
using Swashbuckle.AspNetCore.Annotations;

namespace MSIHelperService.Controllers
{
    /// <summary>
    /// GetMSITokenController
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [SwaggerTag(description: "Gets MSI Token")]
    public class MSITokenController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        /// <summary>
        /// GetMSITokenController ctor
        /// Inject Logger and IHttpClientFactory instance 
        /// </summary>
        /// <param name="logger"></param>
        public MSITokenController(
            ILogger<MSITokenController> logger, 
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets the Managed Service Identity Token of an Azure Resource
        /// </summary>
        /// <param name="uri">URI of the MSI Endpoint</param>
        /// <param name="identityHeader">IDENTITY_HEADER of the MSI Endpoint</param>
        /// <param name="azureResource">Allowed Values :  "WebApp", "Function", "VM", "AzureArc", "ServiceFabric"</param>
        /// <returns>
        /// Returns the MSI token for an azure resource
        /// </returns>
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet]
        [SwaggerOperation(Summary = "Gets the MSI Token from an Azure Resource")]
        [SwaggerResponse(200, "Returns an Azure Resource MSI Token Response", Type = typeof(string))]
        [SwaggerResponse(400, "Returns the error object for any validation failures", Type = typeof(string))]
        [SwaggerResponse(500, "Returns the error object for any Server Errors", Type = typeof(string))]
        public async Task<ActionResult?> RemoteHttpResponse(
            [FromQuery(Name = "uri")] string uri,
            [FromHeader(Name = "X-IDENTITY-HEADER")] string? identityHeader = "default",
            [FromQuery(Name = "azureResource")] string azureResource = MSIHelper.DefaultAzureResource)
        {
            _logger.LogInformation("GetMSITokenController called.");

            //create an HttpClient using IHttpClientFactory
            HttpClient httpClient = _httpClientFactory.CreateClient();

            //Call the MSIHelper method based on the resource
            ActionResult? msiEndpointResult = Enum.Parse<MSIHelper.AzureResource>(azureResource) switch
            {
                MSIHelper.AzureResource.WebApp => await MSIHelper.GetWebAppMSIToken(
                    identityHeader,
                    uri,
                    httpClient,
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.Function => await MSIHelper.GetFunctionAppMSIToken(
                    identityHeader,
                    uri,
                    httpClient,
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.VM => await MSIHelper.GetVirtualMachineMSIToken(
                    identityHeader, 
                    uri, 
                    httpClient,
                    _logger).ConfigureAwait(false),

                MSIHelper.AzureResource.AzureArc => throw new NotImplementedException(),

                MSIHelper.AzureResource.ServiceFabric => throw new NotImplementedException(),

                MSIHelper.AzureResource.CloudShell => throw new NotImplementedException(),
                
                _ => throw new Exception("Provided Option does not exist"),
            };

            _logger.LogInformation("GetMSITokenController returned a response.");

            return msiEndpointResult;
        }
    }
}
