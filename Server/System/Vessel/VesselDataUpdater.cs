using LunaConfigNode.CfgNode;
using Server.Log;
using Server.Settings.Structures;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Server.System.Vessel
{
    /// <summary>
    /// We try to avoid working with protovessels as much as possible as they can be huge files.
    /// This class patches the vessel file with the information messages we receive about a position and other vessel properties.
    /// This way we send the whole vessel definition only when there are parts that have changed 
    /// </summary>
    public partial class VesselDataUpdater
    {
        #region Semaphore

        /// <summary>
        /// To not overwrite our own data we use a lock
        /// </summary>
        private static readonly ConcurrentDictionary<Guid, object> Semaphore = new ConcurrentDictionary<Guid, object>();

        #endregion

        /// <summary>
        /// Sets ORBIT IDENT from the reference body name when provided, for example from position or update messages.
        /// </summary>
        internal static void ApplyOrbitIdent(Classes.Vessel vessel, string bodyName)
        {
            if (vessel == null || string.IsNullOrEmpty(bodyName))
                return;

            if (vessel.Orbit.Exists("IDENT"))
                vessel.Orbit.Update("IDENT", bodyName);
            else
                vessel.Orbit.Add(new CfgNodeValue<string, string>("IDENT", bodyName));
        }

        /// <summary>
        /// Raw updates a vessel in the dictionary and takes care of the locking in case we received another vessel message type.
        /// </summary>
        public static void RawConfigNodeInsertOrUpdate(Guid vesselId, string vesselDataInConfigNodeFormat)
        {
            if (vesselId == Guid.Empty || string.IsNullOrEmpty(vesselDataInConfigNodeFormat))
                return;

            _ = Task.Run(() =>
            {
                try
                {
                    var vessel = new Classes.Vessel(vesselDataInConfigNodeFormat);

                    if (GeneralSettings.SettingsStore.ModControl)
                    {
                        var vesselParts = vessel.Parts
                            .GetAllValues()
                            .Select(p => p.Fields.GetSingle("name")?.Value)
                            .Where(p => !string.IsNullOrEmpty(p));

                        var bannedParts = vesselParts.Except(ModFileSystem.ModControl.AllowedParts);
                        if (bannedParts.Any())
                        {
                            LunaLog.Warning($"Received a vessel with BANNED parts! {vesselId}");
                            return;
                        }
                    }

                    lock (Semaphore.GetOrAdd(vesselId, _ => new object()))
                    {
                        VesselStoreSystem.CurrentVessels.AddOrUpdate(vesselId, vessel, (key, existingVal) => vessel);
                    }
                }
                catch (Exception e)
                {
                    LunaLog.Error($"Error inserting/updating raw vessel {vesselId}: {e}");
                }
            });
        }
    }
}