// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class ScopeHelperTests
    {
        private const string LotsOfScopes = "Agreement.Read.All Agreement.ReadWrite.All AgreementAcceptance.Read AgreementAcceptance.Read.All AllSites.FullControl AllSites.Manage AllSites.Read AllSites.Write AppCatalog.ReadWrite.All AuditLog.Read.All Bookings.Manage.All Bookings.Read.All Bookings.ReadWrite.All BookingsAppointment.ReadWrite.All Calendars.Read Calendars.Read.All Calendars.Read.Shared Calendars.ReadWrite Calendars.ReadWrite.All Calendars.ReadWrite.Shared Contacts.Read Contacts.Read.All Contacts.Read.Shared Contacts.ReadWrite Contacts.ReadWrite.All Contacts.ReadWrite.Shared Device.Command Device.Read DeviceManagementApps.Read.All DeviceManagementApps.ReadWrite.All DeviceManagementConfiguration.Read.All DeviceManagementConfiguration.ReadWrite.All DeviceManagementManagedDevices.PrivilegedOperations.All DeviceManagementManagedDevices.Read.All DeviceManagementManagedDevices.ReadWrite.All DeviceManagementRBAC.Read.All DeviceManagementRBAC.ReadWrite.All DeviceManagementServiceConfig.Read.All DeviceManagementServiceConfig.ReadWrite.All Directory.AccessAsUser.All Directory.Read.All Directory.ReadWrite.All EAS.AccessAsUser.All EduAdministration.Read EduAdministration.ReadWrite EduAssignments.Read EduAssignments.ReadBasic EduAssignments.ReadWrite EduAssignments.ReadWriteBasic EduRoster.Read EduRoster.ReadBasic EduRoster.ReadWrite email EWS.AccessAsUser.All Exchange.Manage Files.Read Files.Read.All Files.Read.Selected Files.ReadWrite Files.ReadWrite.All Files.ReadWrite.AppFolder Files.ReadWrite.Selected Financials.ReadWrite.All Group.Read.All Group.ReadWrite.All IdentityProvider.Read.All IdentityProvider.ReadWrite.All IdentityRiskEvent.Read.All Mail.Read Mail.Read.All Mail.Read.Shared Mail.ReadWrite Mail.ReadWrite.All Mail.ReadWrite.Shared Mail.Send Mail.Send.All Mail.Send.Shared MailboxSettings.Read MailboxSettings.ReadWrite Member.Read.Hidden MyFiles.Read MyFiles.Write Notes.Create Notes.Read Notes.Read.All Notes.ReadWrite Notes.ReadWrite.All Notes.ReadWrite.CreatedByApp offline_access openid People.Read People.Read.All People.ReadWrite PrivilegedAccess.ReadWrite.AzureAD PrivilegedAccess.ReadWrite.AzureResources profile Reports.Read.All SecurityEvents.Read.All SecurityEvents.ReadWrite.All Sites.FullControl.All Sites.Manage.All Sites.Read.All Sites.ReadWrite.All Sites.Search.All Subscription.Read.All Tasks.Read Tasks.Read.Shared Tasks.ReadWrite Tasks.ReadWrite.Shared TermStore.Read.All TermStore.ReadWrite.All User.Export.All User.Invite.All User.Read User.Read.All User.ReadBasic.All User.ReadWrite User.ReadWrite.All UserActivity.ReadWrite.CreatedByApp";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void ScopeHelperPerf()
        {
            ISet<string> scopeSet = null;
            using (new PerformanceValidator(100, "Convert scope string to set"))
            {
                // about 500ms for 5000 iterations -> down to 160ms after replacing SortedSet with HashSet
                for (int i = 0; i < 1000; i++)
                {
                    scopeSet = ScopeHelper.ConvertStringToScopeSet(LotsOfScopes);
                }
            }
            bool contains = true;
            using (new PerformanceValidator(100, "Scope contains"))
            {
                // about 150ms for 10000 iterations -> down to 5ms after replacing SortedSet with HashSet
                for (int i = 0; i < 10000; i++)
                {
                    contains = ScopeHelper.ScopeContains(
                    scopeSet,
                    new[] { "Tasks.ReadWrite", "Agreement.ReadWrite.All", "bogus" });
                }
            }
            Assert.IsFalse(contains);           
        }
    }
}
