// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Android.Runtime;
using Microsoft.Intune.Mam.Client.Notification;
using Microsoft.Intune.Mam.Policy;
using Microsoft.Intune.Mam.Policy.Notification;

namespace Intune_xamarin_Android
{
    /// <summary>
    /// Receives enrollment notifications from the Intune service and performs the corresponding action for the enrollment result.
    /// See: https://docs.microsoft.com/en-us/intune/app-sdk-android#mamnotificationreceiver
    /// </summary>
    class EnrollmentNotificationReceiver : Java.Lang.Object, IMAMNotificationReceiver
    {
        /// <summary>
        /// When using the MAM-WE APIs found in IMAMEnrollManager, your app wil receive 
        /// IMAMEnrollmentNotifications back to signal the result of your calls.
        /// When enrollment is successful, this will signal that app has been registered and it can proceed ahead.
        /// </summary>
        /// <param name="notification">The notification that was received.</param>
        /// <returns>
        /// The receiver should return true if it handled the notification without error(or if it decided to ignore the notification). 
        /// If the receiver tried to take some action in response to the notification but failed to complete that action it should return false.
        /// </returns>
        public bool OnReceive(IMAMNotification notification)
        {
            if (notification.Type == MAMNotificationType.MamEnrollmentResult)
            {
                IMAMEnrollmentNotification enrollmentNotification = notification.JavaCast<IMAMEnrollmentNotification>();
                MAMEnrollmentManagerResult result = enrollmentNotification.EnrollmentResult;

                if (result.Equals(MAMEnrollmentManagerResult.EnrollmentSucceeded))
                {
                    // this signals that MAM registration is complete and the app can proceed
                    IntuneSampleApp.MAMRegsiteredEvent.Set();
                }
            }

            return true;
        }
    }
}
