using Microsoft.Extensions.Hosting;
using WireMock.Server;
using Yarp.Timeouts.Playground.Web;

namespace Yarp.Timeouts.Playground.Tests
{
    public class IntegrationFixture : IDisposable
    {
        private readonly IHost _webApp;

        public IntegrationFixture()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:5233");

            var builder = Program.CreateHostBuilder([]);

            _webApp = builder.Build();
            _webApp.Start();

            MockServer = WireMockServer.Start(5234);
        }

        public WireMockServer MockServer { get; }

        public void Dispose()
        {
            MockServer.Dispose();
            _webApp.StopAsync().Wait();
        }

        public HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5233")
            };
        }
    }
}
