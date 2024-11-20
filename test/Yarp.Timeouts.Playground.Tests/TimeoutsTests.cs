using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Yarp.Timeouts.Playground.Tests
{
    /// <summary>
    /// Tests checking different use cases of the Request Timeouts feature of YARP.
    /// There are two routes configured in the Web app:
    /// <list type="number">
    ///     <item>
    ///         <term>route1</term>
    ///         <description>matches all request starting with <c>route1/</c>, timeout is set on the cluster</description>
    ///     </item>
    ///     <item>
    ///         <term>route2</term>
    ///         <description>matches all request starting with <c>route2/</c>, timeout is set on the route</description>
    ///     </item>
    /// </list>
    /// </summary>
    public sealed class TimeoutsTests : IClassFixture<IntegrationFixture>, IDisposable
    {
        private readonly IntegrationFixture _fixture;

        public TimeoutsTests(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        public void Dispose()
        {
            _fixture.MockServer.Reset();
        }

        /// <summary>
        /// An integration test ensuring that reverse proxy is configured correctly.
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
                    .WithStatusCode(StatusCodes.Status200OK));

            using var httpClient = _fixture.CreateHttpClient();

            // Act
            var response = await httpClient.GetAsync("route1/path");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from Mock Server!", await response.Content.ReadAsStringAsync());

            Assert.Single(_fixture.MockServer.LogEntries);

            var proxied = _fixture.MockServer.LogEntries.First().RequestMessage;

            Assert.Equal(HttpMethod.Get.Method, proxied.Method);
            Assert.Equal("/route1/path", proxied.Path);
        }

        /// <summary>
        /// Test checking that reverse proxy returns <see cref="HttpStatusCode.GatewayTimeout"/> when timeout is configured on a cluster.
        /// </summary>
        [Fact]
        public async Task TimeoutIsConfiguredOnCluster_ReturnsGatewayTimeout()
        {
            // Arrange
            _fixture.MockServer
                .Given(Request.Create()
                    .UsingGet()
                    .WithPath("/route1/path"))
                .RespondWith(Response.Create()
                    // The configuration setting "cluster1:HttpRequest:ActivityTimeout" is set to "00:00:10"
                    .WithDelay(TimeSpan.FromSeconds(15))
                    .WithStatusCode(StatusCodes.Status200OK));

            using var httpClient = _fixture.CreateHttpClient();

            // Act
            var response = await httpClient.GetAsync("route1/path");

            // Assert
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);

            if (Debugger.IsAttached)
            {
                // Setting ForwarderRequestConfig.ActivityTimeout doesn't affect debugger, but we're checking to be sure
                Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            }
        }

        /// <summary>
        /// Tect clarifying if reverse proxy should return <see cref="HttpStatusCode.GatewayTimeout"/> or <see cref="HttpStatusCode.BadRequest"/>
        /// when timeout is configured on a route.
        /// </summary>
        [Fact]
        public async Task TimeoutIsConfiguredOnRoute_ShouldReturnGatewayTimeoutOrBadRequest()
        {
            // Arrange
            _fixture.MockServer
                .Given(Request.Create()
                    .UsingGet()
                    .WithPath("/route2/path"))
                .RespondWith(Response.Create()
                    .WithDelay(TimeSpan.FromSeconds(15))
                    .WithStatusCode(StatusCodes.Status200OK));

            using var httpClient = _fixture.CreateHttpClient();

            // Act
            var response = await httpClient.GetAsync("route2/path");

            // Assert
            if (Debugger.IsAttached)
            {
                // Timeout set to a route is not applied when the debugger is attached
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else
            {
                // Reverse proxy returns 400 BadRequest when "Timeout" is set to a route
                // Should it return 504 GatewayTimeout the same as when "HttpRequest:ActivityTimeout" is set to a cluster?
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }
    }
}