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

        public MessageName ReceivedMessageName { get; private set; }
        public string ReceivedMessageText { get; private set; }

        public MessageName SendMessageName { get; private set; }
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
            ReceivedMessageName = MessageName.NONE;
            ReceivedMessageText = string.Empty;
            SendMessageName = MessageName.NONE;
            SendMessageText = string.Empty;
        }

        public void ReceivedSpawnPlayer(ulong clientId, FastBufferReader reader)
        {
            ReceivedClientId = clientId;
            ReceivedMessageName = MessageName.SPAWN_PLAYER_TO_SERVER;
            ReceivedInternal(reader);
        }

        public void ReceivedHelloWorld(ulong clientId, FastBufferReader reader)
        {
            ReceivedClientId = clientId;
            ReceivedMessageName = MessageName.HELLO_WORLD_TO_SERVER;
            ReceivedInternal(reader);
        }

        private void ReceivedInternal(FastBufferReader reader)
        {
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            OnMessageReceived?.Invoke();
        }

        public bool SendHelloWorldToClients(List<ulong> clientIds)
        {
            SendClientIds = clientIds;
            SendMessageName = MessageName.HELLO_WORLD_TO_CLIENT;
            SendMessageText = "Hello World";
            return SendInternal(SendType.ToClient);
        }

        public bool SendHelloWorldToAllClients()
        {
            SendMessageName = MessageName.HELLO_WORLD_TO_ALL_CLIENTS;
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
                    return ngoServer.SendMessageToClients(SendClientIds, SendMessageName.ToString(), messageStream);
                }
                case SendType.ToAllClients:
                {
                    ngoServer.SendMessageToAllClients(SendMessageName.ToString(), messageStream);
                    return true;
                }
                default:
                {
                    throw new Exception("Unexpected Case");
                }
            }
        }

        private void OnServerStartedEventHandler()
        {
            ngoServer.RegisterMessageHandler(MessageName.SPAWN_PLAYER_TO_SERVER.ToString(), ReceivedSpawnPlayer);
            ngoServer.RegisterMessageHandler(MessageName.HELLO_WORLD_TO_SERVER.ToString(), ReceivedHelloWorld);
        }
        private void OnServerStoppingEventHandler()
        {
            ngoServer.UnregisterMessageHandler(MessageName.SPAWN_PLAYER_TO_SERVER.ToString());
            ngoServer.UnregisterMessageHandler(MessageName.HELLO_WORLD_TO_SERVER.ToString());
        }

        private enum SendType
        {
            ToClient,
            ToAllClients,
        }
    }
}
