using System;

namespace Gley.NavigationSystem.Editor
{
    public interface IFormatMigration
    {
        Type AssetType { get; }
        int FromVersion { get; }

        void Migrate(UnityEngine.Object asset);
    }
}
