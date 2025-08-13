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
    /// Entry point and orchestration for the SynapseHealth CLI application.
    /// This program reads a physician's note from a file, parses it into structured order details,
    /// validates configuration, and submits the order to an external API. It provides robust error handling,
    /// user feedback, and logging for troubleshooting and operational monitoring.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main application logic:
        /// - Loads configuration and sets up dependency injection.
        /// - Validates command-line arguments and input file existence.
        /// - Reads and parses the physician's note.
        /// - Validates required API configuration values.
        /// - Submits the parsed order details to the API.
        /// - Handles and logs all expected error scenarios, providing user-friendly console output and exit codes.
        /// </summary>
        /// <param name="args">Command-line arguments. Expects a single argument: the path to the physician's note file.
        /// </param>
        /// <returns>Exit code indicating the result (see below).</returns>
        /// <remarks>
        /// Exit codes:
        /// 0 - Success
        /// 1 - Invalid input, file read error, or configuration error
        /// 2 - Regex operation timed out during note parsing
        /// 3 - JSON serialization error during order submission
        /// 4 - HTTP request was canceled or timed out during order submission
        /// 5 - HTTP error occurred during order submission
        /// 6 - Unhandled exception during submission process
        /// </remarks>
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
                await LogAndWriteErrorAsync(logger, "Please provide the path to the physician's note file as " +
                                                    "a command-line argument.");
                return 1;
            }

            var filePath = args[0];
            logger.LogInformation("Reading physician's note from {FilePath}", filePath);

            var noteText = await ReadNoteFileAsync(filePath, logger);
            if (noteText == null)
                return 1;

            var apiSettings = serviceProvider.GetRequiredService<IOptions<OrderApiSettings>>().Value;
            if (!ValidateApiSettings(apiSettings, logger))
                return 1;

            try
            {
                var noteParser = serviceProvider.GetRequiredService<INoteParser>();
                var orderDetails = noteParser.Parse(noteText);

                var submissionService = serviceProvider.GetRequiredService<IOrderSubmissionService>();
                await submissionService.SubmitOrderAsync(orderDetails);
            }
            catch (ArgumentException ex)
            {
                await LogAndWriteErrorAsync(logger, ex.Message, ex);
                return 1;
            }
            catch (RegexMatchTimeoutException ex)
            {
                await LogAndWriteErrorAsync(logger, "Regex operation timed out during note parsing.", ex);
                return 2;
            }
            catch (System.Text.Json.JsonException ex)
            {
                await LogAndWriteErrorAsync(logger, "JSON serialization error during order submission.", ex);
                return 3;
            }
            catch (TaskCanceledException ex)
            {
                await LogAndWriteErrorAsync(logger, "HTTP request was canceled or timed out during order " +
                                                    "submission.", ex);
                return 4;
            }
            catch (HttpRequestException ex)
            {
                // Print a user-friendly message for connection errors
                if (ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
                {
                    await Console.Error.WriteLineAsync("Error: Could not connect to the API endpoint. Please check " +
                                                       "that the API is running and the endpoint is correct.");
                }
                else
                {
                    await Console.Error.WriteLineAsync("Error: HTTP error occurred during order submission.");
                }
                logger.LogError(ex, "HTTP error occurred during order submission.");
                return 5;
            }
            catch (Exception ex)
            {
                await LogAndWriteErrorAsync(logger, "An unexpected error occurred during the submission process.",
                    ex, LogLevel.Critical);
                return 6;
            }

            logger.LogInformation("Application finished. File processed: {FilePath}", filePath);

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

        /// <summary>
        /// Reads the physician's note file and extracts the note text, supporting both plain text and JSON formats.
        /// Provides error handling and logging for file existence and read errors.
        /// </summary>
        /// <param name="filePath">Path to the physician's note file.</param>
        /// <param name="logger">Logger for error reporting.</param>
        /// <returns>The extracted note text, or null if an error occurs.</returns>
        private static async Task<string?> ReadNoteFileAsync(string filePath, ILogger logger)
        {
            if (!File.Exists(filePath))
            {
                await LogAndWriteErrorAsync(logger, $"The file '{filePath}' does not exist.");
                return null;
            }
            try
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || fileContent.TrimStart().StartsWith('{'))
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(fileContent);
                    return jsonDoc.RootElement.GetProperty("data").GetString() ?? string.Empty;
                }
                else
                {
                    return fileContent;
                }
            }
            catch (Exception ex)
            {
                await LogAndWriteErrorAsync(logger, $"Could not read the note file '{filePath}'.", ex);
                return null;
            }
        }

        /// <summary>
        /// Validates that required API configuration values are present.
        /// Logs and reports any missing values.
        /// </summary>
        /// <param name="apiSettings">API settings to validate.</param>
        /// <param name="logger">Logger for error reporting.</param>
        /// <returns>True if configuration is valid; false otherwise.</returns>
        private static bool ValidateApiSettings(OrderApiSettings apiSettings, ILogger logger)
        {
            if (!string.IsNullOrWhiteSpace(apiSettings.BaseUrl) &&
                !string.IsNullOrWhiteSpace(apiSettings.EndpointPath)) return true;
            LogAndWriteErrorAsync(logger, $"API configuration is missing required values (BaseUrl or EndpointPath). " +
                                          $"BaseUrl: '{apiSettings.BaseUrl}', EndpointPath: '{apiSettings.EndpointPath}'").Wait();
            return false;
        }

        /// <summary>
        /// Logs an error message and writes it to the console in a consistent format.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="message">Error message to log and display.</param>
        /// <param name="ex">Optional exception for detailed logging.</param>
        /// <param name="logLevel">Log level (default: Error).</param>
        private static async Task LogAndWriteErrorAsync(ILogger logger,
            string message,
            Exception? ex = null,
            LogLevel logLevel = LogLevel.Error)
        {
            await Console.Error.WriteLineAsync($"Error: {message}");
            if (ex != null)
                logger.Log(logLevel, ex, "{ErrorMessage}", message);
            else
                logger.Log(logLevel, "{ErrorMessage}", message);
        }
    }
}
