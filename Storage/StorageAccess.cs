using UnityEngine;

namespace HoardFinder.Storage
{
    /// <summary>
    /// Mirrors what vanilla lets the local player open: ward permission at the position and, for containers, the
    /// privacy setting (Container.CheckAccess: Public yes, Private only its builder, Group never). When
    /// RespectContainerAccess is off, everything replicated to this client is listed.
    /// </summary>
    internal static class StorageAccess
    {
        public static bool Enabled = true;

        public static bool Ward(Vector3 position)
        {
            return !Enabled || PrivateArea.CheckAccess(position, 0f, false, false);
        }

        public static bool Container(Container container, Piece piece)
        {
            if (!Enabled)
            {
                return true;
            }
            switch (container.m_privacy)
            {
                case global::Container.PrivacySetting.Public:
                    return Ward(container.transform.position);
                case global::Container.PrivacySetting.Private:
                    return piece != null && piece.GetCreator() == LocalPlayerId() && Ward(container.transform.position);
                default:
                    return false;
            }
        }

        private static long LocalPlayerId()
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            return profile != null ? profile.GetPlayerID() : 0L;
        }
    }
}
