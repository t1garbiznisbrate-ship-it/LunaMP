using System;
using LmpClient.Base;
using LmpCommon.Message.Data.Vessel;

namespace LmpClient.Systems.VesselUpdateSys
{
    public class VesselUpdateQueue : CachedConcurrentQueue<VesselUpdate, VesselUpdateMsgData>
    {
        protected override void AssignFromMessage(VesselUpdate value, VesselUpdateMsgData msgData)
        {
            value.GameTime = msgData.GameTime;
            value.VesselId = msgData.VesselId;
            value.Name = msgData.Name ?? string.Empty;
            value.Type = msgData.Type ?? string.Empty;
            value.DistanceTraveled = msgData.DistanceTraveled;
            value.Situation = msgData.Situation ?? string.Empty;
            value.Landed = msgData.Landed;
            value.Splashed = msgData.Splashed;
            value.Persistent = msgData.Persistent;
            value.LandedAt = msgData.LandedAt ?? string.Empty;
            value.DisplayLandedAt = msgData.DisplayLandedAt ?? string.Empty;
            value.MissionTime = msgData.MissionTime;
            value.LaunchTime = msgData.LaunchTime;
            value.LastUt = msgData.LastUt;
            value.RefTransformId = msgData.RefTransformId;
            value.AutoClean = msgData.AutoClean;
            value.AutoCleanReason = msgData.AutoCleanReason ?? string.Empty;
            value.WasControllable = msgData.WasControllable;
            value.Stage = msgData.Stage;
            Array.Copy(msgData.Com, value.Com, 3);
            value.BodyName = msgData.BodyName ?? string.Empty;
        }
    }
}