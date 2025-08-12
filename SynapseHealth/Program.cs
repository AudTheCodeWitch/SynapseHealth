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

            var noteParser = serviceProvider.GetRequiredService<INoteParser>();
            var orderDetails = noteParser.Parse(noteText);

            // This block is now replaced by the OrderSubmissionService
            var resultJson = System.Text.Json.JsonSerializer.Serialize(orderDetails);

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
