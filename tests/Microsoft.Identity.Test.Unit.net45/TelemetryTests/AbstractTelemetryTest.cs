// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    public abstract class AbstractTelemetryTest
    {
        internal ErrorStore _errorStore;
        internal TestTelemetryDispatcher _dispatcher;

        public virtual void Setup()
        {
            TestCommon.ResetInternalStaticCaches();
            _errorStore = new ErrorStore();
            _dispatcher = new TestTelemetryDispatcher();
        }

        public virtual void TearDown()
        {
            _errorStore = null;
            _dispatcher = null;
        }

        protected HashSet<string> GetAllowedScopes()
        {
            return new HashSet<string> { "scope", "scope1", "scope2" };
        }

        internal bool CheckError(List<PropertyBagContents> expectedErrors)
        {
            var observedErrors = _errorStore.GetEventsForUpload();
            int errorMatchCount = 0;

            foreach (var observedError in observedErrors)
            {
                foreach (var expectedError in expectedErrors)
                {
                    if (IsErrorPropertyBagContentsEqual(observedError.GetContents(), expectedError))
                    {
                        errorMatchCount++;
                    }
                }
            }

            if (errorMatchCount == expectedErrors.Count)
            {
                return true;
            }

            return false;
        }

        internal bool IsErrorPropertyBagContentsEqual(PropertyBagContents c1, PropertyBagContents c2)
        {
            try
            {
                if (c1.IntProperties[MatsErrorPropertyNames.TypeConstStrKey] != c2.IntProperties[MatsErrorPropertyNames.TypeConstStrKey])
                {
                    return false;
                }

                if (c1.StringProperties[MatsErrorPropertyNames.ErrorMessageConstStrKey] != c2.StringProperties[MatsErrorPropertyNames.ErrorMessageConstStrKey])
                {
                    return false;
                }

                // todo: discrepancy.  c++ mats code looks this up in StringProperties, but count is definitely an int...
                if (c1.IntProperties[MatsErrorPropertyNames.CountConstStrKey] != c2.IntProperties[MatsErrorPropertyNames.CountConstStrKey])
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        internal PropertyBagContents CreateErrorPropertyBagContents(ErrorType errorType, string errorMessage, ErrorSeverity errorSeverity)
        {
            return CreateErrorPropertyBagContents(errorType, errorMessage, errorSeverity, 1);
        }

        internal PropertyBagContents CreateErrorPropertyBagContents(ErrorType errorType, string errorMessage, ErrorSeverity errorSeverity, int errorCount)
        {
            var propertyBag = new PropertyBag(EventType.LibraryError, null);

            propertyBag.Add(MatsErrorPropertyNames.TypeConstStrKey, (int)errorType);
            propertyBag.Add(MatsErrorPropertyNames.ErrorMessageConstStrKey, errorMessage);
            propertyBag.Add(MatsErrorPropertyNames.SeverityConstStrKey, (int)errorSeverity);
            propertyBag.Add(MatsErrorPropertyNames.CountConstStrKey, errorCount);

            return propertyBag.GetContents();
        }
    }
}
