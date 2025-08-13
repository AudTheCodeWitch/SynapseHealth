using SynapseHealth.Core.Models;

namespace SynapseHealth.Core.Services
{
    /// <summary>
    /// Defines a service for submitting structured order details to an external API.
    /// Implementations should handle serialization, HTTP communication, and error handling.
    /// </summary>
    public interface IOrderSubmissionService
    {
        /// <summary>
        /// Submits the specified order details to the configured API endpoint.
        /// </summary>
        /// <param name="orderDetails">The structured order details extracted from a physician's note.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SubmitOrderAsync(OrderDetails orderDetails);
    }
}
