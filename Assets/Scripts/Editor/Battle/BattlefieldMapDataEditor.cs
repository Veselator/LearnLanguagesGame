using LearnLanguages.Battle.Data;
using UnityEditor;
using UnityEngine;

namespace LearnLanguages.Editor.Battle
{
    /// <summary>
    /// Paints battlefield tiles: pick a brush, click cells. Row 0 is the farthest line.
    /// </summary>
    [CustomEditor(typeof(BattlefieldMapData))]
    public sealed class BattlefieldMapDataEditor : UnityEditor.Editor
    {
        private static readonly Color[] Colors =
        {
            new Color(0.3f, 0.75f, 0.3f), new Color(0.95f, 0.85f, 0.3f), new Color(0.65f, 0.45f, 0.2f),
            new Color(0.5f, 0.35f, 0.2f), new Color(0.4f, 0.75f, 0.95f), new Color(0.55f, 0.55f, 0.55f)
        };

        private BattleTileType _brush;

        public override void OnInspectorGUI()
        {
            var data = (BattlefieldMapData)target;
            _brush = (BattleTileType)GUILayout.Toolbar((int)_brush, System.Enum.GetNames(typeof(BattleTileType)));

            if (GUILayout.Button($"Fill with {_brush}"))
            {
                Undo.RecordObject(data, "Fill battlefield");
                data.Fill(_brush);
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.Space();
            var previous = GUI.backgroundColor;
            for (var row = 0; row < BattlefieldMapData.Rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (var column = 0; column < BattlefieldMapData.Columns; column++)
                {
                    var type = data.Get(row, column);
                    GUI.backgroundColor = Colors[(int)type];
                    if (GUILayout.Button(GUIContent.none, GUILayout.Width(14), GUILayout.Height(18)) && type != _brush)
                    {
                        Undo.RecordObject(data, "Paint battlefield");
                        data.Set(row, column, _brush);
                        EditorUtility.SetDirty(data);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUI.backgroundColor = previous;
            EditorGUILayout.HelpBox("Columns 0-2: attacker deployment, 27-29: defender deployment.", MessageType.None);
        }
    }
}
