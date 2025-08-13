# SynapseHealth DME Order Submission

This project is a C# console application designed to parse physician's notes, extract medical equipment order details, 
and submit them to an API.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Setup

1.  Clone the repository.
2.  Navigate to the solution directory: `cd SynapseHealth`
3.  Restore the .NET dependencies:
    ```bash
    dotnet restore
    ```
## Configuration
The application uses an `appsettings.json` file for configuration. You can specify the API endpoint in this file.
If you'd like to test this locally, I suggest using [Mockoon](https://mockoon.com/) to easily spin up a mock API.

The default configuration is as follows (not a real link):

```json
{
  "OrderApiSettings": {
    "BaseUrl": "https://alert-api.com/",
    "EndpointPath": "DrExtract"
  }
}
```

## Running the Application

To run the application, you need to provide the path to a physician's note file as a command-line argument.

```bash
dotnet run --project SynapseHealth/SynapseHealth.csproj -- AssessmentResources/physician_note1.txt
```

You can also use the other three physician notes in the `AssessmentResources` directory.

## Exit Codes

The application returns the following exit codes to indicate the result:

| Code | Meaning                                                        |
|------|----------------------------------------------------------------|
| 0    | Success                                                        |
| 1    | Invalid input, file read error, or configuration error         |
| 2    | Regex operation timed out during note parsing                  |
| 3    | JSON serialization error during order submission               |
| 4    | HTTP request was canceled or timed out during order submission |
| 5    | HTTP error occurred during order submission                    |
| 6    | Unhandled exception during submission process                  |

## Test Coverage

This project uses the MSTest framework for unit testing. The following areas are covered by tests:

- **Services:**
  - `NoteParser`: Comprehensive tests for parsing physician notes, including all supported devices (CPAP, Oxygen Tank, 
  Wheelchair, and Walking Aid), missing/partial fields, multiple device keywords, edge cases, and logging of 
  warnings/errors.
  - `OrderSubmissionService`: Tests for successful order submission, error scenarios (HTTP errors, timeouts, serialization 
  failures), and correct logging.
- **Serializers:**
  - `NewtonsoftJsonSerializer`: Tests for correct serialization of models, handling of nulls and empty objects, and 
  compatibility with expected JSON output.
- **Edge Cases:**
  - Exception handling for invalid input, regex timeouts, and unexpected errors.
  - Logging verification for warning and error paths in parsing and submission services.

To run all tests:

```bash
dotnet test
```

## Features & Functionality

- **Device Support:** Parses physician notes for CPAP, Oxygen Tank, Wheelchair, and Walking Aid orders.
- **Robust Parsing:** Uses regular expressions to extract patient name, DOB, diagnosis, device, ordering provider, and 
device-specific details (e.g., mask type, add-ons, AHI for CPAP; liters and usage for Oxygen Tank).
- **Error Handling:** Gracefully handles missing fields, malformed notes, regex timeouts, and unexpected errors. Provides 
clear exit codes and user-friendly console messages.
- **Logging:** Logs all parsing, submission, and error events using Microsoft.Extensions.Logging. Warnings are logged for 
missing fields or unrecognized devices.
- **Configuration:** Reads API endpoint and settings from `appsettings.json`. Validates required configuration values 
before running.
- **Extensible:** Easily add support for new devices or note formats by updating parsing logic and configuration.

## Assumptions
- The physician's notes are expected to follow a specific format for the regex-based parsing to work correctly. Examples
of expected formats are provided in the `AssessmentResources` directory.
- The application assumes that the physician's notes are in plain text format or JSON-wrapped text
- The API is assumed to be available at the endpoint specified in `appsettings.json`.
- To standardize the output, the application will always extract the following fields from the physician's note and will 
default to "Unknown" if they are not present:
  - Patient name
  - DOB
  - Diagnosis
  - Device (e.g., CPAP, Oxygen Tank, or Wheelchair)
  - Ordering provider

## Limitations
- The application currently only supports parsing for CPAP, Oxygen Tank, and Wheelchair devices.
- It does not handle multiple devices in a single note.
- The regex patterns are designed for specific formats and may not work with all variations of physician notes.
- The application does not validate the correctness of the extracted data beyond basic presence checks (that is, it only 
ensures required fields are not empty).

## Future Improvements
- Allow multiple files to be processed in a single run.
- Allow multiple devices to be extracted from a single physician's note.
- Accept different input formats (e.g., .JSON files).
- Support more DME device types or qualifiers.
- Replace the manual extraction logic with an LLM (e.g., OpenAI or Azure OpenAI) for more robust parsing.

## Tools Used

- **IDE**: JetBrains Rider
- **AI Development Tools**: GitHub Copilot (Gemini 2.5 Pro and ChatGPT-4)
- **Testing Framework**: MSTest
- **Logging**: Microsoft.Extensions.Logging

## Assessment Benchmarks

### Core Requirements
✅ **Refactor logic into well-named, testable methods:** Logic is separated into services and helper methods with clear 
naming and structure.

✅ **Introduce logging and basic error handling:** Logging is implemented throughout, and all major error scenarios are 
handled gracefully.

✅ **Write at least one unit test:** Comprehensive unit tests exist for parsing, submission, and serialization logic.

✅ **Replace misleading or unclear comments with helpful ones:** Comments and XML documentation are clear and accurate.

✅ **Keep it functional:** The application reads a physician note from a file, extracts structured data, and POSTs to 
the API endpoint.

### Stretch Goals
✅ **Accept multiple input formats (e.g., JSON-wrapped notes):** Both plain text and JSON-wrapped notes are supported.

✅ **Add configurability for file path or API endpoint:** File path is a CLI argument; API endpoint is configurable via
`appsettings.json`.

✅ **Support more DME device types or qualifiers:** CPAP, Oxygen Tank, Wheelchair, and Walking Aid (walker, cane, 
crutches, knee scooter) are supported.

❌ **Replace manual extraction logic with an LLM:**

