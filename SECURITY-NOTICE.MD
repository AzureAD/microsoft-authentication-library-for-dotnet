## Security Issue in Multiple Versions of ADAL .Net ###

A defect in ADAL .Net can result in an elevation of privilege in specific problem scenarios. The problem scenarios involve the On Behalf Of protocol flow and specific use cases of a ClientAssertion/ClientAssertionCertificate/ClientCredential and UserAssertion being passed to the AcquireToken* API. Multiple versions of the library are affected. Affected versions are listed below.

We have emailed owners of active applications that are using an impacted version of the library in the specific problem scenarios.

The latest stable version of the library does not have the defect. To avoid being impacted we strongly recommend you update to at least 2.28.1 for 2.x, 3.13.4 for 3.x, or the latest stable version. If you have questions about this issue, please email aadintegrate@microsoft.com.

Affected 2.x versions: 2.27.306291202, 2.26.305102204, 2.26.305100852, 2.25.305061457, 2.21.301221612, 2.20.301151232, 2.19.208020213, 2.18.206251556, 2.17.206230854, 2.16.204221202, 2.15.204151539, 2.14.201151115, 2.13.112191810, 2.12.111071459, 2.11.10918.1222, 2.10.10910.1511, 2.9.10826.1824, 2.8.10804.1442-rc, 2.7.10707.1513-rc, 2.6.2-alpha, 2.6.1-alpha, 2.5.1-alpha

Affected 3.x versions: 3.11.305310302-alpha, 3.10.305231913, 3.10.305161347, 3.10.305110106, 3.5.208051316-alpha, 3.5.208012240-alpha, 3.5.207081303-alpha, 3.4.206191646-alpha, 3.3.205061641-alpha, 3.2.204281119-alpha, 3.1.203031538-alpha, 3.0.110281957-alpha
