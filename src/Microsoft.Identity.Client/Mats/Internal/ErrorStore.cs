// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class ErrorStore : IErrorStore
    {
        private readonly List<IPropertyBag> _propertyBagList = new List<IPropertyBag>();
        private readonly object _lockPropertyBagList = new object();

        public void Append(IErrorStore errorStore) => throw new NotImplementedException();
        public void Clear()
        {
            lock (_lockPropertyBagList)
            {
                _propertyBagList.Clear();
            }
        }

        public IEnumerable<IPropertyBag> GetEventsForUpload()
        {
            lock (_lockPropertyBagList)
            {
                return _propertyBagList;
            }
        }

        public void ReportError(string errorMessage, ErrorType errorType, ErrorSeverity errorSeverity)
        {
            ReportError(errorMessage, errorType, errorSeverity, 1);
        }

        private void ReportError(string errorMessage, ErrorType errorType, ErrorSeverity errorSeverity, int count)
        {
            lock (_lockPropertyBagList)
            {
                if (UpdateErrorCountIfPreviouslySeen(errorMessage, count))
                {
                    // Since we just updated the error count, no need to make any other changes.
                    return;
                }

                // Record the time the first error was hit
                DateTime currentTime = DateTime.UtcNow;

                //A nullptr is passed as we don't want errors being logged on this property bag, since it could cause an infinite loop of errors
                var propertyBag = new PropertyBag(EventType.LibraryError, null);
                propertyBag.Add(MatsErrorPropertyNames.TypeConstStrKey, (int)errorType);
                propertyBag.Add(MatsErrorPropertyNames.SeverityConstStrKey, (int)errorSeverity);
                propertyBag.Add(MatsErrorPropertyNames.ErrorMessageConstStrKey, errorMessage);
                propertyBag.Add(MatsErrorPropertyNames.TimestampConstStrKey, DateTimeUtils.GetMillisecondsSinceEpoch(currentTime));
                propertyBag.Add(MatsErrorPropertyNames.CountConstStrKey, count);

                _propertyBagList.Add(propertyBag);
            }
        }

        private bool UpdateErrorCountIfPreviouslySeen(string errorMessage, int additionalCount)
        {
            lock (_lockPropertyBagList)
            {
                try
                {
                    foreach (var propertyBag in _propertyBagList)
                    {
                        var contents = propertyBag.GetContents();

                        if (!contents.StringProperties.ContainsKey(MatsErrorPropertyNames.ErrorMessageConstStrKey))
                        {
                            continue;
                        }

                        if (string.Compare(
                            contents.StringProperties[MatsErrorPropertyNames.ErrorMessageConstStrKey],
                            errorMessage,
                            StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            int currentErrorCount;
                            if (contents.IntProperties.ContainsKey(MatsErrorPropertyNames.CountConstStrKey))
                            {
                                // This means an error was logged without a count, which should never happen. In this case, we'll just set the current error count at 1.
                                currentErrorCount = 1;
                            }
                            else
                            {
                                currentErrorCount = contents.IntProperties[MatsErrorPropertyNames.CountConstStrKey];
                            }

                            currentErrorCount += additionalCount;

                            propertyBag.Update(MatsErrorPropertyNames.CountConstStrKey, currentErrorCount);
                            return true;

                        }
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
