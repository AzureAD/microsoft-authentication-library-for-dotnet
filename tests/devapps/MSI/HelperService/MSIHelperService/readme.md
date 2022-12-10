---
title: Managed identities for Azure resources Testing
description: An overview of the managed identities for Azure resources test framework and setup.
services: MSAL
author: gljohns
manager: bogavril

#Customer intent: As a developer I would like to test Managed Service Identity on my dev box and run integration tests. This document provides the test setup on how this can be achieved.
---

# Testing managed identities for Azure resources using MSAL .Net?

A common challenge for developers is the management of secrets, credentials, certificates, and keys used to secure communication between services. Managed identities eliminate the need for developers to manage these credentials. 

While developers can securely store the secrets in [Azure Key Vault](../../key-vault/general/overview.md), services need a way to access Azure Key Vault. 

Managed identities provide an automatically managed identity in Azure Active Directory for applications to use when connecting to resources that support Azure Active Directory (Azure AD) authentication. 

Applications can use managed identities to obtain Azure AD tokens without having to manage any credentials.

MSAL .Net now supports acquiring token for Managed Identities. 

## Managed identity types

There are two types of managed identities:

- **System-assigned**. Some Azure resources, such as virtual machines allow you to enable a managed identity directly on the resource. When you enable a system-assigned managed identity: 
    - A service principal of a special type is created in Azure AD for the identity. The service principal is tied to the lifecycle of that Azure resource. When the Azure resource is deleted, Azure automatically deletes the service principal for you. 
    - By design, only that Azure resource can use this identity to request tokens from Azure AD.
    - You authorize the managed identity to have access to one or more services.

- **User-assigned**. You may also create a managed identity as a standalone Azure resource. You can [create a user-assigned managed identity](how-to-manage-ua-identity-portal.md) and assign it to one or more Azure Resources. When you enable a user-assigned managed identity:      
    - A service principal of a special type is created in Azure AD for the identity. The service principal is managed separately from the resources that use it. 
    - User-assigned identities can be used by multiple resources.
    - You authorize the managed identity to have access to one or more services.

## How can I use managed identities for Azure resources?

You can use managed identities by following the steps below: 

1. Create a managed identity in Azure. You can choose between system-assigned managed identity or user-assigned managed identity. 
    1. When using a user-assigned managed identity, you assign the managed identity to the "source" Azure Resource, such as a Virtual Machine, Azure Logic App or an Azure Web App.
3. Authorize the managed identity to have access to the "target" service.
4. Use the managed identity to access a resource. In this step, you can use the Azure SDK with the Azure.Identity library. Some "source" resources offer connectors that know how to use Managed identities for the connections. In that case, you use the identity as a feature of that "source" resource.

## What Azure services support the feature?<a name="which-azure-services-support-managed-identity"></a>

Managed identities for Azure resources can be used to authenticate to services that support Azure AD authentication. For a list of supported Azure services, see [services that support managed identities for Azure resources](./services-support-managed-identities.md).

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

The protected test service can be accessed by going to https://service.msidlab.com/. You can also use the [Swagger](https://service.msidlab.com/swagger/index.html) to test this service. 


