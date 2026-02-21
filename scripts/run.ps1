# Quick build and run
$ErrorActionPreference = "Stop"
dotnet build "$PSScriptRoot\..\Arcmark\Arcmark.csproj" -c Debug
dotnet run --project "$PSScriptRoot\..\Arcmark\Arcmark.csproj" -c Debug
