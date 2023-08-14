using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MI429
{

    public sealed class MIClientSingleton
    {
        private static ManagedIdentityApplication instance = null;
        private static readonly object padlock = new object();

        private MIClientSingleton()
        {
            
        }

        public static ManagedIdentityApplication Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        Console.WriteLine("Instance created");

                        instance = (ManagedIdentityApplication)ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned)
                            .Build();
                    }

                    Console.WriteLine("Instance returned");

                    return instance;
                }
            }
        }
    }

    internal class Program
    {
        static IIdentityLogger s_identityLogger = new IdentityLogger();

        static readonly string s_scope = "https://management.azure.com";

        static IManagedIdentityApplication s_mi;

        private static int _sourceIDP, _sourceCache, _errors = 0;


        static async Task Main(string[] args)
        {

            // s_mi = MIClientSingleton.Instance;

            var managedIdentityApplication = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned)
                            .Build();


            Console.Write("Enter the number of tasks: ");
            if (int.TryParse(Console.ReadLine(), out int numTasks) && numTasks > 0)
            {
                Task[] tasks = new Task[numTasks];

                for (int i = 0; i < numTasks; i++)
                {
                    tasks[i] = WorkerMethodAsync($"Task {i + 1}", managedIdentityApplication);
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks).ConfigureAwait(false);

                Console.WriteLine("All tasks have finished.");
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid number greater than 0.");
            }

            Console.WriteLine("All tasks have finished.");
            Console.WriteLine("IDP Source : " + _sourceIDP);
            Console.WriteLine("Cache Source : " + _sourceCache);
            Console.WriteLine("Errors : " + _errors);
            Console.ReadLine();

        }

        static async Task WorkerMethodAsync(object threadObj, IManagedIdentityApplication managedIdentityApplication)
        {
            string threadName = (string)threadObj;
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"Thread {threadName}: Iteration {i}");
                //Thread.Sleep(1000); 

                try
                {
                    var authenticationResult = await managedIdentityApplication.AcquireTokenForManagedIdentity(s_scope)
                .ExecuteAsync().ConfigureAwait(false);

                    // Set console text color to green
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Success....");
                    Console.WriteLine("Token Source : " + authenticationResult.AuthenticationResultMetadata.TokenSource);

                    if (authenticationResult.AuthenticationResultMetadata.TokenSource == TokenSource.Cache)
                    {
                        _sourceCache++;
                    }
                    else if (authenticationResult.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider)
                    {
                        _sourceIDP++;
                    }

                    // Reset console text color
                    Console.ResetColor();
                }
                catch (MsalServiceException e)
                {
                    // Set console text color to red
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.ErrorCode);
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    // Reset console text color
                    Console.ResetColor();
                    _errors++;
                }
            }
        }
    }

    class IdentityLogger : IIdentityLogger
    {
        public EventLogLevel MinLogLevel { get; }

        public IdentityLogger()
        {
            MinLogLevel = EventLogLevel.Verbose;
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        public void Log(LogEntry entry)
        {
            //Log Message here:
            Console.WriteLine(entry.Message);
        }
    }
}
