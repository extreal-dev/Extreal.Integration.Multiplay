#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using Unity.Netcode;
using Unity.WebRTC;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Class that handles WebRTC client for native application.
    /// </summary>
    public class NativeWebRtcClient : WebRtcClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeWebRtcClient));
        private static readonly string Label = "multiplay";
        private static readonly string ConnectionApprovalRejectedMessage = "Connection approval rejected";

        private readonly Dictionary<string, RTCDataChannel> dcDict;
        private readonly NativeIdMapper idMapper;
        private readonly HashSet<ulong> disconnectedRemoteClients;
        private readonly HashSet<ulong> connectedClients;
        private readonly NativePeerClient peerClient;
        private CancellationTokenSource cancellation;

        /// <summary>
        /// Creates NativeWebRtcClient with peerClient.
        /// </summary>
        /// <param name="peerClient">Peer client.</param>
        public NativeWebRtcClient(NativePeerClient peerClient)
        {
            dcDict = new Dictionary<string, RTCDataChannel>();
            idMapper = new NativeIdMapper();
            disconnectedRemoteClients = new HashSet<ulong>();
            connectedClients = new HashSet<ulong>();
            this.peerClient = peerClient;
            cancellation = new CancellationTokenSource();

            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (dcDict.ContainsKey(id))
            {
                // Not covered by testing due to defensive implementation
                return;
            }

            // In NGO, The client connects only to the host.
            // The host connects to all clients.
            if (peerClient.Role == PeerRole.Client && id != peerClient.HostId)
            {
                return;
            }

            if (isOffer)
            {
                var dc = pc.CreateDataChannel(Label);
                HandleDc(id, dc);
            }
            else
            {
                pc.OnDataChannel += (dc) => HandleDc(id, dc);
            }
        }

        private void HandleDc(string id, RTCDataChannel dc)
        {
            if (dc.Label != Label)
            {
                // Not covered by testing but passed by peer review
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"New DataChannel: id={id} label={dc.Label}");
            }

            dcDict.Add(id, dc);
            idMapper.Add(id);
            var clientId = idMapper.Get(id);

            // Host only
            if (peerClient.Role == PeerRole.Host)
            {
                dc.OnOpen = () =>
                {
                    if (Logger.IsDebug())
                    {
                        Logger.LogDebug($"{nameof(dc.OnOpen)}: clientId={clientId}");
                    }
                    FireOnConnected(clientId);
                };
            }

            // Both Host and Client
            dc.OnMessage = message =>
            {
                var messageStr = Encoding.ASCII.GetString(message);
                if (messageStr == ConnectionApprovalRejectedMessage)
                {
                    FireOnDisconnected(clientId);
                }
                else
                {
                    connectedClients.Add(clientId);
                    FireOnDataReceived(clientId, Encoding.ASCII.GetString(message));
                }
            };
            dc.OnClose = () =>
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"{nameof(dc.OnClose)}: clientId={clientId}");
                }

                if (peerClient.Role == PeerRole.Host
                    && (disconnectedRemoteClients.Remove(clientId) || !connectedClients.Remove(clientId)))
                {
                    return;
                }
                FireOnDisconnected(clientId);
            };
        }

        private void ClosePc(string id)
        {
            if (!dcDict.TryGetValue(id, out var dc))
            {
                return;
            }
            dc.Close();
            dcDict.Remove(id);
            idMapper.Remove(id);
        }

        /// <inheritdoc/>
        protected override async UniTask DoConnectAsync()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{nameof(Connect)}: role={peerClient.Role}");
            }

            if (peerClient.Role == PeerRole.Client)
            {
                var hostId = peerClient.HostId;
                await UniTask.WaitUntil(() => idMapper.Has(hostId), cancellationToken: cancellation.Token);
                var clientId = GetHostId(hostId);
                if (!IsHostIdNotFound(clientId))
                {
                    FireOnConnected(clientId);
                }
            }
        }

        private const ulong HostIdNotFound = 0;
        private static bool IsHostIdNotFound(ulong hostId) => hostId == HostIdNotFound;

        [SuppressMessage("Usage", "CC0014")]
        private ulong GetHostId(string hostId)
            => hostId is not null && idMapper.Has(hostId) ? idMapper.Get(hostId) : HostIdNotFound;

        /// <inheritdoc/>
        protected override void DoSend(ulong clientId, string payload)
        {
            var fixedClientId =
                clientId != NetworkManager.ServerClientId
                    ? clientId
                    : GetHostId(peerClient.HostId);
            if (!idMapper.TryGet(fixedClientId, out var id))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"DoSend: id not found. clientId={clientId}");
                }
                return;
            }
            dcDict[id].Send(payload);
        }

        /// <inheritdoc/>
        protected override void DoClear()
        {
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = new CancellationTokenSource();
            disconnectedRemoteClients.Clear();
            connectedClients.Clear();
            dcDict.Keys.ToList().ForEach(ClosePc);
            dcDict.Clear();
            idMapper.Clear();
        }

        /// <inheritdoc/>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            disconnectedRemoteClients.Add(clientId);
            DoSend(clientId, ConnectionApprovalRejectedMessage);
        }

        /// <inheritdoc/>
        protected override void ReleaseManagedResources() {
            cancellation?.Cancel();
            cancellation?.Dispose();
        }
    }
}
#endif
