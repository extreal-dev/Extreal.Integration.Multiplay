# Extreal.Integration.Multiplay.NGO

## How to test

- Open one Unity editor using ParrelSync.
- We will call the originally opened editor `MAIN` and the newly opened editor `SUB`.
- SUB> Play the `ServerMain` scene.
- MAIN> Run `Extreal.Integration.Multiplay.NGO.Test.dll`.
- SUB> Stop playing the `ServerMain` scene when `NgoClientTest` has finished running.
- SUB> Run `Extreal.Integration.Multiplay.NGO.Test.Sub.dll`.
- SUB> Run the `UNetTransportServer` scene in `SUB` after all tests in `SUB`.
- MAIN> All tests are completed.
