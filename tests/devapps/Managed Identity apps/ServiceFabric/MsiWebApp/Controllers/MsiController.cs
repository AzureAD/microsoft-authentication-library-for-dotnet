// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static System.Net.WebRequestMethods;

namespace MsiWebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MsiController : ControllerBase
    {
        private readonly ILogger<MsiController> _logger;

        public MsiController(ILogger<MsiController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetMsiToken")]
        [ProducesResponseType(typeof(HttpResponse), StatusCodes.Status200OK)]
        [SwaggerResponse(200, "Returns an Azure Resource MSI Token Response", Type = typeof(HttpResponse))]
        [SwaggerResponse(400, "Returns the error object for any validation failures", Type = typeof(HttpResponse))]
        [SwaggerResponse(500, "Returns the error object for any Server Errors", Type = typeof(HttpResponse))]
        public Task<ActionResult?> Get([FromQuery(Name = "azureResource")] string azureResource = "https://management.azure.com/",
            [FromQuery(Name = "userAssignedId")] string? userAssignedId = null)
        {
            _logger.LogInformation("Get token from Msi Endpoint.");
            try
            {
                string? identityEndpoint = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
                string? identityHeader = Environment.GetEnvironmentVariable("IDENTITY_HEADER");
                

                if (identityEndpoint == null && identityHeader == null)
                {
                    throw new Exception("Service Fabric managed identity unavailable.");
                }
 
                if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri? endpointUri))
                {
                    throw new Exception("Invalid endpoint: " + identityEndpoint);
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, endpointUri);
                requestMessage.Headers.Add("secret", identityHeader);
                requestMessage.RequestUri = BuildRequestUri(endpointUri, userAssignedId, azureResource);

                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.SendAsync(requestMessage).Result;
                
                return Task.FromResult<ActionResult?>(Ok(response.Content.ReadAsStringAsync().Result));
            }
            catch (Exception ex)
            {
                return Task.FromResult<ActionResult?>(BadRequest(ex.Message));
            }
        }

        private Uri BuildRequestUri(Uri endpointUri, string userAssignedId, string azureResource)
        {
            StringBuilder queryString = new StringBuilder($"?api-version=2019-07-01-preview&resource={azureResource}");

            if (userAssignedId != null)
            {
                if (Guid.TryParse(userAssignedId, out Guid userAssignedGuid))
                {
                    queryString.Append($"&client_id={userAssignedId}");
                }
                else
                {
                    queryString.Append($"&mi_res_id={userAssignedId}");
                }
            }

            return new Uri(endpointUri, queryString.ToString());
        }
    }
}
