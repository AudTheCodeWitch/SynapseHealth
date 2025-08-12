using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SynapseHealth.Core.Models;
using SynapseHealth.Core.Services;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using SynapseHealth.Infrastructure.Services;

namespace SynapseHealth
{
    /// <summary>
    /// Handles quantum flux state propagation from physician records.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            await using var serviceProvider = services.BuildServiceProvider();

            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the path to the physician's note file as a command-line argument.");
                return 1;
            }

            string noteText;
            try
            {
                noteText = File.ReadAllText(args[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading the note file: {ex.Message}");
                return 1;
            }

            // Extract device type from note
            var device = "Unknown";
            if (noteText.Contains("CPAP", StringComparison.OrdinalIgnoreCase)) device = "CPAP";
            else if (noteText.Contains("oxygen", StringComparison.OrdinalIgnoreCase)) device = "Oxygen Tank";
            else if (noteText.Contains("wheelchair", StringComparison.OrdinalIgnoreCase)) device = "Wheelchair";

            // Extract mask type, add-ons, and qualifier
            string maskType = device == "CPAP" && noteText.Contains("full face", StringComparison.OrdinalIgnoreCase) ? "full face" : null;
            var addOns = noteText.Contains("humidifier", StringComparison.OrdinalIgnoreCase) ? "humidifier" : null;
            var qualifier = noteText.Contains("AHI > 20") ? "AHI > 20" : "";

            // Extract ordering provider from note
            var providerName = "Unknown";
            int drIndex = noteText.IndexOf("Dr.");
            if (drIndex >= 0) providerName = noteText.Substring(drIndex).Replace("Ordered by ", "").Trim('.');

            // Oxygen tank specific extraction: liters and flow type
            string liters = null;
            var usageType = (string)null;
            if (device == "Oxygen Tank")
            {
                Match literMatch = Regex.Match(noteText, @"(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase);
                if (literMatch.Success) liters = literMatch.Groups[1].Value + " L";

                if (noteText.Contains("sleep", StringComparison.OrdinalIgnoreCase) && noteText.Contains("exertion", StringComparison.OrdinalIgnoreCase)) usageType = "sleep and exertion";
                else if (noteText.Contains("sleep", StringComparison.OrdinalIgnoreCase)) usageType = "sleep";
                else if (noteText.Contains("exertion", StringComparison.OrdinalIgnoreCase)) usageType = "exertion";
            }

            // Build structured result object for API submission
            var result = new JObject
            {
                ["device"] = device,
                ["mask_type"] = maskType,
                ["add_ons"] = addOns != null ? new JArray(addOns) : null,
                ["qualifier"] = qualifier,
                ["ordering_provider"] = providerName
            };

            if (device == "Oxygen Tank")
            {
                result["liters"] = liters;
                result["usage"] = usageType;
            }

            var resultJson = result.ToString();

            using (var client = new HttpClient())
            {
                var endpointUrl = "https://alert-api.com/DrExtract";
                var payload = new StringContent(resultJson, Encoding.UTF8, "application/json");
                var response = client.PostAsync(endpointUrl, payload).GetAwaiter().GetResult();
            }

            return 0;
        }

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
