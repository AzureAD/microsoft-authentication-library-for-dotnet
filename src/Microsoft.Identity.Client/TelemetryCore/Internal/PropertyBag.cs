// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class PropertyBag : IPropertyBag
    {
        private readonly object _lockObjContent = new object();
        private readonly PropertyBagContents _contents;
        private readonly object _lockErrorType = new object();
        private ErrorType _errorType;
        private int _count;
        private const string _error = "Failed to modify PropertyBag: ";
        private readonly IErrorStore _errorStore;

        public PropertyBag(EventType eventType, IErrorStore errorStore)
        {
            _errorStore = errorStore;
            _contents = new PropertyBagContents(eventType);
            SetErrorType(eventType);
            _count = 1;
        }

        public void Add(string key, string value)
        {
            lock (_lockObjContent)
            {
                if (!IsNameValidForAdd(key, _contents, out string errorMessage))
                {
                    LogError(errorMessage);
                    return;
                }

                _contents.StringProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
            }
        }

        public void Add(string key, int value)
        {
            lock (_lockObjContent)
            {
                if (!IsNameValidForAdd(key, _contents, out string errorMessage))
                {
                    LogError(errorMessage);
                    return;
                }

                _contents.IntProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
            }
        }

        public void Add(string key, long value)
        {
            lock (_lockObjContent)
            {
                if (!IsNameValidForAdd(key, _contents, out string errorMessage))
                {
                    LogError(errorMessage);
                    return;
                }

                _contents.Int64Properties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
            }
        }

        public void Add(string key, bool value)
        {
            lock (_lockObjContent)
            {
                if (!IsNameValidForAdd(key, _contents, out string errorMessage))
                {
                    LogError(errorMessage);
                    return;
                }

                _contents.BoolProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
            }
        }

        public PropertyBagContents GetContents() => _contents;

        public void Update(string key, int value)
        {
            lock (_lockObjContent)
            {
                if (!IsValidExistingName(_contents.IntProperties, key, out string errorMessage))
                {
                    LogError(errorMessage);
                    return;
                }

                _contents.IntProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
            }
        }

        public void Sum(string key, int value)
        {
            lock (_lockObjContent)
            {
                if (!IsValidExistingName(_contents.IntProperties, key, out string errorMessage) &&
                    !IsNameValidForAdd(key, _contents, out errorMessage))
                {
                    LogError(_error + "Sum: " + key);
                    return;
                }

                string idx = MatsUtils.NormalizeValidPropertyName(key, out errorMessage);
                if (_contents.IntProperties.ContainsKey(idx))
                {
                    _contents.IntProperties[idx] += value;
                }
                else
                {
                    _contents.IntProperties[idx] = value;
                }
            }
        }

        public void Max(string key, int value)
        {
            lock (_lockObjContent)
            {
                bool containsProperty = IsValidExistingName(_contents.IntProperties, key, out string errorMessage);
                if (!containsProperty && !IsNameValidForAdd(key, _contents, out errorMessage))
                {
                    LogError(_error + "Max: " + key);
                    return;
                }

                if ((containsProperty && _contents.IntProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] < value) ||
                    !containsProperty)
                {
                    _contents.IntProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
                }
            }
        }

        public void Min(string key, int value)
        {
            lock (_lockObjContent)
            {
                bool containsProperty = IsValidExistingName(_contents.IntProperties, key, out string errorMessage);
                if (!containsProperty && !IsNameValidForAdd(key, _contents, out errorMessage))
                {
                    LogError(_error + "Min: " + key);
                    return;
                }

                if ((containsProperty && _contents.IntProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] > value) ||
                    !containsProperty)
                {
                    _contents.IntProperties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
                }
            }
        }

        public void Sum(string key, long value)
        {
            lock (_lockObjContent)
            {
                if (!IsValidExistingName(_contents.Int64Properties, key, out string errorMessage) &&
                    !IsNameValidForAdd(key, _contents, out errorMessage))
                {
                    LogError(_error + "Sum: " + key);
                    return;
                }

                string idx = MatsUtils.NormalizeValidPropertyName(key, out errorMessage);
                if (_contents.Int64Properties.ContainsKey(idx))
                {
                    _contents.Int64Properties[idx] += value;
                }
                else
                {
                    _contents.Int64Properties[idx] = value;
                }
            }
        }

        public void Max(string key, long value)
        {
            lock (_lockObjContent)
            {
                bool containsProperty = IsValidExistingName(_contents.Int64Properties, key, out string errorMessage);
                if (!containsProperty && !IsNameValidForAdd(key, _contents, out errorMessage))
                {
                    LogError(_error + "Max: " + key);
                    return;
                }

                if ((containsProperty && _contents.Int64Properties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] < value) ||
                    !containsProperty)
                {
                    _contents.Int64Properties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
                }
            }
        }
        
        public void Min(string key, long value) 
        {
            lock (_lockObjContent)
            {
                bool containsProperty = IsValidExistingName(_contents.Int64Properties, key, out string errorMessage);
                if (!containsProperty && !IsNameValidForAdd(key, _contents, out errorMessage))
                {
                    LogError(_error + "Min: " + key);
                    return;
                }

                if ((containsProperty && _contents.Int64Properties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] > value) ||
                    !containsProperty)
                {
                    _contents.Int64Properties[MatsUtils.NormalizeValidPropertyName(key, out errorMessage)] = value;
                }
            }
        }

        public void IncrementCount()
        {
            lock (_lockObjContent)
            {
                _count++;
            }
        }

        public int GetCount()
        {
            lock (_lockObjContent)
            {
                return _count;
            }
        }

        private void LogError(string error)
        {
            if (_errorStore != null)
            {
                lock (_lockErrorType)
                {
                    _errorStore.ReportError(error, _errorType, ErrorSeverity.Warning);
                }
            }
        }

        private bool IsValidExistingName<T>(ConcurrentDictionary<string, T> map, string key, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!MatsUtils.IsValidPropertyName(key, out errorMessage))
            {
                return false;
            }

            if (!map.ContainsKey(MatsUtils.NormalizeValidPropertyName(key, out errorMessage)))
            {
                errorMessage = string.Format(CultureInfo.InvariantCulture, "Property '{0}' does not exist in the property map", key);
                return false;
            }

            return true;
        }

        private bool IsNameValidForAdd(string name, PropertyBagContents content, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!MatsUtils.IsValidPropertyName(name, out errorMessage))
            {
                return false;
            }

            if (!IsPropertyNameUnique(name, content))
            {
                errorMessage = string.Format(CultureInfo.InvariantCulture, "Property Name '{0}' is not unique", name);
                return false;
            }

            return true;
        }

        private bool IsPropertyNameUnique(string name, PropertyBagContents content)
        {
            if (content.BoolProperties.ContainsKey(name) ||
                content.StringProperties.ContainsKey(name) ||
                content.IntProperties.ContainsKey(name) ||
                content.Int64Properties.ContainsKey(name))
            {
                return false;
            }

            return true;
        }

        private void SetErrorType(EventType eventType)
        {
            lock (_lockErrorType)
            {
                switch (eventType)
                {
                case EventType.Scenario:
                    _errorType = ErrorType.Scenario;
                    break;

                case EventType.Action:
                    _errorType = ErrorType.Action;
                    break;

                default:
                    _errorType = ErrorType.Other;
                    break;
                }
            }
        }
    }
}
