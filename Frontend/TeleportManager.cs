using System;
using System.Collections.Generic;
using System.Numerics;

namespace Frontend
{
    public sealed class TeleportManager : ITeleportManager
    {
        private Dictionary<int, TeleportInfo> _teleportIds = new Dictionary<int, TeleportInfo>();
        private IRandomProvider _randomProvider;

        public TeleportManager(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
        }

        private readonly struct TeleportInfo
        {
            public readonly int EntityId;
            public readonly Vector3 Target;

            public TeleportInfo(int entityId, Vector3 target)
            {
                EntityId = entityId;
                Target = target;
            }
        }
        
        public int BeginTeleport(int entityId, Vector3 target)
        {
            int id;
            do
            {
                id = _randomProvider.NextInt();
            } while (_teleportIds.ContainsKey(id));

            _teleportIds[id] = new TeleportInfo(entityId, target);
            return id;
        }

        public Vector3? EndTeleport(int entityId, int teleportId)
        {
            if (!_teleportIds.ContainsKey(teleportId))
                return null;

            var info = _teleportIds[teleportId];

            if (info.EntityId != entityId)
                return null;

            return info.Target;
        }
    }
}
