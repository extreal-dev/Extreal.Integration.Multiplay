using System;
using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ClientMessagingHub : IDisposable
    {
        public event Action OnMessageReceived;

        public string ReceivedMessageName { get; private set; }
        public string ReceivedMessageText { get; private set; }

        public string SendMessageName { get; private set; }
        public string SendMessageText { get; private set; }

        private readonly INgoClient ngoClient;

        public ClientMessagingHub(INgoClient ngoClient)
        {
            this.ngoClient = ngoClient;
            ngoClient.OnConnected += OnConnectedEventHandler;
            ngoClient.OnDisconnecting += OnDisconnectingEventHandler;
        }

        public void Dispose()
        {
            ngoClient.OnConnected -= OnConnectedEventHandler;
            ngoClient.OnDisconnecting -= OnDisconnectingEventHandler;
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            ReceivedMessageName = string.Empty;
            ReceivedMessageText = string.Empty;
            SendMessageName = string.Empty;
            SendMessageText = string.Empty;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        public void ReceivedHelloWorldToClient(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageNameConst.HELLO_WORLD_TO_CLIENT.ToString();
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            OnMessageReceived?.Invoke();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        public void ReceivedHelloWorldToAllClients(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS.ToString();
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            OnMessageReceived?.Invoke();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        public void ReceivedHelloWorldToAllClientsExcept(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS_EXCEPT.ToString();
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            OnMessageReceived?.Invoke();
        }

        public void SendHelloWorldToServer()
        {
            SendMessageName = MessageNameConst.HELLO_WORLD_TO_SERVER.ToString();
            SendMessageText = "Hello World";
            SendInternal();
        }

        private void SendInternal()
        {
            using var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            messageStream.WriteValueSafe(SendMessageText);
            ngoClient.SendMessage(SendMessageName, messageStream);
        }

        private void OnConnectedEventHandler()
        {
            ngoClient.RegisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_CLIENT.ToString(), ReceivedHelloWorldToClient);
            ngoClient.RegisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS.ToString(), ReceivedHelloWorldToAllClients);
            ngoClient.RegisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS_EXCEPT.ToString(), ReceivedHelloWorldToAllClientsExcept);
        }

        private void OnDisconnectingEventHandler()
        {
            ngoClient.UnregisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_CLIENT.ToString());
            ngoClient.UnregisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS.ToString());
            ngoClient.UnregisterMessageHandler(MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS_EXCEPT.ToString());
        }
    }
}
