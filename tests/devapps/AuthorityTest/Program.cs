using System;
using Microsoft.Identity.Client;

namespace AuthorityTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing Authority Override Fix...");
            
            // Test scenario: WithAuthority followed by WithTenantIdFromAuthority
            TestWithAuthorityFollowedByWithTenantIdFromAuthority();
            
            // Test scenario: WithAuthority followed by WithTenantId  
            TestWithAuthorityFollowedByWithTenantId();
        }

        static void TestWithAuthorityFollowedByWithTenantIdFromAuthority()
        {
            Console.WriteLine("\n=== Test 1: WithAuthority + WithTenantIdFromAuthority ===");
            
            string tenantA = "tenant-a-guid";
            string tenantB = "tenant-b-guid";
            string authorityWithTenantA = $"https://login.microsoftonline.com/{tenantA}";
            string authorityWithTenantB = $"https://login.microsoftonline.com/{tenantB}";

            var app = ConfidentialClientApplicationBuilder
                .Create("test-client-id")
                .WithClientSecret("test-secret")
                .Build();

            try 
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var builder = app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                    .WithAuthority(authorityWithTenantA)  // First, set authority to tenantA
                    .WithTenantIdFromAuthority(new Uri(authorityWithTenantB)); // Then, override tenant to tenantB
#pragma warning restore CS0618 // Type or member is obsolete

                Console.WriteLine($"✓ PASS: Builder created successfully with both WithAuthority and WithTenantIdFromAuthority");
                Console.WriteLine($"  Initial Authority (tenantA): {authorityWithTenantA}");
                Console.WriteLine($"  Override Authority (tenantB): {authorityWithTenantB}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception creating builder: {ex.Message}");
            }
        }

        static void TestWithAuthorityFollowedByWithTenantId()
        {
            Console.WriteLine("\n=== Test 2: WithAuthority + WithTenantId ===");
            
            string tenantA = "tenant-a-guid";
            string tenantB = "tenant-b-guid";
            string authorityWithTenantA = $"https://login.microsoftonline.com/{tenantA}";

            var app = ConfidentialClientApplicationBuilder
                .Create("test-client-id")
                .WithClientSecret("test-secret")
                .Build();

            try 
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var builder = app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                    .WithAuthority(authorityWithTenantA)  // First, set authority to tenantA
                    .WithTenantId(tenantB); // Then, override tenant to tenantB
#pragma warning restore CS0618 // Type or member is obsolete

                Console.WriteLine($"✓ PASS: Builder created successfully with both WithAuthority and WithTenantId");
                Console.WriteLine($"  Initial Authority (tenantA): {authorityWithTenantA}");
                Console.WriteLine($"  Override Tenant (tenantB): {tenantB}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception creating builder: {ex.Message}");
            }
        }
    }
}