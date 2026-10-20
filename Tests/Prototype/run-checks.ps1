$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$verificationRoot = Join-Path $projectRoot '.utmp\PrototypeChecks'
New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null
@'
<configuration><packageSources><clear /></packageSources></configuration>
'@ | Set-Content -LiteralPath (Join-Path $verificationRoot 'NuGet.Config')
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><Compile Include="../../Assets/Main/02_Scripts/Data/*.cs" /><Compile Include="../../Assets/Main/02_Scripts/Core/*.cs" /></ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $verificationRoot 'Core.csproj')
$runnerRoot = Join-Path $verificationRoot 'Runner'
New-Item -ItemType Directory -Path $runnerRoot -Force | Out-Null
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net9.0</TargetFramework><LangVersion>9.0</LangVersion><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><ProjectReference Include="../Core.csproj" /><Compile Include="../../../Tests/Prototype/PrototypeChecks.cs" /></ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $runnerRoot 'Checks.csproj')
& dotnet run --project (Join-Path $runnerRoot 'Checks.csproj') --configuration Release
if ($LASTEXITCODE -ne 0) { throw "프로토타입 검사 실패: $LASTEXITCODE" }
