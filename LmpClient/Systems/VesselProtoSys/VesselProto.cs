using LmpClient.Extensions;
using LmpClient.Systems.VesselRemoveSys;
using LmpClient.Utilities;
using LmpClient.VesselUtilities;
using System;

namespace LmpClient.Systems.VesselProtoSys
{
    public class VesselProto
    {
        public Guid VesselId;
        public byte[] RawData = new byte[0];
        public int NumBytes;
        public double GameTime;
        public bool ForceReload;

        public ProtoVessel CreateProtoVessel()
        {
            if (RawData == null || NumBytes <= 0 || NumBytes > RawData.Length)
            {
                LunaLog.LogError($"Received malformed vessel data from SERVER. Id {VesselId}");
                VesselRemoveSystem.Singleton.KillVessel(VesselId, true, "Malformed vessel data");
                return null;
            }

            var configNode = RawData.DeserializeToConfigNode(NumBytes);
            if (configNode == null || configNode.VesselHasNaNPosition())
            {
                LunaLog.LogError($"Received a malformed vessel from SERVER. Id {VesselId}");
                VesselRemoveSystem.Singleton.KillVessel(VesselId, true, "Malformed vessel");
                return null;
            }

            var newProto = VesselSerializer.CreateSafeProtoVesselFromConfigNode(configNode, VesselId);
            if (newProto == null)
            {
                LunaLog.LogError($"Received a malformed vessel from SERVER. Id {VesselId}");
                VesselRemoveSystem.Singleton.KillVessel(VesselId, true, "Malformed vessel");
                return null;
            }

            return newProto;
        }
    }
}