using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace OSR4Rights.Web.IntegrationTests
{
    // Base CustomWebApplicationFactory class sets the ASPNETCORE_ENVIRONMENT to Test
    // so that pages that require a Db connection will not fail
    public class GeneralPageTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public GeneralPageTests(CustomWebApplicationFactory<Startup> factory)
        {
            factory.ClientOptions.AllowAutoRedirect = false;
            _factory = factory;
        }

        // should be only pages that don't need auth
        public static IEnumerable<object[]> ValidUrls => new List<object[]>
        {
            new object[] {"/"},
            new object[] {"/Index"},
            new object[] {"/Privacy"},
            //new object[] {"/Enquiry"}
        };

        [Theory]
        [MemberData(nameof(ValidUrls))]
        public async Task ValidUrls_ReturnSuccessAndExpectedContentType(string path)
        {
            var expected = new MediaTypeHeaderValue("text/html");

            var client = _factory.CreateClient();
            //var client = _factory.CreateDefaultClient();

            var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();

            Assert.Equal(expected.MediaType, response.Content.Headers.ContentType.MediaType);
        }

    }
}
