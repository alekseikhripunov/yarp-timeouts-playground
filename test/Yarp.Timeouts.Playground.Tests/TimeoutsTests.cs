using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Yarp.Timeouts.Playground.Tests
{
    public class TimeoutsTests : IClassFixture<IntegrationFixture>
    {
        private readonly IntegrationFixture _fixture;

        public TimeoutsTests(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// An integration test insures that reverse proxy is configured correctly.
        /// </summary>
        [Fact]
        public async Task ReverseProxyIsConfiguredCorrectly_ProxiesCorrectly()
        {
            // Arrange
            _fixture.MockServer
                .Given(Request.Create()
                    .UsingGet()
                    .WithPath("/route1/path"))
                .RespondWith(Response.Create()
                    .WithBody("Hello from Mock Server!")
                    .WithStatusCode(StatusCodes.Status202Accepted));

            using var httpClient = _fixture.CreateHttpClient();

            // Act
            var response = await httpClient.GetAsync("route1/path");

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("Hello from Mock Server!", await response.Content.ReadAsStringAsync());

            Assert.Single(_fixture.MockServer.LogEntries);

            var proxied = _fixture.MockServer.LogEntries.First().RequestMessage;

            Assert.Equal(HttpMethod.Get.Method, proxied.Method);
            Assert.Equal("/route1/path", proxied.Path);
        }

        [Fact]
        public async Task TimeoutIsConfiguredPerCluster_ReturnsGatewayTimeout()
        {
            // Arrange
            _fixture.MockServer
                .Given(Request.Create()
                    .UsingGet()
                    .WithPath("/route1/path"))
                .RespondWith(Response.Create()
                    // In configuration cluster1:HttpRequest:ActivityTimeout is set to "00:00:10"
                    .WithDelay(TimeSpan.FromSeconds(15))
                    .WithStatusCode(StatusCodes.Status202Accepted));

            using var httpClient = _fixture.CreateHttpClient();

            // Act
            var response = await httpClient.GetAsync("route1/path");

            // Assert
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);

            // Setting ForwarderRequestConfig.ActivityTimeout doesn't affect debugger, but we're checking to be sure
            if (Debugger.IsAttached)
            {
                Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            }
        }

        [Fact]
        public async Task TimeoutIsConfiguredPerRoute_ReturnsBadRequest()
        {
            // Arrange
            _fixture.MockServer
                .Given(Request.Create()
                    .UsingGet()
                    .WithPath("/route2/path"))
                .RespondWith(Response.Create()
                    .WithDelay(TimeSpan.FromSeconds(15))
                    .WithStatusCode(StatusCodes.Status202Accepted));

            using var httpClient = _fixture.CreateHttpClient();

            // Act
            var response = await httpClient.GetAsync("route2/path");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}