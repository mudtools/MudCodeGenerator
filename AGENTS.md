# AGENTS.md - Mud Code Generator Development Guide

This file contains guidelines and commands for agentic coding agents working on the Mud Code Generator project.

## Project Overview

Mud Code Generator is a C# source generator project built on Roslyn that provides automatic code generation for:
- Entity classes (DTO, VO, QueryInput, CrInput, UpInput, Builder patterns)
- Service code (HttpClient API wrappers, dependency injection, COM object wrapping)
- Auto-registration and event handling

## Build Commands

### Basic Build Commands
```bash
# Build entire solution (run from root directory)
dotnet build

# Build specific project
dotnet build Core/Mud.ServiceCodeGenerator/Mud.ServiceCodeGenerator.csproj
dotnet build Core/Mud.EntityCodeGenerator/Mud.EntityCodeGenerator.csproj
dotnet build Test/CodeGeneratorTest/CodeGeneratorTest.csproj

# Build with specific configuration and framework
dotnet build --configuration Release --framework net9.0
dotnet build --configuration Debug --framework net8.0
```

### Test Commands
```bash
# Run all tests in solution
dotnet test

# Run tests for specific project
dotnet test Test/CodeGeneratorTest/CodeGeneratorTest.csproj

# Run single test method (using test filter)
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run tests with specific framework
dotnet test --framework net9.0

# Run tests with detailed output
dotnet test --verbosity normal
```

### Package Commands
```bash
# Create NuGet packages
dotnet pack Core/Mud.ServiceCodeGenerator/Mud.ServiceCodeGenerator.csproj --configuration Release
dotnet pack Core/Mud.EntityCodeGenerator/Mud.EntityCodeGenerator.csproj --configuration Release

# Pack with version suffix
dotnet pack --version-suffix preview
```

### Clean Commands
```bash
# Clean build outputs
dotnet clean

# Clean specific project
dotnet clean Core/Mud.ServiceCodeGenerator/Mud.ServiceCodeGenerator.csproj

# Clean all artifacts and bin/obj folders
dotnet clean && dotnet clean --verbosity detailed
```

## Code Style Guidelines

### File Structure and Organization
- Use `GlobalUsings.cs` for common imports
- Source generators should inherit from `TransitiveCodeGenerator`
- Generator classes should be marked with `[Generator(LanguageNames.CSharp)]`
- Use partial classes for generated code extensions
- Separate generator logic into dedicated files with clear naming

### Naming Conventions
- **Classes**: PascalCase (e.g., `CodeInjectGenerator`, `TransitiveDtoGenerator`)
- **Methods**: PascalCase with descriptive names (e.g., `GenerateMethod`, `BuildProperty`)
- **Properties**: PascalCase with backing fields using underscore prefix (`_fieldName`)
- **Constants**: PascalCase or UPPER_CASE for public constants
- **Private fields**: CamelCase with underscore prefix (`_privateField`)
- **Generated files**: End with `.g.cs` suffix

### Code Style Requirements
- **Language**: C# 13.0 with nullable reference types enabled
- **Target Frameworks**: net8.0, net9.0, net10.0 (where applicable)
- **Indentation**: 4 spaces (no tabs)
- **Braces**: Allman style (opening brace on new line)
- **Line Length**: Keep under 120 characters when possible

### Import Organization
- Use `global using` for common namespaces in `GlobalUsings.cs`
- Place using statements at top of file, sorted alphabetically
- Avoid unused using statements
- Prefer specific imports over generic ones

### Documentation Standards
- All public members must have XML documentation
- Use Chinese for user-facing documentation (as per project convention)
- Include parameter descriptions and return value documentation
- Use `<summary>`, `<param>`, `<returns>`, `<remarks>` tags appropriately
- Generated code should include `[CompilerGenerated]` attribute

### Error Handling
- Use proper argument validation with `ArgumentNullException.ThrowIfNull`
- Wrap generator operations in try-catch blocks with meaningful error messages
- Use `ErrorHandler.SafeExecute` for safe code execution
- Log generator warnings and errors using `SourceProductionContext.ReportDiagnostic`

### Type Safety and Nullability
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use proper null checking and null-coalescing operators
- Avoid null suppression operators (`!`) unless absolutely necessary
- Use `?` for nullable value types appropriately

### Generator-Specific Guidelines
- Use `SourceProductionContext` for reporting diagnostics
- Implement proper incremental generation patterns
- Use `SyntaxFactory` for code generation, avoid string concatenation
- Generate partial classes to allow user extensions
- Include proper namespace handling
- Use `[GeneratedCode]` and `[CompilerGenerated]` attributes

## Configuration Properties

Common MSBuild properties used by the generators:
- `EmitCompilerGeneratedFiles`: Set to `true` to save generated code to obj/
- `EntitySuffix`: Default entity class suffix (default: "Entity")
- `EntityAttachAttributes`: Attributes to add to generated entity classes
- `UsingNameSpaces`: Additional namespaces to include in generated code
- `PropertyNameLowerCaseFirstLetter`: Controls property naming (default: true)
- `HttpClientOptionsName`: Name of HttpClient options class

## Testing Guidelines

### Test Structure
- Test projects should end with `Test.csproj`
- Test classes should end with `Test` suffix
- Use descriptive test method names that indicate what is being tested
- Include test data and helper classes in separate folders

### Test Categories
- **Unit tests**: Test individual generator methods
- **Integration tests**: Test complete generation scenarios
- **Compilation tests**: Verify generated code compiles without errors
- **Functionality tests**: Test generated code behavior

### Test Data
- Place test entities and interfaces in appropriate subfolders
- Use consistent naming patterns for test classes
- Include edge cases and error conditions in test scenarios

## Development Workflow

1. **Setup**: Clone repository and restore dependencies
2. **Development**: Make changes to generator code
3. **Testing**: Run tests to verify functionality
4. **Build**: Ensure project builds successfully on all target frameworks
5. **Verification**: Check generated code output in obj/ folder when `EmitCompilerGeneratedFiles=true`
6. **Documentation**: Update XML comments and README files as needed

## Common Issues and Solutions

### Build Issues
- Missing analyzer references: Ensure `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`
- Framework compatibility: Check target framework support for used APIs
- NuGet package conflicts: Use consistent package versions across projects

### Generator Issues
- Incremental generation not updating: Check caching keys and provider setup
- Generated code not visible: Ensure `EmitCompilerGeneratedFiles=true` is set
- Compilation errors in generated code: Verify SyntaxFactory usage and namespace imports

### Testing Issues
- Tests not finding generated code: Ensure proper project references and analyzer setup
- Intermittent test failures: Check for race conditions in generator execution

## Performance Considerations

- Use incremental generation patterns for better compilation performance
- Cache frequently used syntax nodes and symbols
- Avoid expensive operations in generator initialization
- Use appropriate collection types for generator state

This guide should be followed by all agentic coding agents working on this project to ensure consistency and quality.