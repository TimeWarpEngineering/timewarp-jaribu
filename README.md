# TimeWarp.Jaribu

Lightweight testing helpers for single-file C# programs and scripts.

Jaribu (Swahili: test/trial) provides a convention-based TestRunner pattern and assertion helpers for executable .cs files. It enables easy testing in single-file scenarios without heavy test frameworks.

## Features

- **Convention over Configuration**: Discover public static async Task methods as tests via reflection.
- **Assertion Helpers**: Simple, fluent assertions inspired by Shouldly.
- **Attributes**: Support for [Skip], [TestTag], [Timeout], [Input], and [ClearRunfileCache].
- **Parameterized Tests**: Easy data-driven testing.
- **Tag Filtering**: Run specific test groups.
- **Cache Management**: Clear runfile cache for consistent testing.
- **Minimal Dependencies**: Only Shouldly for assertions.

## Installation

Add the NuGet package:

```
dotnet add package TimeWarp.Jaribu
```

## Usage

### Basic Test File

Create a single-file test script (e.g., `my-tests.cs`):

```csharp
using static TimeWarp.Jaribu.TestHelpers;

public static class MyTests
{
    public static async Task BasicTest()
    {
        1.ShouldBe(1);
    }

    [TestTag("integration")]
    public static async Task IntegrationTest()
    {
        // Test logic here
    }
}
```

Run with:

```
dotnet run --project my-tests.cs
```

### TestRunner

For programmatic use:

```csharp
using TimeWarp.Jaribu;

await TestRunner.RunTests<MyTests>();
```

## Documentation

See the [developer documentation](documentation/) for advanced usage, attributes, and best practices.

## Building from Source

1. Clone the repository.
2. Run `dotnet build`.
3. Run tests with `dotnet run --project Tests/TimeWarp.Jaribu.Tests/TimeWarp.Jaribu.Tests.csproj`.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

[MIT License](LICENSE)
