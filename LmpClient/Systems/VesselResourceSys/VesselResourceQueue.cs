using LmpClient.Base;
using LmpCommon.Message.Data.Vessel;
using System;

namespace LmpClient.Systems.VesselResourceSys
{
    public class VesselResourceQueue : CachedConcurrentQueue<VesselResource, VesselResourceMsgData>
    {
        protected override void AssignFromMessage(VesselResource value, VesselResourceMsgData msgData)
        {
            value.GameTime = msgData.GameTime;
            value.VesselId = msgData.VesselId;

            value.ResourcesCount = Math.Max(msgData.ResourcesCount, 0);

            if (value.Resources.Length < value.ResourcesCount)
                value.Resources = new VesselResourceInfo[value.ResourcesCount];

            for (var i = 0; i < value.ResourcesCount; i++)
            {
                if (msgData.Resources[i] == null)
                    throw new InvalidOperationException("Cannot queue a null vessel resource.");

                if (value.Resources[i] == null)
                    value.Resources[i] = new VesselResourceInfo();

                value.Resources[i].Amount = msgData.Resources[i].Amount;
                value.Resources[i].FlowState = msgData.Resources[i].FlowState;
                value.Resources[i].PartFlightId = msgData.Resources[i].PartFlightId;
                value.Resources[i].ResourceName = msgData.Resources[i].ResourceName ?? string.Empty;
            }
        }
    }
}