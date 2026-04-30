using Lidgren.Network;
using LmpCommon.Message.Types;
using System;

namespace LmpCommon.Message.Data.Vessel
{
    public class VesselProtoMsgData : VesselBaseMsgData
    {
        internal VesselProtoMsgData() { }

        public int NumBytes;
        public byte[] Data = new byte[0];
        public bool ForceReload;

        public override VesselMessageType VesselMessageType => VesselMessageType.Proto;

        public override string ClassName { get; } = nameof(VesselProtoMsgData);

        internal override void InternalSerialize(NetOutgoingMessage lidgrenMsg)
        {
            base.InternalSerialize(lidgrenMsg);

            lidgrenMsg.Write(ForceReload);

            if (Data == null)
            {
                NumBytes = 0;
                Data = new byte[0];
            }

            Common.ThreadSafeCompress(this, ref Data, ref NumBytes);

            if (NumBytes < 0 || NumBytes > Data.Length)
                throw new InvalidOperationException("Invalid vessel proto data length after compression.");

            lidgrenMsg.Write(NumBytes);
            lidgrenMsg.Write(Data, 0, NumBytes);
        }

        internal override void InternalDeserialize(NetIncomingMessage lidgrenMsg)
        {
            base.InternalDeserialize(lidgrenMsg);

            ForceReload = lidgrenMsg.ReadBoolean();

            NumBytes = lidgrenMsg.ReadInt32();
            if (NumBytes < 0)
                throw new InvalidOperationException("Invalid vessel proto data length received.");

            if (Data == null || Data.Length < NumBytes)
                Data = new byte[NumBytes];

            lidgrenMsg.ReadBytes(Data, 0, NumBytes);

            Common.ThreadSafeDecompress(this, ref Data, NumBytes, out NumBytes);
        }

        internal override int InternalGetMessageSize()
        {
            return base.InternalGetMessageSize()
                   + sizeof(bool)
                   + sizeof(int)
                   + sizeof(byte) * Math.Max(NumBytes, 0);
        }
    }
}