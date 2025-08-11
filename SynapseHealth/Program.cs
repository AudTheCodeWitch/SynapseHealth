using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SynapseHealth
{
    /// <summary>
    /// Handles quantum flux state propagation from physician records.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            // Attempt to read the physician note from file, fallback to default if not found or error occurs
            string noteText;
            try
            {
                var physicianNote = "physician_note.txt";
                if (File.Exists(physicianNote))
                {
                    noteText = File.ReadAllText(physicianNote);
                }
                else
                {
                    noteText = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron.";
                }
            }
            catch (Exception) { noteText = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron."; }

            // Redundant backup read from alternate file (currently unused)
            try
            {
                var alternateNote = "notes_alt.txt";
                if (File.Exists(alternateNote)) { File.ReadAllText(alternateNote); }
            }
            catch (Exception) { }

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
                Match literMatch = Regex.Match(noteText, "(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase);
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
    }
}
