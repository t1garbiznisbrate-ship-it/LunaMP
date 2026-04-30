using Lidgren.Network;
using LmpCommon.Message.Base;
using LmpCommon.Message.Types;
using System;

namespace LmpCommon.Message.Data.Vessel
{
    public class VesselSyncMsgData : VesselBaseMsgData
    {
        /// <inheritdoc />
        internal VesselSyncMsgData() { }

        public override VesselMessageType VesselMessageType => VesselMessageType.Sync;

        public int VesselsCount;
        public Guid[] VesselIds = new Guid[0];

        public override string ClassName { get; } = nameof(VesselSyncMsgData);

        internal override void InternalSerialize(NetOutgoingMessage lidgrenMsg)
        {
            base.InternalSerialize(lidgrenMsg);

            var safeVesselsCount = VesselIds == null
                ? 0
                : Math.Min(VesselsCount, VesselIds.Length);

            lidgrenMsg.Write(safeVesselsCount);

            for (var i = 0; i < safeVesselsCount; i++)
            {
                GuidUtil.Serialize(VesselIds[i], lidgrenMsg);
            }
        }

        internal override void InternalDeserialize(NetIncomingMessage lidgrenMsg)
        {
            base.InternalDeserialize(lidgrenMsg);

            VesselsCount = lidgrenMsg.ReadInt32();

            if (VesselsCount < 0)
                VesselsCount = 0;

            if (VesselIds == null || VesselIds.Length < VesselsCount)
                VesselIds = new Guid[VesselsCount];

            for (var i = 0; i < VesselsCount; i++)
            {
                VesselIds[i] = GuidUtil.Deserialize(lidgrenMsg);
            }
        }

        internal override int InternalGetMessageSize()
        {
            var safeVesselsCount = VesselIds == null
                ? 0
                : Math.Min(VesselsCount, VesselIds.Length);

            return base.InternalGetMessageSize()
                   + sizeof(int)
                   + GuidUtil.ByteSize * safeVesselsCount;
        }
    }
}