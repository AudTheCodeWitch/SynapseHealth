using SynapseHealth.Core.Serializers;
using SynapseHealth.Core.Models;
using Newtonsoft.Json;

namespace SynapseHealth.Tests.Serializers
{
    [TestClass]
    public class NewtonsoftJsonSerializerTests
    {
        private NewtonsoftJsonSerializer _serializer = null!;

        [TestInitialize]
        public void Setup()
        {
            _serializer = new NewtonsoftJsonSerializer();
        }

        [TestMethod]
        public void Serialize_OrderDetails_ReturnsExpectedJson()
        {
            var order = new OrderDetails
            {
                Device = "CPAP",
                Diagnosis = "OSA",
                DateOfBirth = "1970-01-01",
                OrderingProvider = "Dr. Smith",
                PatientName = "John Doe",
                MaskType = "full face",
                AddOns = ["humidifier"],
                Qualifier = "AHI: 5",
                Usage = "sleep"
            };

            var json = _serializer.Serialize(order);
            var expected = JsonConvert.SerializeObject(order);
            Assert.AreEqual(expected, json);
        }

        [TestMethod]
        public void Serialize_NullObject_ReturnsNullJson()
        {
            string json = _serializer.Serialize<object?>(null);
            Assert.AreEqual("null", json);
        }

        [TestMethod]
        public void Serialize_EmptyOrderDetails_ReturnsJsonWithDefaults()
        {
            var order = new OrderDetails();
            var json = _serializer.Serialize(order);
            var expected = JsonConvert.SerializeObject(order);
            Assert.AreEqual(expected, json);
        }
    }
}
