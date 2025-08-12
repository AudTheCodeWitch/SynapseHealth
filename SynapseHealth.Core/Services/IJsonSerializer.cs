namespace SynapseHealth.Core.Services
{
    /// <summary>
    /// Defines a contract for a JSON serializer.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <returns>A JSON string representation of the object.</returns>
        string Serialize<T>(T obj);
    }
}

