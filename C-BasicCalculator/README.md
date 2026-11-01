# C# GrokBot Calculator

Simple console calculator with the four basic operations: add, subtract, multiply, divide.

## Requirements

- .NET 10 SDK

## Build and test

```bash
dotnet test BasicCalculator.Tests/BasicCalculator.Tests.csproj
```

## Run

```bash
dotnet run --project BasicCalculator.csproj
```

Enter two numbers and an operator (`+`, `-`, `*`, `/`). Divide-by-zero and invalid input are handled without crashing.

## Changelog

- **Sun Nov 1, 2026 ET** — Retargeted from .NET 8 to **.NET 10** (`net10.0`) because .NET 8 support ends Nov 10, 2026 and stops getting security patches. Builds Debug + Release on the .NET 10 SDK; no package changes.
