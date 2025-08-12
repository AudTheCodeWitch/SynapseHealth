using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SynapseHealth.Core.Models;

namespace SynapseHealth.Core.Services
{
    /// <summary>
    /// A service responsible for submitting order details to an external API.
    /// This service handles JSON serialization, HTTP POST requests, and robust error handling.
    /// </summary>
    /// <param name="logger">The logger for recording submission status and errors.</param>
    /// <param name="httpClient">The HttpClient instance used for making API requests.</param>
    /// <param name="apiSettings">The configuration options containing the API endpoint URL.</param>
    public class OrderSubmissionService(ILogger<OrderSubmissionService> logger, HttpClient httpClient, IOptions<OrderApiSettings> apiSettings) : IOrderSubmissionService
    {
        private readonly string _apiEndpoint = apiSettings.Value.EndpointUrl;

        /// <summary>
        /// Serializes the order details to JSON and POSTs them to the configured API endpoint.
        /// It logs the outcome and throws an exception if the submission fails.
        /// </summary>
        /// <param name="orderDetails">The order details to be submitted.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SubmitOrderAsync(OrderDetails orderDetails)
        {
            try
            {
                var json = JsonConvert.SerializeObject(orderDetails);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                logger.LogInformation("Submitting order to {ApiEndpoint}", _apiEndpoint);
                var response = await httpClient.PostAsync(_apiEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Order submitted successfully.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("Failed to submit order. Status: {StatusCode}. Reason: {ReasonPhrase}. Details: {ErrorContent}", 
                        response.StatusCode, response.ReasonPhrase, errorContent);
                    response.EnsureSuccessStatusCode(); // Throws HttpRequestException for non-success codes
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "An HTTP error occurred while submitting the order.");
                throw; // Re-throw the exception to be handled by the caller
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while submitting the order.");
                throw; // Re-throw for global error handling
            }
        }
    }
}
