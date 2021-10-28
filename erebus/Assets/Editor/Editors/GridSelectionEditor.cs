using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UnityEditor.Tilemaps.GridSelection))]
public class GridSelectionEditor : Editor {

    private static readonly string PrefabPath = "Assets/Resources/Prefabs/MapEvent2D.prefab";

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        UnityEditor.Tilemaps.GridSelection selection = (UnityEditor.Tilemaps.GridSelection)target;
        Transform parent = UnityEditor.Tilemaps.GridSelection.grid.transform.parent;
        if (parent != null && parent.GetComponent<Map>()) {
            if (GUILayout.Button("Create MapEvent2D")) {
                MapEvent2D mapEvent = Instantiate(AssetDatabase.LoadAssetAtPath<MapEvent2D>(PrefabPath)).GetComponent<MapEvent2D>();
                mapEvent.name = "Event" + Random.Range(1000000, 9999999);
                Map map = parent.GetComponent<Map>();
                GameObjectUtility.SetParentAndAlign(mapEvent.gameObject, map.objectLayer.gameObject);
                Undo.RegisterCreatedObjectUndo(mapEvent, "Create " + mapEvent.name);
                mapEvent.SetLocation(MapEvent2D.GridLocationTileCoords(UnityEditor.Tilemaps.GridSelection.position));
                Selection.activeObject = mapEvent.gameObject;
            }
        }

    }
}
