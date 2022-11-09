using System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for the mock to be used as NgoSpawnObserver when testing.
    /// </summary>
    public interface INgoSpawnObserver : IDisposable
    {
        /// <summary>
        /// Invokes when several NetworkObjects are spawned.
        /// </summary>
        /// <value>Observable object</value>
        IObservable<NetworkObject[]> OnSpawnedAsObservable { get; }

        /// <summary>
        /// Invokes when several NetworkObjects are despawned.
        /// </summary>
        /// <value>Observable object</value>
        IObservable<ulong[]> OnDespawnedAsObservable { get; }

        /// <summary>
        /// Clears INgoSpawnObserver
        /// </summary>
        void Clear();

        /// <summary>
        /// Starts observation
        /// </summary>
        void Start();
    }
}
