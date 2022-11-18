# Extreal.Integration.Multiplay

## How to test

1. ParrelSync を使用してプロジェクトを 1 つ複製します。
1. 複製したプロジェクトを立ち上げます。
    - これをウィンドウ A 、元の画面をメインウィンドウと呼ぶことにします。
1. ウィンドウ A で `ServerMain` シーンを開きプレイします。
1. メインウィンドウで Test Runner 内の `Extreal.Integration.Multiplay.NGO.Test.dll` を Run させます。
1. `NgoClientTest` 内の全テストが終わったらウィンドウ A のプレイを止めます。
1. ウィンドウ A で `Extreal.Integration.Multiplay.NGO.Test.Sub.dll` を Run させます。
1. ウィンドウ A の全テストが終わったらウィンドウ A で `UNetTransportServer` シーンを開いてプレイします。
