// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

namespace MsiSFWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AcquireTokenController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<AcquireTokenController> _logger;

        public AcquireTokenController(ILogger<AcquireTokenController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> GetAsync([FromQuery(Name = "resourceuri")] string? resourceUri,
            [FromQuery(Name = "userAssignedId")] string? userAssignedId = null)
        {
            _logger.LogInformation("Get token from MSAL for managed identity.");
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                MyIdentityLogger logger = new MyIdentityLogger(_logger, stringBuilder);

                IManagedIdentityApplication mi = CreateManagedIdentityApplication(userAssignedId, logger);

                var result = await mi.AcquireTokenForManagedIdentity(resourceUri).ExecuteAsync().ConfigureAwait(false);

                stringBuilder.AppendLine("Access token received. Token Source: " + result.AuthenticationResultMetadata.TokenSource);
                return stringBuilder.ToString();
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

        private IManagedIdentityApplication CreateManagedIdentityApplication(string? userAssignedId, IIdentityLogger logger)
        {
            if (userAssignedId == null)
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithLogging(logger)
                    .Build();
            }
            else
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedId))
                    .WithLogging(logger)
                    .Build();
            }
        }
    }

    class MyIdentityLogger : IIdentityLogger
    {
        private readonly ILogger _logger;
        private StringBuilder _stringBuilder;

        public EventLogLevel MinLogLevel { get; }

        public MyIdentityLogger(ILogger logger, StringBuilder stringBuilder)
        {
            //Recommended default log level
            MinLogLevel = EventLogLevel.Verbose;
            _logger = logger;
            _stringBuilder = stringBuilder;
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        public void Log(LogEntry entry)
        {
            //Log Message here:
            _stringBuilder.AppendLine(entry.Message);
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
