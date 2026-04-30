using System;
using LmpClient;
using LmpClient.Extensions;
using LmpClient.VesselUtilities;
using LmpCommon.Message.Data.Vessel;

namespace LmpClient.Systems.VesselResourceSys
{
    /// <summary>
    /// Class that maps a message class to a system class. This way we avoid the message caching issues
    /// </summary>
    public class VesselResource
    {
        #region Fields and Properties

        public double GameTime;
        public Guid VesselId;
        public int ResourcesCount;
        public VesselResourceInfo[] Resources = new VesselResourceInfo[0];

        #endregion

        public void ProcessVesselResource()
        {
            var vessel = FlightGlobals.FindVessel(VesselId);
            if (vessel == null)
                return;

            if (!VesselCommon.DoVesselChecks(vessel.id))
                return;

            UpdateVesselFields(vessel);
        }

        private void UpdateVesselFields(Vessel vessel)
        {
            if (vessel.protoVessel == null)
                return;

            for (var i = 0; i < ResourcesCount; i++)
            {
                if (Resources == null || i >= Resources.Length || Resources[i] == null)
                {
                    LunaLog.LogWarning(
                        $"[LMP]: Skipping ProtoPart resource write due to failure to match (vessel {VesselId}, index {i}, reason: invalid resource entry).");
                    continue;
                }

                var resourceInfo = Resources[i];
                var resourceName = resourceInfo.ResourceName ?? string.Empty;

                var partSnapshot = vessel.protoVessel.GetProtoPart(resourceInfo.PartFlightId);
                if (partSnapshot == null)
                {
                    LunaLog.LogWarning(
                        $"[LMP]: Skipping ProtoPart resource write due to failure to match (vessel {VesselId}, part {resourceInfo.PartFlightId}, resource '{resourceName}', reason: proto part not found).");
                    continue;
                }

                var resourceSnapshot = partSnapshot.FindResourceInProtoPart(resourceName);
                if (resourceSnapshot == null)
                {
                    LunaLog.LogWarning(
                        $"[LMP]: Skipping ProtoPart resource write due to failure to match (vessel {VesselId}, part {resourceInfo.PartFlightId}, resource '{resourceName}', reason: proto resource not found).");
                    continue;
                }

                resourceSnapshot.amount = resourceInfo.Amount;
                resourceSnapshot.flowState = resourceInfo.FlowState;

                // Using "resourceSnapshot.resourceRef" sometimes returns null so we also try to get the resource from the part.
                if (resourceSnapshot.resourceRef == null)
                {
                    if (partSnapshot.partRef != null)
                    {
                        var foundResource = partSnapshot.partRef.FindResource(resourceSnapshot.resourceName);
                        if (foundResource != null)
                        {
                            foundResource.amount = resourceInfo.Amount;
                            foundResource.flowState = resourceInfo.FlowState;
                        }
                        else
                        {
                            LunaLog.LogWarning(
                                $"[LMP]: Skipping ProtoPart resource write due to failure to match (vessel {VesselId}, part {resourceInfo.PartFlightId}, resource '{resourceSnapshot.resourceName}', reason: live resource not found on part).");
                        }
                    }
                }
                else
                {
                    resourceSnapshot.resourceRef.amount = resourceInfo.Amount;
                    resourceSnapshot.resourceRef.flowState = resourceInfo.FlowState;
                }
            }
        }
    }
}