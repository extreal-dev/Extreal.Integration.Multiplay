using System;
using System.Collections.Generic;
using System.Linq;
using Extreal.Core.Logging;
using UniRx;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    public class NgoSpawnObserver : INgoSpawnObserver
    {
        public IObservable<NetworkObject[]> OnSpawnAsObservable => spawnSubject;
        private readonly Subject<NetworkObject[]> spawnSubject = new Subject<NetworkObject[]>();

        public IObservable<ulong[]> OnDespawnAsObservable => despawnSubject;
        private readonly Subject<ulong[]> despawnSubject = new Subject<ulong[]>();

        private readonly NetworkSpawnManager spawnManager;

        private ulong[] beforeKeys = Array.Empty<ulong>();
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoSpawnObserver));

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NgoSpawnObserver)}");
            }

            disposables.Dispose();
            spawnSubject.Dispose();
            despawnSubject.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Clear {nameof(NgoSpawnObserver)}");
            }

            disposables.Clear();
            beforeKeys = Array.Empty<ulong>();
        }

        public void Start()
        {
            if (NetworkManager.Singleton == null)
            {
                throw new InvalidOperationException($"{nameof(NetworkManager)} must exist in Scenes");
            }

            var spawnManager = NetworkManager.Singleton.SpawnManager;
            if (spawnManager is null)
            {
                throw new InvalidOperationException($"{nameof(NetworkManager)} must be connected to server or listening before start");
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Start Observation");
            }
            Clear();

            _ = spawnManager.SpawnedObjects
                .ObserveEveryValueChanged(dict => dict.Count)
                .Subscribe(count =>
                {
                    var spawnedObjectKeys = spawnManager.SpawnedObjects.Keys.Except(beforeKeys);
                    var despawnedObjectKeys = beforeKeys.Except(spawnManager.SpawnedObjects.Keys);

                    beforeKeys = spawnManager.SpawnedObjects.Keys.ToArray();

                    var spawnedObjects = new List<NetworkObject>();
                    foreach (var key in spawnedObjectKeys)
                    {
                        if (spawnManager.SpawnedObjects.TryGetValue(key, out var networkObject))
                        {
                            spawnedObjects.Add(networkObject);
                        }
                    }

                    if (spawnedObjects.Count > 0)
                    {
                        spawnSubject.OnNext(spawnedObjects.ToArray());
                    }
                    if (despawnedObjectKeys.Count() > 0)
                    {
                        despawnSubject.OnNext(despawnedObjectKeys.ToArray());
                    }
                })
                .AddTo(disposables);
        }
    }
}
