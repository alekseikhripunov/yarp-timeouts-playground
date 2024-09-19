namespace Yarp.Timeouts.Playground.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddRequestTimeouts()
                .AddReverseProxy()
                    .LoadFromConfig(_configuration.GetSection("ReverseProxy"));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseRequestTimeouts();
            app.UseEndpoints(builder => builder.MapReverseProxy());
        }
    }
}