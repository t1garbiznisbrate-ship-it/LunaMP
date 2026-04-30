using LmpClient.Base;
using LmpCommon.Message.Data.Vessel;
using System;

namespace LmpClient.Systems.VesselProtoSys
{
    public class VesselProtoQueue : CachedConcurrentQueue<VesselProto, VesselProtoMsgData>
    {
        protected override void AssignFromMessage(VesselProto value, VesselProtoMsgData msgData)
        {
            value.GameTime = msgData.GameTime;
            value.VesselId = msgData.VesselId;
            value.NumBytes = Math.Max(msgData.NumBytes, 0);
            value.ForceReload = msgData.ForceReload;

            if (msgData.Data == null || msgData.Data.Length < value.NumBytes)
                throw new InvalidOperationException("Cannot queue vessel proto with invalid raw data.");

            if (value.RawData == null || value.RawData.Length < value.NumBytes)
                value.RawData = new byte[value.NumBytes];

            Array.Copy(msgData.Data, value.RawData, value.NumBytes);
        }
    }
}