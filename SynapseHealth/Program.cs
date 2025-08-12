using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SynapseHealth.Core.Models;
using SynapseHealth.Core.Services;
using Microsoft.Extensions.Options;
using SynapseHealth.Core.Serializers;

namespace SynapseHealth
{
    /// <summary>
    /// Processes physician's notes to extract medical equipment orders and submits them to an API.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application. It sets up configuration, dependency injection,
        /// reads the physician's note from a file specified in the command-line arguments,
        /// parses the note to extract order details, and submits the order to the API.
        /// </summary>
        /// <param name="args">Command-line arguments, expecting the path to the physician's note file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the exit code.</returns>
        /// <summary>
        /// Exit codes:
        /// 0 - Success
        /// 1 - Invalid input or file read error
        /// 2 - Regex operation timed out during note parsing
        /// 3 - JSON serialization error during order submission
        /// 4 - HTTP request was canceled or timed out during order submission
        /// 5 - HTTP error occurred during order submission
        /// 6 - Unhandled exception during submission process
        /// </summary>
        private static async Task<int> Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            await using var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Application starting.");

            if (args.Length == 0)
            {
                logger.LogError("Please provide the path to the physician's note file as a command-line argument.");
                return 1;
            }

            var filePath = args[0];
            logger.LogInformation("Reading physician's note from {FilePath}", filePath);

            string noteText;
            try
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || fileContent.TrimStart().StartsWith("{"))
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(fileContent);
                    noteText = jsonDoc.RootElement.GetProperty("data").GetString() ?? string.Empty;
                }
                else
                {
                    noteText = fileContent;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading the note file from {FilePath}", filePath);
                return 1;
            }

            try
            {
                var noteParser = serviceProvider.GetRequiredService<INoteParser>();
                var orderDetails = noteParser.Parse(noteText);

                var submissionService = serviceProvider.GetRequiredService<IOrderSubmissionService>();
                await submissionService.SubmitOrderAsync(orderDetails);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex, "Invalid input: {Message}", ex.Message);
                return 1;
            }
            catch (RegexMatchTimeoutException ex)
            {
                logger.LogError(ex, "Regex operation timed out during note parsing.");
                return 2;
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogError(ex, "JSON serialization error during order submission: {Message}", ex.Message);
                return 3;
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "HTTP request was canceled or timed out during order submission: {Message}", ex.Message);
                return 4;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error occurred during order submission: {Message}", ex.Message);
                return 5;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An unhandled exception occurred during the submission process.");
                return 6;
            }

            logger.LogInformation("Application finished.");

            return 0;
        }

        /// <summary>
        /// Configures the application's services for dependency injection.
        /// </summary>
        /// <param name="services">The collection of services to configure.</param>
        /// <param name="configuration">The application's configuration.</param>
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(builder => builder.AddConsole());
            services.Configure<OrderApiSettings>(configuration.GetSection("OrderApiSettings"));

            services.AddHttpClient<IOrderSubmissionService, OrderSubmissionService>((serviceProvider, client) =>
            {
                var apiSettings = serviceProvider.GetRequiredService<IOptions<OrderApiSettings>>().Value;
                client.BaseAddress = new Uri(apiSettings.BaseUrl);
            });

            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddSingleton<INoteParser, NoteParser>();
        }
    }
}
