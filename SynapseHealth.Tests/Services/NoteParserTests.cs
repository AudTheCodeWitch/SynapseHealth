using Microsoft.Extensions.Logging;
using Moq;
using SynapseHealth.Core.Services;

namespace SynapseHealth.Tests.Services;

public class NoteParserTests
{
    private NoteParser _parser = null!;
    private Mock<ILogger<NoteParser>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<NoteParser>>();
        _parser = new NoteParser(_mockLogger.Object);
    }

    [TestClass]
    public class ParseTests : NoteParserTests
    {
        [TestMethod]
        public void Should_CorrectlyParseFullOxygenNote()
        {
            // Arrange
            const string noteText = """
                                    Patient Name: Harold Finch
                                    DOB: 04/12/1952
                                    Diagnosis: COPD
                                    Prescription: Requires a portable oxygen tank delivering 2 L per minute.
                                    Usage: During sleep and exertion.
                                    Ordered by Dr. Cuddy
                                    """;

            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Oxygen Tank", result.Device);
            Assert.AreEqual("Harold Finch", result.PatientName);
            Assert.AreEqual("04/12/1952", result.DateOfBirth);
            Assert.AreEqual("COPD", result.Diagnosis);
            Assert.AreEqual("2 L", result.Liters);
            Assert.AreEqual("sleep and exertion", result.Usage);
            Assert.AreEqual("Dr. Cuddy", result.OrderingProvider);
        }

        [TestMethod]
        public void Should_CorrectlyParseFullCpapNote()
        {
            // Arrange
            const string noteText = """
                                    Patient Name: Lisa Turner
                                    DOB: 09/23/1984
                                    Diagnosis: Severe sleep apnea
                                    Recommendation: CPAP therapy with full face mask and heated humidifier.
                                    AHI: 28
                                    Ordered by Dr. Foreman
                                    """;

            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("CPAP", result.Device);
            Assert.AreEqual("Lisa Turner", result.PatientName);
            Assert.AreEqual("09/23/1984", result.DateOfBirth);
            Assert.AreEqual("Severe sleep apnea", result.Diagnosis);
            Assert.AreEqual("full face", result.MaskType);
            Assert.IsNotNull(result.AddOns);
            CollectionAssert.Contains(result.AddOns, "humidifier");
            Assert.AreEqual("AHI: 28", result.Qualifier);
            Assert.AreEqual("Dr. Foreman", result.OrderingProvider);
        }

        [TestMethod]
        public void Should_CorrectlyParseWheelchairNote()
        {
            // Arrange
            const string noteText = """
                                    Patient Name: John Locke
                                    DOB: 05/23/1948
                                    Diagnosis: Spinal Cord Injury
                                    Prescription: Standard wheelchair for mobility assistance.
                                    Ordered by Dr. House
                                    """;

            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Wheelchair", result.Device);
            Assert.AreEqual("John Locke", result.PatientName);
            Assert.AreEqual("05/23/1948", result.DateOfBirth);
            Assert.AreEqual("Spinal Cord Injury", result.Diagnosis);
            Assert.AreEqual("Dr. House", result.OrderingProvider);
        }

        [DataTestMethod]
        [DataRow("Requires oxygen during sleep.", "sleep", null, DisplayName = "Oxygen with sleep usage")]
        [DataRow("Requires oxygen during exertion.", "exertion", null, DisplayName = "Oxygen with exertion usage")]
        [DataRow("Requires 2.5L of oxygen during sleep.", "sleep", "2.5 L",
            DisplayName = "Oxygen with sleep usage and liters")]
        public void Should_CorrectlyParsePartialOxygenNotes(string noteText, string expectedUsage,
            string? expectedLiters)
        {
            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Oxygen Tank", result.Device);
            Assert.AreEqual(expectedUsage, result.Usage);
            Assert.AreEqual(expectedLiters, result.Liters);
        }

        [DataTestMethod]
        [DataRow("CPAP with full face mask.", "full face", null, "", DisplayName = "CPAP with mask only")]
        [DataRow("CPAP with heated humidifier.", null, "humidifier", "", DisplayName = "CPAP with add-on only")]
        [DataRow("CPAP with AHI: 28", null, null, "AHI: 28", DisplayName = "CPAP with AHI: 28 qualifier")]
        [DataRow("CPAP with AHI = 28", null, null, "AHI = 28", DisplayName = "CPAP with AHI = 28 qualifier")]
        [DataRow("CPAP with AHI > 28", null, null, "AHI > 28", DisplayName = "CPAP with AHI > 28 qualifier")]
        [DataRow("CPAP with AHI < 28", null, null, "AHI < 28", DisplayName = "CPAP with AHI < 28 qualifier")]
        public void Should_CorrectlyParsePartialCpapNotes(string noteText, string? expectedMask, string? expectedAddon,
            string expectedQualifier)
        {
            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("CPAP", result.Device);
            Assert.AreEqual(expectedMask, result.MaskType);
            Assert.AreEqual(expectedQualifier, result.Qualifier);

            if (expectedAddon is not null)
            {
                Assert.IsNotNull(result.AddOns);
                CollectionAssert.Contains(result.AddOns, expectedAddon);
            }
            else
            {
                Assert.IsNull(result.AddOns);
            }
        }

        [TestMethod]
        public void Should_UseDefaultValues_WhenNoteIsMinimal()
        {
            // Arrange
            const string noteText = "Order for CPAP machine.";

            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("CPAP", result.Device);
            Assert.AreEqual("Unknown", result.PatientName);
            Assert.AreEqual("Unknown", result.DateOfBirth);
            Assert.AreEqual("Unknown", result.Diagnosis);
            Assert.AreEqual("Unknown", result.OrderingProvider);
            Assert.AreEqual(string.Empty, result.Qualifier);
            Assert.IsNull(result.Liters);
            Assert.IsNull(result.MaskType);
            Assert.IsNull(result.Usage);
            Assert.IsNull(result.AddOns);

            VerifyLog(LogLevel.Warning, "Could not extract Patient Name.");
            VerifyLog(LogLevel.Warning, "Could not extract Date of Birth.");
            VerifyLog(LogLevel.Warning, "Could not extract Diagnosis.");
            VerifyLog(LogLevel.Warning, "Could not extract Ordering Provider.");
        }

        [TestMethod]
        public void Should_DefaultDevice_WhenNotSpecified()
        {
            // Arrange
            const string noteText = "Patient requires mobility assistance.";

            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Unknown", result.Device);

            VerifyLog(LogLevel.Warning, "Could not determine device from note. Using default: Unknown");
        }

        [TestMethod]
        public void Should_PrioritizeFirstMatchingDevice_WhenMultipleKeywordsExist()
        {
            // Arrange
            const string noteText = "Patient requires a CPAP machine and a portable oxygen tank.";

            // Act
            var result = _parser.Parse(noteText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("CPAP", result.Device); // "CPAP" appears before "oxygen" in DeviceMappings
        }

        private void VerifyLog(LogLevel level, string message)
        {
            _mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
