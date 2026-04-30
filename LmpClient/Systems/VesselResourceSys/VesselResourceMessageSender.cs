using LmpClient.Base;
using LmpClient.Base.Interface;
using LmpClient.Network;
using LmpClient.Systems.TimeSync;
using LmpCommon.Message.Client;
using LmpCommon.Message.Data.Vessel;
using LmpCommon.Message.Interface;
using System.Collections.Generic;

namespace LmpClient.Systems.VesselResourceSys
{
    public class VesselResourceMessageSender : SubSystem<VesselResourceSystem>, IMessageSender
    {
        private static readonly List<VesselResourceInfo> Resources = new List<VesselResourceInfo>();

        public void SendMessage(IMessageData msg)
        {
            NetworkSender.QueueOutgoingMessage(MessageFactory.CreateNew<VesselCliMsg>(msg));
        }

        public void SendVesselResources(Vessel vessel)
        {
            if (vessel == null || vessel.protoVessel == null || vessel.protoVessel.protoPartSnapshots == null)
                return;

            var resourceCount = 0;

            var msgData = NetworkMain.CliMsgFactory.CreateNewMessageData<VesselResourceMsgData>();
            msgData.GameTime = TimeSyncSystem.UniversalTime;
            msgData.VesselId = vessel.id;

            for (var i = 0; i < vessel.protoVessel.protoPartSnapshots.Count; i++)
            {
                var partSnapshot = vessel.protoVessel.protoPartSnapshots[i];
                if (partSnapshot?.resources == null)
                    continue;

                for (var j = 0; j < partSnapshot.resources.Count; j++)
                {
                    var resource = partSnapshot.resources[j]?.resourceRef;
                    if (resource == null)
                        continue;

                    if (Resources.Count > resourceCount)
                    {
                        Resources[resourceCount].ResourceName = resource.resourceName ?? string.Empty;
                        Resources[resourceCount].PartFlightId = partSnapshot.flightID;
                        Resources[resourceCount].Amount = resource.amount;
                        Resources[resourceCount].FlowState = resource.flowState;
                    }
                    else
                    {
                        Resources.Add(new VesselResourceInfo
                        {
                            ResourceName = resource.resourceName ?? string.Empty,
                            PartFlightId = partSnapshot.flightID,
                            Amount = resource.amount,
                            FlowState = resource.flowState
                        });
                    }

                    resourceCount++;
                }
            }

            msgData.ResourcesCount = resourceCount;

            if (msgData.Resources.Length < resourceCount)
            {
                msgData.Resources = new VesselResourceInfo[resourceCount];
            }

            for (var i = 0; i < resourceCount; i++)
            {
                if (msgData.Resources[i] == null)
                    msgData.Resources[i] = new VesselResourceInfo(Resources[i]);
                else
                    msgData.Resources[i].CopyFrom(Resources[i]);
            }

            SendMessage(msgData);
        }
    }
}