using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapOverlayDrawer
    {
        private readonly NavigationEditorPrefs prefs;
        private Material material;

        public MapOverlayDrawer(NavigationEditorPrefs prefs)
        {
            this.prefs = prefs;
        }

        public void DrawSceneOverlay(NavigationMap map, float unitsPerMeter)
        {
            if (!prefs.ShowMapOverlay)
            {
                return;
            }
            if (map.MapData == null || map.MapData.Image == null)
            {
                return;
            }
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureMaterial();
            if (material == null)
            {
                return;
            }

            if (prefs.OverlayAlwaysOnTop)
            {
                material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            else
            {
                material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            material.mainTexture = map.MapData.Image;

            MapFrame frame = map.MapData.CreateFrame();
            Vector2 size = map.MapData.RectangleSize;
            float worldHeight = map.transform.position.y;

            Vector3 bottomLeft = ToWorld(frame.MapToTrue(new Vector2(0f, 0f), 0f), unitsPerMeter, worldHeight);
            Vector3 bottomRight = ToWorld(frame.MapToTrue(new Vector2(size.x, 0f), 0f), unitsPerMeter, worldHeight);
            Vector3 topRight = ToWorld(frame.MapToTrue(new Vector2(size.x, size.y), 0f), unitsPerMeter, worldHeight);
            Vector3 topLeft = ToWorld(frame.MapToTrue(new Vector2(0f, size.y), 0f), unitsPerMeter, worldHeight);

            material.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Color(Color.white);
            GL.TexCoord2(0f, 0f);
            GL.Vertex(bottomLeft);
            GL.TexCoord2(1f, 0f);
            GL.Vertex(bottomRight);
            GL.TexCoord2(1f, 1f);
            GL.Vertex(topRight);
            GL.TexCoord2(0f, 1f);
            GL.Vertex(topLeft);
            GL.End();
        }

        public void Dispose()
        {
            if (material != null)
            {
                Object.DestroyImmediate(material);
                material = null;
            }
        }

        private void EnsureMaterial()
        {
            if (material != null)
            {
                return;
            }

            Shader shader = Shader.Find("Hidden/Gley/NavigationSystem/MapOverlay");
            if (shader == null)
            {
                CustomLogger.LogWarning("MapOverlayDrawer: MapOverlay shader not found.");
                return;
            }

            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }

        private Vector3 ToWorld(Vector3 truePos, float unitsPerMeter, float worldHeight)
        {
            return new Vector3(truePos.x * unitsPerMeter, worldHeight, truePos.z * unitsPerMeter);
        }
    }
}
