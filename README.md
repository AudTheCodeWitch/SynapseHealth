# SynapseHealth DME Order Submission

This project is a C# console application designed to parse physician's notes, extract medical equipment order details, and submit them to an API.

## Prerequisites

- [.NET 9.0 SDK](httpss://dotnet.microsoft.com/download/dotnet/9.0)

## Setup

1.  Clone the repository.
2.  Navigate to the solution directory: `cd SynapseHealth`
3.  Restore the .NET dependencies:
    ```bash
    dotnet restore
    ```

## Running the Application

To run the application, you need to provide the path to a physician's note file as a command-line argument.

```bash
dotnet run --project SynapseHealth/SynapseHealth.csproj -- physician_note1.txt
```

You can also use `physician_note2.txt` for a different example.

## Running Tests

To run the unit tests for the project:

```bash
dotnet test
```

## Assumptions
- The physician's notes are expected to follow a specific format for the regex-based parsing to work correctly.
- The API is assumed to be available at the endpoint specified in `appsettings.json`.
- To standardize the output, the application requires the following details in the physician's note:
  - Patient name
  - DOB
  - Diagnosis
  - Device (e.g., CPAP, Oxygen Tank, Wheelchair)
  - Ordering provider

## Limitations
- The API endpoint for order submission is currently hardcoded in `appsettings.json`.
- The application currently only supports parsing for CPAP, Oxygen Tank, and Wheelchair devices.

## Future Improvements
- Allow multiple files to be processed in a single run.
- Allow multiple devices to be extracted from a single physician's note.
- Accept different input formats (e.g., JSON-wrapped notes).
- Support more DME device types or qualifiers.
- Replace the manual extraction logic with an LLM (e.g., OpenAI or Azure OpenAI) for more robust parsing.

## Tools Used

- **IDE**: JetBrains Rider
- **AI Development Tools**: GitHub Copilot (Gemini 2.5 Pro)

