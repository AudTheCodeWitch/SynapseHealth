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
        if (noteText.Contains("full face", StringComparison.OrdinalIgnoreCase))
        {
            details.MaskType = "full face";
        }
        if (noteText.Contains("humidifier", StringComparison.OrdinalIgnoreCase))
        {
            details.AddOns ??= [];
            details.AddOns.Add("humidifier");
        }
        if (noteText.Contains("AHI > 20"))
        {
            details.Qualifier = "AHI > 20";
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

    [GeneratedRegex(@"(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase)]
    private static partial Regex LiterRegex();

    private static string? ExtractInfo(string text, Regex regex)
    {
        var match = regex.Match(text);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}