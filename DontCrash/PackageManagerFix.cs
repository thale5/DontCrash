using ColossalFramework.Packaging;

namespace DontCrash
{
    public sealed class PackageManagerFix : DetourUtility
    {
        public static PackageManagerFix instance;

        public PackageManagerFix()
        {
            instance = this;
            init(typeof(PackageManager), "FindAssetByName");
        }

        internal override void Dispose()
        {
            Revert();
            base.Dispose();
            instance = null;
        }

        /// <summary>
        /// Fix for the annoying bug in the default version: the search stops when a package with the correct name is found.
        /// In reality, package names are *not* unique. Think of workshop submissions that contain multiple crp files.
        /// </summary>
        public static Package.Asset FindAssetByName(string fullName)
        {
            int i = fullName.IndexOf(".");

            if (i >= 0)
            {
                string packageName = fullName.Substring(0, i);
                string assetName = fullName.Substring(i + 1);
                Package.Asset a;

                foreach (Package p in PackageManager.allPackages)
                    if (p.packageName == packageName && (a = p.Find(assetName)) != null)
                        return a;
            }

            return null;
        }
    }
}
