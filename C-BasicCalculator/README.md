# C# GrokBot Calculator

Simple console calculator with the four basic operations: add, subtract, multiply, divide.

## Requirements

- .NET 8 SDK

## Build and test

```bash
dotnet test BasicCalculator.Tests/BasicCalculator.Tests.csproj
```

## Run

```bash
dotnet run --project BasicCalculator.csproj
```

Enter two numbers and an operator (`+`, `-`, `*`, `/`). Divide-by-zero and invalid input are handled without crashing.
