using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class SettingsPanel
    {
        private readonly List<RoadNetworkAuthoring> allAssets;
        private readonly RoadTypeDeletion roadTypeDeletion;
        private readonly SpeedUnits speedUnits;

        private int pendingDeleteTypeId;
        private int pendingReassignTypeId;
        private bool hasPendingDelete;

        public SettingsPanel()
        {
            allAssets = new List<RoadNetworkAuthoring>();
            roadTypeDeletion = new RoadTypeDeletion();
            speedUnits = new SpeedUnits();
        }

        public void Draw(NavigationSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            DrawRoadTypes(settings);
            EditorGUILayout.Space();
            DrawChannelNames(settings);
            EditorGUILayout.Space();
            DrawProjectSettings(settings);
        }

        private void DrawRoadTypes(NavigationSettings settings)
        {
            EditorGUILayout.LabelField("Road Types", EditorStyles.boldLabel);

            IReadOnlyList<RoadType> roadTypes = settings.RoadTypes;
            for (int i = 0; i < roadTypes.Count; i++)
            {
                DrawRoadTypeRow(settings, roadTypes[i], i, roadTypes.Count);
            }

            if (hasPendingDelete)
            {
                DrawPendingDelete(settings);
            }

            if (GUILayout.Button("Add Road Type"))
            {
                Undo.RecordObject(settings, "Add Road Type");
                settings.AddRoadType("New Type");
                EditorUtility.SetDirty(settings);
            }
        }

        private void DrawRoadTypeRow(NavigationSettings settings, RoadType roadType, int index, int count)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.DelayedTextField(roadType.Name, GUILayout.MinWidth(80));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Rename Road Type");
                settings.SetRoadTypeName(roadType.Id, name);
                EditorUtility.SetDirty(settings);
            }

            EditorGUI.BeginChangeCheck();
            float displaySpeed = ToDisplaySpeed(settings, roadType.SpeedMetersPerSecond);
            float newDisplaySpeed = EditorGUILayout.DelayedFloatField(displaySpeed, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Road Type Speed");
                settings.SetRoadTypeSpeed(roadType.Id, Mathf.Max(0f, FromDisplaySpeed(settings, newDisplaySpeed)));
                EditorUtility.SetDirty(settings);
            }

            EditorGUI.BeginChangeCheck();
            float width = EditorGUILayout.DelayedFloatField(roadType.WidthMeters, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Road Type Width");
                settings.SetRoadTypeWidth(roadType.Id, Mathf.Max(0.1f, width));
                EditorUtility.SetDirty(settings);
            }

            EditorGUI.BeginChangeCheck();
            Color color = EditorGUILayout.ColorField(roadType.EditorColor, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Road Type Color");
                settings.SetRoadTypeColor(roadType.Id, color);
                EditorUtility.SetDirty(settings);
            }

            GUI.enabled = index > 0;
            if (GUILayout.Button("Up", GUILayout.Width(30)))
            {
                Undo.RecordObject(settings, "Reorder Road Types");
                settings.MoveRoadType(index, index - 1);
                EditorUtility.SetDirty(settings);
            }

            GUI.enabled = index < count - 1;
            if (GUILayout.Button("Down", GUILayout.Width(45)))
            {
                Undo.RecordObject(settings, "Reorder Road Types");
                settings.MoveRoadType(index, index + 1);
                EditorUtility.SetDirty(settings);
            }

            GUI.enabled = true;

            if (count > 1)
            {
                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    BeginDelete(settings, roadType.Id);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private float ToDisplaySpeed(NavigationSettings settings, float metersPerSecond)
        {
            if (settings.ImperialUnits)
            {
                return speedUnits.MetersPerSecondToMph(metersPerSecond);
            }
            return speedUnits.MetersPerSecondToKmh(metersPerSecond);
        }

        private float FromDisplaySpeed(NavigationSettings settings, float displayValue)
        {
            if (settings.ImperialUnits)
            {
                return speedUnits.MphToMetersPerSecond(displayValue);
            }
            return speedUnits.KmhToMetersPerSecond(displayValue);
        }

        private void BeginDelete(NavigationSettings settings, int typeId)
        {
            RefreshAllAssets();
            int usage = roadTypeDeletion.CountUsage(typeId, allAssets);
            if (usage == 0)
            {
                Undo.RecordObject(settings, "Delete Road Type");
                settings.RemoveRoadType(typeId);
                EditorUtility.SetDirty(settings);
                return;
            }

            hasPendingDelete = true;
            pendingDeleteTypeId = typeId;
            pendingReassignTypeId = FindFirstOtherTypeId(settings, typeId);
        }

        private void RefreshAllAssets()
        {
            allAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:RoadNetworkAuthoring");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RoadNetworkAuthoring asset = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
                if (asset != null)
                {
                    allAssets.Add(asset);
                }
            }
        }

        private int FindFirstOtherTypeId(NavigationSettings settings, int excludeTypeId)
        {
            IReadOnlyList<RoadType> roadTypes = settings.RoadTypes;
            for (int i = 0; i < roadTypes.Count; i++)
            {
                if (roadTypes[i].Id != excludeTypeId)
                {
                    return roadTypes[i].Id;
                }
            }
            return excludeTypeId;
        }

        private void DrawPendingDelete(NavigationSettings settings)
        {
            RoadType deletedType = settings.FindRoadType(pendingDeleteTypeId);
            if (deletedType == null)
            {
                hasPendingDelete = false;
                return;
            }

            RefreshAllAssets();
            int usage = roadTypeDeletion.CountUsage(pendingDeleteTypeId, allAssets);
            int mapCount = CountAffectedMaps(pendingDeleteTypeId);

            string message = usage + " road(s) in " + mapCount + " map(s) use " + deletedType.Name + ". Move them to:";
            EditorGUILayout.HelpBox(message, MessageType.Warning);

            pendingReassignTypeId = DrawTypePopup(settings, pendingReassignTypeId, pendingDeleteTypeId);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Confirm"))
            {
                Undo.RecordObject(settings, "Delete Road Type");
                for (int i = 0; i < allAssets.Count; i++)
                {
                    if (allAssets[i] != null)
                    {
                        Undo.RecordObject(allAssets[i], "Delete Road Type");
                    }
                }

                roadTypeDeletion.Reassign(pendingDeleteTypeId, pendingReassignTypeId, allAssets);
                settings.RemoveRoadType(pendingDeleteTypeId);

                EditorUtility.SetDirty(settings);
                for (int i = 0; i < allAssets.Count; i++)
                {
                    if (allAssets[i] != null)
                    {
                        EditorUtility.SetDirty(allAssets[i]);
                    }
                }

                hasPendingDelete = false;
            }

            if (GUILayout.Button("Cancel"))
            {
                hasPendingDelete = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        private int CountAffectedMaps(int typeId)
        {
            int mapCount = 0;
            for (int i = 0; i < allAssets.Count; i++)
            {
                RoadNetworkAuthoring asset = allAssets[i];
                if (asset == null)
                {
                    continue;
                }

                List<AuthoringRoad> roads = asset.Roads;
                bool used = false;
                for (int j = 0; j < roads.Count; j++)
                {
                    if (roads[j].TypeId == typeId)
                    {
                        used = true;
                        break;
                    }
                }

                if (used)
                {
                    mapCount++;
                }
            }
            return mapCount;
        }

        private int DrawTypePopup(NavigationSettings settings, int typeId, int excludeTypeId)
        {
            IReadOnlyList<RoadType> roadTypes = settings.RoadTypes;
            List<string> names = new List<string>();
            List<int> ids = new List<int>();
            int selectedIndex = 0;

            for (int i = 0; i < roadTypes.Count; i++)
            {
                if (roadTypes[i].Id == excludeTypeId)
                {
                    continue;
                }

                if (roadTypes[i].Id == typeId)
                {
                    selectedIndex = ids.Count;
                }

                names.Add(roadTypes[i].Name);
                ids.Add(roadTypes[i].Id);
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, names.ToArray());
            if (newIndex >= 0 && newIndex < ids.Count)
            {
                return ids[newIndex];
            }
            return typeId;
        }

        private void DrawChannelNames(NavigationSettings settings)
        {
            EditorGUILayout.LabelField("View Channel Names", EditorStyles.boldLabel);

            for (int i = 0; i < NavigationSettings.ChannelCount; i++)
            {
                EditorGUI.BeginChangeCheck();
                string channelName = EditorGUILayout.DelayedTextField("Channel " + i, settings.GetChannelName(i));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(settings, "Rename View Channel");
                    settings.SetChannelName(i, channelName);
                    EditorUtility.SetDirty(settings);
                }
            }
        }

        private void DrawProjectSettings(NavigationSettings settings)
        {
            EditorGUILayout.LabelField("Project", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Change this before authoring maps; existing maps won't match.", MessageType.Warning);
            EditorGUI.BeginChangeCheck();
            float unitsPerMeter = EditorGUILayout.DelayedFloatField("Units Per Meter", settings.UnitsPerMeter);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Units Per Meter");
                settings.SetUnitsPerMeter(Mathf.Max(0.0001f, unitsPerMeter));
                EditorUtility.SetDirty(settings);
            }

            EditorGUI.BeginChangeCheck();
            bool imperialUnits = EditorGUILayout.Toggle("Imperial Units", settings.ImperialUnits);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Imperial Units");
                settings.SetImperialUnits(imperialUnits);
                EditorUtility.SetDirty(settings);
            }

            EditorGUI.BeginChangeCheck();
            bool blockBuildOnProblems = EditorGUILayout.Toggle("Block Build On Problems", settings.BlockBuildOnProblems);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Block Build On Problems");
                settings.SetBlockBuildOnProblems(blockBuildOnProblems);
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
