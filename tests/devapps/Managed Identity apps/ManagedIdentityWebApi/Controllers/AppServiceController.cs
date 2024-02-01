using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

namespace ManagedIdentityWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    // Get MSI Token
    public class AppServiceController : ControllerBase
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="logger"></param>
        public AppServiceController(ILogger<AppServiceController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Acquires token for managed identity
        /// </summary>
        /// <param name="resourceUri"></param>
        /// <param name="userAssignedId"></param>
        /// <returns>
        /// 200, "Returns an Azure Resource MSI Token Response", Type = typeof(HttpResponse)
        /// 400, "Returns the error object for any validation failures", Type = typeof(HttpResponse)
        /// 500, "Returns the error object for any Server Errors", Type = typeof(HttpResponse)
        /// </returns>
        [HttpGet]
        public async Task<string> GetAsync([FromQuery(Name = "resourceuri")] string? resourceUri,
            [FromQuery(Name = "userAssignedId")] string? userAssignedId = null)
        {
            _logger.LogInformation("Get token from MSAL for managed identity.");
            try
            {
                IManagedIdentityApplication mi = CreateManagedIdentityApplication(userAssignedId);

                var result = await mi.AcquireTokenForManagedIdentity(resourceUri).ExecuteAsync().ConfigureAwait(false);

                return "Access token received. Token Source: " + result.AuthenticationResultMetadata.TokenSource;
            }
            catch (MsalException ex)
            {
                return ex.ToJsonString();
            }
            catch (Exception ex)
            {
                return ex.Message + ex.Source + ex.StackTrace;
            }
        }

        private IManagedIdentityApplication CreateManagedIdentityApplication(string? userAssignedId)
        {
            if (userAssignedId == null) 
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithLogging(new MyIdentityLogger(_logger))
                    .Build();
            }
            else
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedId))
                    .WithLogging(new MyIdentityLogger(_logger))
                    .Build();
            }
        }
    }

    class MyIdentityLogger : IIdentityLogger
    {
        private readonly ILogger _logger;

        public EventLogLevel MinLogLevel { get; }

        public MyIdentityLogger(ILogger logger)
        {
            //Recommended default log level
            MinLogLevel = EventLogLevel.Informational;
            _logger = logger;
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        public void Log(LogEntry entry)
        {
            //Log Message here:
            if (entry.EventLogLevel == EventLogLevel.Error) 
            {
                _logger.LogError(entry.Message);
            }
            else
            {
                _logger.LogInformation(entry.Message);
            }
        }
    }
}
