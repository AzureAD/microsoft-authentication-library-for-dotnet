using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
{
    public interface ITestController
    {
        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to tap</param>
        void Tap(string elementID);

        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to tap</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void Tap(string elementID, bool isWebElement);

        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to tap</param>
        /// <param name="waitTime">The time in seconds in which the test controller will search for the element before it times out</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void Tap(string elementID, int waitTime, bool isWebElement);

        /// <summary>
        /// enters text into a text field on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to enter text into</param>
        /// <param name="text">The text to be entered into the element</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void EnterText(string elementID, string text, bool isWebElement);

        /// <summary>
        /// enters text into a text field on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to enter text into</param>
        /// <param name="waitTime">The time in seconds in which the test controller will search for the element before it times out</param>
        /// <param name="text">The text to be entered into the element</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void EnterText(string elementID, int waitTime, string text, bool isWebElement);

        /// <summary>
        /// Querys the Ui until an element is found on the UI
        /// </summary>
        /// <param name="automationID">ID of the element to look for</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        /// <returns>return details of the object that is being searched for.</returns>
        object[] WaitForElement(string automationID, bool isWebElement);

        /// <summary>
        /// Returns the text of an element wisible on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to get text from</param>
        /// <returns>The text value of the element</returns>
        string GetText(string elementID);

        /// <summary>
        /// Returns a test user account for use in testing.
        /// An exception is thrown if no matching user is found.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>A single user that matches the given query parameters.</returns>
        IUser GetUser(UserQueryParameters query);
    }
}
