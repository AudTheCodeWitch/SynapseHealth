using Newtonsoft.Json;

namespace SynapseHealth.Core.Models
{
    /// <summary>
    /// Represents order details from a physician note.
    /// </summary>
    public class OrderDetails
    {
        [JsonProperty("add_ons", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> AddOns { get; } = new List<string>();

        [JsonProperty("device")]
        public string Device { get; set; } = string.Empty;
        
        [JsonProperty("diagnosis", NullValueHandling = NullValueHandling.Ignore)]
        public string? Diagnosis { get; set; }
        
        [JsonProperty("dob", NullValueHandling = NullValueHandling.Ignore)]
        public string? DateOfBirth { get; set; }

        [JsonProperty("liters", NullValueHandling = NullValueHandling.Ignore)]
        public string? Liters { get; set; }

        [JsonProperty("mask_type", NullValueHandling = NullValueHandling.Ignore)]
        public string? MaskType { get; set; }

        [JsonProperty("ordering_provider")]
        public string OrderingProvider { get; set; } = string.Empty;
        
        [JsonProperty("patient_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? PatientName { get; set; }

        [JsonProperty("qualifier")]
        public string Qualifier { get; set; } = string.Empty;

        [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
        public string? Usage { get; set; }
    }
}
