using GitIssues.Configuration;
using GitIssues.Fingerprinting;
using GitIssues.GitHub;
using GitIssues.Sanitization;
using GitIssues.Service;
using Microsoft.Extensions.DependencyInjection;

namespace GitIssues.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGitHubErrorReporter(
            this IServiceCollection services,
            Action<GitHubErrorReporterOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services.Configure(configure);

            services.AddSingleton<
                IErrorFingerprintGenerator,
                ErrorFingerprintGenerator>();

            services.AddSingleton<
                IErrorSanitizer,
                ErrorSanitizer>();

            services.AddHttpClient<
                IGitHubIssueClient,
                GitHubIssueClient>(client =>
                {
                    client.BaseAddress =
                    new Uri("https://api.github.com/");

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "GitHubErrorReporter");
                });

            services.AddTransient<
                IGitHubErrorReporter,
                Service.GitHubErrorReporter>();

            return services;
        }
    }
}
