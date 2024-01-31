// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//Register an instance of type IHttpClientFactory
//When you dispose of a HttpClient instance, the connection remains open for up to four minutes.
//Further, the number of sockets that you can open at any point in time has a limit —
//you can’t have too many sockets open at once. So when you use too many HttpClient instances,
//you might end up exhausting your supply of sockets.
//Here’s where IHttpClientFactory comes to the rescue.
//You can take advantage of IHttpClientFactory to create HttpClient instances for invoking
//HTTP API methods by adhering to the best practices to avoid issues faced with HttpClient.
//The primary goal of IHttpClientFactory in ASP.NET Core is to ensure that HttpClient instances
//are created using the factory while at the same time eliminating socket exhaustion.
builder.Services.AddHttpClient();

//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Register the Swagger generator, defining 1 or more Swagger documents
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MSI Helper Service", Version = "1.0.0.0" });
//    c.IncludeXmlComments(string.Format(@"{0}\MSIHelperService.XML", AppDomain.CurrentDomain.BaseDirectory));
//    c.EnableAnnotations();
//});

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
      .RequireAuthenticatedUser()
      .Build();
});

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.UseHttpsRedirection();

app.Run();
