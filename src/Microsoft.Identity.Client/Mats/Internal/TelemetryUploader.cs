// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class TelemetryUploader : IUploader
    {
        private readonly ITelemetryDispatcher _telemetryDispatcher;

        public TelemetryUploader(ITelemetryDispatcher dispatcher, string appName)
        {
            _telemetryDispatcher = dispatcher;
            AppName = appName;
        }

        public string AppName {get;}

        public void Upload(IEnumerable<PropertyBagContents> uploadEvents)
        {
            if (_telemetryDispatcher == null)
            {
                return;
            }

            foreach (var uploadEvent in uploadEvents)
            {
                string name = UploadEventUtils.GetUploadEventName(uploadEvent.EventType, AppName);
                var data = new MatsTelemetryData(name, uploadEvent);
                _telemetryDispatcher.DispatchEvent(data);
            }
        }
    }
}
