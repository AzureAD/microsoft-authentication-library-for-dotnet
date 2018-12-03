using System;
using System.Globalization;

namespace Microsoft.Identity.Test.Common.Core.Mocks.Exceptions
{
    public class TestServiceException : TestException
    {
        public bool IsUiRequired { get; set; }

        public TestServiceException(string errorCode, string errorMessage)
            : base(
                errorCode, errorMessage)
        {
        }

        public TestServiceException(string errorCode, string errorMessage, int statusCode)
            : this(errorCode, errorMessage)
        {
            StatusCode = statusCode;
        }

        public TestServiceException(string errorCode, string errorMessage,
            Exception innerException)
            : base(
                errorCode, errorMessage, innerException)
        {
        }

        /// </param>
        public TestServiceException(string errorCode, string errorMessage, int statusCode,
            Exception innerException)
            : base(
                errorCode, errorMessage, innerException)
        {
            StatusCode = statusCode;
        }


        public TestServiceException(string errorCode, string errorMessage, int statusCode, string claims,
            Exception innerException)
            : this(
                errorCode, errorMessage, statusCode, innerException)
        {
            Claims = claims;
        }

        public int StatusCode { get; internal set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public string Claims { get; internal set; }

        /// <summary>
        /// Raw response body received from the server.
        /// </summary>
        public string ResponseBody { get; internal set; }

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            return base.ToString() + string.Format(CultureInfo.InvariantCulture, "\n\tStatusCode: {0}\n\tClaims: {1}", StatusCode, Claims);
        }
    }
}
