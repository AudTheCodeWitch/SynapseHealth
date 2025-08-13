namespace SynapseHealth.Core.Models
{
    /// <summary>
    /// Represents the settings for the Order API.
    /// </summary>
    public class OrderApiSettings
    {
        public required string BaseUrl { get; set; }
        public required string EndpointPath { get; set; }
    }
}
