# Refactor Baseline

Captured on 2026-05-17 for branch `refactor/phase-1`.

## Commands

| Command | Exit status | Outcome |
| --- | ---: | --- |
| `dotnet restore KoiFengShuiSystem.sln` | 0 | Restore succeeded. Reported pre-existing package vulnerability warnings: `AutoMapper` 14.0.0 NU1903, `MailKit` 4.8.0 NU1902, `MimeKit` 4.11.0 NU1902. |
| `dotnet build KoiFengShuiSystem.sln --no-restore` | 0 | Build succeeded. Confirmation run reported `106 Warning(s), 0 Error(s)`. |
| `dotnet test KoiFengShuiSystem.sln --no-build` | 0 | Solution-level test command exited successfully but produced no test result details. This did not exercise `KoiFengShuiSystem.Tests` because `KoiFengShuiSystem.Tests/KoiFengShuiSystem.Tests.csproj` is not included in `KoiFengShuiSystem.sln`. |
| `dotnet test KoiFengShuiSystem.Tests\KoiFengShuiSystem.Tests.csproj` | 1 | Direct test-project command discovered the test assembly but aborted before running tests because the machine lacks `Microsoft.NETCore.App` version `8.0.0` x64. |

## Baseline Issues

- Pre-existing solution coverage issue: `KoiFengShuiSystem.Tests` exists but is not included in `KoiFengShuiSystem.sln`, so `dotnet test KoiFengShuiSystem.sln --no-build` does not run the test project.
- Pre-existing environment/runtime issue: direct execution of `KoiFengShuiSystem.Tests` aborts because `Microsoft.NETCore.App 8.0.0` x64 is missing. Installed x64 runtimes reported by the testhost error were `3.1.32`, `6.0.16`, and `10.0.8`.
- Pre-existing package vulnerability warnings were reported during restore/build for `AutoMapper`, `MailKit`, and `MimeKit`.
- Pre-existing compiler warnings remain in the build baseline; build still exits 0.

## Direct Test Project Error Summary

`dotnet test KoiFengShuiSystem.Tests\KoiFengShuiSystem.Tests.csproj` output included:

```text
Test run for C:\Users\Karui\Desktop\Works\KoiFengShuiSystemAPI\KoiFengShuiSystem.Tests\bin\Debug\net8.0\KoiFengShuiSystem.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.
Testhost process for source(s) 'C:\Users\Karui\Desktop\Works\KoiFengShuiSystemAPI\KoiFengShuiSystem.Tests\bin\Debug\net8.0\KoiFengShuiSystem.Tests.dll' exited with error: You must install or update .NET to run this application.
Architecture: x64
Framework: 'Microsoft.NETCore.App', version '8.0.0' (x64)
The following frameworks were found:
  3.1.32 at [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  6.0.16 at [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  10.0.8 at [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
Test Run Aborted.
```
