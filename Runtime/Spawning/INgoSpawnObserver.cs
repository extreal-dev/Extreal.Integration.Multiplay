using System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INgoSpawnObserver : IDisposable
    {
        IObservable<NetworkObject[]> OnSpawnedAsObservable { get; }

        IObservable<ulong[]> OnDespawnedAsObservable { get; }
        void Clear();
        void Start();
    }
}
