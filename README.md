# Extreal.Integration.Multiplay.NGO

## How to test

- Open one Unity editor using ParrelSync.
- We will call the originally opened editor `MAIN` and the newly opened editor `SUB`.
- MAIN> Open `Build Settings` and put `Main` scene for testing into `Scenes In Build`.
- SUB> Play the `ServerMain` scene.
- MAIN> Run `Extreal.Integration.Multiplay.NGO.Test.dll`.
- SUB> Stop playing the `ServerMain` scene when `NgoClientTest` has finished running.
- SUB> Run `Extreal.Integration.Multiplay.NGO.Test.Sub.dll`.
- SUB> Run the `UNetTransportServer` scene in `SUB` after all tests in `SUB`.
- MAIN> All tests are completed.

## How to play the sample

### Installation

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
