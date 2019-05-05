using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrackerCam3D))]
public class TrackerCam3DEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        TrackerCam3D camera = (TrackerCam3D)target;
        if (GUILayout.Button("Go to position")) {
            camera.ManualUpdate();
        }
        if (GUILayout.Button("Memory position")) {
            camera.MemorizePosition();
        }
    }
}
