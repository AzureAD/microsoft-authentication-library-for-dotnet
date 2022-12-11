---
title: How to effectively test Managed identities on Azure resources
description: An overview of the managed identities for Azure resources test framework and setup.
services: MSAL
author: gljohns
manager: bogavril

#Customer intent: As a developer I would like to test Managed Service Identity on my dev box and run integration tests. This document provides the test setup on how this can be achieved.
---

# Testing managed identities on Azure resources for MSAL .Net and other Auth SDKs

A common challenge for developers testing Managed Identities is to deploy their code to a Managed Identity Service, so they can send requests to the Managed Identity endpoints. While this may be feasible for an end-to-end testing scenario, but it could be challenging to run integration tests on CI pipelines or on a local dev box.

To eliminate this problem, MSAL .Net team has built a MSI Helper service that exposes several managed identity sources through a web api using which you can access the identity endpoints from your tests and acquire Managed Identity token responses. The API accepts the same query params of a managed identity endpoint, this will allow your test(s) to call the api just like how you would make a http request to the managed identity endpoint while your code runs inside of a managed identity source.   

This protected web service is able to send http requests to Managed Identity endpoints (for e.g. Azure Web App, Function App or a Virtual machine) and transfer the managed identity response back to you. Since Identity Labs manages all the azure resources there will be zero cost and zero maintenance for you and your service. 

Developers / Applications wanting to test managed identity can use this test service and take advantage of the same features of testing on a managed identity source. **To use this test service you will need to be part of the MSAL .Net team or the Identity for Services (ID4S) team or one of it's partner teams.** 

