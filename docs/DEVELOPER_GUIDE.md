# Developer Quick Start Guide

This guide helps you get started developing aws-cur-anonymize.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

## Initial Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR-USERNAME/aws-cur-anonymize.git
   cd aws-cur-anonymize
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

## Project Structure

```
aws-cur-anonymize/
├── src/
│   └── aws-cur-anonymize/          # Main application
│       ├── Cli/                    # CLI commands (Spectre.Console.Cli)
│       ├── Core/                   # Business logic (DuckDB operations)
│       └── Program.cs              # Entry point
├── tests/
│   └── aws-cur-anonymize.Tests/    # Unit tests (xUnit)
├── examples/                       # Usage examples and scripts
├── .github/
│   └── workflows/                  # CI/CD pipelines
└── docs/                           # Additional documentation
```

## Building and Running

### Debug Build
```bash
dotnet build --configuration Debug
```

### Release Build
```bash
dotnet build --configuration Release
```

### Run the Application
```bash
dotnet run --project src/aws-cur-anonymize -- "data/*.csv" --output ./output --salt "test"
```

### Run with Hot Reload (Development)
```bash
dotnet watch --project src/aws-cur-anonymize run -- "data/*.csv" --output ./output
```

## Testing

### Run All Tests
```bash
dotnet test
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~CommandsTests"
```

### Watch Mode (Continuous Testing)
```bash
dotnet watch test
```

## Publishing

**Important**: The standalone executable includes a `cur-config.yaml` file copied to the output directory. This file is distributed alongside the executable for easy customization.

### Self-Contained Executable (Windows)
```bash
# Windows x64
dotnet publish src/aws-cur-anonymize/aws-cur-anonymize.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish/win-x64

# Windows ARM64 (for ARM-based Windows devices)
dotnet publish src/aws-cur-anonymize/aws-cur-anonymize.csproj `
  -c Release `
  -r win-arm64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish/win-arm64
```

The published output includes:
1. A single-file .exe with all dependencies embedded (23 MB)
2. `cur-config.yaml` alongside the executable for user customization
3. No XML documentation files (removed via post-publish target)

### Self-Contained Executable (Linux)
```bash
dotnet publish src/aws-cur-anonymize/aws-cur-anonymize.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/linux-x64
```

### Self-Contained Executable (macOS)
```bash
# macOS ARM64 (Apple Silicon)
dotnet publish src/aws-cur-anonymize/aws-cur-anonymize.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/osx-arm64

# macOS x64 (Intel)
dotnet publish src/aws-cur-anonymize/aws-cur-anonymize.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/osx-x64
```

### Framework-Dependent (Smaller Size)
```bash
dotnet publish -c Release -o ./publish/framework-dependent
```

## Development Workflow

1. **Create a feature branch**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make changes and test**
   ```bash
   # Edit code
   dotnet build
   dotnet test
   ```

3. **Commit with conventional commits**
   ```bash
   git add .
   git commit -m "feat: add new anonymization algorithm"
   ```

4. **Push and create PR**
   ```bash
   git push origin feature/my-feature
   # Create pull request on GitHub
   ```

## Common Tasks

### Add a New NuGet Package
```bash
dotnet add src/aws-cur-anonymize package PackageName
```

### Add a New Test File
```bash
# Create in tests/aws-cur-anonymize.Tests/
# Follow naming: *Tests.cs
# Use xUnit attributes: [Fact], [Theory]
```

### Update Dependencies
```bash
dotnet list package --outdated
dotnet add package PackageName --version X.Y.Z
```

### Format Code
```bash
dotnet format
```

## Debugging

### Visual Studio
1. Open `aws-cur-anonymize.sln`
2. Set `aws-cur-anonymize` as startup project
3. Configure command-line arguments in project properties
4. Press F5 to debug

### VS Code
1. Open folder in VS Code
2. Install C# extension
3. Use `.vscode/launch.json` configuration
4. Press F5 to debug

### Command Line
```bash
dotnet run --project src/aws-cur-anonymize -- "test/*.csv" --output ./out --salt "debug"
```

## Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Test Failures
```bash
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### DuckDB Issues
- Ensure input files exist and are valid CSV or Parquet format
- Check file permissions
- Verify glob patterns work in your shell

## Resources

- [.NET CLI Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [xUnit Documentation](https://xunit.net/)
- [Spectre.Console](https://spectreconsole.net/)
- [DuckDB .NET](https://github.com/Giorgi/DuckDB.NET)
- [Serilog](https://serilog.net/)

## Code Style

- Follow [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use XML documentation comments for public APIs
- Write unit tests for new features
- Keep methods focused and concise
- Use meaningful variable names

## Release Process

See [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed release process.

Quick version:
1. Update version in `.csproj` files
2. Update `CHANGELOG.md`
3. Create git tag: `git tag v1.x.x`
4. Push tag: `git push origin v1.x.x`
5. GitHub Actions will build and create release
