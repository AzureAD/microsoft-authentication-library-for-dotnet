// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Lab.Api
{
    /// <summary>
    /// Test categories for MSAL.NET tests. These categories can be used to filter which tests to run in different environments, such as running only Selenium tests or only regression tests. They also help to organize tests based on their characteristics, such as the type of authentication they are testing (ADFS, MSA, B2C) or the specific features they are validating (Broker, TokenCacheTests, PromptTests, BuilderTests, UnifiedSchemaValidation).
    /// </summary>
    public static class TestCategories
    {
        /// <summary>
        /// Tests under this category use a Selenium driven browser (Chrome) to automate the web ui.
        /// When run in the lab, the browser is configured to run headless.
        /// For debugging, consider running with the actual browser.
        /// </summary>
        public const string Selenium = "Selenium";

        /// <summary>
        /// LabAccess tests are tests that require access to the lab environment, which is a controlled testing environment used by Microsoft for testing various scenarios. These tests may involve interactions with real services, accounts, or configurations that are only available in the lab environment. Running these tests typically requires special permissions and access to the lab resources, and they may not be suitable for running in a local development environment due to their dependencies on external services and configurations.
        /// </summary>
        public const string LabAccess = "LabAccess";

        /// <summary>
        /// ADFS, MSA, and B2C are categories that represent different types of authentication scenarios being tested. ADFS (Active Directory Federation Services) tests focus on scenarios involving federated authentication with ADFS servers. MSA (Microsoft Account) tests cover scenarios related to Microsoft Accounts, which are commonly used for consumer applications. B2C (Business-to-Consumer) tests involve scenarios related to Azure AD B2C, which is a service that enables businesses to provide identity management for their consumer-facing applications. These categories help to organize tests based on the specific authentication scenarios they are validating, allowing for targeted test runs and better organization of test cases.
        /// </summary>
        public const string ADFS = "ADFS";

        /// <summary>
        /// MSA (Microsoft Account) tests cover scenarios related to Microsoft Accounts, which are commonly used for consumer applications. These tests may involve interactions with Microsoft Account services, such as authentication flows, token acquisition, and account management. MSA tests help ensure that MSAL.NET correctly handles authentication scenarios involving Microsoft Accounts, providing a seamless experience for users of consumer applications that rely on Microsoft Account authentication.
        /// </summary>
        public const string MSA = "MSA";

        /// <summary>
        /// B2C (Business-to-Consumer) tests involve scenarios related to Azure AD B2C, which is a service that enables businesses to provide identity management for their consumer-facing applications. These tests may cover various authentication flows, user management scenarios, and token acquisition processes specific to Azure AD B2C. By categorizing tests under B2C, it allows for targeted test runs that focus on validating the functionality and behavior of MSAL.NET in the context of Azure AD B2C authentication scenarios, ensuring that consumer applications using Azure AD B2C can authenticate users effectively and securely.
        /// </summary>
        public const string B2C = "B2C";

        /// <summary>
        /// Regression tests are tests that are designed to validate that existing functionality continues to work as expected after changes or updates have been made to the codebase. These tests typically cover a wide range of scenarios and features to ensure that any modifications do not introduce new bugs or break existing functionality. By categorizing tests under Regression, it allows for focused test runs that specifically target the validation of existing features and behaviors, providing confidence that changes have not negatively impacted the overall stability and reliability of the application.
        /// </summary>
        public const string Regression = "Regression";

        /// <summary>
        /// Arlington tests are tests that are specifically designed to validate the functionality and behavior of MSAL.NET in the context of the Arlington environment. Arlington is a specific testing environment or configuration that may have unique characteristics or requirements, and these tests are intended to ensure that MSAL.NET works correctly within that environment. By categorizing tests under Arlington, it allows for targeted test runs that focus on validating the compatibility and performance of MSAL.NET in the Arlington environment, ensuring that any issues specific to that environment are identified and addressed effectively.
        /// </summary>
        public const string Arlington = "Arlington";

        /// <summary>
        /// Broker tests are tests that focus on validating the functionality and behavior of MSAL.NET when using a broker for authentication. A broker is an intermediary component that handles authentication flows on behalf of the application, often providing additional features such as single sign-on (SSO) and enhanced security. These tests may cover various scenarios involving broker interactions, such as token acquisition, account management, and error handling. By categorizing tests under Broker, it allows for targeted test runs that specifically validate the integration and functionality of MSAL.NET with brokers, ensuring that applications relying on broker-based authentication can operate smoothly and securely.
        /// </summary>
        public const string Broker = "Broker";

        /// <summary>
        /// TokenCacheTests are tests that focus on validating the functionality and behavior of MSAL.NET's token cache. The token cache is a critical component that stores authentication tokens, allowing for efficient token management and reducing the need for repeated authentication requests. These tests may cover various scenarios related to token caching, such as cache serialization and deserialization, cache eviction policies, and cache access patterns. By categorizing tests under TokenCacheTests, it allows for targeted test runs that specifically validate the correctness and performance of the token cache implementation in MSAL.NET, ensuring that applications can effectively manage authentication tokens and provide a seamless user experience.
        /// </summary>
        public const string TokenCacheTests = "TokenCacheTests";

        /// <summary>
        /// PromptTests are tests that focus on validating the functionality and behavior of MSAL.NET's prompt parameter, which is used to control the user experience during authentication flows. The prompt parameter can be set to various values (e.g., "login", "select_account", "consent") to specify how the authentication process should interact with the user. These tests may cover scenarios such as ensuring that the correct prompts are displayed based on the specified parameters, validating the behavior of the application when different prompt values are used, and verifying that the user experience aligns with expectations. By categorizing tests under PromptTests, it allows for targeted test runs that specifically validate the implementation and behavior of the prompt parameter in MSAL.NET, ensuring that applications can provide a consistent and user-friendly authentication experience.
        /// </summary>
        public const string PromptTests = "PromptTests";

        /// <summary>
        /// BuilderTests are tests that focus on validating the functionality and behavior of MSAL.NET's builder pattern for constructing authentication requests. The builder pattern is a design pattern that allows for the creation of complex objects (in this case, authentication requests) through a step-by-step process, providing a fluent and intuitive API for developers. These tests may cover scenarios such as ensuring that the builder correctly constructs authentication requests based on the specified parameters, validating the behavior of the application when using the builder to create requests, and verifying that the resulting authentication flows align with expectations. By categorizing tests under BuilderTests, it allows for targeted test runs that specifically validate the implementation and behavior of the builder pattern in MSAL.NET, ensuring that developers can effectively utilize this pattern to create robust and flexible authentication requests.
        /// </summary>
        public const string BuilderTests = "BuilderTests";

        /// <summary>
        /// UnifiedSchemaValidation tests are tests that focus on validating the functionality and behavior of MSAL.NET in the context of a unified schema for authentication requests and responses. A unified schema is a standardized format for representing authentication data, which can help to simplify the development process and improve interoperability between different components and services. These tests may cover scenarios such as ensuring that MSAL.NET correctly handles authentication requests and responses that conform to the unified schema, validating the behavior of the application when using the unified schema, and verifying that the overall authentication flow aligns with expectations when using this standardized format. By categorizing tests under UnifiedSchemaValidation, it allows for targeted test runs that specifically validate the implementation and behavior of MSAL.NET in relation to the unified schema, ensuring that applications can effectively utilize this approach for authentication scenarios.
        /// </summary>
        public const string UnifiedSchemaValidation = "UnifiedSchema_Validation";
    }
}
