using Lidgren.Network;
using LmpCommon.Enums;
using LmpCommon.Message.Data.Vessel;
using LmpCommon.Message.Server.Base;
using LmpCommon.Message.Types;
using System;
using System.Collections.Generic;

namespace LmpCommon.Message.Server
{
    public class VesselSrvMsg : SrvMsgBase<VesselBaseMsgData>
    {
        /// <inheritdoc />
        internal VesselSrvMsg() { }

        /// <inheritdoc />
        public override string ClassName { get; } = nameof(VesselSrvMsg);

        /// <inheritdoc />
        protected override Dictionary<ushort, Type> SubTypeDictionary { get; } = new Dictionary<ushort, Type>
        {
            [(ushort)VesselMessageType.Proto] = typeof(VesselProtoMsgData),
            [(ushort)VesselMessageType.Remove] = typeof(VesselRemoveMsgData),
            [(ushort)VesselMessageType.Position] = typeof(VesselPositionMsgData),
            [(ushort)VesselMessageType.Flightstate] = typeof(VesselFlightStateMsgData),
            [(ushort)VesselMessageType.Update] = typeof(VesselUpdateMsgData),
            [(ushort)VesselMessageType.Resource] = typeof(VesselResourceMsgData),
            [(ushort)VesselMessageType.PartSyncField] = typeof(VesselPartSyncFieldMsgData),
            [(ushort)VesselMessageType.PartSyncUiField] = typeof(VesselPartSyncUiFieldMsgData),
            [(ushort)VesselMessageType.PartSyncCall] = typeof(VesselPartSyncCallMsgData),
            [(ushort)VesselMessageType.ActionGroup] = typeof(VesselActionGroupMsgData),
            [(ushort)VesselMessageType.Fairing] = typeof(VesselFairingMsgData),
            [(ushort)VesselMessageType.Decouple] = typeof(VesselDecoupleMsgData),
            [(ushort)VesselMessageType.Couple] = typeof(VesselCoupleMsgData),
            [(ushort)VesselMessageType.Undock] = typeof(VesselUndockMsgData),
        };

        public override ServerMessageType MessageType => ServerMessageType.Vessel;

        protected override int DefaultChannel => IsUnreliableMessage() ? 0 : 8;

        public override NetDeliveryMethod NetDeliveryMethod => IsUnreliableMessage()
            ? NetDeliveryMethod.UnreliableSequenced
            : NetDeliveryMethod.ReliableOrdered;

        private bool IsUnreliableMessage()
        {
            if (Data == null)
                return false;

            switch ((VesselMessageType)Data.SubType)
            {
                case VesselMessageType.Position:
                case VesselMessageType.Flightstate:
                case VesselMessageType.Update:
                case VesselMessageType.Resource:
                    return true;

                default:
                    return false;
            }
        }
    }
}