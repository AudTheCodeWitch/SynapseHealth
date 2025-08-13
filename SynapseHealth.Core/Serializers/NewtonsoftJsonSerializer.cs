using Newtonsoft.Json;
using SynapseHealth.Core.Serializers;

namespace SynapseHealth.Core.Serializers
{
    /// <summary>
    /// An implementation of <see cref="IJsonSerializer"/> that uses the Newtonsoft.Json library.
    /// </summary>
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        /// <summary>
        /// Serializes the specified object to a JSON string using Newtonsoft.Json.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}

