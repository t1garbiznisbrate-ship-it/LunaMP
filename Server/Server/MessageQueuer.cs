using LmpCommon.Message.Interface;
using Server.Client;
using Server.Context;
using Server.Log;
using System;
using System.Linq;
using System.Reflection;

namespace Server.Server
{
    public class MessageQueuer
    {
        /// <summary>
        /// Sends a message to all the clients except the one given as parameter that are in the same subspace.
        /// </summary>
        public static void RelayMessageToSubspace<T>(ClientStructure exceptClient, IMessageData data) where T : class, IServerMessageBase
        {
            if (data == null || exceptClient == null)
                return;

            RelayMessageToSubspace<T>(exceptClient, data, exceptClient.Subspace);
        }

        /// <summary>
        /// Sends a message to all the clients in the given subspace.
        /// </summary>
        public static void SendMessageToSubspace<T>(IMessageData data, int subspace) where T : class, IServerMessageBase
        {
            if (data == null)
                return;

            foreach (var otherClient in ServerContext.Clients.Values.Where(c => c.Subspace == subspace))
                SendToClient(otherClient, GenerateMessage<T>(data));
        }

        /// <summary>
        /// Sends a message to all the clients except the one given as parameter that are in the subspace given as parameter.
        /// </summary>
        public static void RelayMessageToSubspace<T>(ClientStructure exceptClient, IMessageData data, int subspace) where T : class, IServerMessageBase
        {
            if (data == null)
                return;

            foreach (var otherClient in ServerContext.Clients.Values.Where(c => !Equals(c, exceptClient) && c.Subspace == subspace))
                SendToClient(otherClient, GenerateMessage<T>(data));
        }

        /// <summary>
        /// Sends a message to all the clients except the one given as parameter.
        /// </summary>
        public static void RelayMessage<T>(ClientStructure exceptClient, IMessageData data) where T : class, IServerMessageBase
        {
            if (data == null)
                return;

            foreach (var otherClient in ServerContext.Clients.Values.Where(c => !Equals(c, exceptClient)))
                SendToClient(otherClient, GenerateMessage<T>(data));
        }

        /// <summary>
        /// Sends a message to all the clients.
        /// </summary>
        public static void SendToAllClients<T>(IMessageData data) where T : class, IServerMessageBase
        {
            if (data == null)
                return;

            foreach (var otherClient in ServerContext.Clients.Values)
                SendToClient(otherClient, GenerateMessage<T>(data));
        }

        /// <summary>
        /// Sends a message to the given client.
        /// </summary>
        public static void SendToClient<T>(ClientStructure client, IMessageData data) where T : class, IServerMessageBase
        {
            if (data == null)
                return;

            SendToClient(client, GenerateMessage<T>(data));
        }

        /// <summary>
        /// Disconnects the given client.
        /// </summary>
        public static void SendConnectionEnd(ClientStructure client, string reason)
        {
            ClientConnectionHandler.DisconnectClient(client, reason);
        }

        /// <summary>
        /// Disconnect all clients.
        /// </summary>
        public static void SendConnectionEndToAll(string reason)
        {
            foreach (var client in ClientRetriever.GetAuthenticatedClients())
                SendConnectionEnd(client, reason);
        }

        #region Private

        private static void SendToClient(ClientStructure client, IServerMessageBase msg)
        {
            if (client == null || msg?.Data == null)
            {
                msg?.Recycle();
                return;
            }

            client.SendMessageQueue.Enqueue(msg);
        }

        private static T GenerateMessage<T>(IMessageData data) where T : class, IServerMessageBase
        {
            var dataCopy = CloneMessageData(data);
            if (dataCopy == null)
                return null;

            return ServerContext.ServerMessageFactory.CreateNew<T>(dataCopy);
        }

        private static IMessageData CloneMessageData(IMessageData data)
        {
            try
            {
                return DeepCloneObject(data) as IMessageData;
            }
            catch (Exception e)
            {
                LunaLog.Error($"Error cloning message data {data?.GetType().Name}: {e}");
                return null;
            }
        }

        private static object DeepCloneObject(object source)
        {
            if (source == null)
                return null;

            var type = source.GetType();

            if (type.IsValueType || type.IsEnum || type == typeof(string))
                return source;

            if (type == typeof(byte[]))
                return ((byte[])source).ToArray();

            if (type.IsArray)
            {
                var sourceArray = (Array)source;
                var elementType = type.GetElementType();
                var clonedArray = Array.CreateInstance(elementType, sourceArray.Length);

                for (var i = 0; i < sourceArray.Length; i++)
                    clonedArray.SetValue(DeepCloneObject(sourceArray.GetValue(i)), i);

                return clonedArray;
            }

            var clone = Activator.CreateInstance(type, true);

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                field.SetValue(clone, DeepCloneObject(field.GetValue(source)));
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                    continue;

                property.SetValue(clone, DeepCloneObject(property.GetValue(source, null)), null);
            }

            return clone;
        }

        #endregion
    }
}