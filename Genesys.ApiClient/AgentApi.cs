using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Genesys.ApiClient
{
    public static class AgentApi
    {
        public static async Task<JObject> PostAsync(HttpClient httpClient, string uri, object json, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"POST {uri}\n{json}");

            var response = await httpClient.PostAsync(
                uri,
                new StringContent(
                    json == null ? "" : JsonConvert.SerializeObject(json),
                    Encoding.UTF8,
                    "application/json"),
                cancellationToken);

            //if (!response.IsSuccessStatusCode)
            //{
                var responseStr = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Received: {responseStr}");
            //}

            response.EnsureSuccessStatusCode();

            //var responseToken = JToken.ReadFrom(new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())));

            var responseToken = JToken.Parse(responseStr);

            return (JObject)responseToken;
        }
    }
}
