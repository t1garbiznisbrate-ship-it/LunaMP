using Lidgren.Network;
using LmpCommon.Message.Types;
using System;

namespace LmpCommon.Message.Data.Vessel
{
    public class VesselResourceMsgData : VesselBaseMsgData
    {
        /// <inheritdoc />
        internal VesselResourceMsgData() { }

        public override VesselMessageType VesselMessageType => VesselMessageType.Resource;

        public int ResourcesCount;
        public VesselResourceInfo[] Resources = new VesselResourceInfo[0];

        public override string ClassName { get; } = nameof(VesselResourceMsgData);

        internal override void InternalSerialize(NetOutgoingMessage lidgrenMsg)
        {
            base.InternalSerialize(lidgrenMsg);

            var safeResourcesCount = Resources == null
                ? 0
                : Math.Min(ResourcesCount, Resources.Length);

            lidgrenMsg.Write(safeResourcesCount);

            for (var i = 0; i < safeResourcesCount; i++)
            {
                if (Resources[i] == null)
                    throw new InvalidOperationException("Cannot serialize a null vessel resource.");

                Resources[i].Serialize(lidgrenMsg);
            }
        }

        internal override void InternalDeserialize(NetIncomingMessage lidgrenMsg)
        {
            base.InternalDeserialize(lidgrenMsg);

            ResourcesCount = lidgrenMsg.ReadInt32();

            if (ResourcesCount < 0)
                ResourcesCount = 0;

            if (Resources.Length < ResourcesCount)
                Resources = new VesselResourceInfo[ResourcesCount];

            for (var i = 0; i < ResourcesCount; i++)
            {
                if (Resources[i] == null)
                    Resources[i] = new VesselResourceInfo();

                Resources[i].Deserialize(lidgrenMsg);
            }
        }

        internal override int InternalGetMessageSize()
        {
            var safeResourcesCount = Resources == null
                ? 0
                : Math.Min(ResourcesCount, Resources.Length);

            var arraySize = 0;
            for (var i = 0; i < safeResourcesCount; i++)
            {
                arraySize += Resources[i]?.GetByteCount() ?? 0;
            }

            return base.InternalGetMessageSize() + sizeof(int) + arraySize;
        }
    }
}