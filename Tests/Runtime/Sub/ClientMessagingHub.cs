using System;
using UniRx;
using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ClientMessagingHub : IDisposable
    {
        public IObservable<Unit> OnMessageReceived => onMessageReceived;
        private readonly Subject<Unit> onMessageReceived = new Subject<Unit>();

        public MessageName ReceivedMessageName { get; private set; }
        public string ReceivedMessageText { get; private set; }

        public MessageName SendMessageName { get; private set; }
        public string SendMessageText { get; private set; }

        private readonly INgoClient ngoClient;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public ClientMessagingHub(INgoClient ngoClient)
        {
            this.ngoClient = ngoClient;

            _ = ngoClient.OnConnected
                .Subscribe(_ =>
                {
                    ngoClient.RegisterMessageHandler(MessageName.HELLO_WORLD_TO_CLIENT.ToString(), ReceivedHelloWorldToClient);
                    ngoClient.RegisterMessageHandler(MessageName.HELLO_WORLD_TO_ALL_CLIENTS.ToString(), ReceivedHelloWorldToAllClients);
                    ngoClient.RegisterMessageHandler(MessageName.HELLO_WORLD_TO_ALL_CLIENTS_EXCEPT.ToString(), ReceivedHelloWorldToAllClientsExcept);
                })
                .AddTo(disposables);

            _ = ngoClient.OnDisconnecting
                .Subscribe(_ =>
                {
                    ngoClient.UnregisterMessageHandler(MessageName.HELLO_WORLD_TO_CLIENT.ToString());
                    ngoClient.UnregisterMessageHandler(MessageName.HELLO_WORLD_TO_ALL_CLIENTS.ToString());
                    ngoClient.UnregisterMessageHandler(MessageName.HELLO_WORLD_TO_ALL_CLIENTS_EXCEPT.ToString());
                })
                .AddTo(disposables);
        }

        public void Dispose()
        {
            onMessageReceived.Dispose();
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            ReceivedMessageName = MessageName.NONE;
            ReceivedMessageText = string.Empty;
            SendMessageName = MessageName.NONE;
            SendMessageText = string.Empty;
        }

        public void SendSpawnPlayer()
        {
            SendMessageName = MessageName.SPAWN_PLAYER_TO_SERVER;
            SendMessageText = "Spawn Player";
            SendInternal();
        }

        public void SendHelloWorldToServer()
        {
            SendMessageName = MessageName.HELLO_WORLD_TO_SERVER;
            SendMessageText = "Hello World";
            SendInternal();
        }

        private void SendInternal()
        {
            using var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            messageStream.WriteValueSafe(SendMessageText);
            ngoClient.SendMessage(SendMessageName.ToString(), messageStream);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        private void ReceivedHelloWorldToClient(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageName.HELLO_WORLD_TO_CLIENT;
            ReceivedInternal(reader);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        private void ReceivedHelloWorldToAllClients(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageName.HELLO_WORLD_TO_ALL_CLIENTS;
            ReceivedInternal(reader);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        private void ReceivedHelloWorldToAllClientsExcept(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageName.HELLO_WORLD_TO_ALL_CLIENTS_EXCEPT;
            ReceivedInternal(reader);
        }

        private void ReceivedInternal(FastBufferReader reader)
        {
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            onMessageReceived.OnNext(Unit.Default);
        }
    }
}
