# Contributing to aws-cur-anonymize

Thank you for considering contributing to aws-cur-anonymize! This document provides guidelines for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Commit Guidelines](#commit-guidelines)

## Code of Conduct

This project adheres to a code of conduct adapted from the Contributor Covenant. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Git
- An IDE that supports C# (VS Code with C# extension, Visual Studio, or JetBrains Rider)
- Basic knowledge of C# and .NET
- Familiarity with Git and GitHub workflow

### Setting Up Development Environment

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/yourusername/aws-cur-anonymize.git
   cd aws-cur-anonymize
   ```
3. Add the original repository as upstream:
   ```bash
   git remote add upstream https://github.com/originalowner/aws-cur-anonymize.git
   ```
4. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```
5. Restore dependencies:
   ```bash
   dotnet restore
   ```
6. Build the project:
   ```bash
   dotnet build
   ```

## Development Workflow

1. Make your changes in your feature branch
2. **Build Verification**: Run `dotnet build` and ensure zero errors
3. **Test Verification**: Run `dotnet test` and ensure all tests pass
4. Commit your changes (see Commit Guidelines below)
5. Push to your fork and submit a pull request

### Pre-Commit Checklist

Before committing, ensure:

- [ ] Code builds successfully: `dotnet build`
- [ ] All tests pass: `dotnet test`
- [ ] Code follows the project's coding standards
- [ ] Documentation has been updated if needed
- [ ] CHANGELOG.md has been updated with your changes
- [ ] Commit message follows the guidelines below

## Coding Standards

### Naming Conventions

- **PascalCase** for:
  - Class names
  - Interface names (with `I` prefix)
  - Public members
  - Constants
  - Method names

- **camelCase** for:
  - Local variables
  - Parameters
  - Private fields (with underscore `_` prefix)

### Code Formatting

- Use 4 spaces for indentation (not tabs)
- Keep line length reasonable (< 120 characters)
- Add XML documentation comments for public methods and classes
- Keep methods focused and concise (< 30 lines when possible)

### C# Best Practices

- Use file-scoped namespaces
- Prefer LINQ for data manipulation
- Use proper exception handling with specific exception types
- Validate all inputs
- Use expression-bodied members for simple methods
- Enable nullable reference types
- Use pattern matching where appropriate
- Prefer string interpolation over concatenation

### Logging

- Use Serilog for logging
- Use appropriate log levels:
  - `Information`: Normal operations
  - `Warning`: Unexpected but handled situations
  - `Error`: Errors that prevent operation
  - `Debug`: Detailed troubleshooting information

## Testing

The project maintains comprehensive test coverage:

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test end-to-end functionality with real data

Testing requirements:

- Write tests for all new functionality
- Ensure all tests pass before submitting PR
- Maintain or improve code coverage
- Test edge cases and error conditions
- Include both positive and negative test cases

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/aws-cur-anonymize.Tests
```

## Pull Request Process

1. Update the README.md with details of changes if appropriate
2. Update the CHANGELOG.md with details of your changes
3. Ensure all automated checks pass
4. PRs require approval from at least one maintainer
5. The PR should work on Windows, macOS, and Linux

### PR Checklist

- [ ] Clear description of changes
- [ ] Link to related issue (if applicable)
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] All CI checks passing

## Commit Guidelines

Follow these commit message conventions:

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests after the first line

### Conventional Commits Format (Optional but Recommended)

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types:
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code changes that neither fix bugs nor add features
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Changes to build process or tools

Examples:
```
feat(cli): add support for CSV input files

fix(pipeline): correct handling of null values in cost data

docs: update README with new examples
```

## Version Management

This project follows [Semantic Versioning](https://semver.org/):

- **MAJOR** (1.0.0 → 2.0.0): Incompatible API changes
- **MINOR** (1.0.0 → 1.1.0): New backward-compatible functionality
- **PATCH** (1.0.0 → 1.0.1): Backward-compatible bug fixes

Version updates are managed through GitHub Actions workflows.

## Communication

- Use GitHub Issues for bug reports and feature requests
- Use GitHub Discussions for questions and general discussions
- Be respectful and constructive in all communications
- Follow the Code of Conduct

## Questions?

If you have questions about contributing:

1. Check existing documentation
2. Search closed issues
3. Create a new discussion
4. Contact the maintainers

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
