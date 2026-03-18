# Create TimeWarp.Jaribu.TestingPlatform Project

## Summary

Create a new project `TimeWarp.Jaribu.TestingPlatform` that provides the Microsoft.Testing.Platform adapter for Jaribu tests. This enables `dotnet test` and IDE Test Explorer integration.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### Project Setup
- [ ] Create `source/TimeWarp.Jaribu.TestingPlatform/` directory
- [ ] Create `TimeWarp.Jaribu.TestingPlatform.csproj`
- [ ] Add to solution file (`TimeWarp.Jaribu.slnx`)
- [ ] Configure NuGet package metadata

### Dependencies
- [ ] Add `Microsoft.Testing.Platform` package reference
- [ ] Add `Microsoft.Testing.Platform.MSBuild` package reference
- [ ] Add project reference to `TimeWarp.Jaribu`

### Project Structure
- [ ] Create `JaribuExtension.cs` (IExtension implementation)
- [ ] Create `JaribuCapabilities.cs` (ITestFrameworkCapabilities)
- [ ] Create `JaribuTestFramework.cs` (stub - detailed in Task 013)
- [ ] Create `TestingPlatformBuilderHook.cs`
- [ ] Create `build/` directory for MSBuild props/targets

### Build Configuration
- [ ] Configure package to include build props/targets
- [ ] Set up proper PackagePath for NuGet

## Notes

### Project File

```xml
<!-- source/TimeWarp.Jaribu.TestingPlatform/TimeWarp.Jaribu.TestingPlatform.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Description>Microsoft.Testing.Platform adapter for TimeWarp.Jaribu test framework</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Testing.Platform" Version="1.*" />
    <PackageReference Include="Microsoft.Testing.Platform.MSBuild" Version="1.*" />
    <ProjectReference Include="../TimeWarp.Jaribu/TimeWarp.Jaribu.csproj" />
  </ItemGroup>

  <!-- Pack build props/targets -->
  <ItemGroup>
    <None Include="build/**" Pack="true" PackagePath="build/" />
    <None Include="build/**" Pack="true" PackagePath="buildTransitive/" />
  </ItemGroup>
</Project>
```

### JaribuExtension.cs

```csharp
using Microsoft.Testing.Platform.Extensions;

namespace TimeWarp.Jaribu.TestingPlatform;

internal sealed class JaribuExtension : IExtension
{
    public string Uid => "TimeWarp.Jaribu";
    public string Version => typeof(JaribuExtension).Assembly
        .GetName().Version?.ToString() ?? "1.0.0";
    public string DisplayName => "TimeWarp.Jaribu Test Framework";
    public string Description => "Lightweight test framework for .NET runfiles and compiled projects";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
```

### JaribuCapabilities.cs

```csharp
using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace TimeWarp.Jaribu.TestingPlatform;

internal sealed class JaribuCapabilities : ITestFrameworkCapabilities
{
    public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => [];
}
```

### TestingPlatformBuilderHook.cs

```csharp
using Microsoft.Testing.Platform.Builder;

namespace TimeWarp.Jaribu.TestingPlatform;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder builder, string[] args)
    {
        var extension = new JaribuExtension();
        
        builder.RegisterTestFramework(
            _ => new JaribuCapabilities(),
            (capabilities, serviceProvider) => new JaribuTestFramework(extension, serviceProvider));
    }
}
```

### Directory Structure

```
source/TimeWarp.Jaribu.TestingPlatform/
├── build/
│   └── TimeWarp.Jaribu.TestingPlatform.props
├── JaribuCapabilities.cs
├── JaribuExtension.cs
├── JaribuTestFramework.cs
├── TestingPlatformBuilderHook.cs
└── TimeWarp.Jaribu.TestingPlatform.csproj
```

## Results

_Added after completion._
