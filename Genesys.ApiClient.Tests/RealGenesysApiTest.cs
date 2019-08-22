using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Genesys.Bayeux.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Genesys.ApiClient.Tests
{
    [TestClass]
    public class RealGenesysApiTest
    {
        public TestContext TestContext { get; set; }

        string GetTestParam(string key)
        {
            var result = TestContext.Properties[key];
            if (result == null)
                throw new Exception($"TestRunParameter [{key}] not found. (See file test.template.runsettings)");
            return (string)result;
        }

        string BaseUrl;
        string ApiKey;
        AuthApi.PasswordGrantTypeCredentials credentials;
        object statistics;

        [TestInitialize]
        public void Init()
        {
            BaseUrl = GetTestParam("BaseUrl");
            ApiKey = GetTestParam("ApiKey");

            credentials = new AuthApi.PasswordGrantTypeCredentials()
            {
                UserName = GetTestParam("UserNamePath") + @"\" + GetTestParam("UserName"),
                Password = GetTestParam("Password"),
                ClientId = GetTestParam("ClientId"),
                ClientSecret = GetTestParam("ClientSecret"),
            };

            // Random int, padded to 3 digits, with leading zeros if needed.
            var id = new Random().Next(0, 1000).ToString("D3");

            statistics = new
            {
                operationId = $"SUBSCRIPTION_ID_{id}",
                data = new
                {
                    statistics = new[]
                    {
                        new
                        {
                            statisticId = $"STATISTIC_ID_{id}_0",
                            definition = new
                            {
                                notificationMode = "Periodical",
                                subject = "DNStatus",
                                insensitivity = 0,
                                category = "CurrentTime",
                                mainMask = "*",
                                notificationFrequency = 5,
                            },
                            objectId = GetTestParam("UserName"),
                            objectType = "Agent"
                        },
                    }
                }
            };
        }

        async Task<HttpClient> InitHttpClient()
        {
            var httpClient = new HttpClient();
            await InitHttpClient(httpClient);
            return httpClient;
        }

        async Task<HttpClient> InitHttpClient(HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            var token = await AuthApi.Authenticate(httpClient, BaseUrl, credentials);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            return httpClient;
        }

        BayeuxClient InitStatisticsBayeuxClient(HttpClient httpClient)
        {
            var bayeuxClient = new BayeuxClient(new HttpLongPollingTransportOptions()
            {
                HttpClient = httpClient,
                Uri = BaseUrl + "/statistics/v3/notifications",
            });

            bayeuxClient.EventReceived += (e, args) =>
                Debug.WriteLine($"Event received on channel {args.Channel} with data\n{args.Data}");

            bayeuxClient.ConnectionStateChanged += (e, args) =>
                Debug.WriteLine($"Bayeux connection state changed to {args.ConnectionState}");

            return bayeuxClient;
        }

        [TestMethod]
        public async Task Subscribe_statistic()
        {
            var httpClient = await InitHttpClient();

            using (var bayeuxClient = InitStatisticsBayeuxClient(httpClient))
            {
                bayeuxClient.AddSubscriptions(
                    "/statistics/v3/updates",
                    "/statistics/v3/service");

                await bayeuxClient.Start();

                var response = await httpClient.PostAsync(
                    BaseUrl + "/statistics/v3/subscriptions?verbose=INFO",
                    new StringContent(
                        JsonConvert.SerializeObject(statistics),
                        Encoding.UTF8,
                        "application/json"));

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Response to Subscribe: " + responseContent);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                // I have received the following non-compliant error response from the Statistics API:
                // request: [{"clientId":"256fs7hljxavbz317cdt1d7t882v","channel":"/meta/subscribe","subscription":"/pepe"}]
                // response: {"timestamp":1536851691737,"status":500,"error":"Internal Server Error","message":"java.lang.IllegalArgumentException: Invalid channel id: pepe","path":"/statistics/v3/notifications"}

                Thread.Sleep(TimeSpan.FromSeconds(11));

                await bayeuxClient.Unsubscribe("/statistics/v3/service");
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        [ExpectedException(typeof(BayeuxRequestException))]
        // response: {"timestamp":1536851691737,"status":500,"error":"Internal Server Error",
        // "message":"java.lang.IllegalArgumentException: Invalid channel id: pepe",
        // "path":"/statistics/v3/notifications"}
        public async Task Subscribe_invalid_channel_id()
        {
            var httpClient = await InitHttpClient();
            var bayeuxClient = new BayeuxClient(new HttpLongPollingTransportOptions()
            {
                HttpClient = httpClient,
                Uri = BaseUrl + "/statistics/v3/notifications",
            });
            await bayeuxClient.Start();
            await bayeuxClient.Subscribe("pepe");
        }

        [TestMethod]
        public async Task Too_long_connect_delay_causes_unauthorized_error_in_Workspace_API()
        {
            var httpPost = 
                new AgentApiHttpPoster(
                    new HttpClientHttpPost(await InitHttpClient()),
                    BaseUrl);

            //var initResponse = await httpClient.PostAsync(
            //    BaseURL + "/workspace/v3/initialize-workspace", 
            //    new StringContent(""));
            //initResponse.EnsureSuccessStatusCode();

            var bayeuxClient = new BayeuxClient(new HttpLongPollingTransportOptions()
            {
                HttpPost = httpPost,
                Uri = BaseUrl + "/workspace/v3/notifications",
            });
            
            using (bayeuxClient)
            {
                await bayeuxClient.Start();
                await bayeuxClient.Subscribe("/**");
                Thread.Sleep(TimeSpan.FromSeconds(11));
            }
        }

        [TestMethod]
        public async Task Agent_API_error()
        {
            var httpClient = new HttpClient();

            var result = await AgentApi.PostAsync(httpClient,
                BaseUrl + "/workspace/v3/initialize-workspace", 
                new { },
                CancellationToken.None);

        }
    }
}
