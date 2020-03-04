Appium Provision:

1. Install appium (http://appium.io/)
2. Make sure you have ChromeDriver installed locally. The version must match Chrome version on Android device (don't worry if it doesn't - the error message is pretty clear)
3. Make you have ANDROID_HOME added as env variable and %ANDROID_HOME%/tools and %ANDROID_HOME%/platform_tools added to path

On my machine, ANDROID_HOME = C:\Program Files (x86)\Android\android-sdk

Android Provision:

1. Archive the test app (select "AppiumAutomation.Android" > right clock > archive)
2. Copy path to apk and add it to config section.
3. Copy path to chromedriver and add it to config section
4. Start an emulator (or plug in a phone - haven't tried this yet)

Run test: 

Run test Android_AAD_SystemWebView_Async

Note: config section is is UnitTest1.cs at the moment.

