import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { addAction, callback } from "@extreal-dev/extreal.integration.web.common";
import { WebRtcClient } from "./WebRtcClient";

class WebRtcAdapter {
    private webRtcClient: WebRtcClient | undefined;

    public adapt = (getPeerClient: PeerClientProvider) => {
        addAction(this.withPrefix("WebGLWebRtcClient"), (jsonConfig) => {
            this.webRtcClient = new WebRtcClient(JSON.parse(jsonConfig), getPeerClient, {
                onConnected: (clientId) => callback(this.withPrefix("HandleOnConnected"), clientId.toString()),
                onDataReceived: (clientId, payload) =>
                    callback(this.withPrefix("HandleOnDataReceived"), clientId.toString(), payload),
                onDisconnected: (clientId) => callback(this.withPrefix("HandleOnDisconnected"), clientId.toString()),
            });
        });

        addAction(this.withPrefix("DoConnectAsync"), () => this.getWebRtcClient().connect());

        addAction(this.withPrefix("DoSend"), (clientId, payload) =>
            this.getWebRtcClient().send(Number(clientId), payload),
        );

        addAction(this.withPrefix("DoClear"), () => this.getWebRtcClient().clear());

        addAction(this.withPrefix("DisconnectRemoteClient"), (clientId) =>
            this.getWebRtcClient().disconnectRemoteClient(Number(clientId)),
        );
    };

    private withPrefix = (name: string) => `WebGLWebRtcClient#${name}`;

    private getWebRtcClient = () => {
        if (!this.webRtcClient) {
            throw new Error("Call the WebGLWebRtcClient constructor first in Unity.");
        }
        return this.webRtcClient;
    };
}

export { WebRtcAdapter };
