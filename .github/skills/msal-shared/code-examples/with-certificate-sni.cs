using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;

// Load certificate from store or file
var cert = new X509Certificate2("path/to/certificate.pfx", "password");

// Build confidential client application with SNI (sendX5C: true)
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert, sendX5C: true)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
    .Build();

// Acquire token
var resource = "resource-uri";
var result = await app.AcquireTokenForClient(new[] { resource })
    .ExecuteAsync();

// Use the token
var token = result.AccessToken;
