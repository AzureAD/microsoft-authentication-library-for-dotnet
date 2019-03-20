// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class TelemetryUploader : IUploader
    {
        private readonly ITelemetryDispatcher _telemetryDispatcher;
        private readonly IPlatformProxy _platformProxy;

        public TelemetryUploader(ITelemetryDispatcher dispatcher, IPlatformProxy platformProxy, string appName)
        {
            _telemetryDispatcher = dispatcher;
            _platformProxy = platformProxy;
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
                string name = UploadEventUtils.GetUploadEventName(_platformProxy, uploadEvent.EventType, AppName);
                _telemetryDispatcher.DispatchEvent(MatsTelemetryBatch.Create(name, uploadEvent));
            }
        }
    }
}
