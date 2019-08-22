using Genesys.ApiClient.Components.ComponentModel;
using Genesys.Bayeux.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Genesys.ApiClient.Components
{
    public class GenesysConnection : ActiveGenesysComponent
    {
        readonly HttpClient httpClient = new HttpClient();
        BayeuxClient bayeuxClient;

        #region Initialization Properties

        [Category("Initialization")]
        public string ApiBaseUrl { get; set; }

        [Category("Initialization")]
        public string ApiKey { get; set; }

        [Category("Initialization")]
        public string ClientId { get; set; }

        [Category("Initialization")]
        public string ClientSecret { get; set; }

        [Category("Initialization")]
        public string UserName { get; set; }

        [Category("Initialization")]
        public string Password { get; set; }

        int openTimeoutMs = 10000;
        [Category("Initialization"), DefaultValue(10000)]
        public int OpenTimeoutMs
        {
            get { return openTimeoutMs; }
            set { openTimeoutMs = value; }
        }

        //bool webSocketsEnabled = false;
        //[Category("Initialization"), DefaultValue(false)]
        //public bool WebSocketsEnabled
        //{
        //    get { return webSocketsEnabled; }
        //    set { webSocketsEnabled = value; }
        //}

        #endregion Initialization Properties

        /// <summary>
        /// When using this constructor, this instance must be disposed explicitly.
        /// </summary>
        public GenesysConnection() { }

        /// <summary>
        /// When using this constructor, this instance will be automatically disposed by the parent container.
        /// </summary>
        public GenesysConnection(IContainer container)
            : this()
        {
            container.Add(this);
        }

        protected override async Task StartImplAsync(UpdateResult result, CancellationToken cancellationToken)
        {
            httpClient.DefaultRequestHeaders.Remove("x-api-key");
            httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);

            var token = await AuthApi.Authenticate(httpClient, ApiBaseUrl, new AuthApi.PasswordGrantTypeCredentials()
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                UserName = UserName,
                Password = Password,
            }); // TODO: missing cancellationToken as a parameter here

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var initResponse = await PostAsync("/workspace/v3/initialize-workspace", null, cancellationToken);

            bayeuxClient = new BayeuxClient(new HttpLongPollingTransportOptions()
            {
                HttpClient = httpClient,
                Uri = ApiBaseUrl + "/workspace/v3/notifications"
            });

            bayeuxClient.EventReceived += (e, args) =>
            {
                Debug.WriteLine($"Event received on channel {args.Channel} with data\n{args.Data}");
                UpdateTree(result2 =>
                    result2.MessageToChildren = args);
            };

            bayeuxClient.ConnectionStateChanged += (e, args) =>
                Debug.WriteLine($"Bayeux connection state changed to {args.ConnectionState}");

            await bayeuxClient.Start(cancellationToken);
            await bayeuxClient.Subscribe("/**", cancellationToken);
        }

        public Task<JObject> PostAsync(string relativeUrl, object json, CancellationToken cancellationToken)
        {
            return AgentApi.PostAsync(httpClient, ApiBaseUrl + relativeUrl, json, cancellationToken);

            //Debug.WriteLine($"POST {relativeUrl}\n{json}");

            //var result = httpClient.PostAsync(
            //    ApiBaseUrl + relativeUrl,
            //    new StringContent(
            //        json == null ? "" : JsonConvert.SerializeObject(json),
            //        Encoding.UTF8,
            //        "application/json"),
            //    cancellationToken);

            //return result;
        }

        protected override void StopImpl(UpdateResult result)
        {
            if (bayeuxClient != null)
            {
                bayeuxClient.Dispose();
                bayeuxClient = null;
            }
        }

        #region Observable Properties

        [ReadOnly(true)]
        public ConnectionState ConnectionState
        {
            get { return ToConnectionState(InternalActivationStage); }
        }

        ConnectionState ToConnectionState(ActivationStage s)
        {
            switch (s)
            {
                case ActivationStage.Idle:
                    return ConnectionState.Close;
                case ActivationStage.Started:
                    return ConnectionState.Open;
                case ActivationStage.Starting:
                default:
                    return ConnectionState.Opening;
            }
        }

        protected override void OnActivationStageChanged(INotifications notifs)
        {
            base.OnActivationStageChanged(notifs);

            RaisePropertyChanged(notifs, "ConnectionState");
        }

        #endregion Observable Properties

        #region Internal

        [Browsable(false)]
        public HttpClient InternalHttpClient => httpClient;

        #endregion Internal
    }

    public enum ConnectionState
    {
        Close,
        Opening,
        Open
    }
}
