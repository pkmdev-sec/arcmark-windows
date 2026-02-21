# Run all tests
$ErrorActionPreference = "Stop"
dotnet test "$PSScriptRoot\..\Arcmark.Tests\Arcmark.Tests.csproj" -v normal
