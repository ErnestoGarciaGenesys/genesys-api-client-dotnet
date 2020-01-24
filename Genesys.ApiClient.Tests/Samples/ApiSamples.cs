using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Genesys.Bayeux.Client;
using Genesys.Internal.Authentication.Api;
using Genesys.Internal.Provisioning.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using static Genesys.Bayeux.Client.BayeuxClient;

namespace Genesys.ApiClient.Tests.Samples
{
    [TestClass]
    public class ApiSamples
    { 
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Provisioning_API__Awaiting_dbUpdated_event()
        {
            #region Obtain token

            object baseUrl = TestContext.Properties["BaseUrl"];
            var authApi = new AuthenticationApi(
                new Genesys.Internal.Authentication.Client.Configuration
                {
                    BasePath = baseUrl + "/auth/v3",
                    DefaultHeader = new Dictionary<string, string>()
                    {
                        ["x-api-key"] = (string)TestContext.Properties["ApiKey"],
                    },
                });

            var clientId = (string)TestContext.Properties["ClientId"];
            var clientSecret = (string)TestContext.Properties["ClientSecret"];
            var tenant = (string)TestContext.Properties["UserNamePath"];
            var userName = (string)TestContext.Properties["UserName"];
            var userPassword = (string)TestContext.Properties["Password"];

            var tokenResponse = authApi.RetrieveToken(
                grantType: "password",
                authorization:
                    "Basic " +
                    Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(
                        clientId + ":" + clientSecret)),
                username: tenant == null ? userName : tenant + "\\" + userName,
                clientId: clientId,
                password: userPassword);

            Debug.WriteLine("Access token obtained: " + tokenResponse.AccessToken);

            #endregion

            #region Initialize Provisioning API, cookies, and notifications
            Debug.WriteLine("Initializing Provisioning API...");

            var provisioningBaseUrl = (string)TestContext.Properties["ProvisioningBaseUrl"];
            var apiKey = (string)TestContext.Properties["ApiKey"];

            var provisioningApiConfig = new Genesys.Internal.Provisioning.Client.Configuration
            {
                BasePath = provisioningBaseUrl + "/provisioning/v3",
                DefaultHeader = new Dictionary<string, string>()
                {
                    ["x-api-key"] = apiKey,
                    ["Authorization"] = "Bearer " + tokenResponse.AccessToken,
                },
            };

            var cookieContainer = new System.Net.CookieContainer();

            provisioningApiConfig.ApiClient.RestClient.CookieContainer = cookieContainer;

            var sessionApi = new SessionApi(provisioningApiConfig);

            var initResponse = sessionApi.InitializeProvisioning();
            Debug.WriteLine("Provisioning API initialized: " + initResponse);

            Debug.WriteLine("Starting notifications...");
            var bayeuxClient = new BayeuxClient(new HttpLongPollingTransportOptions
            {
                HttpClient = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer }),
                Uri = provisioningBaseUrl + "/provisioning/v3/notifications",
            });

            bayeuxClient.EventReceived += (e, eventArgs) =>
                //Debug.WriteLine($"Event received on channel {eventArgs.Channel} with data\n{eventArgs.Data}\nFull message:\n{eventArgs.Message}");
                Debug.WriteLine($"Event received:\n{eventArgs.Message}");

            bayeuxClient.ConnectionStateChanged += (e, eventArgs) =>
                Debug.WriteLine($"Bayeux connection state changed to {eventArgs.ConnectionState}");

            bayeuxClient.AddSubscriptions("/**");
            bayeuxClient.Start().Wait();
            Debug.WriteLine("Notifications started.");
            #endregion

            var provisioningApiLoaded = new ManualResetEventSlim();

            EventHandler<EventReceivedArgs> dbUpdatedHandler = (_, e) =>
            {
                bool? dbUpdated = e.Data["data"]?["dbUpdated"]?.ToObject<bool>();
                if (dbUpdated.GetValueOrDefault(false))
                    provisioningApiLoaded.Set();
            };

            bayeuxClient.EventReceived += dbUpdatedHandler;

            #region Trigger cache load
            Debug.WriteLine("Calling /args...");
            IRestResponse argsResponse = (IRestResponse)provisioningApiConfig.ApiClient.CallApi(
                method: Method.GET,
                path: "/args",
                queryParams: new List<KeyValuePair<String, String>>(),
                postBody: null,
                headerParams: new Dictionary<String, String>(provisioningApiConfig.DefaultHeader)
                    {
                        { "Accept", "application/json"},
                    },
                formParams: new Dictionary<String, String>(),
                fileParams: new Dictionary<String, FileParameter>(),
                pathParams: new Dictionary<String, String>(),
                contentType: provisioningApiConfig.ApiClient.SelectHeaderContentType(new String[] { "application/json" }));

            Debug.WriteLine("/args called: " + argsResponse);

            int localVarStatusCode = (int)argsResponse.StatusCode;

            var exception = Genesys.Internal.Provisioning.Client.Configuration.DefaultExceptionFactory?.Invoke("GetArgs", argsResponse);
            if (exception != null)
                throw exception;
            #endregion

            Debug.WriteLine("Waiting for dbUpdated...");
            provisioningApiLoaded.Wait(timeout: TimeSpan.FromSeconds(5));
            bayeuxClient.EventReceived -= dbUpdatedHandler;
            Debug.WriteLine("dbUpdated received.");

            //Debug.WriteLine("Sleeping...");
            //Thread.Sleep(TimeSpan.FromSeconds(0));
            //Debug.WriteLine("Finished sleep.");

            #region Call GET users
            var usersApi = new UsersApi(provisioningApiConfig);

            Debug.WriteLine("GETting users...");

            var usersResponse = usersApi.GetUsers(
                //limit: 1,
                //offset: 0,
                filterName: "FirstNameOrLastNameMatches",
                filterParameters: "ernesto.garcia@genesys.com"
            //filterParameters: "tex_lag_daaru_css@office365support.com",
            //filterParameters: "ernesto.garcia@genesys.com",
            //userEnabled: false
            );

            var firstUser = (Newtonsoft.Json.Linq.JObject)usersResponse.Data.Users.FirstOrDefault();

            Debug.WriteLine("User found: {0}", firstUser.ToString() ?? "none");
            #endregion
        }
    }
}
