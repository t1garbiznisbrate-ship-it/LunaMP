using LmpClient.Base;
using LmpClient.Base.Interface;
using LmpClient.Systems.VesselRemoveSys;
using LmpCommon.Message.Data.Vessel;
using LmpCommon.Message.Interface;
using System.Collections.Concurrent;

namespace LmpClient.Systems.VesselProtoSys
{
    public class VesselProtoMessageHandler : SubSystem<VesselProtoSystem>, IMessageHandler
    {
        public ConcurrentQueue<IServerMessageBase> IncomingMessages { get; set; } = new ConcurrentQueue<IServerMessageBase>();

        public void HandleMessage(IServerMessageBase msg)
        {
            if (!(msg.Data is VesselProtoMsgData msgData))
                return;

            // We don't call VesselCommon.DoVesselChecks(msgData.VesselId) because we may receive a
            // proto update on our own vessel, for example when someone docks against us and we don't detect it.
            // Therefore, we manually check only whether the vessel is scheduled to be killed.
            if (VesselRemoveSystem.Singleton.VesselWillBeKilled(msgData.VesselId))
                return;

            var queue = System.VesselProtos.GetOrAdd(msgData.VesselId, _ => new VesselProtoQueue());
            queue.Enqueue(msgData);
        }
    }
}