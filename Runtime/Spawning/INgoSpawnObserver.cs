using System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INgoSpawnObserver : IDisposable
    {
        IObservable<NetworkObject[]> OnSpawnAsObservable { get; }
        IObservable<ulong[]> OnDespawnAsObservable { get; }

        void Clear();
        void Start();
    }
}
