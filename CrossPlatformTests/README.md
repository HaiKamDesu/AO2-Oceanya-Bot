# Cross-Platform Tests for AO2-Oceanya-Bot

This project contains cross-platform unit tests that can run in both Windows and Linux environments, making it suitable for testing in environments like Claude Code.

## Structure

- **CrossPlatformTests.csproj**: .NET 8.0 (non-Windows) project
- **Globals.cs**: A cross-platform implementation of Globals methods
- **CustomConsole.cs**: A cross-platform implementation of CustomConsole
- **GlobalsTests.cs**: Tests for string manipulation functions

## Running Tests

### In Windows
```
dotnet test CrossPlatformTests/CrossPlatformTests.csproj
```

### In Linux/WSL
```
./run-cross-platform-tests.sh
```

## How it Works

- The tests use a standalone implementation of the core functionality
- No Windows-specific dependencies (WPF, etc.) are included
- Directory.Build.props at the solution root defines conditional compilation symbols
- Cross-platform tests focus on the non-UI parts of the codebase

## Benefits

- Unit tests can be run in any environment with .NET 8.0 SDK
- Continuous integration can use Linux runners
- Code assistance tools like Claude Code can validate tests
- Core business logic is separated from UI concerns

## Adding New Tests

To add a new cross-platform test:

1. Add a test class to the CrossPlatformTests project
2. Ensure it only uses cross-platform functionality
3. Use conditional compilation (`#if WINDOWS` / `#if CROSSPLATFORM`) if needed