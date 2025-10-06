        [TestMethod]
        public void ClaimsHelper_ErrorMessage_HandlesEmptyEnvVariable()
        {
            // Test that the error message handles empty environment variable correctly
            string errorMsg = MsalErrorMessage.JsonEncoderIntrinsicsUnsupported("Arm64", true, "");
            Assert.IsTrue(errorMsg.Contains("JSON encoding failed"));
            Assert.IsTrue(errorMsg.Contains("Arm64"));
            Assert.IsTrue(errorMsg.Contains("Is 64-bit process: True"));
            Assert.IsTrue(errorMsg.Contains("DOTNET_EnableHWIntrinsic: (not set)"));
        }
