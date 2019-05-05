using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class _EditorMeshTools : MonoBehaviour
{

    [MenuItem("GameObject/Mesh Edit/Create Custom Mesh", false, 0)]
    public static void createCustomMesh()
    {
        Mesh customCube = new Mesh();
        Vector3[] verts = {
            new Vector3(-0.5f, 0.5f, 0.5f), // Top
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),

            new Vector3(0.5f, -0.5f, 0.5f), // Bottom
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),

            new Vector3(0.5f, 0.5f, 0.5f), // Right
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),

            new Vector3(-0.5f, -0.5f, 0.5f), // Left
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),

            new Vector3(0.5f, 0.5f, 0.5f), // Front
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),

            new Vector3(-0.5f, 0.5f, -0.5f), // Back
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f)};

        int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5,

            8, 9, 10,
            11, 10, 9,

            12, 13, 14,
            15, 14, 13,

            16, 17, 18,
            19, 18, 17,

            20, 21, 22,
            23, 22, 21};

        Color col = Color.white;

        Color[] colours = {
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col
            };

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);
        Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11
            };
        
        customCube.vertices = verts;
        customCube.triangles = tris;
        customCube.colors = colours;
        customCube.uv = uv;
        customCube.RecalculateBounds();
        customCube.RecalculateNormals();

        GameObject[] objects = new GameObject[1];
        objects[0] = new GameObject("Custom Mesh");
        objects[0].transform.position = SceneView.lastActiveSceneView.pivot;

        MeshFilter mf = objects[0].AddComponent<MeshFilter>();
        mf.mesh = customCube;

        MeshRenderer mr = objects[0].AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Particles/Standard Unlit"));

        MeshEdit me = objects[0].AddComponent<MeshEdit>();

        // Full MeshEdit initialisation
        me.deepCopyMesh(ref customCube, ref me._mesh);
        
        me.getVerts(false);

        me.tris = new List<int>();
        for (int i = 0; i < me.mesh.triangles.Length; i++)
        {
            me.tris.Add(me.mesh.triangles[i]);
        }
        
        me.getQuads(false);
        
        me.getConnectedVertsBasedOnPosition();
        
        me.rebuildAdjacentFaces();
        
        me.checkMeshValidity();
        
        me.recalculateNormals(me._mesh);
        
        _EditorMeshInterface.loadAllUVMaps(me);
        
        Selection.objects = objects;

        SceneView sceneView = (SceneView)SceneView.sceneViews[0];
        sceneView.Focus();
    }

    [MenuItem("GameObject/Mesh Edit/Import Custom Mesh")]
    public static void importCustomMesh()
    {
        _EditorMeshInterface.importObj();
    }
}

