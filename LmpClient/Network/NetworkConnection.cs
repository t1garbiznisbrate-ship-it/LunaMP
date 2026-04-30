using Lidgren.Network;
using LmpClient.Base;
using LmpClient.ModuleStore.Patching;
using LmpClient.Systems.Network;
using LmpCommon;
using LmpCommon.Enums;
using LmpCommon.Message.Base;
using System;
using System.Net;
using System.Threading;
using UniLinq;

namespace LmpClient.Network
{
    public class NetworkConnection
    {
        private static readonly object DisconnectLock = new object();
        public static volatile bool ResetRequested;

        /// <summary>
        /// Disconnects the network system. You should kill threads ONLY from main thread
        /// </summary>
        /// <param name="reason">Reason</param>
        public static void Disconnect(string reason = "unknown")
        {
            lock (DisconnectLock)
            {
                if (MainSystem.NetworkState <= ClientState.Disconnected)
                    return;

                // DO NOT set networkstate as disconnected as we are in another thread!
                MainSystem.NetworkState = ClientState.DisconnectRequested;

                LunaLog.Log($"[LMP]: Disconnected, reason: {reason}");

                if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                {
                    MainSystem.Singleton.ForceQuit = true;
                }
                else
                {
                    // User is in flight so just display a message but don't force them to main menu.
                    NetworkSystem.DisplayDisconnectMessage = true;
                }

                MainSystem.Singleton.Status = $"Disconnected: {reason}";

                try
                {
                    NetworkMain.ClientConnection?.Disconnect(reason);
                    NetworkMain.ClientConnection?.Shutdown(reason);
                }
                catch (Exception e)
                {
                    LunaLog.LogError($"[LMP]: Error while disconnecting: {e}");
                }

                NetworkMain.ResetConnectionStaticsAndQueues();
            }
        }

        public static void ConnectToServer(string hostname, int port, string password)
        {
            var endpoints = LunaNetUtils.CreateAddressFromString(hostname)
                .Select(addr => new IPEndPoint(addr, port))
                .ToArray();

            if (endpoints.Length == 0)
            {
                MainSystem.Singleton.Status = "Hostname resolution failed, check for typos";
                LunaLog.LogError("[LMP]: Hostname resolution failed, check for typos");
                Disconnect("Hostname resolution failed");
                return;
            }

            ConnectToServer(endpoints, password);
        }

        public static void ConnectToServer(IPEndPoint[] endpoints, string password)
        {
            if (MainSystem.NetworkState > ClientState.Disconnected || endpoints == null || endpoints.Length == 0)
                return;

            password = password ?? string.Empty;
            MainSystem.NetworkState = ClientState.Connecting;

            SystemBase.TaskFactory.StartNew(() =>
            {
                while (!PartModuleRunner.Ready && !ResetRequested)
                {
                    MainSystem.Singleton.Status = $"Patching part modules (runs on every restart). {PartModuleRunner.GetPercentage()}%";
                    Thread.Sleep(50);
                }

                if (ResetRequested)
                    return;

                foreach (var endpoint in endpoints)
                {
                    if (ResetRequested)
                        break;

                    if (endpoint == null)
                        continue;

                    MainSystem.Singleton.Status = $"Connecting to {endpoint.Address}:{endpoint.Port}";
                    LunaLog.Log($"[LMP]: Connecting to {endpoint.Address} port {endpoint.Port}");

                    try
                    {
                        var client = NetworkMain.ClientConnection;
                        if (client == null)
                        {
                            Disconnect("Client connection is not initialized");
                            return;
                        }

                        if (client.Status == NetPeerStatus.NotRunning)
                        {
                            LunaLog.Log("[LMP]: Starting client");
                            client.Start();
                        }

                        while (client.Status != NetPeerStatus.Running && !ResetRequested)
                        {
                            Thread.Sleep(50);
                        }

                        if (ResetRequested)
                            break;

                        var outMsg = client.CreateMessage(password.GetByteCount());
                        outMsg.Write(password);

                        var conn = client.Connect(endpoint, outMsg);
                        if (conn == null)
                        {
                            // Lidgren says we're already connected, that's not possible.
                            LunaLog.LogError("[LMP]: Invalid connection state, connected without connection");
                            client.Disconnect("Invalid state");
                            break;
                        }

                        client.FlushSendQueue();

                        while ((conn.Status == NetConnectionStatus.InitiatedConnect || conn.Status == NetConnectionStatus.None) && !ResetRequested)
                        {
                            Thread.Sleep(50);
                        }

                        if (ResetRequested)
                            break;

                        if (client.ConnectionStatus == NetConnectionStatus.Connected)
                        {
                            LunaLog.Log($"[LMP]: Connected to {endpoint.Address}:{endpoint.Port}");
                            MainSystem.NetworkState = ClientState.Connected;
                            break;
                        }

                        LunaLog.Log($"[LMP]: Initial connection timeout to {endpoint.Address}:{endpoint.Port}");
                        client.Disconnect("Initial connection timeout");
                    }
                    catch (Exception e)
                    {
                        NetworkMain.HandleDisconnectException(e);
                    }
                }

                if (!ResetRequested && MainSystem.NetworkState < ClientState.Connected)
                {
                    Disconnect(MainSystem.NetworkState == ClientState.Connecting ? "Initial connection timeout" : "Cancelled connection");
                }
            });
        }
    }
}