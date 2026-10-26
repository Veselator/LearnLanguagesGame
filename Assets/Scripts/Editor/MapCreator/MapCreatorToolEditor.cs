using System.Collections.Generic;
using LearnLanguages.MapCreator;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using UnityEditor;
using UnityEngine;

namespace LearnLanguages.Editor.MapCreator
{
    /// <summary>
    /// Province editing tools of the MapCreator scene: generation from the images, selection by clicking in the
    /// Scene view, position handles and all province parameters.
    /// </summary>
    [CustomEditor(typeof(MapCreatorTool))]
    public sealed class MapCreatorToolEditor : UnityEditor.Editor
    {
        private readonly ProvinceLookup _lookup = new ProvinceLookup();
        private Texture2D _lookupSource;
        private ProvincesData _lookupData;
        private int _lookupEntries;

        private MapCreatorTool Tool => (MapCreatorTool)target;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var map = Tool.Map;
            if (map == null)
            {
                EditorGUILayout.HelpBox("Assign a Map asset.", MessageType.Info);
                return;
            }
            if (map.ProvincesData == null)
            {
                EditorGUILayout.HelpBox("The map has no ProvincesData asset.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            var mode = (MapPreviewMode)GUILayout.Toolbar((int)Tool.PreviewMode, new[] { "Landscape", "Provinces", "Height" });
            if (mode != Tool.PreviewMode)
            {
                Undo.RecordObject(Tool, "Preview mode");
                Tool.SetPreviewMode(mode);
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            if (GUILayout.Button("Configure texture import settings")) ProvincesGenerator.ConfigureTextureImport(map);
            if (GUILayout.Button("Sync provinces with image (keeps existing data)")) Generate(false);
            if (GUILayout.Button("Regenerate everything (random gameplay data)") &&
                EditorUtility.DisplayDialog("Regenerate", "Reroll names, landscapes, settlements and spawn points?", "Yes", "No"))
                Generate(true);
            if (GUILayout.Button("Generate map icon")) ProvincesGenerator.GenerateIcon(map);

            EditorGUILayout.Space();
            DrawSelected(map.ProvincesData);
        }

        private void Generate(bool reset)
        {
            new ProvincesGenerator().Generate(Tool.Map, reset);
            _lookupSource = null;
            SceneView.RepaintAll();
        }

        private void DrawSelected(ProvincesData data)
        {
            EditorGUILayout.LabelField($"Province ({data.Count} total)", EditorStyles.boldLabel);
            var selected = EditorGUILayout.IntSlider("Selected id", Tool.SelectedProvince, -1, data.Count - 1);
            if (selected != Tool.SelectedProvince)
            {
                Undo.RecordObject(Tool, "Select province");
                Tool.Select(selected);
                SceneView.RepaintAll();
            }
            if (selected < 0)
            {
                EditorGUILayout.HelpBox("Click a province in the Scene view to select it.", MessageType.Info);
                return;
            }

            var entry = data.Get(selected);
            EditorGUI.BeginChangeCheck();
            var name = EditorGUILayout.TextField("Name", entry.Name);
            EditorGUILayout.ColorField("Map color", entry.MapColor);
            var type = (ProvinceType)EditorGUILayout.EnumPopup("Type", entry.Type);
            var impassable = EditorGUILayout.Toggle("Impassable", entry.IsImpassable);
            var landscape = entry.Landscape;
            var settlement = entry.Settlement;
            var production = entry.Production;
            using (new EditorGUI.DisabledScope(type == ProvinceType.Sea))
            {
                landscape = (LandscapeType)EditorGUILayout.EnumPopup("Landscape", entry.Landscape);
                settlement = (SettlementType)EditorGUILayout.EnumPopup("Settlement", entry.Settlement);
                production = (ProductionType)EditorGUILayout.EnumPopup("Production", entry.Production);
            }
            var settlementPos = EditorGUILayout.Vector2Field("Settlement position", entry.SettlementLocalPos);
            var portPos = EditorGUILayout.Vector2Field("Port position", entry.PortLocalPos);
            var unitPos = EditorGUILayout.Vector2Field("Unit position", entry.LocalUnitPosition);

            var spawns = new List<int>(data.PossibleSpawnPoints);
            var isSpawn = EditorGUILayout.Toggle("Spawn point", spawns.Contains(selected));
            EditorGUILayout.LabelField("Neighbours", string.Join(", ", entry.Neighbours));

            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(data, "Edit province");
            entry.SetName(name);
            entry.SetType(type);
            entry.SetImpassable(impassable);
            if (type == ProvinceType.Land)
            {
                entry.SetLandscape(landscape == LandscapeType.Sea ? LandscapeType.Meadow : landscape);
                entry.SetSettlement(settlement);
                entry.SetProduction(production);
            }
            entry.SetSettlementLocalPos(settlementPos);
            entry.SetPortLocalPos(portPos);
            entry.SetLocalUnitPosition(unitPos);
            if (isSpawn && !spawns.Contains(selected)) spawns.Add(selected);
            if (!isSpawn) spawns.Remove(selected);
            data.SetSpawnPoints(spawns.ToArray());
            EditorUtility.SetDirty(data);
            SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            var tool = Tool;
            var map = tool.Map;
            if (map == null || map.ProvincesData == null || map.ProvincesData.Count == 0) return;
            var data = map.ProvincesData;

            DrawOverview(tool, data);
            DrawSelectedHandles(tool, data);
            HandleClick(tool, data);
        }

        private static void DrawOverview(MapCreatorTool tool, ProvincesData data)
        {
            var spawns = data.PossibleSpawnPoints;
            for (var i = 0; i < data.Count; i++)
            {
                var entry = data.Get(i);
                var position = tool.PixelToWorld(entry.SettlementLocalPos);
                var isSpawn = false;
                foreach (var spawn in spawns) isSpawn |= spawn == i;

                Handles.color = entry.Type == ProvinceType.Sea ? Color.cyan : isSpawn ? Color.red : Color.white;
                Handles.DotHandleCap(0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * 0.03f, EventType.Repaint);
                if (tool.ShowNames)
                {
                    var label = entry.Type == ProvinceType.Sea ? $"{i}" : $"{i} {entry.Name}\n{entry.Settlement}";
                    if (isSpawn) label += " [SPAWN]";
                    Handles.Label(position, label, EditorStyles.miniBoldLabel);
                }
            }
        }

        private static void DrawSelectedHandles(MapCreatorTool tool, ProvincesData data)
        {
            var id = tool.SelectedProvince;
            if (id < 0 || id >= data.Count) return;
            var entry = data.Get(id);

            if (tool.ShowNeighbours)
            {
                Handles.color = Color.yellow;
                var from = tool.PixelToWorld(entry.SettlementLocalPos);
                foreach (var neighbour in entry.Neighbours)
                    Handles.DrawLine(from, tool.PixelToWorld(data.Get(neighbour).SettlementLocalPos), 2f);
            }

            EditorGUI.BeginChangeCheck();
            var settlement = MoveHandle(tool, entry.SettlementLocalPos, "Settlement", Color.green);
            var port = MoveHandle(tool, entry.PortLocalPos, "Port", Color.blue);
            var unit = MoveHandle(tool, entry.LocalUnitPosition, "Unit", Color.magenta);
            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObject(data, "Move province point");
            entry.SetSettlementLocalPos(settlement);
            entry.SetPortLocalPos(port);
            entry.SetLocalUnitPosition(unit);
            EditorUtility.SetDirty(data);
        }

        private static Vector2 MoveHandle(MapCreatorTool tool, Vector2 pixel, string label, Color color)
        {
            var world = tool.PixelToWorld(pixel);
            Handles.color = color;
            Handles.Label(world + Vector3.up * 0.5f, label);
            var size = HandleUtility.GetHandleSize(world) * 0.08f;
            var moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
            moved.y = tool.transform.position.y;
            return tool.WorldToPixel(moved);
        }

        private void HandleClick(MapCreatorTool tool, ProvincesData data)
        {
            var current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || current.alt || GUIUtility.hotControl != 0) return;

            var ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            var plane = new Plane(Vector3.up, tool.transform.position);
            if (!plane.Raycast(ray, out var distance)) return;

            EnsureLookup(tool.Map);
            var id = _lookup.GetProvince(tool.WorldToPixel(ray.GetPoint(distance)));
            if (id < 0) return;

            Undo.RecordObject(tool, "Select province");
            tool.Select(id);
            current.Use();
            Repaint();
        }

        private void EnsureLookup(Map.Data.Map map)
        {
            if (_lookupSource == map.Provinces && _lookupData == map.ProvincesData && _lookupEntries == map.ProvincesData.Count) return;
            _lookup.Build(map.Provinces, map.ProvincesData);
            _lookupSource = map.Provinces;
            _lookupData = map.ProvincesData;
            _lookupEntries = map.ProvincesData.Count;
        }
    }
}
