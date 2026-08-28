![Version](https://img.shields.io/github/v/tag/Diogo-Maia/GitIssues?label=Version)
![License](https://img.shields.io/badge/license-MIT-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)

# GitIssues

`GitIssues` is a .NET NuGet package for automatically reporting application exceptions as issues in a GitHub repository.

It is intended for APIs, Windows Services, background workers, and other .NET applications where critical errors should automatically create GitHub issues for investigation.

## Features

* Creates GitHub issues automatically from .NET exceptions
* Supports private GitHub repositories
* Adds application and environment information to issues
* Includes exception messages and stack traces
* Supports custom GitHub labels
* Supports additional error metadata
* Generates fingerprints for exceptions
* Prevents duplicate issues for the same error
* Can comment on an existing issue when an error occurs again
* Sanitizes common sensitive values before sending data to GitHub
* Uses `IHttpClientFactory`
* Integrates with .NET Dependency Injection
* GitHub reporting failures do not crash the host application by default

## Requirements

* .NET 8.0+
* A GitHub repository with Issues enabled
* A GitHub credential with permission to create issues in the target repository

For private repositories, the credential must have access to the repository.

A fine-grained Personal Access Token can be restricted to:

* The required repository only
* Repository permission: **Issues — Read and write**


## Configuration

Add the non-sensitive configuration to `appsettings.json`:

```json
{
  "GitHubIssues": {
    "Owner": "MyCompany",
    "Repository": "MyPrivateRepository",
    "ServiceName": "TxPlanner.API",
    "Labels": [
      "automatic",
      "service-error"
    ]
  }
}
```

### GitHub Token

Do not store the GitHub token in `appsettings.json` or commit it to source control.

For local development, .NET User Secrets can be used:

```powershell
dotnet user-secrets set "GitHubIssues:Token" "github_pat_xxxxxxxxx"
```

Alternatively, use an environment variable:

```powershell
$env:GitHubIssues__Token = "github_pat_xxxxxxxxx"
```

For a deployed Windows server, the token can be configured as a machine-level environment variable or provided through the organization's secrets-management solution.

## Dependency Injection

Register the reporter in `Program.cs`:

```csharp
using GitHubErrorReporter.DependencyInjection;

builder.Services.AddGitHubErrorReporter(options =>
{
    options.Owner =
        builder.Configuration["GitHubIssues:Owner"]
        ?? throw new InvalidOperationException(
            "GitHub repository owner is not configured.");

    options.Repository =
        builder.Configuration["GitHubIssues:Repository"]
        ?? throw new InvalidOperationException(
            "GitHub repository is not configured.");

    options.Token =
        builder.Configuration["GitHubIssues:Token"]
        ?? throw new InvalidOperationException(
            "GitHub token is not configured.");

    options.ServiceName =
        builder.Configuration["GitHubIssues:ServiceName"];

    options.Environment =
        builder.Environment.EnvironmentName;

    options.Labels =
        builder.Configuration
            .GetSection("GitHubIssues:Labels")
            .Get<string[]>()
        ?? [];
});
```

The ASP.NET Core environment is automatically used for the issue environment:

```text
Development
Staging
Production
```

## Basic Usage

Inject `IGitHubErrorReporter` into the class where errors need to be reported:

```csharp
using GitHubErrorReporter.Services;

public class MyService
{
    private readonly IGitHubErrorReporter _errorReporter;

    public MyService(
        IGitHubErrorReporter errorReporter)
    {
        _errorReporter = errorReporter;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            await DoSomethingAsync();
        }
        catch (Exception ex)
        {
            await _errorReporter.ReportAsync(ex);

            throw;
        }
    }
}
```

## Reporting Additional Metadata

Use `ErrorReport` when additional context should be included:

```csharp
using GitHubErrorReporter.Models;

try
{
    await ProcessInvoiceAsync(invoiceId);
}
catch (Exception ex)
{
    await _errorReporter.ReportAsync(
        new ErrorReport
        {
            Exception = ex,
            Title = "Invoice processing failed",

            Metadata =
            {
                ["InvoiceId"] = invoiceId.ToString(),
                ["JobId"] = jobId.ToString()
            }
        });

    throw;
}
```

The metadata will be included in the generated GitHub issue.

Do not intentionally add passwords, access tokens, connection strings, or other secrets to metadata.

## Testing

A temporary ASP.NET Core endpoint can be used to verify the integration:

```csharp
app.MapGet(
    "/test-github-error",
    async (IGitHubErrorReporter reporter) =>
    {
        try
        {
            throw new InvalidOperationException(
                "Test GitHub issue generated intentionally.");
        }
        catch (Exception ex)
        {
            await reporter.ReportAsync(ex);

            return Results.Ok(
                "Test exception reported to GitHub.");
        }
    });
```

Call:

```text
GET /test-github-error
```

A new issue should appear in the configured GitHub repository.

Remove the test endpoint after verifying the integration.

## Generated Issues

An issue title will look similar to:

```text
[Production] Invoice processing failed [88D23465A917]
```

The issue body contains information such as:

```text
Automatic Error Report

Service: TxPlanner.API
Environment: Production
Server: PROD-SRV-02
Time: 2026-08-27T15:42:31Z
Exception: System.InvalidOperationException
Fingerprint: 88D23465A917...

Message

An unexpected error occurred.

Metadata

InvoiceId: 84329
JobId: 9845

Stack Trace

...
```

## Duplicate Errors

Each exception is assigned a fingerprint based on information about the exception.

When deduplication is enabled:

```text
Error
  |
  v
Generate fingerprint
  |
  v
Search open GitHub issues
  |
  +-- No match --> Create new issue
  |
  +-- Match ----> Add comment to existing issue
```

This prevents a recurring error from creating hundreds of identical GitHub issues.

For example, the first occurrence may create:

```text
#142 [Production] SqlException in TxPlanner.API [88D23465A917]
```

If the same error happens again, the existing issue receives a comment containing the new occurrence time, server, service, and environment.

## Options

The reporter supports the following configuration options:

| Option               | Description                                               | Default                      |
| -------------------- | --------------------------------------------------------- | ---------------------------- |
| `Owner`              | GitHub user or organization that owns the repository      | Required                     |
| `Repository`         | GitHub repository name                                    | Required                     |
| `Token`              | GitHub authentication token                               | Required                     |
| `ServiceName`        | Name of the application reporting the error               | `null`                       |
| `Environment`        | Application environment                                   | `Unknown`                    |
| `Labels`             | Labels added to newly created issues                      | `service-error`, `automatic` |
| `Deduplicate`        | Search for an existing issue before creating one          | `true`                       |
| `CommentOnDuplicate` | Comment on an existing issue when the error happens again | `true`                       |
| `IncludeStackTrace`  | Include exception stack traces                            | `true`                       |
| `ThrowOnFailure`     | Throw if GitHub reporting itself fails                    | `false`                      |

Example:

```csharp
builder.Services.AddGitHubErrorReporter(options =>
{
    options.Owner = "MyCompany";
    options.Repository = "MyRepository";
    options.Token = builder.Configuration["GitHubIssues:Token"]!;

    options.ServiceName = "TxPlanner.API";
    options.Environment = builder.Environment.EnvironmentName;

    options.Labels =
    [
        "automatic",
        "service-error"
    ];

    options.Deduplicate = true;
    options.CommentOnDuplicate = true;
    options.IncludeStackTrace = true;
    options.ThrowOnFailure = false;
});
```

## Error Handling

By default:

```csharp
options.ThrowOnFailure = false;
```

This means a failure while communicating with GitHub will not cause the application being monitored to fail.

For example:

```text
Application error
       |
       v
Report to GitHub
       |
       +-- Success --> Continue normal error handling
       |
       +-- GitHub unavailable
               |
               v
          Log reporting error
               |
               v
          Do not crash application
```

Set:

```csharp
options.ThrowOnFailure = true;
```

only when the calling application explicitly needs to know that GitHub reporting failed.

## Security

Exception information may contain sensitive application data.

Before sending an issue, the package attempts to sanitize common patterns such as:

```text
Password=secret
Token=secret
ApiKey=secret
Authorization: Bearer ...
```

Sensitive values are replaced with:

```text
[REDACTED]
```

Sanitization should be treated as an additional safeguard, not as a replacement for proper secret handling.

Applications should avoid placing secrets, authentication information, personal data, or confidential business data in exception messages or custom metadata.
