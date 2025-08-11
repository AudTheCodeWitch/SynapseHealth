using SynapseHealth.Core.Models;

namespace SynapseHealth.Core.Services;

/// <summary>
/// Defines a service for parsing physician's note text into structured order details.
/// </summary>
public interface INoteParser
{
    /// <summary>
    /// Parses the provided note text.
    /// </summary>
    /// <param name="noteText">The raw text from the physician's note.</param>
    /// <returns>An <see cref="OrderDetails"/> object populated with the parsed data.</returns>
    OrderDetails Parse(string noteText);
}
