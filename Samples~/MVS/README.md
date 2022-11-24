# MVP (Minimum Viable Sample)

## Installation

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
- Install this sample from Package Manager.
- Add the asset to Addressables.
  - Name: PlayerPrefab
  - Path: MVS/Common/NetworkPlayer

## How to play

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

