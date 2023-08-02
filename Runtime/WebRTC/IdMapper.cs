using System;
using System.Collections.Generic;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    public class IdMapper
    {
        private readonly Dictionary<string, ulong> strToLongMapping = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> ulongToStrMapping = new Dictionary<ulong, string>();

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

        public bool Has(string id) => strToLongMapping.ContainsKey(id);

        public ulong Get(string id) => strToLongMapping[id];

        public bool Has(ulong id) => ulongToStrMapping.ContainsKey(id);

        public string Get(ulong id) => ulongToStrMapping[id];

        public bool TryGet(ulong id, out string outId) => ulongToStrMapping.TryGetValue(id, out outId);

        public void Remove(string id)
        {
            if (!Has(id))
            {
                return;
            }
            var ulongId = strToLongMapping[id];
            strToLongMapping.Remove(id);
            ulongToStrMapping.Remove(ulongId);
        }

        public void Clear()
        {
            strToLongMapping.Clear();
            ulongToStrMapping.Clear();
        }
    }
}
