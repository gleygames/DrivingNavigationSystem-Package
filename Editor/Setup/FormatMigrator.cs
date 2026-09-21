using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Gley.Common;

namespace Gley.NavigationSystem.Editor
{
    public enum FormatMigrationResult
    {
        UpToDate,
        Migrated,
        NewerThanCode,
        MissingStep
    }

    public class FormatMigrator
    {
        private readonly IReadOnlyList<IFormatMigration> migrations;

        public FormatMigrator(IReadOnlyList<IFormatMigration> migrations)
        {
            this.migrations = migrations;
        }

        public FormatMigrationResult MigrateIfNeeded(Object asset)
        {
            IFormatVersioned versioned = asset as IFormatVersioned;
            if (versioned == null)
            {
                return FormatMigrationResult.UpToDate;
            }

            if (versioned.FormatVersion > versioned.CurrentFormatVersion)
            {
                CustomLogger.LogError("Asset '" + asset.name + "' was created by a newer version of the format and cannot be read.", asset);
                return FormatMigrationResult.NewerThanCode;
            }

            bool migrated = false;
            while (versioned.FormatVersion < versioned.CurrentFormatVersion)
            {
                IFormatMigration migration = FindMigration(asset.GetType(), versioned.FormatVersion);
                if (migration == null)
                {
                    CustomLogger.LogError("Asset '" + asset.name + "' has no migration step from format version " + versioned.FormatVersion + ".", asset);
                    return FormatMigrationResult.MissingStep;
                }

                migration.Migrate(asset);
                migrated = true;
            }

            if (migrated)
            {
                EditorUtility.SetDirty(asset);
                CustomLogger.Log("Migrated asset '" + asset.name + "' to format version " + versioned.FormatVersion + ".", asset);
                return FormatMigrationResult.Migrated;
            }

            return FormatMigrationResult.UpToDate;
        }

        private IFormatMigration FindMigration(System.Type assetType, int fromVersion)
        {
            for (int i = 0; i < migrations.Count; i++)
            {
                IFormatMigration migration = migrations[i];
                if (migration.AssetType == assetType && migration.FromVersion == fromVersion)
                {
                    return migration;
                }
            }
            return null;
        }
    }
}
