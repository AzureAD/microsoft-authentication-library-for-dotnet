// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.Common
{
    public static class TestCategories
    {
        /// <summary>
        /// Tests under this category use a Selenium driven browser (Chrome) to automate the web ui.
        /// When run in the lab, the browser is configured to run headless.
        /// For debugging, consider running with the actual browser.
        /// </summary>
        public const string Selenium = "Selenium";
        public const string LabAccess = "LabAccess";

        public const string ADFS = "ADFS";
        public const string MSA = "MSA";
        public const string B2C = "B2C";

        public const string Regression = "Regression";

        public const string Arlington = "Arlington";

        public const string Broker = "Broker";
        public const string TokenCacheTests = "TokenCacheTests";
        public const string PromptTests = "PromptTests";
        public const string BuilderTests = "BuilderTests";
        public const string UnifiedSchemaValidation = "UnifiedSchema_Validation";
    }
}
