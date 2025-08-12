using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SynapseHealth.Core.Models;

namespace SynapseHealth.Core.Services;

/// <summary>
/// A service that parses raw text from a physician's note into a structured <see cref="OrderDetails"/> object.
/// This class uses pre-compiled regular expressions for efficient parsing.
/// </summary>
public partial class NoteParser : INoteParser
{
    private readonly ILogger<NoteParser> _logger;

    private static readonly List<(string Keyword, string DeviceName)> DeviceMappings =
    [
        ("CPAP", "CPAP"),
        ("oxygen", "Oxygen Tank"),
        ("wheelchair", "Wheelchair"),
        ("walker", "Walking Aid"),
        ("cane", "Walking Aid"),
        ("crutches", "Walking Aid"),
        ("knee scooter", "Walking Aid"),
    ];

    private static readonly List<string> CpapMaskTypes = ["full face"];
    private static readonly List<string> CpapAddOns = ["humidifier"];
    private static readonly List<string> WalkingAidTypes = ["walker", "cane", "crutches", "knee scooter"];

    /// <summary>
    /// Initializes a new instance of the <see cref="NoteParser"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging information and warnings.</param>
    public NoteParser(ILogger<NoteParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses the physician's note by calling a series of specialized helper methods.
    /// </summary>
    /// <param name="noteText">The raw text from the physician's note.</param>
    /// <returns>A populated <see cref="OrderDetails"/> object.</returns>
    public OrderDetails Parse(string noteText)
    {
        if (string.IsNullOrWhiteSpace(noteText))
        {
            _logger.LogError("Input note text is null or empty.");
            throw new ArgumentException("Note text cannot be null or empty.", nameof(noteText));
        }

        try
        {
            _logger.LogInformation("Starting note parsing.");
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
                case "Walking Aid":
                    ParseWalkingAidDetails(noteText, orderDetails);
                    break;
            }

            _logger.LogInformation("Note parsing complete.");
            return orderDetails;
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "Regex operation timed out during note parsing.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during note parsing.");
            throw;
        }
    }

    private void ParseDevice(string noteText, OrderDetails details)
    {
        var deviceName = DeviceMappings
            .FirstOrDefault(mapping => noteText.Contains(mapping.Keyword, StringComparison.OrdinalIgnoreCase))
            .DeviceName;

        if (deviceName is null)
        {
            _logger.LogWarning("Could not determine device from note. Using default: {DefaultDevice}", details.Device);
            return;
        }

        details.Device = deviceName;
    }

    private void ParsePatientInfo(string noteText, OrderDetails details)
    {
        details.PatientName = ExtractInfo(noteText, PatientNameRegex(), "Patient Name") ?? details.PatientName;
        details.DateOfBirth = ExtractInfo(noteText, DobRegex(), "Date of Birth") ?? details.DateOfBirth;
        details.Diagnosis = ExtractInfo(noteText, DiagnosisRegex(), "Diagnosis") ?? details.Diagnosis;
    }

    private void ParseOrderingProvider(string noteText, OrderDetails details)
    {
        details.OrderingProvider = ExtractInfo(noteText, OrderingProviderRegex(), "Ordering Provider") ?? details.OrderingProvider;
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

        if (usageTypes.Any())
        {
            details.Usage = string.Join(" and ", usageTypes);
        }
    }

    private static void ParseWalkingAidDetails(string noteText, OrderDetails details)
    {
        details.MaskType = null;
        details.Liters = null;
        details.Usage = null;
        details.AddOns = null;
        details.Qualifier = null;

        var aidType = WalkingAidTypes.FirstOrDefault(type => noteText.Contains(type, StringComparison.OrdinalIgnoreCase));
        if (aidType != null)
        {
            details.Qualifier = $"Type: {aidType}";
        }
    }

    [GeneratedRegex(@"Patient Name: (.*)", RegexOptions.IgnoreCase)]
    private static partial Regex PatientNameRegex();

    [GeneratedRegex(@"DOB: (.*)", RegexOptions.IgnoreCase)]
    private static partial Regex DobRegex();

    [GeneratedRegex(@"Diagnosis: (.*)", RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosisRegex();

    [GeneratedRegex(@"(?:Ordered by|Ordering Physician:)\s*(Dr\. .*)", RegexOptions.IgnoreCase)]
    private static partial Regex OrderingProviderRegex();

    [GeneratedRegex(@"AHI\s*(>|<|=|:)\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AhiRegex();

    [GeneratedRegex(@"(\d+(\.\d+)?) ?L", RegexOptions.IgnoreCase)]
    private static partial Regex LiterRegex();

    private string? ExtractInfo(string text, Regex regex, string fieldName)
    {
        try
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "Regex operation timed out while extracting {FieldName}.", fieldName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while extracting {FieldName}.", fieldName);
            throw;
        }

        _logger.LogWarning("Could not extract {FieldName}.", fieldName);
        return null;
    }
}