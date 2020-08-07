using AutoFixture;
using Moq;
using Moq.Protected;
using Orchestration.Tasks.Clients;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestration.Tasks.Test.Mocks
{
    public class MockReportingServicesClient : IReportingServicesClient
    {

        private string _result;
        public HttpClient Client { get; }

        public MockReportingServicesClient(string result)
        {

            var fixture = new Fixture();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken cancellationToken) => GetMockResponse(request, cancellationToken));

            Client = new HttpClient(mockHttpMessageHandler.Object);
            Client.BaseAddress = fixture.Create<Uri>();

            _result = result;

        }

        private Task<HttpResponseMessage> GetMockResponse(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(_result, Encoding.UTF8, "application/json");

                return Task.FromResult(response);            
            
        }

    }
}
