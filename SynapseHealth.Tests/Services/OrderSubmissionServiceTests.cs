using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using SynapseHealth.Core.Models;
using SynapseHealth.Core.Services;

namespace SynapseHealth.Tests.Services;

public class OrderSubmissionServiceTests
{
    private Mock<ILogger<OrderSubmissionService>> _mockLogger = null!;
    private Mock<IOptions<OrderApiSettings>> _mockApiSettings = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private Mock<IJsonSerializer> _mockJsonSerializer = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<OrderSubmissionService>>();
        _mockApiSettings = new Mock<IOptions<OrderApiSettings>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockJsonSerializer = new Mock<IJsonSerializer>();

        // Set up the settings to return a specific URL
        var apiSettings = new OrderApiSettings { BaseUrl = "https://testhost.com/", EndpointPath = "api/order" };
        _mockApiSettings.Setup(o => o.Value).Returns(apiSettings);

        // Setup HttpClient using the mocked handler
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(apiSettings.BaseUrl)
        };
    }

    [TestClass]
    public class SubmitOrderAsyncTests : OrderSubmissionServiceTests
    {
        private OrderSubmissionService _service = null!;
        private OrderDetails _orderDetails = null!;

        [TestInitialize]
        public void SubmitOrderAsyncTestsSetup()
        {
            _orderDetails = new OrderDetails { Device = "CPAP" };
            _service = new OrderSubmissionService(_mockLogger.Object, _httpClient, _mockApiSettings.Object, _mockJsonSerializer.Object);
            _mockJsonSerializer.Setup(s => s.Serialize(It.IsAny<OrderDetails>())).Returns(JsonConvert.SerializeObject(_orderDetails));
        }

        [TestMethod]
        public async Task WhenApiCallIsSuccessful_Should_CompleteWithoutError()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Order received"),
                });

            // Act
            await _service.SubmitOrderAsync(_orderDetails);

            // Assert
            // Verify that the logger was called with success information
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Order submitted successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task WhenApiCallFails_Should_ThrowHttpRequestException()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ReasonPhrase = "Internal Server Error"
                });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _service.SubmitOrderAsync(_orderDetails));

            // Verify that the logger was called with error information
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Failed to submit order")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task WhenJsonSerializationFails_Should_ThrowExceptionAndLogError()
        {
            // Arrange
            // Set up the mock serializer to throw an exception
            _mockJsonSerializer.Setup(s => s.Serialize(It.IsAny<OrderDetails>()))
                .Throws(new JsonSerializationException("Serialization failed."));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<JsonSerializationException>(() => _service.SubmitOrderAsync(_orderDetails));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("An unexpected error occurred while submitting the order.")),
                    It.IsAny<JsonSerializationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task WhenNetworkFailureOccurs_Should_ThrowHttpRequestExceptionAndLogError()
        {
            // Arrange
            // Set up the handler to throw an exception, simulating a network failure
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _service.SubmitOrderAsync(_orderDetails));

            // Verify that the logger was called with the correct error information
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("An HTTP error occurred while submitting the order.")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
