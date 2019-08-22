using Genesys.ApiClient.Components.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Genesys.Bayeux.Client.BayeuxClient;

namespace Genesys.ApiClient.Components
{
    public class GenesysAgent : ActiveGenesysComponent
    {
        #region Initialization Properties

        [Category("Initialization")]
        public GenesysConnection Connection
        {
            get { return (GenesysConnection)Parent; }
            set
            {
                if (InternalActivationStage != ActivationStage.Idle)
                    throw new InvalidOperationException("Property must be set while component is not started");

                Parent = value;
            }
        }

        string agentId;

        [Category("Initialization")]
        public string AgentId
        {
            get => agentId ?? Connection?.UserName;
            set => agentId = value;
        }

        [Category("Initialization")]
        public string DN { get; set; }

        #endregion Initialization Properties

        protected override Exception CanStart()
        {
            if (Connection == null)
                return new InvalidOperationException("Connection property must be set");

            if (Connection.ConnectionState != ConnectionState.Open)
                return new ActivationException("Connection is not open");

            return null;
        }

        /// <summary>
        /// Available means that this object has been correctly initialized and all its
        /// resource properties and methods are available to use.
        /// </summary>
        public bool Available { get; private set; }

        //{ return InternalActivationStage == ActivationStage.Started; } }

        public event EventHandler AvailableChanged;

        protected override async Task StartImplAsync(UpdateResult result, CancellationToken cancellationToken)
        {
            //eventSubscription = Connection.InternalEventReceiver.SubscribeAll(OnEventReceived);

            //// Documentation about recovering existing state:
            //// http://docs.genesys.com/Documentation/HTCC/8.5.2/API/RecoveringExistingState#Reading_device_state_and_active_calls_together

            //var response =
            //    await Connection.InternalClient.CreateRequest("GET", "/api/v2/me?subresources=*")
            //        .SendAsync<UserResourceResponse>(cancellationToken);

            //Update(result,
            //    (IDictionary<string, object>)response.AsDictionary["user"],
            //    response.AsType.user);

            ChangeAndNotifyProperty(result.Notifications, "Available", true);
        }

        protected override void StopImpl(UpdateResult result)
        {
            ChangeAndNotifyProperty(result.Notifications, "Available", false);
        }

        protected override void OnParentUpdated(object message, UpdateResult result)
        {
            //if (Connection.ConnectionState == ConnectionState.Open && AutoRecover)
            //    Start(result);

            //if (Connection.ConnectionState == ConnectionState.Close)
            //    Stop(result);


            var userData = (message as EventReceivedArgs)?.Data["data"]?["user"];
            if (userData != null)
                UpdateAttributes(result.Notifications, userData.ToObject<Dictionary<string, object>>());

            result.MessageToChildren = message;
        }

        //void OnEventReceived(object sender, GenesysEvent genesysEvent)
        //{
        //    UpdateTree(results =>
        //    {
        //        if (genesysEvent.Data.ContainsKey("user"))
        //        {
        //            Update(results,
        //                genesysEvent.GetResourceAsType<IDictionary<string, object>>("user"),
        //                genesysEvent.GetResourceAsType<UserResource>("user"));
        //        }
        //        else
        //        {
        //            results.Changed = false;
        //        }

        //        results.MessageToChildren = genesysEvent;
        //    });
        //}

        //void Update(UpdateResult result, IDictionary<string, object> untypedResource, UserResource typedResource)
        //{
        //    UpdateAttributes(result.Notifications, untypedResource);

        //    UserResource = typedResource;

        //    var untypedSettings = (IDictionary<string, object>)untypedResource["settings"];

        //    // Concretizing dictionary type to a dictionary of dictionaries,
        //    // because Settings contains sections, which contain key-value pairs.
        //    Settings = untypedSettings.ToDictionary(kvp => kvp.Key, kvp => (IDictionary<string, object>)kvp.Value);
        //}

        #region Internal

        //public UserResource UserResource { get; private set; }

        #endregion Internal

        //public IDictionary<string, IDictionary<string, object>> Settings { get; private set; }

        #region Operations

        // This is what Workspace does: {"data":{"placeName":"ernesto.garcia","channels":["voice"],"agentId":"ernesto.garcia","autoCompleteCall":false}}

        public Task ActivateChannels(CancellationToken cancellationToken = default(CancellationToken))
            => Connection.PostAsync(
                "/workspace/v3/activate-channels",
                new
                {
                    data = new
                    {
                        agentId = AgentLogin,
                        channels = new[] { "voice" },
                        //dn = DN,
                    },
                },
                cancellationToken);

        public Task Logout(CancellationToken cancellationToken = default(CancellationToken))
            => Connection.PostAsync("/workspace/v3/logout", null, cancellationToken);

        #endregion Operations

        #region Attributes

        public int? Dbid { get { return GetAttribute() as int?; } }
        public int? TenantDbid { get { return GetAttribute() as int?; } }
        public string FirstName { get { return GetAttribute() as string; } }
        public string LastName { get { return GetAttribute() as string; } }
        public string UserName { get { return GetAttribute() as string; } }
        public string EmployeeId { get { return GetAttribute() as string; } }
        public string DefaultPlace { get { return GetAttribute() as string; } }
        public string AgentLogin { get { return GetAttribute() as string; } }

        // TODO: "userProperties": [
        //  {
        //    "key": "htcc",
        //    "type": "kvlist",
        //    "value": [
        //      {
        //        "key": "roles",
        //        "type": "str",
        //        "value": "Agent"
        //      },
        //      {
        //        "key": "Place_1",
        //        "type": "str",
        //        "value": "7199"
        //      }
        //    ]
        //  }
        //],
        #endregion Attributes
    }
}