To gain access to the MSI Helper service you will need access to [Identity Labs](https://docs.msidlab.com/)

## Managed identity types support

There are two types of managed identities, and both are supported by this service:

- **System-assigned**. Some Azure resources, such as virtual machines allow you to enable a managed identity directly on the resource. When you enable a system-assigned managed identity: 
    - A service principal of a special type is created in Azure AD for the identity. The service principal is tied to the lifecycle of that Azure resource. When the Azure resource is deleted, Azure automatically deletes the service principal for you. 
    - By design, only that Azure resource can use this identity to request tokens from Azure AD.
    - Using the MSI Helper service you will be able to test this type

- **User-assigned**. You may also create a managed identity as a standalone Azure resource. You can [create a user-assigned managed identity](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal) and assign it to one or more Azure Resources. When you enable a user-assigned managed identity:      
    - A service principal of a special type is created in Azure AD for the identity. The service principal is managed separately from the resources that use it. 
    - User-assigned identities can be used by multiple resources.
    - MSI Helper service uses a single user identity shared across all azure resources

## What Azure services support the feature?<a name="which-azure-services-support-managed-identity"></a>

Managed identities for Azure resources can be used to authenticate to services that support Azure AD authentication. For a list of supported Azure services, see [services that support managed identities for Azure resources](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/services-support-managed-identities).

## What managed identity sources does MSAL .Net support?

MSAL .Net supports the following Managed Identity sources: 

1. App Service (version : 2019-08-01)
2. Virtual Machine (IMDS)
3. Service Fabric
4. Azure ARC
5. Cloud Shell

## How can I test these different sources?

MSAL .Net team has deployed a Managed Identity helper service called the MSIHelper service, using which you should be able to test the following sources. 

1. App Service (version : 2019-08-01)
2. Virtual Machine (IMDS)
3. Service Fabric
4. Azure ARC

## Where is this service deployed?

The protected test service can be accessed by going to [https://service.msidlab.com/](https://service.msidlab.com/). You can also use the [MSI Helper Swagger](https://service.msidlab.com/swagger/index.html) to test this service. 

## What are the different endpoints the MSI Helper service exposes?

MSI Helper Service exposes two endpoints : 

<img src="images/endpoints.PNG" alt="endpoints" width="400"/>

- GetEnvironmentVariables, and 
- GetMSIToken

A sample request to the `GetEnvironmentVariables` 

```
curl -X 'GET' \
  'https://service.msidlab.com/GetEnvironmentVariables?resource=webapp' \
  -H 'accept: text/plain'
```

And here is a successful response from the service. 

```
{
  "IDENTITY_HEADER": "69C62B109AAF4EB7B01061197C14F550",
  "IDENTITY_ENDPOINT": "http://127.0.0.1:41292/msi/token/",
  "IDENTITY_API_VERSION": "2019-08-01"
}
```

> **_NOTE:_**  

- The `GetEnvironmentVariables` api, accepts an MSI supported Azure resource as a query parameter and returns all the environment variables needed for you to form a http request.  
- Once you have formed the URI you can use the `GetMSIToken` endpoint and send the request to the Helper Service, this will inturn call the MSI endpoint and return a MSI token response.  

```
"{\"statusCode\":500,\"message\":\"An unexpected error occured while fetching the AAD Token.\",\"correlationId\":\"91acf506-d323-4bdd-a5f5-b5b71a09e1dc\"}"
```

You should also be able to test for exceptions that the MSI endpoint throws

```
"An attempt was made to access a socket in a way forbidden by its access permissions. (127.0.0.1:41292) \n\nAn attempt was made to access a socket in a way forbidden by its access permissions."
```

## How Auth SDKs teams can takes advantage of this service for testing? 

Your code running from any dev box or CI pipeline will get service credentials from the lab vault and connect to the protected helper service, and then proxy all environment variable reads and http requests to this web service hosted on an Azure Web App. Depending upon what resource is being testing the Helper service will make calls to the Azure Resources under test and get the Managed Identity token response back to MSAL.

## How does this service work?

The service is deployed to an Azure Web App and it exposes the Azure Web Apps MSI endpoint for testing. i.e. it exposes it's own MSI endpoint so the Web App resource can be tested. In addition to that it also calls into other Azure resources like Azure Function, ServiceFabric, AzureARC and IMDS and exposes their MSI endpoints as well. 

### How to build and deploy the helper service (exposes Web App MSI)

Build the current project (The MSI Helper Service - MSIHelperService.csproj) and deploy to the Azure Web App. 

- Once you have built the MSIHelperService.csproj 
- Right click on the project and select the `publish` option
<br>
  <img src="images/publish_vs.PNG" alt="publish" width="400"/>

- Select the appropriate [Web App](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourceGroups/MSAL_MSI/providers/Microsoft.Web/sites/msihelperservice/appServices) under the Lab Subscription and click `publish` 
<br>
  <img src="images/msihelper_azure_settings.PNG" alt="settings" width="600"/>

- make sure you are publishing to the staging slot of the app service so you do not break the existing CI runs. Also, select the `Deploy as a Zip Package` checkbox
<br>
  <img src="images/staging.PNG" alt="staging" width="400"/>


> **_NOTE:_**  You will need to have Identity Lab permissions to deploy to the helper service. Make sure to keep the settings same as how it is shown in the above screenshot

- Once the service has been deployed you can test the service using the swagger or run MSAL integration tests pointing to the staging slot 

- To test the service under the staging slot, you can either use the [staging slot swagger](https://msihelperservice-staging.azurewebsites.net/swagger/index.html) but we highly recommend testing using MSAL integration tests as this will tests all endpoints for all resources. To do so, edit the service base URL in the MSI integration test and change it from [https://service.msidlab.com](https://service.msidlab.com) to [https://msihelperservice-staging.azurewebsites.net/](https://msihelperservice-staging.azurewebsites.net/) and run the the tests 
<br>
<img src="images/replace.PNG" alt="replace" width="800"/>

<br>

- After validation go to [Azure Portal](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourceGroups/MSAL_MSI/providers/Microsoft.Web/sites/msihelperservice/deploymentSlotsV2) and from under the deployment slot, swap the services

<br>
<img src="images/swap.PNG" alt="swap" width="800"/>
<br>

> **_NOTE:_**  Once you have swapped the slot make sure to point the base url to the production slot again in your code and test it again with the production endpoint fron the MSAL integration testing

### How to build and deploy the Function App 

Function app deployment is easy but can also be risky. There is no failover mechanism here since we do not have a staging slot for the Azure functions. But there shouldn't be a need ever to deploy to the function app or to any other Azure resources (VM / Azure ARC / Service Fabric) after MSAL MSI has gone live. The function app code can be found `AzureFunction` folders 
<br>
<img src="images/function.PNG" alt="function" width="800"/>
<br>

- The [function app](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourceGroups/MSAL_MSI/providers/Microsoft.Web/sites/msalmsifunction/appServices) also exposes two protected endpoints. These endpoints are called internally by the MSI Helper service. 
<br>
<img src="images/func_endpoints.PNG" alt="func_endpoints" width="800"/>
<br>

- To make changes simply copy paste the code from the appropriate Get*.cs files into these endpoints in the function app
<br>

> **_NOTE:_**  Any changes made to this function app will affect both the production and the staging slot of the MSI Helper Service. There are several ID4S teams that are dependent on these services, so before making any change please ensure that you have tested the code in a sample azure function app. 

## User Assigned Identity

This helper service also exposed the [User Identity](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourceGroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/MSAL_MSI_USERID/overview) for testing. 

<br>
<img src="images/uid.PNG" alt="uid" width="800"/>
<br>
<br>

Following are some useful information to test the User Identity. 

| Syntax      | Description |
| ----------- | ----------- |
| Resource ID      | /subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/MSAL_MSI_USERID       |
| Name   | MSAL_MSI_USERID        |
| Type      | Microsoft.ManagedIdentity/userAssignedIdentities       |
| Location   | eastus2        |
| Tenant Id      | 72f988bf-86f1-41af-91ab-2d7cd011db47       |
| Principal Id   | 3b57c42c-3201-4295-ae27-d6baec5b7027        |
| Client Id      | 3b57c42c-3201-4295-ae27-d6baec5b7027       |


## Troubleshooting 

The MSI Helper Service has been deployed with Application Insights and a good amount of logging for troubleshooting. You can go to the Azure Portal and select the [MSI Helper Service App Insights](https://ms.portal.azure.com/#view/AppInsightsExtension/DetailsV2Blade/ComponentId~/%7B%22SubscriptionId%22%3A%22c1686c51-b717-4fe0-9af3-24a20a41fb0c%22%2C%22ResourceGroup%22%3A%22MSAL_MSI%22%2C%22Name%22%3A%22msihelperservice%22%2C%22LinkedApplicationType%22%3A0%2C%22ResourceId%22%3A%22%252Fsubscriptions%252Fc1686c51-b717-4fe0-9af3-24a20a41fb0c%252FresourceGroups%252FMSAL_MSI%252Fproviders%252Fmicrosoft.insights%252Fcomponents%252Fmsihelperservice%22%2C%22ResourceType%22%3A%22microsoft.insights%252Fcomponents%22%2C%22IsAzureFirst%22%3Afalse%7D/DataModel~/%7B%22eventId%22%3A%22e83141d4-78ec-11ed-9983-000d3a54144f%22%2C%22timestamp%22%3A%222022-12-11T00%3A43%3A16.617Z%22%2C%22cacheId%22%3A%2283de9b73-774b-4ec7-94cf-2663b362e6f6%22%2C%22eventTable%22%3A%22requests%22%2C%22timeContext%22%3A%7B%22durationMs%22%3A86400000%2C%22endTime%22%3A%222022-12-11T01%3A13%3A14.404Z%22%7D%7D) and see the transaction logs 


<img src="images/logs.PNG" alt="logs" width="800"/>
<br>
<br>

For, the Function App. Go to Azure Portal and select [Monitor](https://ms.portal.azure.com/#view/WebsitesExtension/FunctionMenuBlade/~/monitor/resourceId/%2Fsubscriptions%2Fc1686c51-b717-4fe0-9af3-24a20a41fb0c%2FresourceGroups%2FMSAL_MSI%2Fproviders%2FMicrosoft.Web%2Fsites%2Fmsalmsifunction%2Ffunctions%2FGetEnvironmentVariables) under the Function App Endpoints and this will give you the invocations and logs 

<img src="images/invocation.PNG" alt="invocation" width="800"/>
<br>

## Need Help? 

Contact Neha Bhargava / Gladwin Johnson / Bogdan Gavril for further assistance.
