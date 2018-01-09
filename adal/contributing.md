# CONTRIBUTING

Azure Active Directory SDK projects welcomes new contributors.  This document will guide you
through the process.

### Contributor License agreement

Please visit [https://cla.microsoft.com/](https://cla.microsoft.com/) and sign the Contributor License
Agreement.  You only need to do that once. We can not look at your code until you've submitted this request.


### Fork
> Important node: 
Because Nuget brings very long assemblies file names, you'd want to clone ADAL.NET in a folder which has a short name and is very close to the root of your hard drive (for instance C:\aad). then,  might want to rename the cloned folder to a shorted name (see below).

Fork the project [on GitHub][] and check out
your copy.

Example for .NET:

```
$ git clone git@github.com:username/azure-activedirectory-library-for-dotnet.git adal.net
$ cd adal.net
$ git remote add upstream git@github.com:AzureAD/azure-activedirectory-library-for-dotnet.git
```

### Initial build of ADAL.Net
We recommend that you use Visual Studio 2017 with Xamarin, .Net Core and UWP installed. 
You will also need Visual Studio 2015 update 3 with the windows 8.1 SDK installed in order to build ADAL.NET for the WinRT platform
> Important node: 
ADAL.Net uses a multi-targeting projects to generate assemblies for several platforms. There are issues with the Nuget package manager in Visual Studio 2017, not recogizing nuget pacakges in the ADAL.NET project. Therefore you'd want to restore the nuget packages using this command line:

```
$ msbuild ADAL.NET.NoWinRT.sln /t:restore 
```

you can then either build ADAL.Net in Visual Studio or from the command line:
```
$ msbuild ADAL.NET.NoWinRT.sln /t:build /p:configuration=release 
```

To run the tests:
-Skip strong name Verification for this assembly for on your machine (run in command prompt as an admin)
```
sn -Vr *,31bf3856ad364e35
```

Run the unit tests:
```
vstest.console tests\Test.ADAL.NET.Unit\bin\release\net462\Test.ADAL.NET.Unit.dll
```

Run the integration tests:
```
vstest.console tests\Test.ADAL.NET.Integration\bin\Release\net462\Test.ADAL.NET.Integration.dll
```

When you are done re-enable  strong name Verification for this assembly for on your machine (run in command prompt as an admin)
```
sn -Vu *,31bf3856ad364e35
```


### Decide on which branch to create
Now decide if you want your feature or bug fix to go into the current stable version or the next version of the library. 

**Bug fixes for the current stable version need to go to 'servicing' branch.**

**New features for the next version should go to 'dev' branch.** 

The master branch is effectively frozen; patches that change the SDKs
protocols or API surface area or affect the run-time behavior of the SDK will be rejected.

Some of our SDKs have bundled dependencies that are not part of the project proper.  Any changes to files in those directories or its subdirectories should be sent to their respective
projects.  Do not send your patch to us, we cannot accept it.

In case of doubt, open an issue in the [issue tracker][].

Especially do so if you plan to work on a major change in functionality.  Nothing is more
frustrating than seeing your hard work go to waste because your vision
does not align with our goals for the SDK.


### Branch

Okay, so you have decided on the proper branch.  Create a feature branch
and start hacking:

```
$ git checkout -b my-feature-branch 
```


### Commit

Make sure git knows your name and email address:

```
$ git config --global user.name "J. Random User"
$ git config --global user.email "j.random.user@example.com"
```

Writing good commit logs is important.  A commit log should describe what
changed and why.  Follow these guidelines when writing one:

1. The first line should be 50 characters or less and contain a short
   description of the change prefixed with the name of the changed
   subsystem (e.g. "net: add localAddress and localPort to Socket").
2. Keep the second line blank.
3. Wrap all other lines at 72 columns.

A good commit log looks like this:

```
fix: explaining the commit in one line

Body of commit message is a few lines of text, explaining things
in more detail, possibly giving some background about the issue
being fixed, etc etc.

The body of the commit message can be several paragraphs, and
please do proper word-wrap and keep columns shorter than about
72 characters or so. That way `git log` will show things
nicely even when it is indented.
```

The header line should be meaningful; it is what other people see when they
run `git shortlog` or `git log --oneline`.

Check the output of `git log --oneline files_that_you_changed` to find out
what directories your changes touch.


### Rebase

Use `git rebase` (not `git merge`) to sync your work from time to time.

```
$ git fetch upstream
$ git rebase upstream/v0.1  # or upstream/master
```


### Test

Bug fixes and features should come with tests.  Add your tests in the
test directory. This varies by repository but often follows the same convention of /src/test.  Look at other tests to see how they should be
structured (license boilerplate, common includes, etc.).

Before you can run tests you will need to enable Skip Verification for on your machine.  Open the 'Developer Command Prompt for VS2017' as an administrator and run the following command:

```
sn -Vr *,31bf3856ad364e35
```

Make sure that all tests pass.


### Push

```
$ git push origin my-feature-branch
```

Go to https://github.com/username/azure-activedirectory-library-for-***.git and select your feature branch.  Click
the 'Pull Request' button and fill out the form.

Pull requests are usually reviewed within a few days.  If there are comments
to address, apply your changes in a separate commit and push that to your
feature branch.  Post a comment in the pull request afterwards; GitHub does
not send out notifications when you add commits.





[on GitHub]: https://github.com/AzureAD/azure-activedirectory-library-for-dotnet
[issue tracker]: https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues
