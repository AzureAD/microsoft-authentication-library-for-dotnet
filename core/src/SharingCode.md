## Sharing Code between ADAL and MSAL

#### Goals

1. A lot of code is shared between ADAL and MSAL, so do not use duplication;
2. Customers want to be able to run ADAL and MSAL togheter in the same app. The 2 libraries should only communicate via the cache, there should be no other sharing of state such as static variables etc. 

#### Approaches

We have looked at 3 possibilities of sharing code and decided on #3:

1. Share code through a common assembly - core.dll. Include this assembly in the ADAL and MSAL nuget packages.

**PROS:** Core.dll does not have a public API therefore it is not packaged; sharing code is simple;
**CONS:** ADAL and MSAL will NOT run togheter because .net will load core.dll only once and all the dependency injections will fail (e.g. CoreLogger will be initialized to AdalLogger and then to MsalLogger and will remain as MsalLogger even if called from ADAL); 

1. Share code through a common assembly - core.dll. Package core.dll separately.

**PROS:** sometimes ADAL and MSAL will be able to run toghter
**CONS:** If ADAL 4.0 depends on Core 1.5 and MSAL 2.0 depends on Core 1.4, then a user consuming both can add an [assembly binding configuration](https://stackoverflow.com/questions/3158928/referencing-2-different-versions-of-log4net-in-the-same-solution) to use both - not a great user experience but it works. If however ADAL 4.0 and MSAL 2.0 both depend on Core 1.2, then the assembly loader will load Core 1.2 a single time and the problem of Approach #1 occurs;

1. Do not share code through a common assembly. Instead, include code files to be shared in ADAL and MSAL. 

**PROS:** code sharing still works; no more core.dll; simplified packaging tasks (no more manual updating of nuspec)
**CONS:** Some developer tasks become more complex - conditional compilation details from Core will need to be duplicated in ADAL and MSAL



### How code sharing works

We keep core.dll as a container for the code files. Both ADAL and MSAL include the code files directly, and do NOT reference core.dll:

```
<!-- LinkBase shows up as a directory in SolutionExplorer for all the included files -->
<Compile Include="..\..\..\core\src\**\*.cs"  LinkBase="Core" />
```

The unified solution still contains a Core.dll with associated unit tests. However, you should not take a dependency on Core.dll from anywhere else (you'll get duplicated item exceptions anyway). 

### Authoring code and tests

If you add new code to core, you'll need to copy any <PackageReference/> to ADAL and MSAL (you'll get errors if you don't anyway).

If you add or remove  platform specific code to core, outside the Platform directories, you'll need to update the ADAL and MSAL csproj for the conditional compilation.

Tests run as normal, and core.dll has associated unit tests.

### Exposing types from Core

We currently do not expose anything public from Core, and we should not do this unless we discuss it first. 

In the future, if we decide to expose public types from core, we need to do a bit more work to play nice with customer solutions that use both ADAL and MSAL. Consider the scenario where we'd like to expose:

```csharp
namespace core
{
    public interface IFoo {}
}
```
Since IFoo gets included in both ADAL and MSAL, if a user tries to reference IFoo in his code, he'll get a compilation error: 

```
CS0433	The type 'IFoo' exists in both 'ADAL' and 'MSAL' ...
```

There are 2 solutions for this problem: 

1. Let the user hadle it with [external aliases](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/extern-alias)

2. Use conditional compilation 

```csharp
#if ADAL 
namespace core.adal
#elif
namespace core.msal
#endif
{
    public interface IFoo {}
}
```
