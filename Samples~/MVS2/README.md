# MVP (Minimum Viable Sample)

## Installation

- Install packages that the sample depends on.
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
- Install the sample from Package Manager.
- Add the asset to Addressables.
  - Name: PlayerPrefab
  - Path: MVS/Common/NetworkPlayer.prefab

## How to play

- Open multiple Unity editors using ParrelSync.
- Start the server.
  - Scene: MVS/MultiplayServer/MultiplayServer
- Start the client.
  - Scene: MVS/App/App

