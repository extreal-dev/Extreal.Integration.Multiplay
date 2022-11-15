# Extreal.Integration.Multiplay

## How to test

1. ParrelSync を使用してプロジェクトを 2 つ複製します。
1. 複製したプロジェクトをそれぞれ立ち上げます（それぞれをウィンドウ A 、ウィンドウ B 、元の画面をメインウィンドウと呼ぶことにする）。
1. ウィンドウ A で `ServerMain` シーンを開きプレイします。
1. メインウィンドウで Test Runner 内の `Extreal.Integration.Multiplay.NGO.Test.dll` を Run させます。
1. `NgoClientTest` 内の全テストが終わったらウィンドウ A のプレイを止めます。
1. `NgoServerTest` 内でテストが止まっていたらそのテスト名に対応するテストをウィンドウ A およびウィンドウ B で実行します。
    - 対応するテスト名は末尾が `Sub` または `SubFirst` `SubSecond` です。
    - `Sub` で終わるテストはそれだけを実行します。
    - `SubFirst` `SubSecond` で終わるテストは対になっており、ウィンドウ A およびウィンドウ B でそれぞれを順に実行します。
    - `NgoServerTestSubSecond` のテスト内容がすべて完了したらウィンドウ B では `UNetTransportServer` シーンを開いて実行しておいてください。
