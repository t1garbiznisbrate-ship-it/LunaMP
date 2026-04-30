using Lidgren.Network;
using LmpCommon.Enums;
using LmpCommon.Message.Data.PlayerConnection;
using LmpCommon.Message.Server;
using Server.Context;
using Server.Log;
using Server.Plugin;
using Server.Server;
using Server.System;
using System;

namespace Server.Client
{
    public class ClientConnectionHandler
    {
        public static void ConnectClient(NetConnection newClientConnection)
        {
            if (newClientConnection == null)
                return;

            var newClientObject = new ClientStructure(newClientConnection);

            LmpPluginHandler.FireOnClientConnect(newClientObject);

            ServerContext.Clients.TryAdd(newClientObject.Endpoint, newClientObject);
            LunaLog.Debug($"Online Players: {ServerContext.PlayerCount}, connected: {ServerContext.Clients.Count}");
        }

        public static void DisconnectClient(ClientStructure client, string reason = "")
        {
            if (client == null)
                return;

            if (!string.IsNullOrEmpty(reason))
                LunaLog.Debug($"{client.PlayerName} sent Connection end message, reason: {reason}");

            if (ServerContext.Clients.TryRemove(client.Endpoint, out var removedClient))
            {
                client = removedClient;
                LunaLog.Debug($"Online Players: {ServerContext.PlayerCount}, connected: {ServerContext.Clients.Count}");
            }

            if (client.ConnectionStatus != ConnectionStatus.Disconnected)
            {
                client.ConnectionStatus = ConnectionStatus.Disconnected;
                LmpPluginHandler.FireOnClientDisconnect(client);

                if (client.Authenticated)
                {
                    var msgData = ServerContext.ServerMessageFactory.CreateNewMessageData<PlayerConnectionLeaveMsgData>();
                    msgData.PlayerName = client.PlayerName ?? string.Empty;

                    MessageQueuer.RelayMessage<PlayerConnectionSrvMsg>(client, msgData);
                    LockSystem.ReleasePlayerLocks(client);
                    WarpSystem.RemoveSubspace(client.Subspace);
                }

                try
                {
                    client.Connection?.Disconnect(reason);
                }
                catch (Exception e)
                {
                    LunaLog.Error($"Error closing client Connection: {e.Message}");
                }
            }

            while (client.SendMessageQueue.TryDequeue(out var message))
            {
                message?.Recycle();
            }

            // As this is the last client connected to the server, run a safety backup once they disconnect.
            if (ServerContext.Clients.Count == 0)
            {
                BackupSystem.RunBackup();
            }
        }
    }
}