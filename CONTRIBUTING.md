# Contributing to MSAL.NET

Microsoft Authentication Library (MSAL) for .NET welcomes new contributors.  This document will guide you through the process.

## Contributor License Agreement

Please visit [https://cla.microsoft.com/](https://cla.microsoft.com/) and sign the Contributor License Agreements.  You only need to do that once. We can not look at your code until you've submitted this request.

## Contributing your own code changes

### Finding an issue to work on

Over the years we've seen many pull requests targeting areas of the code which are not urgent or critical for us to address, or areas which we didn't plan to expand further at the time. In all these cases we had to say no to those PRs and close them. That, obviously, is not a great outcome for us. And it's especially bad for the contributor, as they've spent a lot of effort preparing the change. To resolve this problem, we've decided to separate a bucket of issues, which would be great candidates for community members to contribute to. We mark these issues with the `help wanted` label. You can find all these issues [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues?q=is%3Aopen+is%3Aissue+label%3A%22help+wanted%22+label%3A%22good+first+issue%22+).

With that said, we have additionally marked issues that are good candidates for first-time contributors. Those do not require too much familiarity with the authentication and authorization and are more novice-friendly. Those are marked with the `good first issue` label.

If you would like to make a contribution to an [area not captured](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues?q=is%3Aopen+is%3Aissue+label%3A%22help+wanted%22+label%3A%22good+first+issue%22+), first open an issue with a description of the change you would like to make and the problem it solves so it can be discussed before a pull request is submitted.

If you would like to work on an involved feature, please file a design proposal first; more instructions can be found below, under [Before writing code](#before-writing-code).

### Before writing code

We've seen pull requests, where customers would solve an issue in a way which either wouldn't fit into the end-to-end design because of how it's implemented, or it would affect the design in a way, which is not something we'd like to do. To avoid these situations and potentially save you a lot of time, we encourage customers to discuss the preferred design with the team first. To do so, file a [new design proposal issue]((https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?assignees=&labels=design-proposal&projects=&template=design_proposal.md)), link it to the issue you'd like to address, and provide detailed information about how you'd like to solve a specific problem.

### Identifying scale

If you would like to contribute to one of our repositories, first identify the scale of what you would like to change. If it is small (grammar/spelling or a bug fix), feel free to start working on a fix. If you are submitting a feature or a substantial code contribution, please discuss it with the team and ensure it follows the product roadmap. You might also read these two blogs posts on contributing code: [Open Source Contribution Etiquette by Miguel de Icaza](http://tirania.org/blog/archive/2010/Dec-31.html) and [Don't "Push" Your Pull Requests by Ilya Grigorik](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/). All code submissions will be rigorously reviewed and tested further by the team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

### Before submitting a pull request

Before submitting a pull request, make sure that it passes the following requirements:

- You found an existing issue with a `help-wanted` label or discussed with the team and agreed on adding a new issue with that label.
- You posted a high-level description of how the changes will be implemented and received a positive acknowledgement from the team before getting too committed to the approach or investing too much effort in implementing it.
- You added test coverage following existing patterns within the codebase.
- Your code matches the existing syntax conventions within the codebase.
- Your PR is small, focused, and avoids making unrelated changes.

If your pull request contains any of the below, it's less likely to be merged.

- Changes that break backward compatibility
- Changes that are only wanted by one person or company. Changes need to benefit a large enough proportion of developers using our auth libraries.
- Changes that add entirely new feature areas without prior agreement
- Changes that are mostly about refactoring existing code or code style

Very large PRs that would take hours to review (remember, we're trying to help lots of people at once). For larger work areas, please discuss with us to find ways of breaking it down into smaller, incremental pieces that can go into separate PRs.

### Ensuring test coverage

- Tests need to be provided for every bug and feature that is completed.
  - Unit tests must cover all new aspects of the code.
- Before and after performance and stress tests results are successfully evaluated - no regressions are allowed.
- Performance and stress tests are extended as relevant.

Note that you won't be able to run integration tests locally because they connect to Azure Key Vault to fetch some test users and passwords.

### Submitting a pull request

If you're not sure how to create a pull request, read GitHub's [About pull requests
](https://help.github.com/articles/using-pull-requests) article. Make sure the repository can build and all tests pass. Familiarize yourself with the project workflow and our coding conventions. The coding style and general engineering guidelines are published on the Engineering guidelines page.

### Pull request feedback

The subject matter experts on our team will review your pull request and provide feedback. Please be patient; we have hundreds of pull requests across all of our repositories. Update your pull request according to feedback until it is approved by one of the team members.

### Merging a pull request

When your pull request has had all feedback addressed and has been signed off by one or more core contributors, we will finalize and commit it.
We commit pull requests as a single Squash commit unless there are special circumstances. This creates a simpler history than a Merge or Rebase commit. "Special circumstances" are rare, and typically mean that there are a series of cleanly separated changes that will be too hard to understand if squashed together, or for some reason we want to preserve the ability to dissect them.

#### How the MSAL team deals with forks

The Continuous Integration builds will not run on a pull request opened from a fork, as a security measure. The MSAL team will manually move your branch from your fork to the main repository to be able to run the CI. This will preserve the identity of the commit to give you credit for your work.

```bash
# list existing remotes
git remote -v 

# add a remote to the fork of the contributor
git remote add joe joes_repo_url

# sync
git fetch joe

# checkout the contributor's branch 
git checkout joes_feature_branch

# push it to the original repository (AzureAD/MSAL)
git push origin
```

## Submitting bugs and feature requests

### Before submitting an issue

First, please do a search for [open issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues) to see if the issue or feature request has already been filed. Use the tags to narrow down your search. Here's an example of a [query for Xamarin iOS specific issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues?utf8=%E2%9C%93&q=is:issue+is:open+label:scenario:Mobile-iOS).

If you find your issue already exists, add a relevant comment. You can also use an upvote or downvote reaction in place of a "+1" comment.

If your issue is a question, and is not addressed in the documentation, please ask the question on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-ad-msal) using the tag `azure-ad-msal`.

If you cannot find an existing issue that describes your bug or feature request, submit an issue using the guidelines below.

### Write detailed bug reports and feature requests

File a single issue per bug and feature request:

- Do not enumerate multiple bugs or feature requests in the same issue.
- Do not add your issue as a comment to an existing issue unless it's for the identical input. Many issues look similar, but have different causes.

When [submitting an issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new/choose), select the correct category, **Bug report**, **Documentation**, or **Feature request**.

#### Bug reports

The more information you provide, the more likely someone will be successful in reproducing the issue and finding a fix.
Please use the [Bug report template](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?assignees=&labels=untriaged%2Cneeds+attention&projects=&template=bug_report.yaml&title=%5BBug%5D+) and complete as much of the information listed as possible. Please use the [latest version of the library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases) and see if your bug still exists before filing an issue.

Remember to do the following:

- Search the issue repository to see if there exists a duplicate issue.
- Update to the latest version of the library to see if the issue still exists.
- Submit an issue by filling out all as much of the information in the Bug report as possible.

#### Documentation requests

If you find our documentation or XML comments lacking in necessary detail, submit a [Documentation request](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?assignees=&labels=documentation&projects=&template=documentation.md&title=%5BDocumentation%5D+).

If you have found errors in the documentation, or if an example or code snippet is needed, [open an issue in the documentation repository](https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/issues).

#### Feature requests

Have a feature request for MSAL? Complete a [Feature request](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?assignees=&labels=feature+request%2Cuntriaged%2Cneeds+attention&projects=&template=feature_request.yaml&title=%5BFeature+Request%5D+++++) or consider making a contribution to the library. Make sure your feature request is clear and concise and contains a detailed description of the problem. Please include any alternative solutions or features you may have considered.

## Building and testing MSAL.NET


### Visual Studio

Use the latest Visual Studio. It will guide you on the components needed to be installed. 
For MAUI, edit the .csproj and enable the MAUI targets.

### Package

You can create a package from Visual Studio or from the command line with custom version parameters:

```bash
msbuild <msal>.csproj /t:pack /p:MsalClientSemVer=1.2.3-preview
```
