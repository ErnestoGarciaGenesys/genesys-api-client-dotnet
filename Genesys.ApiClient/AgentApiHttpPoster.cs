using Genesys.Bayeux.Client;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Genesys.ApiClient
{
    public class AgentApiHttpPoster : IHttpPost
    {
        readonly IHttpPost innerPoster;
        readonly string baseUrl;

        public AgentApiHttpPoster(IHttpPost innerPoster, String baseUrl)
        {
            this.innerPoster = innerPoster;
            this.baseUrl = baseUrl;
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, string jsonContent, CancellationToken cancellationToken)
        {
            var response = await innerPoster.PostAsync(requestUri, jsonContent, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // TODO: check JSON content for unauthorized response

                var initResponse = await innerPoster.PostAsync(baseUrl + "/workspace/v3/initialize-workspace", "", cancellationToken);

                if (initResponse.IsSuccessStatusCode)
                {
                    var secondResponse = await innerPoster.PostAsync(requestUri, jsonContent, cancellationToken);
                    return secondResponse;
                }
            }

            return response;
        }
    }
}
