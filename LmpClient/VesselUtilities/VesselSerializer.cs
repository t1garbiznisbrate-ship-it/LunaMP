using LmpClient.Extensions;
using LmpClient.Utilities;
using System;

namespace LmpClient.VesselUtilities
{
    public class VesselSerializer
    {
        /// <summary>
        /// Deserialize a byte array into a protovessel
        /// </summary>
        public static ProtoVessel DeserializeVessel(byte[] data, int numBytes)
        {
            try
            {
                if (data == null || numBytes <= 0 || numBytes > data.Length)
                    return null;

                var vesselNode = data.DeserializeToConfigNode(numBytes);
                if (vesselNode == null)
                    return null;

                var configGuid = vesselNode.GetValue("pid");
                if (!Guid.TryParse(configGuid, out var vesselId))
                {
                    LunaLog.LogError($"[LMP]: Error while deserializing vessel: invalid vessel pid '{configGuid}'");
                    return null;
                }

                return CreateSafeProtoVesselFromConfigNode(vesselNode, vesselId);
            }
            catch (Exception e)
            {
                LunaLog.LogError($"[LMP]: Error while deserializing vessel: {e}");
                return null;
            }
        }

        /// <summary>
        /// Serialize a protovessel into a byte array
        /// </summary>
        public static byte[] SerializeVessel(ProtoVessel protoVessel)
        {
            return PreSerializationChecks(protoVessel, out var configNode) ? configNode.Serialize() : new byte[0];
        }

        /// <summary>
        /// Serializes a vessel to a previous preallocated array (avoids garbage generation)
        /// </summary>
        public static void SerializeVesselToArray(ProtoVessel protoVessel, byte[] data, out int numBytes)
        {
            numBytes = 0;

            if (data == null || data.Length == 0)
            {
                LunaLog.LogError("[LMP]: Cannot serialize vessel to a null or empty byte array");
                return;
            }

            if (PreSerializationChecks(protoVessel, out var configNode))
            {
                configNode.SerializeToArray(data, out numBytes);
            }
        }

        /// <summary>
        /// Creates a protovessel from a ConfigNode
        /// </summary>
        public static ProtoVessel CreateSafeProtoVesselFromConfigNode(ConfigNode inputNode, Guid protoVesselId)
        {
            try
            {
                if (inputNode == null)
                    return null;

                // Cannot create a protovessel if HighLogic.CurrentGame is null as we don't have a CrewRoster
                // and the protopartsnapshot constructor needs it.
                if (HighLogic.CurrentGame == null)
                    return null;

                // Cannot reuse the Protovessel to save memory garbage as it does not have any clear method.
                return new ProtoVessel(inputNode, HighLogic.CurrentGame);
            }
            catch (Exception e)
            {
                LunaLog.LogError($"[LMP]: Damaged vessel {protoVesselId}, exception: {e}");
                return null;
            }
        }

        #region Private methods

        private static bool PreSerializationChecks(ProtoVessel protoVessel, out ConfigNode configNode)
        {
            configNode = new ConfigNode();

            if (protoVessel == null)
            {
                LunaLog.LogError("[LMP]: Cannot serialize a null protovessel");
                return false;
            }

            try
            {
                protoVessel.Save(configNode);
            }
            catch (Exception e)
            {
                LunaLog.LogError($"[LMP]: Error while saving vessel: {e}");
                return false;
            }

            var vesselPid = configNode.GetValue("pid");
            if (!Guid.TryParse(vesselPid, out var vesselId))
            {
                LunaLog.LogError($"[LMP]: Cannot serialize vessel with invalid pid '{vesselPid}'");
                return false;
            }

            // Defend against NaN orbits.
            if (configNode.VesselHasNaNPosition())
            {
                LunaLog.LogError($"[LMP]: Vessel {vesselId} has NaN position");
                return false;
            }

            return true;
        }

        #endregion
    }
}