using System;
using System.Collections.Generic;
using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class RoadImporterDiscovery
    {
        public void Discover(List<IRoadImporter> output)
        {
            output.Clear();

            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IRoadImporter>();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                if (type.IsAbstract)
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                IRoadImporter importer = (IRoadImporter)Activator.CreateInstance(type);
                if (importer.IsAvailable())
                {
                    output.Add(importer);
                }
            }
        }
    }
}
