#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Class that maps client ID and local ID.
    /// </summary>
    public class NativeIdMapper
    {
        private readonly Dictionary<string, ulong> strToLongMapping = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> ulongToStrMapping = new Dictionary<ulong, string>();

        /// <summary>
        /// Adds the client ID.
        /// </summary>
        /// <param name="id">Client ID.</param>
        public void Add(string id)
        {
            var ulongId = Generate();
            strToLongMapping.Add(id, ulongId);
            ulongToStrMapping.Add(ulongId, id);
        }

        private ulong Generate()
        {
            var now = DateTimeOffset.UtcNow;
            var id = now.ToUnixTimeMilliseconds() + strToLongMapping.Count;
            return (ulong)id;
        }

        /// <summary>
        /// Checks if this instance already contains the client ID.
        /// </summary>
        /// <param name="id">Client ID.</param>
        /// <returns>True if contained, false otherwise.</returns>
        public bool Has(string id) => strToLongMapping.ContainsKey(id);

        /// <summary>
        /// Gets the local ID mapped to the client ID.
        /// </summary>
        /// <param name="id">Client ID.</param>
        /// <returns>Local ID mapped to the client ID.</returns>
        public ulong Get(string id) => strToLongMapping[id];

        /// <summary>
        /// Tries to get the client ID mapped to the local ID.
        /// </summary>
        /// <param name="id">Local ID.</param>
        /// <param name="outId">Client ID.</param>
        /// <returns>True if the client ID could be got, false otherwise.</returns>
        public bool TryGet(ulong id, out string outId) => ulongToStrMapping.TryGetValue(id, out outId);

        /// <summary>
        /// Removes the client ID.
        /// </summary>
        /// <param name="id">Client ID.</param>
        public void Remove(string id)
        {
            if (!Has(id))
            {
                // Not covered by testing due to defensive implementation
                return;
            }
            var ulongId = strToLongMapping[id];
            strToLongMapping.Remove(id);
            ulongToStrMapping.Remove(ulongId);
        }

        /// <summary>
        /// Clears mapping.
        /// </summary>
        public void Clear()
        {
            strToLongMapping.Clear();
            ulongToStrMapping.Clear();
        }
    }
}
#endif
