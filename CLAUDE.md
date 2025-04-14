# CLAUDE.md
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure
- **AO2-Client/**: Git submodule pointing to the original Attorney Online client (reference code only)
- **AOBot-Testing/**: My implementation of an Attorney Online bot client
- **Common/**: Shared utilities and code for my implementation
- **OceanyaClient/**: My main WPF client implementation
- **UnitTests/**: Tests for my implementation

## Reference Code Usage
The code in the AO2-Client submodule is from the original Attorney Online project and is checked out at tag `v2.11.0-release`. It should be used for:
- Understanding network protocols and packet structures
- Referencing game mechanics and logic
- Studying UI workflows and features

Claude should NOT copy this code directly but use it to understand patterns and functionality while helping me implement my own C# WPF version.

## Working with Git Submodules
- The AO2-Client submodule is set to the v2.11.0-release tag
- When cloning this repository for the first time, use: `git clone --recurse-submodules`
- To update the submodule after cloning without the above flag: `git submodule update --init --recursive`

## Environment Requirements
- **.NET SDK**: Version 8.0 required (the project targets .NET 8.0-windows)
- **Windows**: The project uses WPF (.NET Windows-specific UI framework)
- **Visual Studio**: Recommended for development, but not required if using dotnet CLI

## Build/Lint/Test Commands
- **Install .NET SDK**: On Windows, download from https://dotnet.microsoft.com/download
- **Build**: `dotnet build "AOBot-Testing/AOBot-Testing.sln"` 
- **Run Client**: `dotnet run --project OceanyaClient/OceanyaClient.csproj`
- **Clean**: `dotnet clean`
- **Test**: `dotnet test`

## Alternative Build Methods
- **Using Visual Studio**: Open the solution file (.sln) and build/run from the IDE
- **Using MSBuild**: `msbuild "AOBot-Testing/AOBot-Testing.sln" /p:Configuration=Release`

## Environment Limitations
- **Linux/WSL Environment**: Building and running the main application in a Linux or WSL environment is not supported as the project uses WPF, which requires Windows
  - However, the `LinuxCompatibleTests` project can be built and run in Linux environments
- **Docker**: Building in a Docker container requires a Windows-based container with .NET SDK 8.0 installed
- **CI/CD**: Use Windows runners for build pipelines, or Linux runners for the Linux-compatible tests

## Known Test Issues
The unit tests in this project have several failing tests:

1. **JSON Handling Tests**:
   - `Test_JSON_Serialization`: Fails with `FormatException` - Input string formatting issue in string.Format
   - `Test_JSON_WithSpecialCharacters`: Same formatting issue
   - `Test_PlayerListParsing`: Player parsing issue - truncating character names

2. **Network Tests**:
   - Some tests require an actual connection to an AO2 server and are skipped
   - Connection tests show an `ArgumentOutOfRangeException` when processing messages

3. **W2G Tests**:
   - Many tests are skipped because they require real credentials

4. **Running Tests**:
   - Tests can only be run in a Windows environment
   - Use `dotnet test` or Visual Studio Test Explorer to run tests
   - When working on tests, focus on fixing the JSON formatting issues first

## Code Style Guidelines
- **Naming**: Use PascalCase for classes, methods, and properties. Use camelCase for local variables and parameters.
- **Formatting**: Use 4-space indentation. Keep line length under 120 characters.
- **Types**: Always use explicit types. Enable nullable reference types with `<Nullable>enable</Nullable>`.
- **Error Handling**: Use try-catch blocks for expected exceptions. Log errors with CustomConsole.Error(message, exception). Use appropriate log levels (Info, Warning, Error, Debug).
- **Async**: Use async/await pattern for asynchronous operations. Avoid blocking calls.
- **Comments**: Use XML documentation comments for public methods and classes.
- **Dependencies**: Use dependency injection where possible.
- **Organization**: Keep code organized in the existing namespace structure (AOBot_Testing.Agents, AOBot_Testing.Structures).

## Special Character Handling

The Attorney Online protocol uses special markup characters that are handled differently than regular text:

- **Special Characters**: The following characters are treated as special and not displayed in IC messages by default: `-"\s`, `~`, `{`, and `}`
- **Escaping Special Characters**: Special characters can be displayed by prefixing them with a backslash (e.g., `\~` will display as `~`)
- **Implementation**: Special character processing is implemented in `Globals.ProcessSpecialCharacters()` which:
  - Removes special characters from displayed text
  - Preserves special characters that are escaped with backslash
  - Handles the special `\s` sequence which is always hidden
  - Properly manages escaped backslashes (\\) which display as a single backslash
  - Correctly processes complex combinations of escaped and non-escaped special characters

This approach is consistent with the original client's behavior for handling special formatting in messages.

### Special Character Handling Rules

1. **Basic Characters**: Normal characters are displayed as-is
2. **Special Characters**: The characters `-`, `"`, `~`, `{`, and `}` are hidden by default when used as formatting
3. **Text Inside Special Characters**: Text between special characters is preserved
   - Example: `Text with {hidden}` displays as `Text with hidden`
4. **Escaped Characters**: Any special character preceded by a backslash (`\`) will be displayed
   - Example: `Text with \{visible\}` displays as `Text with {visible}`
5. **Special `\s` Case**: The sequence `\s` is always hidden regardless of context
6. **Backslash Handling**:
   - A single backslash (`\`) followed by a non-special character is displayed as-is
   - A double backslash (`\\`) is displayed as a single backslash
   - A backslash at the end of a string is displayed as-is
   - An escaped special character (e.g., `\~`) shows only the special character

### Testing the Special Character Handler

A comprehensive set of unit tests in `GlobalsTests.cs` verifies all aspects of special character handling:
- Basic text processing
- Special character hiding
- Character escaping
- Edge cases like backslashes at string ends
- Complex combinations of escaped and non-escaped characters

#### Windows-Specific Tests
The main tests must be run in a Windows environment using: 
```
dotnet test UnitTests/GlobalsTests.cs
```

#### Linux-Compatible Tests
For testing in Linux environments (including Claude Code's environment), a separate project has been created:
```
dotnet test LinuxCompatibleTests/LinuxCompatibleTests.csproj
```

This Linux-compatible test project:
- Contains the same tests but without Windows-specific dependencies
- Targets standard .NET 8.0 (not Windows-specific)
- Can be run in any environment that supports .NET 8.0
- Is particularly useful for automated testing in CI/CD pipelines or in non-Windows environments

#### Python Test Script
For environments where .NET is not available, a Python implementation of the tests is provided:
```
python3 LinuxCompatibleTests/test_special_chars.py
```

This script:
- Implements the core special character handling logic in Python
- Runs the same test cases as the C# tests
- Only requires Python 3 (which is available in most environments)
- Provides detailed output for debugging any issues

The Python test script has been fully tested and all tests pass correctly.

### Standalone Test Application

To make testing easier, especially in environments where running the full unit tests might be difficult, a standalone test application has been created:

1. **SpecialCharacterTest Project**: A simple console application that:
   - Runs all the special character handling tests automatically
   - Provides an interactive mode to test your own inputs
   - Does not require the test framework or WPF dependencies

2. **How to run**:
   - Open the `SpecialCharacterTest.sln` solution in Visual Studio
   - Press F5 to build and run
   - Or from the command line: `dotnet run --project SpecialCharacterTest/SpecialCharacterTest.csproj`

3. **Cross-platform compatible**: Unlike the main application and unit tests which require Windows for WPF, this tester uses .NET 8.0 (not Windows-specific) and can run on any platform that supports .NET 8.0.