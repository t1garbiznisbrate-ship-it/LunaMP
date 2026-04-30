using Server.Context;
using Server.Events;
using Server.Log;
using Server.Settings.Structures;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.System
{
    public class BackupSystem
    {
        // Subscribe to the exit event so a backup is performed when closing the server.
        static BackupSystem() => ExitEvent.ServerClosing += RunBackup;

        private static readonly object LockObj = new object();

        public static async Task PerformBackupsAsync(CancellationToken token)
        {
            while (ServerContext.ServerRunning && !token.IsCancellationRequested)
            {
                if (ServerContext.PlayerCount > 0)
                {
                    RunBackup();
                }

                try
                {
                    await Task.Delay(IntervalSettings.SettingsStore.BackupIntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public static void RunBackup()
        {
            lock (LockObj)
            {
                LunaLog.Debug("Performing backups...");

                TryRunBackupStep("vessels", VesselStoreSystem.BackupVessels);
                TryRunBackupStep("subspaces", WarpSystem.BackupSubspaces);
                TryRunBackupStep("start time", TimeSystem.BackupStartTime);
                TryRunBackupStep("scenarios", ScenarioStoreSystem.BackupScenarios);

                LunaLog.Debug("Backups done");
            }
        }

        private static void TryRunBackupStep(string name, Action backupAction)
        {
            try
            {
                backupAction();
            }
            catch (Exception e)
            {
                LunaLog.Error($"Error backing up {name}: {e}");
            }
        }
    }
}