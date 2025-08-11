using System.Text.RegularExpressions;
using SynapseHealth.Core.Models;

namespace SynapseHealth.Core.Services;

/// <summary>
/// A service that parses raw text from a physician's note into a structured <see cref="OrderDetails"/> object.
/// This class uses pre-compiled regular expressions for efficient parsing.
/// </summary>
public partial class NoteParser : INoteParser
{
    private static readonly List<(string Keyword, string DeviceName)> DeviceMappings =
    [
        ("CPAP", "CPAP"),
        ("oxygen", "Oxygen Tank"),
        ("wheelchair", "Wheelchair")
    ];

    private static readonly List<string> CpapMaskTypes = ["full face"];
    private static readonly List<string> CpapAddOns = ["humidifier"];

    /// <summary>
    /// Parses the physician's note by calling a series of specialized helper methods.
    /// </summary>
    /// <param name="noteText">The raw text from the physician's note.</param>
    /// <returns>A populated <see cref="OrderDetails"/> object.</returns>
    public OrderDetails Parse(string noteText)
    {
        var orderDetails = new OrderDetails();

        ParseDevice(noteText, orderDetails);
        ParsePatientInfo(noteText, orderDetails);
        ParseOrderingProvider(noteText, orderDetails);

        switch (orderDetails.Device)
        {
            case "CPAP":
                ParseCpapDetails(noteText, orderDetails);
                break;
            case "Oxygen Tank":
                ParseOxygenDetails(noteText, orderDetails);
                break;
        }

        return orderDetails;
    }

    private static void ParseDevice(string noteText, OrderDetails details)
    {
        details.Device = DeviceMappings
            .FirstOrDefault(mapping => noteText.Contains(mapping.Keyword, StringComparison.OrdinalIgnoreCase))
            .DeviceName ?? details.Device;
    }

    private static void ParsePatientInfo(string noteText, OrderDetails details)
    {
        details.PatientName = ExtractInfo(noteText, PatientNameRegex()) ?? details.PatientName;
        details.DateOfBirth = ExtractInfo(noteText, DobRegex()) ?? details.DateOfBirth;
        details.Diagnosis = ExtractInfo(noteText, DiagnosisRegex()) ?? details.Diagnosis;
    }

    private static void ParseOrderingProvider(string noteText, OrderDetails details)
    {
        details.OrderingProvider = ExtractInfo(noteText, OrderingProviderRegex()) ?? details.OrderingProvider;
    }

    private static void ParseCpapDetails(string noteText, OrderDetails details)
    {
        details.MaskType = CpapMaskTypes
            .FirstOrDefault(mask => noteText.Contains(mask, StringComparison.OrdinalIgnoreCase));

        var foundAddOns = CpapAddOns
            .Where(addOn => noteText.Contains(addOn, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (foundAddOns.Count != 0)
        {
            details.AddOns ??= [];
            details.AddOns.AddRange(foundAddOns);
        }
        
        var ahiMatch = AhiRegex().Match(noteText);
        if (ahiMatch.Success)
        {
            var op = ahiMatch.Groups[1].Value;
            var value = ahiMatch.Groups[2].Value;
            details.Qualifier = op == ":" ? $"AHI: {value}" : $"AHI {op} {value}";
        }
    }

    private static void ParseOxygenDetails(string noteText, OrderDetails details)
    {
        var literMatch = LiterRegex().Match(noteText);
        if (literMatch.Success)
        {
            details.Liters = literMatch.Groups[1].Value + " L";
        }

        var usageTypes = new List<string>();
        if (noteText.Contains("sleep", StringComparison.OrdinalIgnoreCase))
        {
            usageTypes.Add("sleep");
        }
        if (noteText.Contains("exertion", StringComparison.OrdinalIgnoreCase))
        {
            usageTypes.Add("exertion");
        }

        if (usageTypes.Count != 0)
        {
            details.Usage = string.Join(" and ", usageTypes);
        }
    }

    [GeneratedRegex(@"Patient Name: (.*)", RegexOptions.IgnoreCase)]
    private static partial Regex PatientNameRegex();

    [GeneratedRegex(@"DOB: (.*)", RegexOptions.IgnoreCase)]
    private static partial Regex DobRegex();

    [GeneratedRegex(@"Diagnosis: (.*)", RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosisRegex();

    [GeneratedRegex(@"Ordered by (Dr\. .*)", RegexOptions.IgnoreCase)]
    private static partial Regex OrderingProviderRegex();

    [GeneratedRegex(@"AHI\s*(>|<|=|:)\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AhiRegex();

    [GeneratedRegex(@"(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase)]
    private static partial Regex LiterRegex();

    private static string? ExtractInfo(string text, Regex regex)
    {
        var match = regex.Match(text);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}