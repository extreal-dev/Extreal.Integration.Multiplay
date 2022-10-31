using System.Linq;
using System;
using System.Collections.Generic;
using Extreal.Core.Logging;
using UniRx;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    public class NetcodeSpawnObserver : IDisposable
    {
        public IObservable<NetworkObject[]> OnSpawnAsObservable => spawnSubject;
        private readonly Subject<NetworkObject[]> spawnSubject = new Subject<NetworkObject[]>();

        public IObservable<ulong[]> OnDespawnAsObservable => despawnSubject;
        private readonly Subject<ulong[]> despawnSubject = new Subject<ulong[]>();

        private readonly NetworkSpawnManager spawnManager;

        private ulong[] beforeKeys = Array.Empty<ulong>();
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NetcodeSpawnObserver));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0057")]
        public NetcodeSpawnObserver(NetworkManager networkManager)
            => spawnManager = networkManager.SpawnManager;

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NetcodeSpawnObserver)}");
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
                Logger.LogDebug($"Clear {nameof(NetcodeSpawnObserver)}");
            }

            disposables.Clear();
            beforeKeys = Array.Empty<ulong>();
        }

        public void Start()
        {
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
