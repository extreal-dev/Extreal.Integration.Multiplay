# Extreal.Integration.Multiplay.NGO

## How to test

### For Extreal.Integration.Multiplay.NGO

- Open one Unity editor using ParrelSync.
- We will call the originally opened editor `MAIN` and the newly opened editor `SUB`.
- MAIN> Open `Build Settings` and put `Main` scene for testing into `Scenes In Build`.
- SUB> Play the `ServerMain` scene.
- MAIN> Run `Extreal.Integration.Multiplay.NGO.Test.dll`.
- SUB> Stop playing the `ServerMain` scene when `NgoClientTest` has finished running.
- SUB> Run `Extreal.Integration.Multiplay.NGO.Test.Sub.dll`.
- SUB> Run the `UNetTransportServer` scene in `SUB` after all tests in `SUB`.
- MAIN> All tests are completed.

### For Extreal.Integration.Multiplay.NGO.WebRTC

- Enter the following command in the `WebScripts~` directory.
   ```bash
   yarn
   yarn dev
   ```
- Import the sample MVN and MVS2 from Package Manager.
  - MVN is also required as some materials depend on MVN2 to MVN.
- Enter the following command in the `MVS2/WebScripts` directory.
   ```bash
   yarn
   yarn dev
   ```
   The JavaScript code will be built and output to `/Assets/WebTemplates/Dev`.
- Open `Build Settings` and change the platform to `WebGL`.
- Select `Dev` from `Player Settings > Resolution and Presentation > WebGL Template`.
- Add all scenes in MVS2 to `Scenes In Build`.
- See [README](https://github.com/extreal-dev/Extreal.Integration.P2P.WebRTC/SignalingServer~/README.md) to start a signaling server.
- Play
  - Native
    - Open multiple Unity editors using ParrelSync.
    - Run
      - Scene: MVS2/App/App
  - WebGL
    - Run from `Build And Run`.

## Test cases for manual testing

### Host

- Group selection screen
  - Ability to create a group by specifying a name (host start)
- VirtualSpace
  - Client can join a group (client join)
  - Clients can leave the group (client exit)
  - Ability to return to the group selection screen (host stop)
  - Ability to reject clients if the number of clients exceeds capacity (reject connection)

### Client

- Group selection screen
  - Ability to join a group (join host)
- Virtual space
  - Another client can join a group (other client join)
  - Ability to return to the group selection screen while moving the player (leave host)
  - Error notification if the number of clients exceeds capacity (connection rejection)

## How to play the sample

### Installation

- Install packages that the sample depends on from Package Manager.
  - Unity.Collections
  - Unity.InputSystem
  - Cinemachine
  - Unity.Netcode.Runtime
  - Unity.Netcode.Components
  - UniRx
  - UniTask
  - VContainer
  - Extreal.Core.Logging
  - Extreal.Core.StageNavigation
  - Extreal.Core.Common
- Install this sample from Package Manager.
- Add the asset to Addressables.
  - Name: PlayerPrefab
  - Path: MVS/Common/NetworkPlayer

### How to play

- Add scenes to Build Settings.
  - MVS/App/App
  - MVS/MultiplayControl/MultiplayControl
  - MVS/PlayerControl/PlayerControl
  - MVS/VirtualSpace/VirtualSpace
- Open multiple Unity editors using ParrelSync.
- Start the server.
  - Scene: MVS/MultiplayServer/MultiplayServer
- Start the client.
  - Scene: MVS/App/App
