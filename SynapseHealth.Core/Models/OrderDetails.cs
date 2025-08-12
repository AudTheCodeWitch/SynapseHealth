using Newtonsoft.Json;

namespace SynapseHealth.Core.Models
{
    /// <summary>
    /// Represents order details from a physician note.
    /// </summary>
    public class OrderDetails
    {
        [JsonProperty("add_ons", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? AddOns { get; set; }

        [JsonProperty("device")]
        public string Device { get; set; } = "Unknown";
        
        [JsonProperty("diagnosis", NullValueHandling = NullValueHandling.Ignore)]
        public string Diagnosis { get; set; } = "Unknown";
        
        [JsonProperty("dob", NullValueHandling = NullValueHandling.Ignore)]
        public string DateOfBirth { get; set; } = "Unknown";

        [JsonProperty("liters", NullValueHandling = NullValueHandling.Ignore)]
        public string? Liters { get; set; }

        [JsonProperty("mask_type", NullValueHandling = NullValueHandling.Ignore)]
        public string? MaskType { get; set; }

        [JsonProperty("ordering_provider")]
        public string OrderingProvider { get; set; } = "Unknown";
        
        [JsonProperty("patient_name", NullValueHandling = NullValueHandling.Ignore)]
        public string PatientName { get; set; } = "Unknown";

        [JsonProperty("qualifier")]
        public string? Qualifier { get; set; }

        [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
        public string? Usage { get; set; }
    }
}
