using System;
using System.Collections.Generic;
using Extreal.Core.Common.System;
using UniRx;
using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ServerMessagingManager : DisposableBase
    {
        public IObservable<Unit> OnMessageReceived => onMessageReceived;
        private readonly Subject<Unit> onMessageReceived = new Subject<Unit>();

        public List<ulong> SendClientIds { get; private set; } = new List<ulong>();
        public ulong ReceivedClientId { get; private set; }

        public MessageName ReceivedMessageName { get; private set; }
        public string ReceivedMessageText { get; private set; }

        public MessageName SendMessageName { get; private set; }
        public string SendMessageText { get; private set; }

        private readonly NgoServer ngoServer;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public ServerMessagingManager(NgoServer ngoServer)
        {
            this.ngoServer = ngoServer;

            _ = ngoServer.OnServerStarted
                .Subscribe(_ =>
                {
                    ngoServer.RegisterMessageHandler(MessageName.RestartToServer.ToString(), ReceivedRestartServer);
                    ngoServer.RegisterMessageHandler(MessageName.HelloWorldToServer.ToString(), ReceivedHelloWorld);
                })
                .AddTo(disposables);

            _ = ngoServer.OnServerStopping
                .Subscribe(_ =>
                {
                    ngoServer.UnregisterMessageHandler(MessageName.RestartToServer.ToString());
                    ngoServer.UnregisterMessageHandler(MessageName.HelloWorldToServer.ToString());
                })
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
        {
            onMessageReceived.Dispose();
            disposables.Dispose();
        }

        public void ReceivedRestartServer(ulong clientId, FastBufferReader reader)
        {
            ReceivedClientId = clientId;
            ReceivedMessageName = MessageName.RestartToServer;
            ReceivedInternal(reader);
        }

        public void ReceivedHelloWorld(ulong clientId, FastBufferReader reader)
        {
            ReceivedClientId = clientId;
            ReceivedMessageName = MessageName.HelloWorldToServer;
            ReceivedInternal(reader);
        }

        private void ReceivedInternal(FastBufferReader reader)
        {
            reader.ReadValueSafe(out string message);
            ReceivedMessageText = message;

            onMessageReceived.OnNext(Unit.Default);
        }

        public void SendHelloWorldToClients(List<ulong> clientIds)
        {
            SendClientIds = clientIds;
            SendMessageName = MessageName.HelloWorldToClient;
            SendMessageText = "Hello World";
            SendInternal(SendType.ToClient);
        }

        public void SendHelloWorldToAllClients()
        {
            SendMessageName = MessageName.HelloWorldToAllClients;
            SendMessageText = "Hello World";
            SendInternal(SendType.ToAllClients);
        }

        private void SendInternal(SendType sendType)
        {
            using var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            messageStream.WriteValueSafe(SendMessageText);

            switch (sendType)
            {
                case SendType.ToClient:
                {
                    ngoServer.SendMessageToClients(SendClientIds, SendMessageName.ToString(), messageStream);
                    break;
                }
                case SendType.ToAllClients:
                {
                    ngoServer.SendMessageToAllClients(SendMessageName.ToString(), messageStream);
                    break;
                }
                default:
                {
                    throw new Exception("Unexpected Case");
                }
            }
        }

        private enum SendType
        {
            ToClient,
            ToAllClients,
        }
    }
}
