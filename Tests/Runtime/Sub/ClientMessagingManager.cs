using System;
using Extreal.Core.Common.System;
using UniRx;
using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ClientMessagingManager : DisposableBase
    {
        public IObservable<Unit> OnMessageReceived => onMessageReceived;
        private readonly Subject<Unit> onMessageReceived = new Subject<Unit>();

        public MessageName ReceivedMessageName { get; private set; }
        public string ReceivedMessageText { get; private set; }

        public MessageName SendMessageName { get; private set; }
        public string SendMessageText { get; private set; }

        private readonly NgoClient ngoClient;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public ClientMessagingManager(NgoClient ngoClient)
        {
            this.ngoClient = ngoClient;

            _ = ngoClient.OnConnected
                .Subscribe(_ =>
                {
                    ngoClient.RegisterMessageHandler(MessageName.HelloWorldToClient.ToString(), ReceivedHelloWorldToClient);
                    ngoClient.RegisterMessageHandler(MessageName.HelloWorldToAllClients.ToString(), ReceivedHelloWorldToAllClients);
                    ngoClient.RegisterMessageHandler(MessageName.HelloWorldToAllClientsExcept.ToString(), ReceivedHelloWorldToAllClientsExcept);
                })
                .AddTo(disposables);

            _ = ngoClient.OnDisconnecting
                .Subscribe(_ =>
                {
                    ngoClient.UnregisterMessageHandler(MessageName.HelloWorldToClient.ToString());
                    ngoClient.UnregisterMessageHandler(MessageName.HelloWorldToAllClients.ToString());
                    ngoClient.UnregisterMessageHandler(MessageName.HelloWorldToAllClientsExcept.ToString());
                })
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
        {
            onMessageReceived.Dispose();
            disposables.Dispose();
        }

        public void SendRestartServer()
        {
            SendMessageName = MessageName.RestartToServer;
            SendMessageText = "Restart Server";
            SendInternal();
        }

        public void SendHelloWorld()
        {
            SendMessageName = MessageName.HelloWorldToServer;
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
            ReceivedMessageName = MessageName.HelloWorldToClient;
            ReceivedInternal(reader);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        private void ReceivedHelloWorldToAllClients(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageName.HelloWorldToAllClients;
            ReceivedInternal(reader);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        private void ReceivedHelloWorldToAllClientsExcept(ulong serverId, FastBufferReader reader)
        {
            ReceivedMessageName = MessageName.HelloWorldToAllClientsExcept;
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
