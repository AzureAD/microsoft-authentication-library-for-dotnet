// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using OpenQA.Selenium;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    /// <summary>
    /// Manages a pool of WebDriver instances for parallel test execution.
    /// </summary>
    public class WebDriverPool : IDisposable
    {
        private static readonly object _lockObject = new object();
        private static WebDriverPool _instance;

        private readonly ConcurrentBag<IWebDriver> _availableDrivers;
        private readonly ConcurrentDictionary<IWebDriver, bool> _inUseDrivers;
        private readonly int _maxDrivers;
        private readonly int _timeoutSeconds;
        private bool _isDisposed;

        /// <summary>
        /// Gets the singleton instance of WebDriverPool.
        /// </summary>
        public static WebDriverPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        _instance ??= new WebDriverPool();
                    }
                }
                return _instance;
            }
        }

        private WebDriverPool(int maxDrivers = 5, int timeoutSeconds = 20)
        {
            _maxDrivers = maxDrivers;
            _timeoutSeconds = timeoutSeconds;
            _availableDrivers = new ConcurrentBag<IWebDriver>();
            _inUseDrivers = new ConcurrentDictionary<IWebDriver, bool>();
        }

        /// <summary>
        /// Acquires a WebDriver from the pool or creates a new one if needed.
        /// </summary>
        /// <returns>An initialized WebDriver instance.</returns>
        public IWebDriver AcquireDriver()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(WebDriverPool));

            // Try to get a driver from the available pool
            if (_availableDrivers.TryTake(out IWebDriver driver))
            {
                try
                {
                    // Make sure the driver is still responsive
                    driver.Url = "about:blank";
                    _inUseDrivers.TryAdd(driver, true);
                    return driver;
                }
                catch (Exception ex)
                {
                    // If driver has become unresponsive, dispose it and create a new one
                    Trace.WriteLine($"WebDriver in pool is unresponsive: {ex.Message}");
                    DisposeDriverSafely(driver);
                }
            }

            // Create a new driver if we didn't get one from the pool
            // or if the one we got was unresponsive
            return CreateAndTrackNewDriver();
        }

        /// <summary>
        /// Returns a WebDriver to the pool for reuse.
        /// </summary>
        /// <param name="driver">The WebDriver to return to the pool.</param>
        public void ReleaseDriver(IWebDriver driver)
        {
            if (_isDisposed)
                return;

            if (driver == null)
                return;

            if (_inUseDrivers.TryRemove(driver, out _))
            {
                try
                {
                    // Clear cookies and navigate to blank page to reset state
                    driver.Manage().Cookies.DeleteAllCookies();
                    driver.Navigate().GoToUrl("about:blank");
                    _availableDrivers.Add(driver);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error when releasing driver to pool: {ex.Message}");
                    DisposeDriverSafely(driver);
                }
            }
        }

        private IWebDriver CreateAndTrackNewDriver()
        {
            // Check if we've reached the maximum number of drivers
            if (_inUseDrivers.Count >= _maxDrivers)
            {
                // Wait for a driver to become available
                SpinWait.SpinUntil(() => _inUseDrivers.Count < _maxDrivers || _isDisposed, TimeSpan.FromSeconds(30));

                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(WebDriverPool));

                // If we still have too many drivers, throw an exception
                if (_inUseDrivers.Count >= _maxDrivers)
                    throw new InvalidOperationException($"Maximum number of WebDrivers ({_maxDrivers}) exceeded");
            }

            // Create a new driver and add it to the in-use collection
            IWebDriver newDriver = SeleniumExtensions.CreateDefaultWebDriver(_timeoutSeconds);
            _inUseDrivers.TryAdd(newDriver, true);
            return newDriver;
        }

        private void DisposeDriverSafely(IWebDriver driver)
        {
            try
            {
                driver?.Quit();
                driver?.Dispose();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error disposing WebDriver: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes all WebDriver instances in the pool.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Dispose all drivers in both collections
            foreach (var driver in _inUseDrivers.Keys)
            {
                DisposeDriverSafely(driver);
            }

            while (_availableDrivers.TryTake(out IWebDriver driver))
            {
                DisposeDriverSafely(driver);
            }

            _inUseDrivers.Clear();
        }
    }
}
