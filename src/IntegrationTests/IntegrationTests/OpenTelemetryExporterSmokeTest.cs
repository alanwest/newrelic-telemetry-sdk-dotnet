﻿using Newtonsoft.Json;
using IntegrationTests.Fixtures;
using System.Net.Http;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationTests
{
    public class OpenTelemetryExporterSmokeTest : IClassFixture<OpenTelemetryUsageApplicationFixture>
    {
        private readonly OpenTelemetryUsageApplicationFixture _fixture;

        private const string _traceApiKey = "{YOUR_TRACE_API_KEY}";

        private const string _insightQueryApiKey = "{YOUR_INSIGHT_QUERY_API_KEY}";

        private const string _insightQueryApiEndpoint = "https://insights-api.newrelic.com";

        private const string _accountNumber = "{YOUR_ACCOUNT_NUMBER}";

        public OpenTelemetryExporterSmokeTest(OpenTelemetryUsageApplicationFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.TestLogger = output;

            _fixture.Exercise = () =>
            {
                _fixture.MakeRequestToWeatherforecastEndpoint();
            };

            _fixture.SetEnvironmentVariables(new Dictionary<string, string>()
            {
                {"NewRelic:ApiKey", _traceApiKey}
            });

            _fixture.Initialize();
        }

        [Fact]
        public async void Test()
        {
            using var httpClient = new HttpClient();

            // SELECT * FROM Span WHERE service.name = 'SampleAspNetCoreApp' SINCE 2 minutes ago
            var insightQuery = "SELECT%20*%20FROM%20Span%20WHERE%20service.name%20%3D%20%27SampleAspNetCoreApp%27%20SINCE%202%20minutes%20ago";
            var request = new HttpRequestMessage(HttpMethod.Get, @$"{_insightQueryApiEndpoint}/v1/accounts/{_accountNumber}/query?nrql={insightQuery}");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Query-Key", _insightQueryApiKey);

            var result = await httpClient.SendAsync(request);
            var body = await result.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<NewRelicResponse>(body);

            Assert.NotNull(response);
            Assert.Single(response.Results);
            Assert.Equal(2, response.Results.FirstOrDefault().Events.Count);

            response.Results.FirstOrDefault().Events.ForEach(item =>
            {
                Assert.NotNull(item.Guid);
                Assert.NotNull(item.TraceId);
                Assert.NotNull(item.Name);
                Assert.Equal("SampleAspNetCoreApp", item.ServiceName);
                Assert.Equal("SampleAspNetCoreApp", item.EntityName);
            });
        }
    }
}
