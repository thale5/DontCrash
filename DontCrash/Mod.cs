using ICities;
using UnityEngine;

namespace DontCrash
{
    public sealed class Mod : IUserMod
    {
        public static bool created, disposed;
        public string Name => "Don't Crash";
        public string Description => string.Empty;

        public void OnEnabled()
        {
            disposed = false;

            if (!created)
            {
                ManagerPanel.Setup();
                created = true;
            }
        }

        public void OnDisabled()
        {
            disposed = true;
        }
    }
}
