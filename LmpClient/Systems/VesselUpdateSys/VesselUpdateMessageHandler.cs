using LmpClient.Base;
using LmpClient.Base.Interface;
using LmpClient.VesselUtilities;
using LmpCommon.Message.Data.Vessel;
using LmpCommon.Message.Interface;
using System.Collections.Concurrent;

namespace LmpClient.Systems.VesselUpdateSys
{
    public class VesselUpdateMessageHandler : SubSystem<VesselUpdateSystem>, IMessageHandler
    {
        public ConcurrentQueue<IServerMessageBase> IncomingMessages { get; set; } = new ConcurrentQueue<IServerMessageBase>();

        public void HandleMessage(IServerMessageBase msg)
        {
            if (!(msg.Data is VesselUpdateMsgData msgData))
                return;

            // We received a message for our own controlled/updated vessel, so ignore it.
            if (!VesselCommon.DoVesselChecks(msgData.VesselId))
                return;

            var queue = System.VesselUpdates.GetOrAdd(msgData.VesselId, _ => new VesselUpdateQueue());
            queue.Enqueue(msgData);
        }
    }
}