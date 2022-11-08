using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ServerMessagingHub : IDisposable
    {
        public event Action OnMessageReceived;

        public List<ulong> SendClientIds { get; private set; } = new List<ulong>();
        public ulong ReceivedClientId { get; private set; }

        public string ReceivedMessageName { get; private set; }
        public string ReceivedMessageText { get; private set; }

        public string SendMessageName { get; private set; }
        public string SendMessageText { get; private set; }

        private readonly INgoServer ngoServer;

        public ServerMessagingHub(INgoServer ngoServer)
        {
            this.ngoServer = ngoServer;
            ngoServer.OnServerStarted += OnServerStartedEventHandler;
            ngoServer.OnServerStopping += OnServerStoppingEventHandler;
        }

        public void Dispose()
        {
            ngoServer.OnServerStarted -= OnServerStartedEventHandler;
            ngoServer.OnServerStopping -= OnServerStoppingEventHandler;
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            SendClientIds.Clear();
            ReceivedClientId = 0;
            ReceivedMessageName = string.Empty;
            ReceivedMessageText = string.Empty;
            SendMessageName = string.Empty;
            SendMessageText = string.Empty;
        }

        public void ReceivedHelloWorld(ulong clientId, FastBufferReader reader)
        {
            ReceivedClientId = clientId;
            ReceivedMessageName = MessageNameConst.HELLO_WORLD_TO_SERVER.ToString();
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            OnMessageReceived?.Invoke();
        }

        public bool SendHelloWorldToClients(List<ulong> clientIds)
        {
            SendClientIds = clientIds;
            SendMessageName = MessageNameConst.HELLO_WORLD_TO_CLIENT.ToString();
            SendMessageText = "Hello World";
            return SendInternal(SendType.ToClient);
        }

        public bool SendHelloWorldToAllClients()
        {
            SendMessageName = MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS.ToString();
            SendMessageText = "Hello World";
            return SendInternal(SendType.ToAllClients);
        }

        private bool SendInternal(SendType sendType)
        {
            using var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            messageStream.WriteValueSafe(SendMessageText);

            switch (sendType)
            {
                case SendType.ToClient:
                {
                    return ngoServer.SendMessageToClients(SendClientIds, SendMessageName, messageStream);
                }
                case SendType.ToAllClients:
                {
                    ngoServer.SendMessageToAllClients(SendMessageName, messageStream);
                    return true;
                }
                default:
                {
                    throw new Exception("Unexpected Case");
                }
            }
        }

        private void OnServerStartedEventHandler()
            => ngoServer.RegisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_SERVER.ToString(), ReceivedHelloWorld);

        private void OnServerStoppingEventHandler()
            => ngoServer.UnregisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_SERVER.ToString());

        private enum SendType
        {
            ToClient,
            ToAllClients,
        }
    }
}
