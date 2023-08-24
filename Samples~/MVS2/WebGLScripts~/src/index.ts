import { PeerAdapter } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { WebRtcAdapter } from "@extreal-dev/extreal.integration.multiplay.ngo.webrtc";

const peerAdapter = new PeerAdapter();
peerAdapter.adapt();

const webRtcAdapterter = new WebRtcAdapter();
webRtcAdapterter.adapt(peerAdapter.getPeerClient);
