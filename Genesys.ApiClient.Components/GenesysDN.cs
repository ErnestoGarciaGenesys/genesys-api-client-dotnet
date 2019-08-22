using Genesys.ApiClient.Components.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Genesys.Bayeux.Client.BayeuxClient;

namespace Genesys.ApiClient.Components
{
    public class GenesysDN : GenesysComponent
    {
        #region Initialization Properties

        [Category("Initialization")]
        public GenesysAgent User
        {
            get { return (GenesysAgent)Parent; }
            set
            {
                if (Parent != null && Parent != value)
                    throw new InvalidOperationException("User can only be set once");

                Parent = value;
            }
        }

        //int deviceIndex = 0;

        //[Category("Initialization")]
        //public int DeviceIndex
        //{
        //    get { return deviceIndex; }
        //    set
        //    {
        //        if (value < 0)
        //            throw new ArgumentOutOfRangeException("value", value, "DeviceIndex must be nonnegative");

        //        deviceIndex = value;
        //    }
        //}

        #endregion Initialization Properties

        //string id;

        protected override void OnParentUpdated(object message, UpdateResult result)
        {
            //if (message == null && User.UserResource != null)
            //{
            //    RefreshDevice(result,
            //        User.UserResource.devices,
            //        (object[])User.ResourceData["devices"]);
            //}
            //else
            //{
            //    var genesysEvent = message as GenesysEvent;
            //    if (genesysEvent != null && genesysEvent.MessageType == "DeviceStateChangeMessage")
            //        RefreshDevice(result,
            //            genesysEvent.GetResourceAsType<IReadOnlyList<DeviceResource>>("devices"),
            //            genesysEvent.GetResourceAsType<object[]>("devices"));
            //}

            var dnData = (message as EventReceivedArgs)?.Data["dn"];
            if (dnData != null)
                UpdateAttributes(result.Notifications, dnData.ToObject<Dictionary<string, object>>());
        }

        //void RefreshDevice(UpdateResult result, IReadOnlyList<DeviceResource> devices, object[] devicesData)
        //{
        //    DeviceResource device = null;
        //    IDictionary<string, object> newDeviceData = null;
        //    if (id == null)
        //    {
        //        if (devices.Count() > 0)
        //        {
        //            device = devices[deviceIndex];
        //            id = device.id;
        //            newDeviceData = (IDictionary<string, object>)devicesData[deviceIndex];
        //        }
        //    }
        //    else
        //    {
        //        var i = devices.ToList().FindIndex(d => id == d.id);
        //        if (i >= 0)
        //        {
        //            device = devices[i];
        //            newDeviceData = (IDictionary<string, object>)devicesData[i];
        //        }
        //    }

        //    if (device == null)
        //        return;

        //    UpdateAttributes(result.Notifications, newDeviceData);

        //    ChangeAndNotifyProperty(result.Notifications, "UserState", device.userState);
        //}

        // [Browsable(false)], needs to be Browsable for enabling data binding to its properties.
        //[Bindable(BindableSupport.Yes), ReadOnly(true)]
        //public UserState UserState
        //{
        //    get;
        //    private set;
        //}
        public Task Ready(CancellationToken cancellationToken = default(CancellationToken))
            => User.Connection.PostAsync("/workspace/v3/voice/ready", new { }, cancellationToken);

        public Task NotReady(CancellationToken cancellationToken = default(CancellationToken))
            => User.Connection.PostAsync("/workspace/v3/voice/not-ready", new { }, cancellationToken);
        
        #region Attributes

        //{
        //  "dn": {
        //    "number": "+34696518571",
        //    "switchName": "us-east-1",
        //    "agentId": "Support\\jim.crespino@genesys.com",
        //    "capabilities": [
        //      "ready",
        //      "not-ready",
        //      "dnd-on",
        //      "set-forward",
        //      "start-monitoring"
        //    ],
        //    "telephonyNetwork": "Private",
        //    "agentState": "NotReady",
        //    "agentWorkMode": "Unknown"
        //  },
        //  "messageType": "DnStateChanged"
        //}

        public string Number { get { return GetAttribute() as string; } }
        public string SwitchName { get { return GetAttribute() as string; } }
        public string AgentId { get { return GetAttribute() as string; } }
        public string TelephonyNetwork { get { return GetAttribute() as string; } }
        public string AgentState { get { return GetAttribute() as string; } }
        public string AgentWorkMode { get { return GetAttribute() as string; } }
        
        #endregion Attributes
    }
}
