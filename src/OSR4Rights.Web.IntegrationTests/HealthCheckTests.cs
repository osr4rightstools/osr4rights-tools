using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using OSR4Rights.Web.IntegrationTests.Helpers;
using Xunit;

// *** to get tests to run I need to comment out
namespace OSR4Rights.Web.IntegrationTests
{
    // IClassFixture is a Generic interface which expects a class type which will act as a fixture
    // WebApplicationFactory is the type - which is in AspNetCore.Mvc.Testing
    // Startup is the entry point for the service under test

    // As our app really depends on the database
    // this healthcheck is not that useful
    //public class HealthCheckTests : IClassFixture<WebApplicationFactory<Startup>>
    //{
    //    private readonly HttpClient _httpClient;

    //    public HealthCheckTests(WebApplicationFactory<Startup> factory)
    //    {
    //        // create a client that we can use to send a request to the test server
    //        _httpClient = factory.CreateDefaultClient();
    //    }

    //    [Fact]
    //    public async Task HealthCheck_ReturnsOk()
    //    {
    //        var response = await _httpClient.GetAsync("/healthcheck");

    //        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //    }

    //}


    public class HealthCheckWithDb : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public HealthCheckWithDb(CustomWebApplicationFactory<Startup> factory)
        {
            factory.ClientOptions.AllowAutoRedirect = false;
            _factory = factory;
        }

        // Page is to check db is okay
        // And background services are still alive
        // could use uptime robot to send an error code if background services have gone down
        // or any sort of error
        // ie has background service done a ping in last 5 minutes (db check)
        // has each task run successfully in last 5 minutes
        //   1.clean up VMs
        //   2.alert for vm that is stuck in running state with no good output being logged
        [Fact]
        public async Task Get_HealthCheckDb_ShouldReturn200_AndCorrectH1()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/health-check-db");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // AngleSharp to check html content is what is expected
            using var content = await HtmlHelpers.GetDocumentAsync(response);

            var h1 = content.QuerySelector("h1");
            Assert.Equal("HealthCheckDb", h1?.TextContent);
        }
    }
}
