using Lidgren.Network;
using LmpCommon.Message.Base;
using LmpCommon.Message.Types;

namespace LmpCommon.Message.Data.Vessel
{
    public class VesselUpdateMsgData : VesselBaseMsgData
    {
        /// <inheritdoc />
        internal VesselUpdateMsgData() { }

        public override VesselMessageType VesselMessageType => VesselMessageType.Update;

        public string Name;
        public string Type;
        public double DistanceTraveled;
        public string Situation;
        public bool Landed;
        public bool Splashed;
        public bool Persistent;
        public string LandedAt;
        public string DisplayLandedAt;
        public double MissionTime;
        public double LaunchTime;
        public double LastUt;
        public uint RefTransformId;
        public bool AutoClean;
        public string AutoCleanReason;
        public bool WasControllable;
        public int Stage;
        public float[] Com = new float[3];

        /// <summary>
        /// Reference body name for ORBIT IDENT (same meaning as VesselPositionMsgData.BodyName).
        /// </summary>
        public string BodyName;

        public override string ClassName { get; } = nameof(VesselUpdateMsgData);

        internal override void InternalSerialize(NetOutgoingMessage lidgrenMsg)
        {
            base.InternalSerialize(lidgrenMsg);

            lidgrenMsg.Write(Name ?? string.Empty);
            lidgrenMsg.Write(Type ?? string.Empty);
            lidgrenMsg.Write(DistanceTraveled);
            lidgrenMsg.Write(Situation ?? string.Empty);
            lidgrenMsg.Write(Landed);
            lidgrenMsg.Write(Splashed);
            lidgrenMsg.Write(Persistent);
            lidgrenMsg.Write(LandedAt ?? string.Empty);
            lidgrenMsg.Write(DisplayLandedAt ?? string.Empty);
            lidgrenMsg.Write(MissionTime);
            lidgrenMsg.Write(LaunchTime);
            lidgrenMsg.Write(LastUt);
            lidgrenMsg.Write(RefTransformId);
            lidgrenMsg.Write(AutoClean);
            lidgrenMsg.Write(AutoCleanReason ?? string.Empty);
            lidgrenMsg.Write(WasControllable);
            lidgrenMsg.Write(Stage);

            for (var i = 0; i < 3; i++)
                lidgrenMsg.Write(Com[i]);

            lidgrenMsg.Write(BodyName ?? string.Empty);
        }

        internal override void InternalDeserialize(NetIncomingMessage lidgrenMsg)
        {
            base.InternalDeserialize(lidgrenMsg);

            Name = lidgrenMsg.ReadString();
            Type = lidgrenMsg.ReadString();
            DistanceTraveled = lidgrenMsg.ReadDouble();
            Situation = lidgrenMsg.ReadString();
            Landed = lidgrenMsg.ReadBoolean();
            Splashed = lidgrenMsg.ReadBoolean();
            Persistent = lidgrenMsg.ReadBoolean();
            LandedAt = lidgrenMsg.ReadString();
            DisplayLandedAt = lidgrenMsg.ReadString();
            MissionTime = lidgrenMsg.ReadDouble();
            LaunchTime = lidgrenMsg.ReadDouble();
            LastUt = lidgrenMsg.ReadDouble();
            RefTransformId = lidgrenMsg.ReadUInt32();
            AutoClean = lidgrenMsg.ReadBoolean();
            AutoCleanReason = lidgrenMsg.ReadString();
            WasControllable = lidgrenMsg.ReadBoolean();
            Stage = lidgrenMsg.ReadInt32();

            for (var i = 0; i < 3; i++)
                Com[i] = lidgrenMsg.ReadFloat();

            BodyName = lidgrenMsg.Position < lidgrenMsg.LengthBits
                ? lidgrenMsg.ReadString()
                : string.Empty;
        }

        internal override int InternalGetMessageSize()
        {
            return base.InternalGetMessageSize()
                   + sizeof(double) * 4
                   + sizeof(bool) * 5
                   + sizeof(uint)
                   + sizeof(int)
                   + sizeof(float) * 3
                   + (Name?.GetByteCount() ?? 0)
                   + (Type?.GetByteCount() ?? 0)
                   + (Situation?.GetByteCount() ?? 0)
                   + (LandedAt?.GetByteCount() ?? 0)
                   + (DisplayLandedAt?.GetByteCount() ?? 0)
                   + (AutoCleanReason?.GetByteCount() ?? 0)
                   + (BodyName?.GetByteCount() ?? 0);
        }
    }
}