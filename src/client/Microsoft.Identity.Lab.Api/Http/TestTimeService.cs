// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// Time service implementation for testing purposes. This class allows tests to control the current time by providing methods to move the time forward or backward, enabling the simulation of time-dependent scenarios such as token expiration and refresh logic in MSAL.NET.
    /// </summary>
    public class TestTimeService : ITimeService
    {
        private DateTime _utcNow;

        /// <summary>
        /// Time service implementation for testing purposes. Initializes the current time to the system's UTC time at the moment of instantiation, allowing tests to have a consistent starting point for time-based operations. Tests can then manipulate this time using the provided methods to simulate various scenarios.
        /// </summary>
        public TestTimeService()
        {
            _utcNow = DateTime.UtcNow;
        }

        /// <summary>
        /// Time service implementation for testing purposes. Initializes the current time to a specified UTC time, allowing tests to start with a predefined time context. This is particularly useful for testing scenarios that require a specific time frame or for simulating conditions that depend on a certain point in time.
        /// </summary>
        /// <param name="utcNow"></param>
        public TestTimeService(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        /// <summary>
        /// gets the current UTC time as maintained by this test time service. This method allows tests to retrieve the current time, which can be manipulated using the provided methods to simulate the passage of time or to set specific time conditions for testing purposes.
        /// </summary>
        /// <returns>The current UTC time.</returns>
        public DateTime GetUtcNow()
        {
            return _utcNow;
        }

        /// <summary>
        /// Moves the current time forward by the specified time span. This method allows tests to simulate the passage of time, which is useful for testing scenarios that depend on time-based conditions such as token expiration or refresh logic.
        /// </summary>
        /// <param name="span">The time span to move forward.</param>
        public void MoveToFuture(TimeSpan span)
        {
            _utcNow += span;
        }

        /// <summary>
        /// Moves the current time backward by the specified time span. This method allows tests to simulate the passage of time in the past, which is useful for testing scenarios that depend on time-based conditions such as token expiration or refresh logic.
        /// </summary>
        /// <param name="span">The time span to move backward.</param>
        public void MoveToPast(TimeSpan span)
        {
            _utcNow -= span;
        }
    }
}
