using LunaConfigNode;
using Server.Context;
using Server.Log;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Server.System
{
    /// <summary>
    /// Here we keep a copy of all the player vessels in <see cref="Vessel"/> format and we also save them to files at a specified rate
    /// </summary>
    public static class VesselStoreSystem
    {
        public const string VesselFileFormat = ".txt";
        public static string VesselsPath = Path.Combine(ServerContext.UniverseDirectory, "Vessels");

        public static ConcurrentDictionary<Guid, Vessel.Classes.Vessel> CurrentVessels = new ConcurrentDictionary<Guid, Vessel.Classes.Vessel>();

        private static readonly object BackupLock = new object();

        public static bool VesselExists(Guid vesselId) => CurrentVessels.ContainsKey(vesselId);

        /// <summary>
        /// Removes a vessel from the store
        /// </summary>
        public static void RemoveVessel(Guid vesselId)
        {
            CurrentVessels.TryRemove(vesselId, out _);

            _ = Task.Run(() =>
            {
                try
                {
                    lock (BackupLock)
                    {
                        FileHandler.FileDelete(Path.Combine(VesselsPath, $"{vesselId}{VesselFileFormat}"));
                    }
                }
                catch (Exception e)
                {
                    LunaLog.Error($"Error deleting vessel file {vesselId}: {e}");
                }
            });
        }

        /// <summary>
        /// Returns a vessel in the standard KSP format
        /// </summary>
        public static string GetVesselInConfigNodeFormat(Guid vesselId)
        {
            return CurrentVessels.TryGetValue(vesselId, out var vessel)
                ? vessel.ToString()
                : null;
        }

        /// <summary>
        /// Load the stored vessels into the dictionary
        /// </summary>
        public static void LoadExistingVessels()
        {
            Directory.CreateDirectory(VesselsPath);

            ChangeExistingVesselFormats();

            lock (BackupLock)
            {
                foreach (var file in Directory.GetFiles(VesselsPath).Where(f => Path.GetExtension(f) == VesselFileFormat))
                {
                    if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var vesselId))
                        continue;

                    try
                    {
                        var vesselText = FileHandler.ReadFileText(file);
                        if (!string.IsNullOrEmpty(vesselText))
                            CurrentVessels.TryAdd(vesselId, new Vessel.Classes.Vessel(vesselText));
                    }
                    catch (Exception e)
                    {
                        LunaLog.Error($"Error loading vessel file {file}: {e}");
                    }
                }
            }
        }

        /// <summary>
        /// Transform OLD Xml vessels into the new format
        /// TODO: Remove this for next version
        /// </summary>
        public static void ChangeExistingVesselFormats()
        {
            Directory.CreateDirectory(VesselsPath);

            lock (BackupLock)
            {
                foreach (var file in Directory.GetFiles(VesselsPath).Where(f => Path.GetExtension(f) == ".xml"))
                {
                    try
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out _))
                        {
                            var vesselAsCfgNode = XmlConverter.ConvertToConfigNode(FileHandler.ReadFileText(file));
                            FileHandler.WriteToFile(file.Replace(".xml", ".txt"), vesselAsCfgNode);
                        }

                        FileHandler.FileDelete(file);
                    }
                    catch (Exception e)
                    {
                        LunaLog.Error($"Error converting old vessel file {file}: {e}");
                    }
                }
            }
        }

        /// <summary>
        /// Actually performs the backup of the vessels to file
        /// </summary>
        public static void BackupVessels()
        {
            Directory.CreateDirectory(VesselsPath);

            lock (BackupLock)
            {
                var vesselsInCfgNode = CurrentVessels.ToArray();
                foreach (var vessel in vesselsInCfgNode)
                {
                    try
                    {
                        FileHandler.WriteToFile(Path.Combine(VesselsPath, $"{vessel.Key}{VesselFileFormat}"), vessel.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        LunaLog.Error($"Error backing up vessel {vessel.Key}: {e}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes one vessel to disk so live patches (orbit, IDENT, position fields) are reflected in the Vessels folder without waiting for <see cref="BackupVessels"/>.
        /// </summary>
        public static void PersistVesselToFile(Guid vesselId)
        {
            if (!CurrentVessels.TryGetValue(vesselId, out var vessel))
                return;

            Directory.CreateDirectory(VesselsPath);

            lock (BackupLock)
            {
                try
                {
                    FileHandler.WriteToFile(Path.Combine(VesselsPath, $"{vesselId}{VesselFileFormat}"), vessel.ToString());
                }
                catch (Exception e)
                {
                    LunaLog.Error($"Error persisting vessel {vesselId}: {e}");
                }
            }
        }
    }
}