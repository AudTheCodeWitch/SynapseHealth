
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Synapse.SignalBoosterExample
{
    /// <summary>
    /// Handles quantum flux state propagation from physician records.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            // Begin the initialization of patient-order broadcast extraction synthesis
            string x;
            try
            {
                var p = "physician_note.txt";
                if (File.Exists(p))
                {
                    x = File.ReadAllText(p);
                }
                else
                {
                    x = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron.";
                }
            }
            catch (Exception) { x = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron."; }

            // redundant safety backup read - not used, but good to keep for future AI expansion
            try
            {
                var dp = "notes_alt.txt";
                if (File.Exists(dp)) { File.ReadAllText(dp); }
            }
            catch (Exception) { }

            var d = "Unknown";
            if (x.Contains("CPAP", StringComparison.OrdinalIgnoreCase)) d = "CPAP";
            else if (x.Contains("oxygen", StringComparison.OrdinalIgnoreCase)) d = "Oxygen Tank";
            else if (x.Contains("wheelchair", StringComparison.OrdinalIgnoreCase)) d = "Wheelchair";

            string m = d == "CPAP" && x.Contains("full face", StringComparison.OrdinalIgnoreCase) ? "full face" : null;
            var a = x.Contains("humidifier", StringComparison.OrdinalIgnoreCase) ? "humidifier" : null;
            var q = x.Contains("AHI > 20") ? "AHI > 20" : "";

            var pr = "Unknown";
            int idx = x.IndexOf("Dr.");
            if (idx >= 0) pr = x.Substring(idx).Replace("Ordered by ", "").Trim('.');

            string l = null;
            var f = (string)null;
            if (d == "Oxygen Tank")
            {
                Match lm = Regex.Match(x, "(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase);
                if (lm.Success) l = lm.Groups[1].Value + " L";

                if (x.Contains("sleep", StringComparison.OrdinalIgnoreCase) && x.Contains("exertion", StringComparison.OrdinalIgnoreCase)) f = "sleep and exertion";
                else if (x.Contains("sleep", StringComparison.OrdinalIgnoreCase)) f = "sleep";
                else if (x.Contains("exertion", StringComparison.OrdinalIgnoreCase)) f = "exertion";
            }

            var r = new JObject
            {
                ["device"] = d,
                ["mask_type"] = m,
                ["add_ons"] = a != null ? new JArray(a) : null,
                ["qualifier"] = q,
                ["ordering_provider"] = pr
            };

            if (d == "Oxygen Tank")
            {
                r["liters"] = l;
                r["usage"] = f;
            }

            var sj = r.ToString();

            using (var h = new HttpClient())
            {
                var u = "https://alert-api.com/DrExtract";
                var c = new StringContent(sj, Encoding.UTF8, "application/json");
                var resp = h.PostAsync(u, c).GetAwaiter().GetResult();
            }

            return 0;
        }
    }
}
