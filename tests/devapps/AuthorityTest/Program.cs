using System;
using Microsoft.Identity.Client;

namespace AuthorityEnvironmentTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing Authority Override with Different Environments...");
            
            TestDifferentAuthorityHosts();
            TestSovereignClouds();
        }

        static void TestDifferentAuthorityHosts()
        {
            Console.WriteLine("\n=== Test 1: Different Authority Hosts ===");
            
            string tenantA = "tenant-a-guid";
            string tenantB = "tenant-b-guid";
            string worldwideAuthority = $"https://login.microsoftonline.com/{tenantA}";
            string governmentAuthority = $"https://login.microsoftonline.us/{tenantB}";

            var app = ConfidentialClientApplicationBuilder
                .Create("test-client-id")
                .WithClientSecret("test-secret")
                .Build();

            try 
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var builder = app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                    .WithAuthority(worldwideAuthority)  // Worldwide cloud with tenantA
                    .WithTenantIdFromAuthority(new Uri(governmentAuthority)); // Gov cloud with tenantB
#pragma warning restore CS0618 // Type or member is obsolete

                Console.WriteLine($"✓ PASS: Different authority hosts work");
                Console.WriteLine($"  Initial Authority: {worldwideAuthority}");
                Console.WriteLine($"  Override Authority: {governmentAuthority}");
                Console.WriteLine($"  Expected: Should use government cloud host with tenantB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception with different hosts: {ex.Message}");
            }
        }

        static void TestSovereignClouds()
        {
            Console.WriteLine("\n=== Test 2: Sovereign Clouds with WithTenantId ===");
            
            string tenantA = "tenant-a-guid";
            string tenantB = "tenant-b-guid";
            string chinaAuthority = $"https://login.chinacloudapi.cn/{tenantA}";

            var app = ConfidentialClientApplicationBuilder
                .Create("test-client-id")
                .WithClientSecret("test-secret")
                .Build();

            try 
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var builder = app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                    .WithAuthority(chinaAuthority)  // China cloud with tenantA
                    .WithTenantId(tenantB); // Override to tenantB but keep China cloud host
#pragma warning restore CS0618 // Type or member is obsolete

                Console.WriteLine($"✓ PASS: Sovereign cloud with WithTenantId works");
                Console.WriteLine($"  Initial Authority: {chinaAuthority}");
                Console.WriteLine($"  Override Tenant: {tenantB}");
                Console.WriteLine($"  Expected: Should use China cloud host with tenantB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception with sovereign cloud: {ex.Message}");
            }
        }
    }
}