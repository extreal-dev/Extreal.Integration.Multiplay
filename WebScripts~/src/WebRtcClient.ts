import { PeerClientProvider, PeerRole } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { waitUntil } from "@extreal-dev/extreal.integration.web.common";
import { IdMapper } from "./IdMapper";

type WebRtcConfig = {
    ngoServerClientId: number;
    isDebug: boolean;
};

type WebRtcCallbacks = {
    onConnected: (clientId: number) => void;
    onDataReceived: (clientId: number, payload: string) => void;
    onDisconnected: (clientId: number) => void;
};

class WebRtcClient {
    private readonly label: string = "multiplay";
    private readonly isDebug: boolean;
    private readonly webRtcConfig: WebRtcConfig;
    private readonly dcMap: Map<string, RTCDataChannel>;
    private readonly idMapper: IdMapper;
    private readonly disconnectedRemoteClients: Set<number>;
    private readonly getPeerClient: PeerClientProvider;
    private readonly callbacks: WebRtcCallbacks;
    private cancel: boolean;

    constructor(webRtcConfig: WebRtcConfig, getPeerClient: PeerClientProvider, callbacks: WebRtcCallbacks) {
        this.webRtcConfig = webRtcConfig;
        this.isDebug = webRtcConfig.isDebug;
        this.dcMap = new Map();
        this.idMapper = new IdMapper();
        this.disconnectedRemoteClients = new Set();
        this.getPeerClient = getPeerClient;
        this.callbacks = callbacks;
        this.cancel = false;
        this.getPeerClient().addPcCreateHook(this.createPc);
        this.getPeerClient().addPcCloseHook(this.closePc);
    }

    private createPc = (id: string, isOffer: boolean, pc: RTCPeerConnection) => {
        if (this.dcMap.has(id)) {
            return;
        }

        // In NGO, The client connects only to the host.
        // The host connects to all clients.
        if (this.getPeerClient().role === PeerRole.Client && id !== this.getPeerClient().hostId) {
            return;
        }

        if (isOffer) {
            const dc = pc.createDataChannel(this.label);
            this.handleDc(id, dc);
        } else {
            pc.addEventListener("datachannel", (event) => this.handleDc(id, event.channel));
        }
    };

    private handleDc = (id: string, dc: RTCDataChannel) => {
        if (dc.label !== this.label) {
            return;
        }

        if (this.isDebug) {
            console.log(`New DataChannel: id=${id} label=${dc.label}`);
        }

        this.dcMap.set(id, dc);
        this.idMapper.add(id);
        const clientId = this.idMapper.get(id) as number;

        // Host only
        if (this.getPeerClient().role === PeerRole.Host) {
            dc.addEventListener("open", () => {
                if (this.isDebug) {
                    console.log(`OnOpen: clientId=${clientId}`);
                }
                this.callbacks.onConnected(clientId);
            });
        }

        // Both Host and Client
        dc.addEventListener("message", (event) => {
            this.callbacks.onDataReceived(clientId, event.data);
        });
        dc.addEventListener("close", () => {
            if (this.isDebug) {
                console.log(`OnClose: clientId=${clientId}`);
            }

            if (this.getPeerClient().role === PeerRole.Host && this.disconnectedRemoteClients.delete(clientId)) {
                return;
            }
            this.callbacks.onDisconnected(clientId);
        });
    };

    private closePc = (id: string) => {
        const dc = this.dcMap.get(id);
        if (!dc) {
            return;
        }
        dc.close();
        this.dcMap.delete(id);
        this.idMapper.remove(id);
    };

    public connect = async () => {
        if (this.isDebug) {
            console.log(`Connect: role=${this.getPeerClient().role}`);
        }

        if (this.getPeerClient().role === PeerRole.Client) {
            const hostId = this.getPeerClient().hostId;
            if (hostId === null) {
                return;
            }
            this.cancel = false;
            await waitUntil(
                () => this.idMapper.has(hostId),
                () => this.cancel,
            );
            const clientId = this.getHostId("connect", hostId);
            if (!this.isHostIdNotFound(clientId)) {
                this.callbacks.onConnected(clientId);
            }
        }
    };

    private readonly hostIdNotFound = 0;
    private isHostIdNotFound = (hostId: number) => hostId === this.hostIdNotFound;

    private getHostId = (caller: string, hostId: string | null) => {
        let result;
        if (hostId !== null) {
            result = this.idMapper.has(hostId) ? (this.idMapper.get(hostId) as number) : this.hostIdNotFound;
        } else {
            result = this.hostIdNotFound;
        }
        if (this.isDebug) {
            console.log(`getHostId: caller=${caller} hostId=${hostId}`);
        }
        return result;
    };

    public send = (clientId: number, payload: string) => {
        const fixedClientId =
            clientId !== this.webRtcConfig.ngoServerClientId
                ? clientId
                : this.getHostId("send", this.getPeerClient().hostId);
        const id = this.idMapper.get(fixedClientId);
        if (!id) {
            if (this.isDebug) {
                console.log(`DoSend: id not found. clientId=${clientId}`);
            }
            return;
        }
        this.dcMap.get(id as string)?.send(payload);
    };

    public clear = () => {
        this.disconnectedRemoteClients.clear();
        [...this.dcMap.keys()].forEach((id) => this.closePc(id));
        this.dcMap.clear();
        this.idMapper.clear();
        this.cancel = true;
    };

    public disconnectRemoteClient = (clientId: number) => this.disconnectedRemoteClients.add(clientId);
}

export { WebRtcClient };
