using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor.SceneManagement;
using System.Globalization;
using System.Linq;

[CustomEditor(typeof(MeshEdit))]
class _EditorMeshInterface : Editor
{
    Texture[] tilesAvailable;

    GUISkin skin;
    GUISkin _skinDefault;
    GUISkin skinDefault
    {
        get
        {
            if (_skinDefault == null)
            {
                _skinDefault = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Editor/MeshEdit/Default.guiskin");
            }

            return _skinDefault;
        }
    }
    Ray ray;
    
    int checkFrames = 4000;
    int checkFrame = 0;
    int selectedTri = 0;
    int selectedQuad = 0;
    int selectedTile = 0;
    MeshEdit.Triangle t = new MeshEdit.Triangle(Vector3.zero, Vector3.zero, Vector3.zero);

    Texture tileSelected;
    Texture2D texWindow;
    Texture2D texPixel;
    Texture[] tileDirectionTexture;
    int tileDirection;

    int tilesPerRow = 0;
    int tilesPerColumn = 0;
    int tileWidth = 0;
    int tileHeight = 0;
    int tileOutline = 0;
    int editMode = 0;
    string[] editModes = new string[] { "Default", "Mesh Edit", "Tile Edit", "Vertex Colour" };
    int lastUsedEditMode = 1;
    int wasEditMode;

    Texture2D texColourSwatch;

    public static List<Tileset> tilesetsAvailable;
    public static List<string> tilesetTexturesAvailable;
    int selectedTileset;

    bool[] selectedVerts;
    bool[] selectedFaces;
    bool[] selectedEdges;

    int cpWidth = 256, cpHeight = 256;
    float colourPickerHue = 0.0f;
    Texture2D _colourPicker;
    Texture2D colourPicker
    {
        get
        {
            if (_colourPicker == null)
            {
                _colourPicker = new Texture2D(cpWidth, cpHeight, TextureFormat.RGB24, false);
                for (int y = 0; y < cpHeight; y++)
                {

                    for (int x = 0; x < cpWidth; x++)
                    {
                        Color colour = Color.HSVToRGB(colourPickerHue, (float)x / cpWidth, (float)y / cpHeight);
                        _colourPicker.SetPixel(x, y, colour);
                    }
                }
                _colourPicker.Apply();
            }
            return _colourPicker;
        }
        set
        {
            _colourPicker = value;
        }
    }

    Texture2D _huePicker;
    Texture2D huePicker
    {
        get
        {
            if (_huePicker == null)
            {
                _huePicker = new Texture2D(cpWidth, 24, TextureFormat.RGB24, false);
                for (int y = 0; y < 24; y++)
                {

                    for (int x = 0; x < cpWidth; x++)
                    {
                        Color colour = Color.HSVToRGB((float)x / cpWidth, 1.0f, 1.0f);
                        _huePicker.SetPixel(x, y, colour);
                    }
                }
                _huePicker.Apply();
            }
            return _huePicker;
        }
        set
        {
            _huePicker = value;
        }
    }

    DateTime frameStart;
    
    void OnGUI()
    {
        switch (Event.current.commandName)
        {
            case "UndoRedoPerformed":
                GameObject obj = Selection.activeTransform.gameObject;
                MeshEdit meshEdit = obj.GetComponent<MeshEdit>();

                //meshEdit.setToEditorPosition();
                meshEdit.mesh.RecalculateNormals();
                meshEdit.mesh.RecalculateBounds();
                meshEdit.pushLocalMeshToGameObject();

                break;
        }
    }

    Texture2D[,] tiles;
    Texture2D tileset;
    Texture texturePage;
    Color orange = new Color(0.9f, 0.25f, 0.08f);
    int vertMode = 1;

    bool shift = false;
    bool ctrl = false;
    bool alt = false;

    int transformMode = 0;
    Vector2 moveAnchor;
    Vector3 anchorCenter;
    Vector3 transformDimensions;

    List<Vector3> oldVerts;

    bool helpMode = true;

    int circleVerticesSelected = 3;
    string[] circleVertices = { "6", "8", "10", "12", "16", "24", "32", "48", "64" };
    public static int[] circleVerticesCount = { 6, 8, 10, 12, 16, 24, 32, 48, 64 };

    public string settingsPath = "/Editor/MeshEdit/Mesh Edit Settings.txt";

    List<Color> colourHistory;
    int maxColours = 20;

    private void saveSettings()
    {
        checkEditorFoldersExist();

        string settings = "";
        settings += "CTOE=" + copyTexturesOnExport.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "Help=" + helpMode.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "SnapUV=" + snapUVsToPixel.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "UseUVs=" + useCustomUVCoords.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "VMode=" + vertMode.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "PMode=" + vertMode.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "PCol=" + 
            paintColour.r.ToString(CultureInfo.InvariantCulture) + "," + 
            paintColour.g.ToString(CultureInfo.InvariantCulture) + "," + 
            paintColour.b.ToString(CultureInfo.InvariantCulture) + "," + 
            paintColour.a.ToString(CultureInfo.InvariantCulture) + '\n';
        if (colourHistory != null)
        {
            for (int i = 0; i < colourHistory.Count; i++)
            {
                settings += "HCol" + i + "=" +
                    colourHistory[i].r.ToString(CultureInfo.InvariantCulture) + "," +
                    colourHistory[i].g.ToString(CultureInfo.InvariantCulture) + "," +
                    colourHistory[i].b.ToString(CultureInfo.InvariantCulture) + "," +
                    colourHistory[i].a.ToString(CultureInfo.InvariantCulture) + '\n';
            }
        }

        File.WriteAllText(Application.dataPath + settingsPath, settings);
    }

    private void loadSettings()
    {
        checkEditorFoldersExist();

        colourHistory = new List<Color>();

        if (File.Exists(Application.dataPath + settingsPath))
        {
            string settings = File.ReadAllText(Application.dataPath + settingsPath);

            string[] args = settings.Split('\n');
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("CTOE"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out copyTexturesOnExport);
                }
                else if (args[i].StartsWith("Help"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out helpMode);
                }
                else if (args[i].StartsWith("SnapUV"))
                {
                    bool.TryParse(args[i].Remove(0, 7), out snapUVsToPixel);
                }
                else if (args[i].StartsWith("UseUVs"))
                {
                    bool.TryParse(args[i].Remove(0, 7), out useCustomUVCoords);
                }
                else if (args[i].StartsWith("VMode"))
                {
                    int.TryParse(args[i].Remove(0, 6), out vertMode);
                }
                else if (args[i].StartsWith("PMode"))
                {
                    int.TryParse(args[i].Remove(0, 6), out paintMode);
                }
                else if (args[i].StartsWith("PCol"))
                {
                    string colourString = args[i].Remove(0, 5);

                    string[] values = colourString.Split(',');
                    float r, g, b, a;
                    float.TryParse(values[0], out r);
                    float.TryParse(values[1], out g);
                    float.TryParse(values[2], out b);
                    float.TryParse(values[3], out a);

                    paintColour = new Color(r, g, b, a);
                }
                else if (args[i].StartsWith("HCol"))
                {
                    string colourString = args[i].Remove(0, args[i].IndexOf('=') + 1);

                    string[] values = colourString.Split(',');
                    float r, g, b, a;
                    float.TryParse(values[0], out r);
                    float.TryParse(values[1], out g);
                    float.TryParse(values[2], out b);
                    float.TryParse(values[3], out a);
                    Color colour = new Color(r, g, b, a);
                    colourHistory.Add(colour);
                }
            }

            MeshEdit solid = Selection.activeTransform.GetComponent<MeshEdit>();

            solid.setVertMode(vertMode, selectedVerts, selectedFaces);
        }
    }

    bool loadedSettings = false;

    void OnSceneGUI()
    {
        /*
        if (loopDebugMemory != null)
        {
            for (int i = 0; i < loopDebugMemory.Count; i += 2)
            {
                Debug.DrawLine(loopDebugMemory[i], loopDebugMemory[i + 1], Color.red);
            }
        }
        if (loopDebugMemory2 != null)
        {
            for (int i = 0; i < loopDebugMemory2.Count; i += 2)
            {
                Debug.DrawLine(loopDebugMemory2[i], loopDebugMemory2[i + 1], Color.blue);
            }
        }
        if (loopDebugMemory3 != null)
        {
            for (int i = 0; i < loopDebugMemory3.Count; i += 2)
            {
                Debug.DrawLine(loopDebugMemory3[i], loopDebugMemory3[i + 1], Color.yellow);
            }
        }*/
        if (!loadedSettings)
        {
            loadSettings();
            loadedSettings = true;
        }
        if (skin == null)
        {
            skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Editor/MeshEdit/EditorTools.guiskin");
        }
        if (texWindow == null)
        {
            texWindow = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/MeshEdit/texWindow.png");
            skin.box.normal.background = texWindow;
        }
        if (texColourSwatch == null)
        {
            texColourSwatch = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/MeshEdit/texColourSwatch.png");
        }
        if (Selection.activeTransform != null && Selection.activeTransform.gameObject != null && Selection.activeTransform.gameObject.GetComponent<MeshFilter>() != null)
        {
            MeshEdit solid = Selection.activeTransform.GetComponent<MeshEdit>();

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Used)
            {

            }
            if (e.type != EventType.Layout && e.type != EventType.KeyDown && e.type != EventType.Repaint && e.type != EventType.MouseMove && e.type != EventType.MouseEnterWindow && e.type != EventType.MouseLeaveWindow)
            {

            }
            // For some reason, checking thie qualifier breaks the function!?
            //if (Event.current.type != EventType.Layout)
            {
                switch (Event.current.commandName)
                {
                    case "UndoRedoPerformed":
                        {
                            GameObject obj = Selection.activeTransform.gameObject;


                            solid.pushNewGeometry();

                            updateSelectionArray(solid.verts.Count);
                            break;
                        }
                }
            }


            // Shortcut setup
            shift = Event.current.shift;
            ctrl = Event.current.control || Event.current.command;
            alt = Event.current.alt;
            //Debug.Log("ALT: " + alt.ToString() + ", CTRL: " + ctrl.ToString() + ", SHIFT: " + shift.ToString());
            

            if (tilesetsAvailable == null || tilesetTexturesAvailable == null)
            {
                loadTilesets();
            }

            if (tileDirectionTexture == null ||
                tileDirectionTexture[0] == null ||
                tileDirectionTexture[1] == null ||
                tileDirectionTexture[2] == null ||
                tileDirectionTexture[3] == null)
            {
                tileDirectionTexture = new Texture[4];
                for (int i = 0; i < 4; i++)
                {
                    tileDirectionTexture[i] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MeshEdit/TileDirection_" + i.ToString() + ".png");
                }
            }
            if (tileSelected == null)
            {
                tileSelected = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/MeshEdit/TileDirection_0");
            }
            if (texPixel == null)
            {
                texPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texPixel.SetPixel(0, 0, Color.white);
            }


            #region GUILayout


            checkFrame++;
            guiHeader(solid);

            if (editMode == 0)
            {
                guiDefault(solid, controlId);
            }
            else if (editMode == 2)
            {
                guiTextureTiling(solid, controlId);

                operationsTextureTiling(solid, controlId);

            }
            else if (editMode == 1)
            {
                guiMeshEditing(solid, controlId);
                operationsMeshEdit(solid, controlId);
            }
            else if (editMode == 3)
            {
                guiColourEditing(solid, controlId);
                operationsColourEdit(solid, controlId);
            }

            if (editMode == 1)
            {
                solid.setVertMode(vertMode, selectedVerts, selectedFaces);
            }

            if (editMode != 1 || editOperation != MeshEditOperation.LoopCut)
            {

                solid.facesCut = null;
                solid.cutsAB = null;
                solid.cutCount = 0;
            }
            #endregion

            
            #region Keyboard Shortcuts
            // CMD/CTRL can only be used during KeyUp or 
            if (Event.current.type == EventType.KeyUp)
            {
                switch (Event.current.keyCode)
                {
                    case (KeyCode.Tab):
                        if (Event.current.modifiers == EventModifiers.Control ||
                            Event.current.modifiers == EventModifiers.Command)
                        {
                            if (editMode == 1)
                            {
                                vertMode = 1 - vertMode;
                                saveSettings();
                                if (vertMode == 0)
                                {
                                    // To verts
                                    selectionConvertToVerts(solid);
                                    selectionAddTouchingVerts(solid);

                                    selectedFaces = new bool[selectedFaces.Length];
                                }
                                else if (vertMode == 1)
                                {
                                    // To faces
                                    selectionConvertToFaces(solid);
                                    selectedVerts = new bool[selectedVerts.Length];
                                }
                            }
                            else if (editMode == 3)
                            {
                                paintMode = 1 - paintMode;
                                saveSettings();
                            }
                        }
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                        break;
                }
            }
            else if (Event.current.type == EventType.KeyDown)
            {
                #region View shortcuts

                /* This does not work
                if (Event.current.keyCode == KeyCode.C &&
                    (Event.current.modifiers == EventModifiers.Control ||
                    Event.current.modifiers == EventModifiers.Command))
                {
                    Debug.Log("Copy");

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.V &&
                    (Event.current.modifiers == EventModifiers.Control ||
                    Event.current.modifiers == EventModifiers.Command))
                {
                    Debug.Log("Paste");

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                */

                // View
                if (Event.current.keyCode == KeyCode.Keypad1)
                {
                    // Y- Align
                    Camera camera = SceneView.lastActiveSceneView.camera;
                    float d = Vector3.Distance(camera.transform.position, SceneView.lastActiveSceneView.pivot);
                    Vector3 forward = new Vector3(0.0f, 0.0f, 1.0f);

                    if (SceneView.lastActiveSceneView.rotation == Quaternion.LookRotation(forward)) { forward = -forward; }
                    SceneView.lastActiveSceneView.rotation = Quaternion.LookRotation(forward);

                    SceneView.lastActiveSceneView.Repaint();


                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Keypad3)
                {
                    // X- Align
                    Camera camera = SceneView.lastActiveSceneView.camera;
                    float d = Vector3.Distance(camera.transform.position, SceneView.lastActiveSceneView.pivot);
                    Vector3 forward = new Vector3(1.0f, 0.0f, 0.0f);
                    if (shift)
                    {
                        forward = -forward;
                    }

                    if (SceneView.lastActiveSceneView.rotation == Quaternion.LookRotation(forward)) { forward = -forward; }
                    SceneView.lastActiveSceneView.rotation = Quaternion.LookRotation(forward);

                    SceneView.lastActiveSceneView.Repaint();


                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Keypad7)
                {
                    // Z- Align
                    Camera camera = SceneView.lastActiveSceneView.camera;
                    float d = Vector3.Distance(camera.transform.position, SceneView.lastActiveSceneView.pivot);
                    Vector3 forward = new Vector3(0.0f, -1.0f, 0.0f);
                    if (shift)
                    {
                        forward = -forward;
                    }

                    if (SceneView.lastActiveSceneView.rotation == Quaternion.LookRotation(forward)) { forward = -forward; }
                    SceneView.lastActiveSceneView.rotation = Quaternion.LookRotation(forward);

                    SceneView.lastActiveSceneView.Repaint();


                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Keypad5)
                {
                    // ISO/PERSP
                    SceneView.lastActiveSceneView.orthographic = !SceneView.lastActiveSceneView.orthographic;
                    SceneView.lastActiveSceneView.Repaint();

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Slash)
                {
                    //   Debug.Log("Hide other editable meshes");
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();

                }
                #endregion
                switch (Event.current.keyCode)
                {
                    case (KeyCode.Tab):
                        int newEditMode = 0;
                        if (shift)
                        {
                            newEditMode = editMode - 1;
                        }
                        else
                        {
                            newEditMode = editMode + 1;
                        }

                        if (newEditMode >= editModes.Length)
                        {
                            newEditMode = 0;
                        }
                        if (newEditMode < 0)
                        {
                            newEditMode = editModes.Length - 1;
                        }

                        updateEditMode(solid, newEditMode);
                        
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                        break;
                    case (KeyCode.A):
                        if (editMode == 2)
                        {
                            if (selectedTile % tilesPerRow - 1 < 0)
                            {
                                selectedTile += 7;
                            }
                            else
                            {
                                selectedTile--;
                            }

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.D):
                        if (editMode == 2)
                        {
                            if ((selectedTile % tilesPerRow) + 1 >= tilesPerRow || selectedTile + 1 >= tiles.Length)
                            {
                                selectedTile = selectedTile - selectedTile % tilesPerRow;
                            }
                            else
                            {
                                selectedTile++;
                            }

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.W):
                        if (editMode == 2)
                        {
                            if (selectedTile - tilesPerRow < 0)
                            {
                                selectedTile = (tiles.Length / tilesPerRow) * tilesPerRow + selectedTile - tilesPerRow;
                            }
                            else
                            {
                                selectedTile -= tilesPerRow;
                            }

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();

                        }
                        break;
                    case (KeyCode.S):
                        if (editMode == 2)
                        {
                            if (selectedTile + tilesPerRow >= tiles.Length)
                            {
                                int x = selectedTile - (selectedTile / tilesPerRow) * tilesPerRow;

                                selectedTile = x;
                            }
                            else
                            {
                                selectedTile += tilesPerRow;
                            }


                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;

                    case (KeyCode.E):
                        if (editMode == 2)
                        {
                            tileDirection++;

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;

                    case (KeyCode.Q):
                        if (editMode == 2)
                        {
                            tileDirection--;

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.Z):
                        if (Event.current.modifiers == EventModifiers.None)
                        {
                            solid.isMeshTransparent = !solid.isMeshTransparent;
                            solid.GetComponent<MeshRenderer>().enabled = solid.isMeshTransparent;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.Delete):
                        if (editMode != 0)
                        {
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        break;
                }

            }
            /*
            //                          Rotate tile            Select tile from tileset                    Translate  Rotate     Scale      Set transform dimensions         Various utility keys
            KeyCode[] bannedKeyCodes = { KeyCode.Q, KeyCode.E, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.G, KeyCode.R, KeyCode.S, KeyCode.X, KeyCode.Y, KeyCode.Z, KeyCode.Tab, KeyCode.Escape };
            if (Event.current.type == EventType.KeyDown)
            {
                for (int i = 0; i < bannedKeyCodes.Length; i++)
                {
                    if (Event.current.keyCode == bannedKeyCodes[i])
                    {
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                        break;
                    }
                }
            }*/
            #endregion
            
            if (tileDirection > 3)
            {
                tileDirection = 0;
            }
            else if (tileDirection < 0)
            {
                tileDirection = 3;
            }

            Handles.EndGUI();
        }
        else
        {

        }

        
    }
    
    #region bresenham circle
    private void circleBres(Texture2D texture, int xc, int yc, int r, Color colour)
    {
        int x = 0, y = r;
        int d = 3 - 2 * r;
        while (y >= x)
        {
            // for each pixel we will 
            // draw all eight pixels 
            drawCircle(texture, xc, yc, x, y, colour);
            x++;

            // check for decision parameter 
            // and correspondingly  
            // update d, x, y 
            if (d > 0)
            {
                y--;
                d = d + 4 * (x - y) + 10;
            }
            else
            {
                d = d + 4 * x + 6;
                drawCircle(texture, xc, yc, x, y, colour);
            }
        }
    }
    private void drawCircle(Texture2D texture, int xc, int yc, int x, int y, Color colour)
    {
        texture.SetPixel(xc + x, yc + y, colour);
        texture.SetPixel(xc - x, yc + y, colour);
        texture.SetPixel(xc + x, yc - y, colour);
        texture.SetPixel(xc - x, yc - y, colour);
        texture.SetPixel(xc + y, yc + x, colour);
        texture.SetPixel(xc - y, yc + x, colour);
        texture.SetPixel(xc + y, yc - x, colour);
        texture.SetPixel(xc - y, yc - x, colour);
    }
    #endregion
    Texture2D _selectCircleTexture;
    Texture2D selectCircleTexture
    {
        get
        {
            int r = (int)(selectionCircleRadius + 0.5f);
            if (_selectCircleTexture == null ||
                _selectCircleTexture.width / 2 != r)
            {
                _selectCircleTexture = new Texture2D(r * 2, r * 2, TextureFormat.RGBA32, false);
                Color32[] bgPixels = new Color32[(r * 2) * (r * 2)];
                for (int i = 0; i < bgPixels.Length; i++)
                {
                    bgPixels[i] = Color.clear;
                }
                _selectCircleTexture.SetPixels32(bgPixels);
                circleBres(_selectCircleTexture, r, r, r - 1, Color.white);
                _selectCircleTexture.Apply();
            }
            return _selectCircleTexture;
        }
    }
    bool transformDimensionsExtrude = false;
    bool transformDimensionsPlanar = false;
    enum MeshEditOperation { Standard, SelectCircle, LoopCut }
    MeshEditOperation editOperation = MeshEditOperation.Standard;
    float selectionCircleRadius = 10.0f;
    int closestFace = -1;
    bool isNSLoopCut = false;
    List<int> facesCut;
    List<int> cutsAB;
    List<int[]> cutsAdjacentFaces;
    bool isCutLooping = false;
    float cutCount = 1.5f;
    
    public void Awake()
    {
        //Debug.Log("onAwake");
    }
    

    public void OnDestroy()
    {
        //Debug.Log("Destroyed");
    }

    private void operationsMeshEdit(MeshEdit solid, int controlId)
    {
        Vector3 qCenter = Vector2.zero;

        solid.checkMeshValidity();

        Vector2 pos = Vector2.zero;


        bool activateRightClick = false;
        bool isAdditive = false;
        float selectDistance = 25;
        Vector2 mousePos = Vector2.zero;
        mousePos = Event.current.mousePosition;
        mousePos = constrainToScreenSize(mousePos);

        if (editOperation == MeshEditOperation.SelectCircle)
        {
            #region Selection Mode
            if (Event.current.type == EventType.ScrollWheel)
            {
                float s = Event.current.delta.y;
                selectionCircleRadius += s;
                if (selectionCircleRadius < 1)
                {
                    selectionCircleRadius = 1;
                }
                else if (selectionCircleRadius > 100)
                {
                    selectionCircleRadius = 100;
                }
                SceneView.RepaintAll();
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
            GUI.color = Color.white;
            Graphics.DrawTexture(
                new Rect(
                    mousePos.x - selectionCircleRadius,
                    mousePos.y - selectionCircleRadius,
                    selectCircleTexture.width,
                    selectCircleTexture.width),
                selectCircleTexture);

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                if (Event.current.button == 1)
                {
                    editOperation = MeshEditOperation.Standard;
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (vertMode == 0) // Select Verts
                {
                    if (Event.current.button == 0)
                    {
                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isVertCovered(i) || solid.isMeshTransparent)
                                {
                                    selectedVerts[i] = true;
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {

                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            if (selectedVerts[i])
                            {
                                Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                                float d = (mousePos - vertScreen).sqrMagnitude;
                                if (d < selectionCircleRadius * selectionCircleRadius)
                                {
                                    if (!solid.isVertCovered(i) || solid.isMeshTransparent)
                                    {
                                        selectedVerts[i] = false;
                                    }
                                }
                            }
                        }

                    }
                }
                else if (vertMode == 1) // Select Quads
                {
                    if (Event.current.button == 0)
                    {


                        for (int i = 0; i < selectedFaces.Length; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));

                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                {
                                    selectedFaces[i] = true;
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        for (int i = 0; i < selectedFaces.Length; i++)
                        {
                            if (selectedFaces[i])
                            {
                                Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));

                                float d = (mousePos - vertScreen).sqrMagnitude;
                                if (d < selectionCircleRadius * selectionCircleRadius)
                                {
                                    if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                    {
                                        selectedFaces[i] = false;
                                    }
                                }
                            }
                        }
                    }
                }
                SceneView.RepaintAll();
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
            #endregion
        }
        else if (editOperation == MeshEditOperation.LoopCut)
        {
            #region Loop Cut
            float loopCutMinDistance = 256 * 256;

            if (Event.current.type == EventType.ScrollWheel)
            {
                cutCount -= Event.current.delta.y / 3.0f;
                if (cutCount < 1)
                {
                    cutCount = 1;
                }
                if (cutCount > 20)
                {
                    cutCount = 20;
                }
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseMove)
            {
                // Get closest face to mouse
                float dist = float.MaxValue;
                int f = -1;

                bool areAnyCutsFound = false;

                for (int i = 0; i < selectedFaces.Length; i++)
                {
                    Vector3 face = solid.quadCenter(i);
                    Vector2 screen = HandleUtility.WorldToGUIPoint(face);

                    // Get NS/EW axis on face
                    if (Vector3.Dot(SceneView.lastActiveSceneView.camera.transform.forward, solid.faceNormals[i]) < 0 && !solid.isFaceCovered(i))
                    {
                        Vector3 n = (solid.verts[solid.quads[i * 4 + 0]] + solid.verts[solid.quads[i * 4 + 2]]) / 2;
                        Vector3 s = (solid.verts[solid.quads[i * 4 + 3]] + solid.verts[solid.quads[i * 4 + 1]]) / 2;
                        Vector2 vN = HandleUtility.WorldToGUIPoint(n);
                        Vector2 vS = HandleUtility.WorldToGUIPoint(s);
                        float d2NS = (MeshEdit.closestPoint(vN, vS, mousePos, true) - mousePos).sqrMagnitude;

                        Vector3 e = (solid.verts[solid.quads[i * 4 + 0]] + solid.verts[solid.quads[i * 4 + 1]]) / 2;
                        Vector3 w = (solid.verts[solid.quads[i * 4 + 3]] + solid.verts[solid.quads[i * 4 + 2]]) / 2;
                        Vector2 vE = HandleUtility.WorldToGUIPoint(e);
                        Vector2 vW = HandleUtility.WorldToGUIPoint(w);
                        float d2EW = (MeshEdit.closestPoint(vE, vW, mousePos, true) - mousePos).sqrMagnitude;
                        if (i == 0)
                        {
                        }
                        float min = Mathf.Min(d2NS, d2EW);
                        if (min < dist && min < loopCutMinDistance)
                        {
                            isNSLoopCut = (d2NS < d2EW);

                            dist = min;
                            f = i;
                            areAnyCutsFound = true;
                        }

                    }
                }

                closestFace = f * 4;
                facesCut = new List<int>();
                cutsAB = new List<int>();
                cutsAdjacentFaces = new List<int[]>();

                if (areAnyCutsFound)
                {
                    if (isNSLoopCut)
                    {
                        int face = f;
                        int a = 2;
                        int b = 0;
                        int c = -1;
                        int d = -1;

                        // Find the end of the loop before starting the cut, in case the mesh doesn't actually loop
                        solid.getFirstFaceInLoop(ref face, ref a, ref b, ref c, ref d);

                        solid.getOppositeSideOfQuadRelative(face, c, d, out c, out d);

                        isCutLooping = false;
                        //solid.getLoopCut(face, c, d, ref facesCut, ref cutsAB, ref isCutLooping);
                        solid.getLoopCut(face, c, d, ref facesCut, ref cutsAB, ref cutsAdjacentFaces, ref isCutLooping);
                        //solid.getLoopCutV2(f, false, ref facesCut, ref cutDirection, ref cutsAB, ref isCutLooping);
                    }
                    else
                    {
                        int face = f;
                        int a = 0;
                        int b = 1;
                        int c = -1;
                        int d = -1;

                        // Find the end of the loop before starting the cut, in case the mesh doesn't actually loop

                        solid.getFirstFaceInLoop(ref face, ref a, ref b, ref c, ref d);

                        solid.getOppositeSideOfQuadRelative(face, c, d, out c, out d);

                        //Debug.DrawLine((solid.verts[solid.quads[face * 4 + a]] + solid.verts[solid.quads[face * 4 + b]]) / 2, (solid.verts[solid.quads[face * 4 + c]] + solid.verts[solid.quads[face * 4 + d]]) / 2, Color.blue);

                        isCutLooping = false;
                        solid.getLoopCut(face, c, d, ref facesCut, ref cutsAB, ref cutsAdjacentFaces, ref isCutLooping);
                        //solid.getLoopCutV2(f, true, ref facesCut, ref tempAdjacentFaces, ref cutsAB, ref isCutLooping);

                    }

                    SceneView.RepaintAll();
                }
                else
                {
                    solid.cutsAB = new List<int>();
                    solid.facesCut = new List<int>();
                }
            }

            if (closestFace >= 0)
            {
                solid.facesCut = facesCut;
                solid.cutsAB = cutsAB;
                solid.cutCount = cutCount;
            }
            if (Event.current.type == EventType.MouseDown)
            {
                Undo.RegisterCompleteObjectUndo(solid, "Mesh Loop Cut");
                //Undo.RecordObject(solid.gameObject, "Mesh Loop Cut");

                if (Event.current.button == 0)
                {

                    loopCut(solid);

                    selectedFaces = new bool[selectedFaces.Length];
                    editOperation = MeshEditOperation.Standard;
                }
                if (Event.current.button == 1)
                {
                    editOperation = MeshEditOperation.Standard;
                }
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
            #endregion
        }
        else if (editOperation == MeshEditOperation.Standard)
        {
            #region Mesh Editing
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1)
                {
                    activateRightClick = true;

                    if (shift)
                    {
                        isAdditive = true;
                    }

                    if (transformMode > 0)
                    {
                        // TODO: Revert
                        solid.verts = oldVerts;
                        solid.updateMeshVerts();

                        transformDimensions.x = 1.0f;
                        transformDimensions.y = 1.0f;
                        transformDimensions.z = 1.0f;
                        activateRightClick = false;
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                }
                else if (Event.current.button == 0)
                {
                    if (transformMode == 0)
                    {
                        clearSelected(solid);
                        SceneView sceneView = (SceneView)SceneView.sceneViews[0];
                        sceneView.Focus();
                    }
                    //GUIUtility.hotControl = controlId;
                    //Event.current.Use();
                }
                if (transformMode > 0)
                {
                    transformMode = 0;
                    transformDimensions = new Vector3(0, 0, 0);
                    transformDimensionsPlanar = false;
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }



            float closestSelect = float.MaxValue;
            int closestVert = -1;
            int closestEdgeA = -1;
            int closestEdgeB = -1;
            bool wasALineSelected = false;
            Vector3 closestColPoint = Vector3.zero;
            
            #region Vertex/Face selection
            if (transformMode == 0)
            {
                if (activateRightClick)
                {

                    {
                        #region Standard selection
                        if (vertMode == 0)
                        {
                            if (Event.current.modifiers == (EventModifiers.Alt | EventModifiers.Shift) ||
                                Event.current.modifiers == EventModifiers.Alt)
                            {
                                loopDebugMemory4 = new List<Vector3>();
                                // Search for edges instead of verts
                                for (int i = 0; i < solid.quads.Count; i += 4)
                                {
                                    for (int j = 0; j < MeshEdit.quadEdgePatternClockwise.Length; j += 2)
                                    {
                                        int va = solid.quads[i + MeshEdit.quadEdgePatternClockwise[j + 0]];
                                        int vb = solid.quads[i + MeshEdit.quadEdgePatternClockwise[j + 1]];

                                        // TODO: Find a more elegant solution to this issue
                                        Vector2 a = HandleUtility.WorldToGUIPoint(solid.verts[va]);
                                        Vector2 b = HandleUtility.WorldToGUIPoint(solid.verts[vb]);

                                        Vector2 closest = closestPoint(a, b, mousePos);
                                        float d = (closest - mousePos).sqrMagnitude;

                                        if (d < selectDistance * selectDistance && 
                                            d < closestSelect)
                                        {

                                            float r = 0.0f;
                                            float dAB = (b - a).magnitude;
                                            float dAC = (closest - a).magnitude;
                                            if (d > 0)
                                            {
                                                r = dAC / dAB;
                                            }

                                            Vector3 coveredPoint = solid.verts[va] + (solid.verts[vb] - solid.verts[va]) * r;
                                            
                                            if (!solid.isMeshTransparent)
                                            {
                                                if (solid.isVertCovered(coveredPoint))
                                                {
                                                    continue;
                                                }
                                            }
                                            closestSelect = d;
                                            // The root face
                                            closestVert = i / 4;
                                            closestEdgeA = va;
                                            closestEdgeB = vb;
                                            wasALineSelected = true;

                                        }
                                    }
                                }
                            }
                            else
                            {

                                for (int i = 0; i < solid.verts.Count; i++)
                                {
                                    pos = HandleUtility.WorldToGUIPoint(solid.verts[i]);

                                    float d = Vector2.Distance(pos, mousePos);
                                    if (d < selectDistance && d < closestSelect)
                                    {
                                        closestSelect = d;
                                        closestVert = i;
                                    }
                                }
                            }
                        }
                        else if (vertMode == 1)
                        {
                            ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                            float dist2Intersection = float.MaxValue;

                            if (solid.tris != null)
                            {
                                Vector3 colPoint = Vector3.zero;
                                float d = float.MaxValue;
                                for (int i = 0; i < solid.tris.Count; i += 3)
                                {
                                    t = new MeshEdit.Triangle(
                                        solid.verts[solid.tris[i + 0]],
                                        solid.verts[solid.tris[i + 1]],
                                        solid.verts[solid.tris[i + 2]]);

                                    if (rayIntersectsTriangle(ray.origin, ray.direction, t, ref colPoint))
                                    {
                                        pos = HandleUtility.WorldToGUIPoint(colPoint);

                                        if (solid.isMeshTransparent)
                                        {
                                            float dd = Vector2.Distance(pos, HandleUtility.WorldToGUIPoint(solid.quadCenter(i / 6)));
                                            if (dd < d)
                                            {
                                                d = dd;
                                                if (solid.isMeshTransparent)
                                                {
                                                    closestVert = i / 6;
                                                    closestColPoint = colPoint;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            float dist2IntersectionTemp = Vector3.Distance(colPoint, ray.origin);
                                            if (dist2IntersectionTemp < dist2Intersection)
                                            {
                                                dist2Intersection = dist2IntersectionTemp;
                                                closestVert = i / 6;
                                                closestColPoint = colPoint;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (closestVert >= 0)
                        {
                            if (!shift) { clearSelected(solid); }

                            
                            if (wasALineSelected)
                            {
                                #region Edge Loop selection

                                List<int> loopPartial = getEdgeLoop(solid, closestVert, closestEdgeA, closestEdgeB);


                                bool select = true;
                                if (shift && selectedVerts[closestEdgeA] && selectedVerts[closestEdgeB])
                                {
                                    select = false;
                                }

                                for (int i = 0; i < loopPartial.Count; i++)
                                {
                                    setSelectVert(solid, loopPartial[i], select);
                                }
                                #endregion
                            }
                            else if (
                                vertMode == 1 &&
                                (Event.current.modifiers == (EventModifiers.Alt | EventModifiers.Shift) ||
                                Event.current.modifiers == EventModifiers.Alt))
                            {
                                #region Face Loop selection
                                Vector2 middleOfEdge0 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 0]] + solid.verts[solid.quads[closestVert * 4 + 1]]) / 2);
                                Vector2 middleOfEdge2 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 3]] + solid.verts[solid.quads[closestVert * 4 + 2]]) / 2);

                                Vector2 middleOfEdge1 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 1]] + solid.verts[solid.quads[closestVert * 4 + 3]]) / 2);
                                Vector2 middleOfEdge3 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 2]] + solid.verts[solid.quads[closestVert * 4 + 0]]) / 2);

                                float dNS = (closestPoint(middleOfEdge0, middleOfEdge2, mousePos) - mousePos).sqrMagnitude;
                                float dEW = (closestPoint(middleOfEdge1, middleOfEdge3, mousePos) - mousePos).sqrMagnitude;

                                bool isNS = true;

                                if (dEW < dNS)
                                {
                                    isNS = false;
                                }

                                List<int> addedFaces = MeshEdit.getFaceLoop(solid.adjacentFaces, closestVert, isNS);

                                if (selectedFaces[closestVert])
                                {
                                    for (int i = 0; i < addedFaces.Count; i++)
                                    {
                                        selectedFaces[addedFaces[i]] = false;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < addedFaces.Count; i++)
                                    {
                                        selectedFaces[addedFaces[i]] = true;
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                if (vertMode == 0)
                                {
                                    if (!solid.isVertCovered(closestVert) || solid.isMeshTransparent)
                                    {
                                        selectVert(solid, closestVert, isAdditive);
                                    }
                                }
                                else if (vertMode == 1)
                                {
                                    if (!solid.isFaceCovered(closestVert) || solid.isMeshTransparent)
                                    {
                                        selectedFaces[closestVert] = !isAdditive || !selectedFaces[closestVert];
                                    }
                                }
                            }

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        #endregion
                    }
                }
            }
            #endregion
            else if (transformMode == 1)
            {
                Ray r = HandleUtility.GUIPointToWorldRay(moveAnchor);

                Ray r2 = HandleUtility.GUIPointToWorldRay(mousePos);
                float d = Vector3.Distance(anchorCenter, SceneView.lastActiveSceneView.camera.transform.position);

                Vector2 offset = mousePos - moveAnchor;
                Vector3 realOffset =
                    SceneView.lastActiveSceneView.camera.transform.right * offset.x +
                    SceneView.lastActiveSceneView.camera.transform.up * -offset.y;

                realOffset = r.GetPoint(d) - r2.GetPoint(d);
                float s = 0;
                if (transformDimensionsExtrude)
                {
                    s = getValueFromMouseAxisPosition(transformDimensions, anchorCenter, moveAnchor, mousePos, false);
                    realOffset = transformDimensions * s;
                }
                if (transformDimensionsPlanar)
                {
                    Vector3 planeNormal = new Vector3(
                        1.0f - transformDimensions.x,
                        1.0f - transformDimensions.y,
                        1.0f - transformDimensions.z);

                    float planarDistanceToCamera = Vector3.Distance(
                        maskVector(SceneView.lastActiveSceneView.camera.transform.position, planeNormal),
                        maskVector(solid.transform.position, planeNormal));
                    float l2 = maskVector(r2.direction.normalized, planeNormal).magnitude;
                    float l = maskVector(r.direction.normalized, planeNormal).magnitude;

                    Vector3 mouseProjectionOntoPlane = r2.origin + (r2.direction.normalized / l2) * planarDistanceToCamera;
                    Vector3 anchorProjectionOntoPlane = r.origin + (r.direction.normalized / l) * planarDistanceToCamera;

                    realOffset = mouseProjectionOntoPlane - anchorProjectionOntoPlane;

                }

                // Snap
                if (ctrl)
                {
                    float d2target = (SceneView.lastActiveSceneView.camera.transform.position - anchorCenter).magnitude / 20;
                    d2target = Mathf.Round(d2target);

                    float scale = 2 * d2target;
                    if (scale < 1)
                    {
                        scale = 1;
                    }
                    if (shift)
                    {
                        scale /= 10;
                    }
                    if (!transformDimensionsExtrude)
                    {
                        realOffset.x = Mathf.Round(realOffset.x / scale) * scale;
                        realOffset.y = Mathf.Round(realOffset.y / scale) * scale;
                        realOffset.z = Mathf.Round(realOffset.z / scale) * scale;
                    }
                    else
                    {

                        realOffset = transformDimensions * (Mathf.Round(s / scale) * scale);
                    }
                }

                for (int i = 0; i < selectedVerts.Length; i++)
                {
                    if (selectedVerts[i])
                    {

                        if (transformDimensionsExtrude || transformDimensionsPlanar)
                        {
                            solid.verts[i] = oldVerts[i] + realOffset;
                        }
                        else
                        {
                            solid.verts[i] = oldVerts[i] - maskVector(realOffset, transformDimensions);
                        }
                    }
                }
                solid.updateMeshVerts(affectedVerts, affectedFaces);

            }
            else if (transformMode == 2)
            {
                Vector2 screenAnchorCenter = HandleUtility.WorldToGUIPoint(anchorCenter);

                Vector2 originOffset = (screenAnchorCenter - moveAnchor);

                Vector2 currentOffset = (screenAnchorCenter - mousePos);

                float angleOrigin = Mathf.Atan2(-originOffset.y, originOffset.x);
                float angleCurrent = Mathf.Atan2(-currentOffset.y, currentOffset.x);
                float angle = (angleCurrent - angleOrigin) * Mathf.Rad2Deg;

                Vector3 axis = SceneView.lastActiveSceneView.camera.transform.forward;

                if (transformDimensions.sqrMagnitude == 1)
                {
                    axis = transformDimensions;
                }

                // Snap
                if (ctrl)
                {
                    float scale = 180.0f / 8.0f;
                    if (shift)
                    {
                        scale /= 4;
                    }

                    angle = Mathf.Round(angle / scale) * scale;
                }
                Quaternion q = Quaternion.AngleAxis(angle, axis);

                for (int i = 0; i < selectedVerts.Length; i++)
                {
                    if (selectedVerts[i])
                    {
                        solid.verts[i] = anchorCenter + q * (oldVerts[i] - anchorCenter);
                    }
                }
                solid.updateMeshVerts(affectedVerts, affectedFaces);
            }
            else if (transformMode == 3)
            {
                Vector2 screenAnchorCenter = HandleUtility.WorldToGUIPoint(anchorCenter);

                float d2 = (screenAnchorCenter - moveAnchor).magnitude;

                float d = (screenAnchorCenter - mousePos).magnitude;

                float dot = Vector2.Dot(screenAnchorCenter - moveAnchor, screenAnchorCenter - mousePos);

                float s = d / d2;

                // Snap
                if (ctrl)
                {
                    float scale = 10;
                    if (shift)
                    {
                        scale *= 10;
                    }

                    s = Mathf.Round(s * scale) / scale;
                }
                for (int i = 0; i < selectedVerts.Length; i++)
                {
                    if (selectedVerts[i])
                    {
                        Vector3 newVert = Vector3.zero;
                        float sx = s;
                        if (transformDimensions.x == 0) { sx = 1; }
                        newVert.x = anchorCenter.x + (oldVerts[i] - anchorCenter).x * (sx) * Mathf.Sign(dot);
                        sx = s;
                        if (transformDimensions.y == 0) { sx = 1; }
                        newVert.y = anchorCenter.y + (oldVerts[i] - anchorCenter).y * (sx) * Mathf.Sign(dot);
                        sx = s;
                        if (transformDimensions.z == 0) { sx = 1; }
                        newVert.z = anchorCenter.z + (oldVerts[i] - anchorCenter).z * (sx) * Mathf.Sign(dot);

                        solid.verts[i] = newVert;
                    }
                }
                solid.updateMeshVerts(affectedVerts, affectedFaces);
            }

            if (transformMode > 0 && transformDimensions.sqrMagnitude > 0 && transformDimensions.sqrMagnitude < 3 && !transformDimensionsExtrude)
            {
                Vector3 x = new Vector3(1.0f, 0.0f, 0.0f) * transformDimensions.x;
                Vector3 y = new Vector3(0.0f, 1.0f, 0.0f) * transformDimensions.y;
                Vector3 z = new Vector3(0.0f, 0.0f, 1.0f) * transformDimensions.z;
                Debug.DrawLine(anchorCenter, anchorCenter + x * 1000000000000.0f, Color.red);
                Debug.DrawLine(anchorCenter, anchorCenter + x * -1000000000000.0f, Color.red);

                Debug.DrawLine(anchorCenter, anchorCenter + y * 1000000000000.0f, Color.green);
                Debug.DrawLine(anchorCenter, anchorCenter + y * -1000000000000.0f, Color.green);

                Debug.DrawLine(anchorCenter, anchorCenter + z * 1000000000000.0f, Color.blue);
                Debug.DrawLine(anchorCenter, anchorCenter + z * -1000000000000.0f, Color.blue);
            }

            if (Event.current.type == EventType.ExecuteCommand)
            {
                if (Event.current.commandName == "FrameSelected")
                {
                    Event.current.commandName = "";
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }
            if (Event.current.type == EventType.KeyDown)
            {
                bool areAnyVertsSelected = false;

                List<int> verticesSelected = new List<int>();

                bool areAnyFacesSelected = false;

                for (int i = 0; i < selectedFaces.Length; i++)
                {
                    if (selectedFaces[i])
                    {
                        areAnyVertsSelected = true;
                        areAnyFacesSelected = true;
                        break;
                    }
                }

                for (int i = 0; i < selectedVerts.Length; i++)
                {
                    if (selectedVerts[i])
                    {
                        areAnyVertsSelected = true;

                        bool isDuplicateVert = false;

                        // Check if the vert already belongs to the selected list
                        for (int ii = 0; ii < verticesSelected.Count; ii++)
                        {
                            if (solid.connectedVerts[verticesSelected[ii]].Contains(i))
                            {
                                isDuplicateVert = true;
                                break;
                            }
                        }
                        if (!isDuplicateVert) { verticesSelected.Add(i); }
                    }
                }

                // Loop Cut
                if (Event.current.keyCode == KeyCode.R && shift)
                {
                    if (editOperation == MeshEditOperation.LoopCut)
                    {
                        editOperation = MeshEditOperation.Standard;
                    }
                    else
                    {
                        editOperation = MeshEditOperation.LoopCut;
                    }
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }

                if (areAnyVertsSelected)
                {
                    if (Event.current.keyCode == KeyCode.Delete)
                    {
                        Undo.RegisterCompleteObjectUndo(solid, "Face Delete");

                        selectionConvertToFaces(solid);
                        
                        if (vertMode == 0 && !selectedFaces.Contains<bool>(true))
                        {
                            bool canDissolve = solid.dissolve(selectedVerts, selectedFaces);
                            
                            if (!canDissolve)
                            {
                                solid.delete(selectedVerts, selectedFaces);
                            }
                        }
                        else
                        {

                            solid.delete(selectedVerts, selectedFaces);

                            // GUIUtility.hotControl = controlId;
                        }
                        selectedVerts = new bool[solid.verts.Count];
                        selectedFaces = new bool[solid.quads.Count / 4];
                        Event.current.Use();
                    }

                    #region  Create Face / Flip Face
                    if (Event.current.keyCode == KeyCode.F)
                    {
                        bool flip = false;
                        if (verticesSelected.Count == 4 ||
                            verticesSelected.Count == 3 ||
                            verticesSelected.Count == 2)
                        {
                            // TODO: Check to make sure no faces are selected
                            selectionConvertToFaces(solid);
                            bool freeFace = true;
                            for (int i = 0; i < selectedFaces.Length; i++)
                            {
                                if (selectedFaces[i])
                                {
                                    freeFace = false;
                                    break;
                                }
                            }

                            if (freeFace)
                            {
                                Undo.RegisterCompleteObjectUndo(solid, "Mesh Face Create");

                                List<int> vertsToSelect = new List<int>();

                                if (verticesSelected.Count == 4)
                                {
                                    solid.addQuadBetweenFaces(
                                        verticesSelected[0],
                                        verticesSelected[1],
                                        verticesSelected[2],
                                        verticesSelected[3]);
                                }
                                else if (verticesSelected.Count == 3)
                                {
                                    solid.addQuadBetweenFaces(
                                        verticesSelected[0],
                                        verticesSelected[1],
                                        verticesSelected[2]);
                                }
                                else 
                                {
                                    clearSelected(solid);
                                    vertsToSelect = solid.addQuadBetweenFaces(
                                        verticesSelected[0],
                                        verticesSelected[1]);
                                }

                                solid.cleanVertConnections(solid.verts.Count - 1);
                                solid.cleanVertConnections(solid.verts.Count - 2);
                                solid.cleanVertConnections(solid.verts.Count - 3);
                                solid.cleanVertConnections(solid.verts.Count - 4);

                                solid.rebuildAdjacentFaces(0);

                                solid.pushNewGeometry();

                                updateSelectionArray(solid.verts.Count);

                                if (vertsToSelect != null)
                                {
                                    for (int i = 0; i < vertsToSelect.Count;i++)
                                    {
                                        setSelectVert(solid, vertsToSelect[i], true);
                                    }
                                }

                                //GUIUtility.hotControl = controlId;
                                Event.current.Use();
                            }
                            else
                            {
                                flip = true;
                                //Debug.Log("4 vertices and 0 faces must be selected to create a new face");
                            }

                        }
                        else
                        {
                            flip = true;
                        }

                        if (flip)
                        {
                            if (areAnyFacesSelected)
                            {
                                // Error: Undo only works 
                                Undo.RecordObject(solid, "Flip Face");
                                selectionConvertToFaces(solid);
                                solid.flipFaces(selectedFaces);

                                GUIUtility.hotControl = controlId;
                                Event.current.Use();
                            }
                        }


                    }
                    #endregion

                    #region Flip saddling
                    if (Event.current.keyCode == KeyCode.D)
                    {
                        if (areAnyFacesSelected)
                        {
                            Undo.RecordObject(solid, "Flip Saddling");
                            selectionConvertToFaces(solid);
                            solid.flipSaddling(selectedFaces);

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                    }
                    #endregion

                    #region Extrude
                    if (Event.current.keyCode == KeyCode.E && transformMode == 0)
                    {
                        // 1. Extrude function runs on selected object
                        Undo.RegisterCompleteObjectUndo(solid, "Extrusion");
                        /*

                        Undo.RecordObject(solid, "Mesh Extrusion step");
                        Undo.RecordObject(solid.gameObject, "Mesh Extrusion step");*/
                        if (vertMode == 1)
                        {
                            selectionConvertToVerts(solid);
                        }
                        selectionConvertToFaces(solid);
                        selectionAddTouchingVerts(solid);

                        bool[] selectedVertsTemp = new bool[selectedVerts.Length];
                        for (int i = 0; i < selectedVerts.Length; i++)
                        {
                            selectedVertsTemp[i] = selectedVerts[i];
                        }

                        Vector3 n = Vector3.zero;
                        float nCount = 0;
                        for (int i = 0; i < selectedFaces.Length; i++)
                        {

                            if (selectedFaces[i])
                            {
                                n += solid.faceNormals[i];
                                nCount++;
                            }
                        }

                        transformDimensionsExtrude = true;
                        transformDimensions = n.normalized;

                        if (transformDimensions.sqrMagnitude == 0)
                        {
                            transformDimensionsExtrude = false;
                        }

                        // TODO: If no faces, only verts are selected, extrude based on direction of corner verts

                        // Extrude
                        List<int> faceRefs = new List<int>();
                        List<int> selectedEdges = getSelectedPerimeter(solid, selectedVerts, selectedFaces, out faceRefs, vertMode);

                        selectedVerts = solid.extrude(selectedEdges, faceRefs, selectedVerts, selectedFaces, transformDimensions);

                        updateSelectionArray(selectedVerts.Length);

                        // 2. Interface switches to Movement, of type "Movement along an axis 'a'"

                        activateTransformMode(solid, 1);

                        // Add the original verts to the list of verts that should have their normals updated
                        for (int i = 0; i < affectedVerts.Length; i++)
                        {
                            affectedVerts[i] = affectedVerts[i] || selectedVertsTemp[i];
                        }

                        moveAnchor = constrainToScreenSize(mousePos);

                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }

                    #endregion
                    if (Event.current.keyCode == KeyCode.G)
                    {
                        if (transformDimensionsExtrude)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsExtrude = false;
                        }
                        if (transformMode != 1)
                        {
                            if (vertMode == 1)
                            {
                                selectionConvertToVerts(solid);
                            }
                            selectionConvertToFaces(solid);
                            selectionAddTouchingVerts(solid);

                            activateTransformMode(solid, 1);
                        }
                        else if (transformMode == 1)
                        {
                            transformMode = 0;
                        }

                        moveAnchor = constrainToScreenSize(mousePos);
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }


                    if (Event.current.type == EventType.KeyDown &&
                        Event.current.keyCode == KeyCode.R)
                    {
                        if (transformDimensionsExtrude)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsExtrude = false;
                        }
                        if (transformMode != 2)
                        {
                            if (vertMode == 1)
                            {
                                selectionConvertToVerts(solid);
                            }
                            selectionConvertToFaces(solid);
                            selectionAddTouchingVerts(solid);

                            activateTransformMode(solid, 2);
                        }
                        else if (transformMode == 2)
                        {
                            transformMode = 0;
                        }
                        moveAnchor = constrainToScreenSize(mousePos);
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    if (Event.current.keyCode == KeyCode.S)
                    {
                        if (transformDimensionsExtrude)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsExtrude = false;
                        }
                        if (transformMode != 3)
                        {
                            if (vertMode == 1)
                            {
                                selectionConvertToVerts(solid);
                            }
                            selectionConvertToFaces(solid);
                            selectionAddTouchingVerts(solid);

                            activateTransformMode(solid, 3);
                        }
                        else if (transformMode == 3)
                        {
                            transformMode = 0;
                        }
                        moveAnchor = constrainToScreenSize(mousePos);
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    if (transformMode > 0)
                    {
                        Vector3 oldTransformDimensions = transformDimensions;
                        bool dimensionsChanged = false;

                        if (Event.current.keyCode == KeyCode.X)
                        {
                            if (shift && transformMode != 2)
                            {
                                transformDimensions = new Vector3(0.0f, 1.0f, 1.0f);
                                transformDimensionsPlanar = true;
                            }
                            else
                            {
                                transformDimensions = new Vector3(1.0f, 0.0f, 0.0f);
                                transformDimensionsPlanar = false;
                            }


                            dimensionsChanged = true;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        if (Event.current.keyCode == KeyCode.Z)
                        {
                            if (shift && transformMode != 2)
                            {
                                transformDimensions = new Vector3(1.0f, 0.0f, 1.0f);
                                transformDimensionsPlanar = true;
                            }
                            else
                            {
                                transformDimensions = new Vector3(0.0f, 1.0f, 0.0f);
                                transformDimensionsPlanar = false;
                            }

                            dimensionsChanged = true;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        if (Event.current.keyCode == KeyCode.Y)
                        {
                            if (shift && transformMode != 2)
                            {
                                transformDimensions = new Vector3(1.0f, 1.0f, 0.0f);
                                transformDimensionsPlanar = true;
                            }
                            else
                            {
                                transformDimensions = new Vector3(0.0f, 0.0f, 1.0f);
                                transformDimensionsPlanar = false;
                            }

                            dimensionsChanged = true;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }

                        if (dimensionsChanged &&
                            oldTransformDimensions == transformDimensions)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsPlanar = false;
                        }
                    }
                }
                // Selection
                {
                    if (Event.current.keyCode == KeyCode.C && Event.current.modifiers == EventModifiers.None)
                    {
                        if (editOperation != MeshEditOperation.SelectCircle)
                        {
                            editOperation = MeshEditOperation.SelectCircle;
                        }
                        else
                        {
                            editOperation = MeshEditOperation.Standard;
                        }

                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    if (Event.current.keyCode == KeyCode.A && Event.current.modifiers == EventModifiers.None)
                    {
                        bool isFull = true;
                        if (vertMode == 1)
                        {
                            for (int i = 0; i < selectedVerts.Length; i++)
                            {
                                if (!selectedVerts[i])
                                {
                                    isFull = false;
                                    break;
                                }
                            }
                        }
                        else if (vertMode == 0)
                        {
                            for (int i = 0; i < selectedFaces.Length; i++)
                            {
                                if (!selectedFaces[i])
                                {
                                    isFull = false;
                                    break;
                                }
                            }
                        }

                        for (int i = 0; i < selectedVerts.Length; i++)
                        {
                            selectedVerts[i] = !isFull;
                        }
                        for (int i = 0; i < selectedFaces.Length; i++)
                        {
                            selectedFaces[i] = !isFull;
                        }
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.L )
                    {
                        Vector3 origin = SceneView.lastActiveSceneView.camera.transform.position;
                        Vector3 colPoint = Vector3.zero;
                        float d = float.MaxValue;
                        int closestFace = -1;

                        solid.beginTimer("Find nearest face to mouse");
                        ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                        for (int i = 0; i < solid.tris.Count; i += 3)
                        {
                            t = new MeshEdit.Triangle(
                                solid.verts[solid.tris[i + 0]],
                                solid.verts[solid.tris[i + 1]],
                                solid.verts[solid.tris[i + 2]]);

                            if (rayIntersectsTriangle(ray.origin, ray.direction, t, ref colPoint))
                            {
                                float dd = (colPoint - origin).sqrMagnitude;

                                if (dd < d)
                                {
                                    d = dd;
                                    closestFace = i / 6;
                                }
                            }
                        }
                        solid.endTimer();

                        solid.beginTimer("Select all connected verts and faces ");

                        if (!shift)
                        {
                            clearSelected(solid);
                        }

                        if (closestFace >= 0)
                        {
                            selectedFaces[closestFace] = true;
                            selectAllAdjacentFaces(solid, closestFace);
                        }

                        selectionConvertToVerts(solid);

                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                }

                // Use the default delete key on any other mode to prevent the model from being accidentally deleted
                if (Event.current.keyCode == KeyCode.Delete && editMode != 0)
                {
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }



            #region Copy/Paste
            /*
            if (Event.current.type == EventType.ValidateCommand)
            {
                if (Event.current.commandName == "Cut")
                {
                    solid.beginTimer("Copy faces for cut");
                    Undo.RegisterCompleteObjectUndo(solid, "Cut faces");
                    selectionConvertToFaces(solid);
                    EditorGUIUtility.systemCopyBuffer = solid.getCopyString(selectedFaces);
                    solid.endTimer();

                    selectedVerts = new bool[selectedVerts.Length];
                    selectionConvertToVerts(solid);
                    solid.delete(selectedVerts, selectedFaces);
                    selectedVerts = new bool[solid.verts.Count];
                    selectedFaces = new bool[solid.quads.Count / 4];

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.ExecuteCommand)
            {
                if (Event.current.commandName == "Copy")
                {

                    solid.beginTimer("Copy faces");
                    selectionConvertToFaces(solid);
                    EditorGUIUtility.systemCopyBuffer = solid.getCopyString(selectedFaces);
                    solid.endTimer();

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                else if (Event.current.commandName == "Paste")
                {
                    solid.beginTimer("Pasting faces");
                    Undo.RegisterCompleteObjectUndo(solid, "Paste faces");
                    int vertCount = solid.verts.Count;
                    int faceCount = solid.quads.Count / 4;
                    if (!solid.pasteData(EditorGUIUtility.systemCopyBuffer))
                    {
                        Debug.Log("Copied data is not formatted correctly. Cannot paste data from clipboard.");
                    }
                    else
                    {
                        selectedFaces = new bool[solid.quads.Count / 4];
                        selectedVerts = new bool[solid.verts.Count];

                        for (int i = faceCount; i < solid.quads.Count / 4; i++)
                        {
                            selectedFaces[i] = true;
                        }
                        for (int i = vertCount; i < solid.verts.Count; i++)
                        {
                            selectedVerts[i] = true;
                        }
                    }
                    solid.endTimer();

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }*/
            Event e = Event.current;
            if (e.type == EventType.ValidateCommand && e.commandName == "Copy")
            {
                e.Use(); // without this line we won't get ExecuteCommand
            }

            if (e.type == EventType.ExecuteCommand && e.commandName == "Copy")
            {
                copy(solid);
                e.Use();
            }

            if (e.type == EventType.ValidateCommand && e.commandName == "Cut")
            {
                e.Use(); // without this line we won't get ExecuteCommand
            }

            if (e.type == EventType.ExecuteCommand && e.commandName == "Cut")
            {
                cut(solid);
                e.Use();
            }

            if (e.type == EventType.ValidateCommand && e.commandName == "Paste")
            {
                e.Use(); // without this line we won't get ExecuteCommand
            }

            if (e.type == EventType.ExecuteCommand && e.commandName == "Paste")
            {
                paste(solid);
                e.Use();
            }

            #endregion



            if (Event.current.type == EventType.MouseDown)
            {
                //Debug.Log("MP: " + mousePos.ToString("f4"));
                if (Event.current.button == 1)
                {
                    //Tools.viewTool = ViewTool.FPS;
                    //Tools.current = Tool.View;
                    // GUIUtility.hotControl = controlId;
                    // Event.current.Use();
                }
            }

            #endregion

            //Event.current.modifiers == EventModifiers.Alt;
            //Event.current.modifiers = EventModifiers.None;
            //Tools.current = Tool.Move;
        }
    }

    private void cut(MeshEdit solid)
    {
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                solid.beginTimer("Copy faces for cut");
                Undo.RegisterCompleteObjectUndo(solid, "Cut faces");
                selectionConvertToFaces(solid);
                EditorGUIUtility.systemCopyBuffer = solid.getCopyString(selectedFaces);
                solid.endTimer();

                selectedVerts = new bool[selectedVerts.Length];
                selectionConvertToVerts(solid);
                solid.delete(selectedVerts, selectedFaces);
                selectedVerts = new bool[solid.verts.Count];
                selectedFaces = new bool[solid.quads.Count / 4];
            }
        }
    }

    private int breakCount = 0;
    private void selectAllAdjacentFaces(MeshEdit solid, int face)
    {
        int adjacentFace = -1;

        breakCount++;
        if (breakCount > 10000)
        {
            return;
        }

        for (int i = 0; i < solid.adjacentFaces[face].list.Count; i++)
        {
            adjacentFace = solid.adjacentFaces[face].list[i];

            if (adjacentFace >= 0 && 
                !selectedFaces[adjacentFace])
            {
                selectedFaces[adjacentFace] = true;
                selectAllAdjacentFaces(solid, adjacentFace);
            }
        }
    }

    private void copy(MeshEdit solid)
    {
        solid.beginTimer("Copy faces");
        Undo.RegisterCompleteObjectUndo(solid, "Copy faces");
        selectionConvertToFaces(solid);
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                EditorGUIUtility.systemCopyBuffer = solid.getCopyString(selectedFaces);
                break;
            }
        }
        solid.endTimer();
    }

    private void paste(MeshEdit solid)
    {
        solid.beginTimer("Pasting faces");
        {
            Undo.RegisterCompleteObjectUndo(solid, "Paste faces");
            int vertCount = solid.verts.Count;
            int faceCount = solid.quads.Count / 4;
            if (!solid.pasteData(EditorGUIUtility.systemCopyBuffer))
            {
                Debug.Log("Copied data is not formatted correctly. Cannot paste data from clipboard.");
            }
            else
            {
                selectedFaces = new bool[solid.quads.Count / 4];
                selectedVerts = new bool[solid.verts.Count];

                for (int i = faceCount; i < solid.quads.Count / 4; i++)
                {
                    selectedFaces[i] = true;
                }
                for (int i = vertCount; i < solid.verts.Count; i++)
                {
                    selectedVerts[i] = true;
                }
            }
        }

        solid.endTimer();
    }

    private void loopCut(MeshEdit solid)
    {

        // Add new quads
        List<int> facesCutAsIndices = new List<int>();
        List<List<Vector2>> facesCutAsUVs = new List<List<Vector2>>();
        List<List<Vector2>> newUvs = new List<List<Vector2>>();

        if (solid.uvMaps != null && solid.uvMaps.Count > 0)
        {
            for (int u = 0; u < solid.uvMaps.Count; u++)
            {
                newUvs.Add(new List<Vector2>());
                facesCutAsUVs.Add(new List<Vector2>());
            }
        }

        for (int i = 0; i < facesCut.Count; i++)
        {
            // These get overwritten in the next loop
            facesCutAsIndices.Add(0);
            facesCutAsIndices.Add(0);
            facesCutAsIndices.Add(0);
            facesCutAsIndices.Add(0);
            if (solid.uvMaps != null && solid.uvMaps.Count > 0)
            {
                for (int u = 0; u < solid.uvMaps.Count; u++)
                {
                    facesCutAsUVs[u].Add(Vector2.zero);
                    facesCutAsUVs[u].Add(Vector2.zero);
                    facesCutAsUVs[u].Add(Vector2.zero);
                    facesCutAsUVs[u].Add(Vector2.zero);
                }
            }

            int parityCount = 0;
            for (int ii = 0; ii < solid.verts.Count; ii++)
            {
                int faceIndex = solid.quads.IndexOf(ii);
                int face = faceIndex / 4;
                int vert = faceIndex % 4;

                if (facesCut[i] == face)
                {
                    for (int iii = 0; iii < 4; iii++)
                    {
                        if (ii == cutsAB[i * 4 + iii])// solid.verts[solid.quads[facesCut[i] * 4 + iii]])
                        {

                            facesCutAsIndices[i * 4 + iii] = ii;

                            parityCount++;


                            if (solid.uvMaps != null && solid.uvMaps.Count > 0)
                            {
                                for (int u = 0; u < solid.uvMaps.Count; u++)
                                {
                                    facesCutAsUVs[u][i * 4 + iii] = solid.uvMaps[u].uvs[ii];
                                }
                            }
                        }
                    }
                }
            }

            if (parityCount != 4)
            {
                Debug.Log("Parity Error on loop cut!");
                // return;
            }
        }
        int faceLoops = Mathf.FloorToInt(cutCount) + 1;
        bool cutUVsWithoutReapplying = true;

        int newFacesToBeCreated = facesCut.Count * faceLoops;
        int newVertsToBeCreated = newFacesToBeCreated * 4;
        int vertsToBeDeletedInPost = facesCut.Count * 4;
        int vertsBeforeAddingNewFaces = solid.verts.Count;
        int facesBeforeAddingNewFaces = solid.faceNormals.Count;
        
        for (int i = 0; i < facesCut.Count; i++)
        {
            float sep = 1.0f / (faceLoops);
            for (int ii = 0; ii < faceLoops; ii++)
            {
                #region Diagram
                /*
                 *   ------------> ii
                 *  | ..._____ __________________ __________________ ______...
                 *  |  / / / | |a"     [0]     b" |                | |/ / / /
                 *  |   / / /| |                | |                | | / / / 
                 *  |  / / / | |    i=0         | |  i=0           | |/ / / /
                 *  |   / / /| [3]  ii=0       [1]|  ii=1          | | / / /   <--facesCut 0
                 * \/  / / / | |                | |                | |/ / / /
                 *  i   / / /| |                | |                | | / / / 
                 *    ...____| |c"_____[2]_____d" |________________| |_____...
                 *    ..._____ __________________ __________________ ______...  <-Pre-existing edge
                 *     / / / | |a"             b" |                | |/ / / /
                 *      / / /| |                | |                | | / / / 
                 *     / / / | |                | |                | |/ / / /
                 *      / / /| |                | |                | | / / /   <--facesCut 1
                 *     / / / | |                | |                | |/ / / /
                 *      / / /| |                | |                | | / / / 
                 *    ...____| |c"_____________d" |________________| |_____...
                 *         ^           ^         ^         ^        ^
                 *         |           |         |         |        |
                 *     ratio = 0.0     |     ratio = 0.5   |    ratio = 1.0
                 *                     |         |         |
                 *                 faceLoop 0    |     faceLoop 1
                 *                               |
                 *                          A single cut
                 */
                #endregion

                float ratio = sep * ii;

                int currentNewFace = (ii + faceLoops * i);

                //Debug.Log("FC:" + "{ " + cutsAdjacentFaces[i][0] + ", " + cutsAdjacentFaces[i][1] + ", " + cutsAdjacentFaces[i][2] + ", " + cutsAdjacentFaces[i][3] + "}");

                #region Create new list of adjacent faces
                List<int>[] newConnectedVerts = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

                bool addLeftSideVerts = true;
                bool addRightSideVerts = true;

                if (ii == 0)
                {
                    addLeftSideVerts = false;
                    // Adjacent Edge/Face
                    int face = solid.getEdgeIndexFromFaceVerts(facesCut[i], cutsAB[i * 4 + 0], cutsAB[i * 4 + 2]);
                   
                    if (face != -1)
                    {
                        face = solid.adjacentFaces[facesCut[i]].list[face];

                        if (face != -1)
                        {
                            int reverseFaceEdge = solid.adjacentFaces[face].list.IndexOf(facesCut[i]);
                            solid.adjacentFaces[face].list[reverseFaceEdge] = facesBeforeAddingNewFaces + currentNewFace;
                        }
                    }
                    else
                    {
                        Debug.Log("Error in loop cut. Faces not aligned at edge.");
                    }
                   // adjacentFaces.list[3] = face;

                }
                else if (ii == faceLoops - 1)
                {
                    addRightSideVerts = false;
                    int face = solid.getEdgeIndexFromFaceVerts(facesCut[i], cutsAB[i * 4 + 1], cutsAB[i * 4 + 3]);
                    if (face != -1)
                    {
                        face = solid.adjacentFaces[facesCut[i]].list[face];

                        if (face != -1)
                        {
                            int reverseFaceEdge = solid.adjacentFaces[face].list.IndexOf(facesCut[i]);
                            solid.adjacentFaces[face].list[reverseFaceEdge] = facesBeforeAddingNewFaces + currentNewFace;
                        }
                    }
                    else
                    {
                        Debug.Log("Error in loop cut. Faces not aligned at edge.");
                    }

                  //  adjacentFaces.list[1] = face;
                }

                #endregion
                #region Create new list of connected verts

                int upDirection = newVertsToBeCreated - faceLoops;
                int downDirection = faceLoops;

                bool isDownBlocked = i == 0 && cutsAdjacentFaces[i][2] == -1;
                bool isUpBlocked = i == facesCut.Count - 1 && cutsAdjacentFaces[i][0] == -1;

                if (!isDownBlocked)
                {
                    newConnectedVerts[2].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection) * 4 + 0) % newVertsToBeCreated)); // Bottom
                }
                if (!isUpBlocked)
                {
                    newConnectedVerts[0].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection) * 4 + 2) % newVertsToBeCreated)); // Top
                }
                if (addLeftSideVerts)
                {
                    // Top Left vert
                    newConnectedVerts[0].Add(vertsBeforeAddingNewFaces + (((currentNewFace - 1) * 4 + 1) % newVertsToBeCreated)); // Left
                    if (!isUpBlocked) { newConnectedVerts[0].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection - 1) * 4 + 3) % newVertsToBeCreated)); }// Top-Left

                    // Bottom Left vert
                    newConnectedVerts[2].Add(vertsBeforeAddingNewFaces + (((currentNewFace - 1) * 4 + 3) % newVertsToBeCreated)); // Left
                    if (!isDownBlocked) { newConnectedVerts[2].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection - 1) * 4 + 1) % newVertsToBeCreated)); } // Bottom-Left

                }
                else
                {
                    // Connected Verts, A and C If they lie against the "rato=0.0" side quad
                    for (int repeat = 0; repeat < 2; repeat++)
                    {
                        for (int j = 0; j < solid.connectedVerts[cutsAB[i * 4 + repeat * 2]].Count; j++)
                        {
                            // Check if the vert is part of a face that will be deleted at the end of this method
                            // If it is, set it to the matching vert of the new face
                            // Same algorithm as face adjacency, but multiplied by four, + whatever index the vert should be
                            // Do not assume that the connected vert will be whatever comes immedately after in the list of facesCut
                            // Instead, calculate it based on the index of the face in the facesCutList, which will get a definite index for where the new vert will be
                            int newVert = solid.connectedVerts[cutsAB[i * 4 + repeat * 2]].list[j];
                            int newVertFace = solid.getFaceFromVert(newVert);
                            int indexOfNewVertFaceInCutsAB = facesCut.IndexOf(newVertFace);
                            if (indexOfNewVertFaceInCutsAB == -1)
                            {
                                if (newVert < vertsBeforeAddingNewFaces)
                                {
                                    newConnectedVerts[repeat * 2].Add(newVert);
                                    solid.connectedVerts[newVert].Add(vertsBeforeAddingNewFaces + currentNewFace * 4 + repeat * 2);
                                }
                            }
                        }
                    }
                }

                if (!isDownBlocked)
                {
                    newConnectedVerts[3].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection) * 4 + 1) % newVertsToBeCreated)); // Bottom
                }
                if (!isUpBlocked)
                {
                    newConnectedVerts[1].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection) * 4 + 3) % newVertsToBeCreated)); // Top
                }
                if (addRightSideVerts)
                {
                    // Top Right vert
                    newConnectedVerts[1].Add(vertsBeforeAddingNewFaces + (((currentNewFace + 1) * 4 + 0) % newVertsToBeCreated)); // Right
                    if (!isUpBlocked) { newConnectedVerts[1].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection + 1) * 4 + 2) % newVertsToBeCreated)); }// Top-Right 

                    // Bottom Right vert
                    newConnectedVerts[3].Add(vertsBeforeAddingNewFaces + (((currentNewFace + 1) * 4 + 2) % newVertsToBeCreated)); // Right
                    if (!isDownBlocked) { newConnectedVerts[3].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection + 1) * 4 + 0) % newVertsToBeCreated)); }// Bottom-Right
                }
                else
                {
                    // Connected Verts, B and D If they lie against the "rato=0.0" side quad
                    for (int repeat = 0; repeat < 2; repeat++)
                    {
                        for (int j = 0; j < solid.connectedVerts[cutsAB[i * 4 + repeat * 2 + 1]].Count; j++)
                        {
                            // Check if the vert is part of a face that will be deleted at the end of this method
                            // If it is, set it to the matching vert of the new face
                            // Same algorithm as face adjacency, but multiplied by four, + whatever index the vert should be
                            // Do not assume that the connected vert will be whatever comes immedately after in the list of facesCut
                            // Instead, calculate it based on the index of the face in the facesCutList, which will get a definite index for where the new vert will be
                            int newVert = solid.connectedVerts[cutsAB[i * 4 + repeat * 2 + 1]].list[j];
                            int newVertFace = solid.getFaceFromVert(newVert);
                            int indexOfNewVertFaceInCutsAB = facesCut.IndexOf(newVertFace);
                            if (indexOfNewVertFaceInCutsAB == -1)
                            {
                                if (newVert < vertsBeforeAddingNewFaces)
                                {
                                    newConnectedVerts[repeat * 2 + 1].Add(newVert);
                                    solid.connectedVerts[newVert].Add(vertsBeforeAddingNewFaces + currentNewFace * 4 + repeat * 2 + 1);
                                }
                            }
                        }
                    }
                }

                for (int j = 0; j < 4; j++)
                {
                    solid.connectedVerts.Add(new MeshEdit.ListWrapper(newConnectedVerts[j]));
                }
                #endregion

                Vector3 v0 = solid.verts[cutsAB[i * 4 + 0]] + (solid.verts[cutsAB[i * 4 + 1]] - solid.verts[cutsAB[i * 4 + 0]]) * ratio;
                Vector3 v1 = solid.verts[cutsAB[i * 4 + 0]] + (solid.verts[cutsAB[i * 4 + 1]] - solid.verts[cutsAB[i * 4 + 0]]) * (ratio + sep);
                Vector3 v2 = solid.verts[cutsAB[i * 4 + 2]] + (solid.verts[cutsAB[i * 4 + 3]] - solid.verts[cutsAB[i * 4 + 2]]) * ratio;
                Vector3 v3 = solid.verts[cutsAB[i * 4 + 2]] + (solid.verts[cutsAB[i * 4 + 3]] - solid.verts[cutsAB[i * 4 + 2]]) * (ratio + sep);
                        Vector3 n = solid.faceNormals[facesCut[i]];

                #region AddQuad

                int vertCount = solid.verts.Count;

                solid.verts.Add(v0);
                solid.verts.Add(v1);
                solid.verts.Add(v2);
                solid.verts.Add(v3);

                solid.colours.Add(Color.white);
                solid.colours.Add(Color.white);
                solid.colours.Add(Color.white);
                solid.colours.Add(Color.white);

                solid.vertNormals.Add(n);
                solid.vertNormals.Add(n);
                solid.vertNormals.Add(n);
                solid.vertNormals.Add(n);

                solid.faceNormals.Add(n);



                MeshEdit.ListWrapper adjacentFaces = new MeshEdit.ListWrapper();

                // If the cut loops back on itself
                adjacentFaces = new MeshEdit.ListWrapper(new int[] {
                    facesBeforeAddingNewFaces + (ii + faceLoops * (i + 1)) % newFacesToBeCreated,
                    facesBeforeAddingNewFaces + (ii + faceLoops * i + 1),
                    facesBeforeAddingNewFaces + (ii + faceLoops * (i + (facesCut.Count - 1))) % newFacesToBeCreated,
                    facesBeforeAddingNewFaces + (ii + faceLoops * i - 1) });
                
                if (i == 0 && cutsAdjacentFaces[i][2] == -1)
                {
                    adjacentFaces.list[2] = -1;
                }
                if (i == facesCut.Count - 1 && cutsAdjacentFaces[i][0] == -1)
                {
                    adjacentFaces.list[0] = -1;
                }
                if (ii == 0)
                {
                    adjacentFaces.list[3] = cutsAdjacentFaces[i][3];
                }
                if (ii == faceLoops - 1)
                {
                    adjacentFaces.list[1] = cutsAdjacentFaces[i][1];
                }
                
                bool flipped = false;
                // Test the normals, if they don't face outward, flip the triangles. (0, 3 are the verts grabbed)
                Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
                if (Vector3.Dot(cross, n) < 0)
                {
                    flipped = true;
                    solid.tris.Add(vertCount + 0); // a (v1)
                    solid.tris.Add(vertCount + 2); // b (v0)
                    solid.tris.Add(vertCount + 1); // c (v2)

                    solid.tris.Add(vertCount + 3); // a (v1)
                    solid.tris.Add(vertCount + 1); // b
                    solid.tris.Add(vertCount + 2); // c

                    solid.quads.Add(vertCount + 0);
                    solid.quads.Add(vertCount + 2);
                    solid.quads.Add(vertCount + 1);
                    solid.quads.Add(vertCount + 3);

                    int tempAL0 = adjacentFaces.list[0];
                    int tempAL1 = adjacentFaces.list[1];
                    adjacentFaces.list[0] = adjacentFaces.list[3];
                    adjacentFaces.list[1] = adjacentFaces.list[2];
                    adjacentFaces.list[2] = tempAL1;
                    adjacentFaces.list[3] = tempAL0;
                }
                else
                {
                    solid.tris.Add(vertCount + 0); // a (v1)
                    solid.tris.Add(vertCount + 1); // b (v0)
                    solid.tris.Add(vertCount + 2); // c (v2)
                    solid.tris.Add(vertCount + 3); // a (v1)
                    solid.tris.Add(vertCount + 2); // b
                    solid.tris.Add(vertCount + 1); // c

                    solid.quads.Add(vertCount + 0);
                    solid.quads.Add(vertCount + 1);
                    solid.quads.Add(vertCount + 2);
                    solid.quads.Add(vertCount + 3);


                }


                // Resize all data structures representing this mesh to fit the new mesh size.
                for (int uvi = 0; uvi < solid.uvMaps.Count; uvi++)
                {
                    solid.uvMaps[uvi].resizeUVLength(vertCount + 4);//, newVerts);
                }

                solid.adjacentFaces.Add(adjacentFaces);

                #endregion

                int c = solid.colours.Count - 4;
                if (ii == 0)
                {
                    // a side
                    solid.colours[c + 0] = solid.colours[facesCutAsIndices[i * 4 + 0]];
                    solid.colours[c + 2] = solid.colours[facesCutAsIndices[i * 4 + 2]];
                }
                if (ii == faceLoops - 1)
                {
                    // b side
                    solid.colours[c + 1] = solid.colours[facesCutAsIndices[i * 4 + 1]];
                    solid.colours[c + 3] = solid.colours[facesCutAsIndices[i * 4 + 3]];
                }

                if (solid.uvMaps != null && solid.uvMaps.Count > 0)
                {
                    if (cutUVsWithoutReapplying)
                    {
                        for (int u = 0; u < solid.uvMaps.Count; u++)
                        {
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 0] + (facesCutAsUVs[u][i * 4 + 1] - facesCutAsUVs[u][i * 4 + 0]) * ratio);
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 0] + (facesCutAsUVs[u][i * 4 + 1] - facesCutAsUVs[u][i * 4 + 0]) * (ratio + sep));
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 2] + (facesCutAsUVs[u][i * 4 + 3] - facesCutAsUVs[u][i * 4 + 2]) * ratio);
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 2] + (facesCutAsUVs[u][i * 4 + 3] - facesCutAsUVs[u][i * 4 + 2]) * (ratio + sep));
                        }
                    }
                }
            }

        }

        updateSelectionArray(solid.verts.Count);

        // Delete old quads
        selectedVerts = new bool[selectedVerts.Length];
        selectedFaces = new bool[selectedFaces.Length];
        for (int i = 0; i < facesCut.Count; i++)
        {
            selectedFaces[facesCut[i]] = true;
        }
        solid.delete(selectedVerts, selectedFaces);


        updateSelectionArray(solid.verts.Count);


        // Push new UVS onto the stack
        if (solid.uvMaps != null && solid.uvMaps.Count > 0)
        {
            if (cutUVsWithoutReapplying)
            {
                for (int u = 0; u < solid.uvMaps.Count; u++)
                {
                    solid.uvMaps[u].resizeUVLength(selectedVerts.Length);

                    int startIndex = solid.uvMaps[u].vertCount - newUvs[u].Count;

                    for (int i = 0; i < newUvs[u].Count; i++)
                    {
                        solid.uvMaps[u]._uvs[startIndex + i] = newUvs[u][i];
                        solid.uvMaps[u]._newUvs[startIndex + i] = newUvs[u][i];
                    }
                }
            }
        }


        //solid.rebuildAdjacentFaces(0);

        solid.pushNewGeometry();
    }

    public static Vector3 closestPoint(Vector3 a, Vector3 b, Vector3 o)
    {
        float d = Vector3.SqrMagnitude(b - a);

        if (d > 0)
        {
            Vector3 ao = o - a;
            Vector3 ab = b - a;
            float dot = Vector3.Dot(ao, ab) / d;
            if (dot <= 0) { return a; }
            else if (dot >= 1) { return b; }
            else
            {
                return a + ab * dot;
            }
        }
        else
        {
            return a;
        }
    }

    public static Vector2 closestPoint(Vector2 a, Vector2 b, Vector2 o)
    {
        float d = Vector2.SqrMagnitude(b - a);

        Vector2 ao = o - a;
        Vector2 ab = b - a;
        float dot = Vector2.Dot(ao, ab) / d;
        if (dot <= 0) { return a; }
        else if (dot >= 1) { return b; }
        else
        {
            return a + ab * dot;
        }
    }

    private static Vector2 constrainToScreenSize(Vector2 position)
    {/*
        if (position.x < 0)
        {
            position.x = 0;
        }
        else if (position.x >= Screen.width)
        {
            position.x = Screen.width - 1;
        }
        if (position.y < 0)
        {
            position.y = 0;
        }
        else if (position.y >= Screen.height)
        {
            position.y = Screen.height - 1;
        }
        */
        return position;
    }

    // UNTESTED
    private List<int> getSelectedPerimeter(MeshEdit solid, bool[] selectedVerts, bool[] selectedFaces, out List<int> faces, int vertMode)
    {
        // A perimeter edge is a group of four values and 0-2 face references
        // [verts connected to the selected face,, verts connected to the non-selected face,] [selected face, non-selected face]
        // [verts connected to the selected face,, -1, -1] [selected face, -1]
        // [verts connected to no face, that make an edge, -1, -1] [-1, -1]
        List<int> selectedEdges = new List<int>();

        List<int> relevantFace = new List<int>();

        bool[] tempSelectedVerts = new bool[selectedVerts.Length];
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            tempSelectedVerts[i] = selectedVerts[i];
        }

        // For each vert, check if its assosciated face is selected.
        // If it is, add an edge wherever an edge can be made from selected vertices
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            int ii = i * 4;
            for (int q = 0; q < 4; q++)
            {
                int a = -1; int b = -1;
                if (q == 0)
                {
                    a = solid.quads[ii + 0];
                    b = solid.quads[ii + 1];
                }
                else if (q == 1)
                {
                    a = solid.quads[ii + 0];
                    b = solid.quads[ii + 2];
                }
                else if (q == 2)
                {
                    a = solid.quads[ii + 3];
                    b = solid.quads[ii + 1];
                }
                else if (q == 3)
                {
                    a = solid.quads[ii + 3];
                    b = solid.quads[ii + 2];
                }
                if (selectedVerts[a] && selectedVerts[b])
                {
                    int c = a;
                    int d = b;
                    int reverseFace = getFirstConnectedFace(
                        solid, ref c, ref d, i);

                    // If the opposite face is selected, ignore this line (it's not a part of the perimeter).
                    // If the opposite face is selected and the current isn't, skip it. It will be filled in later with the right orientation.
                    // relevantFace[i * 2 + 0] represents the selected face
                    if (reverseFace >= 0 &&
                        ((selectedFaces[i] && selectedFaces[reverseFace]) ||
                        (!selectedFaces[i] && selectedFaces[reverseFace])))
                    {
                        continue;
                    }

                    // If the face is a lone edge between two non-selected faces
                    // Skip on face selection
                    if (!selectedFaces[i] && (reverseFace == -1 || !selectedFaces[reverseFace]))
                    {
                        if (vertMode == 1)
                        {
                            continue;
                        }
                    }
                    // Don't skip on vert selection
                    if (!selectedFaces[i] && (reverseFace >= 0 && !selectedFaces[reverseFace]))
                    {
                        int[] abcd = { a, b, c, d };
                        // Make sure it isn't a duplicate
                        bool isDuplicate = false;
                        for (int f = 0; f < selectedEdges.Count; f += 4)
                        {
                            int sameCount = 0;
                            for (int ff = f; ff < f + 4; ff++)
                            {
                                for (int ffc = 0; ffc < 4; ffc++)
                                {
                                    if (abcd[ffc] == selectedEdges[ff])
                                    {
                                        sameCount++;
                                        break;
                                    }
                                }
                            }
                            if (sameCount == 4)
                            {
                                // Same edge found
                                isDuplicate = true;
                                break;
                            }
                        }
                        if (isDuplicate)
                        {
                            continue;
                        }
                    }

                    selectedEdges.Add(a);
                    selectedEdges.Add(b);
                    relevantFace.Add(i);
                    // Add the reverse face to the same verts (If it exists)
                    if (reverseFace >= 0)
                    {
                        selectedEdges.Add(c);
                        selectedEdges.Add(d);
                        relevantFace.Add(reverseFace);
                    }
                    else
                    {
                        selectedEdges.Add(-1);
                        selectedEdges.Add(-1);
                        relevantFace.Add(-1);
                    }
                }
            }
        }

        faces = relevantFace;
        return selectedEdges;
    }
    public int getFirstConnectedFace(MeshEdit solid, ref int v0, ref int v1, int face)
    {
        for (int i = 0; i < solid.connectedVerts[v0].Count; i++)
        {
            for (int j = 0; j < solid.connectedVerts[v1].Count; j++)
            {
                int vertFaceA = solid.quads.IndexOf(solid.connectedVerts[v0].list[i]) / 4;
                int vertFaceB = solid.quads.IndexOf(solid.connectedVerts[v1].list[j]) / 4;
                if (vertFaceA == vertFaceB && face != vertFaceB)
                {
                    v0 = solid.connectedVerts[v0].list[i];
                    v1 = solid.connectedVerts[v1].list[j];
                    return vertFaceB;
                }
            }
        }
        return -1;
    }

    private List<int> getSelectedEdges(MeshEdit solid, bool[] selectedVerts, bool[] selectedFaces)
    {

        List<int> selectedEdges = new List<int>();


        bool[] tempSelectedVerts = new bool[selectedVerts.Length];
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            tempSelectedVerts[i] = selectedVerts[i];
        }
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (tempSelectedVerts[i])
            {
                for (int ii = 0; ii < solid.quads.Count; ii += 4)
                {
                    for (int iii = 0; iii < 4; iii++)
                    {
                        if (i != solid.quads[ii + iii])
                        {
                            if (tempSelectedVerts[solid.quads[ii + iii]])
                            {
                                tempSelectedVerts[i] = false;
                                tempSelectedVerts[solid.quads[ii + iii]] = false;

                                selectedEdges.Add(i);
                                selectedEdges.Add(solid.quads[ii + iii]);
                            }
                        }
                    }
                }
            }
        }

        // Remove all edges that are exactly equal in positions
        for (int i = 0; i < selectedEdges.Count; i += 2)
        {
            Vector3 a = solid.verts[selectedEdges[i + 0]];
            Vector3 b = solid.verts[selectedEdges[i + 1]];

            for (int ii = i + 2; ii < selectedEdges.Count; ii += 2)
            {
                Vector3 aa = solid.verts[selectedEdges[ii + 0]];
                Vector3 bb = solid.verts[selectedEdges[ii + 1]];

                if ((aa == a && bb == b) ||
                    (aa == b && bb == a))
                {
                    selectedEdges.RemoveAt(ii);
                    selectedEdges.RemoveAt(ii);
                    ii -= 2;
                }
            }
        }

        return selectedEdges;
    }

    private void updateSelectionArray(int length)
    {
        bool[] newSelectedVerts = new bool[length];
        if (selectedFaces != null)
        {
            for (int i = 0; i < Mathf.Min(selectedVerts.Length, length); i++)
            {
                newSelectedVerts[i] = selectedVerts[i];
            }
        }
        selectedVerts = newSelectedVerts;

        bool[] newSelectedFaces = new bool[length / 4];
        if (selectedFaces != null)
        {
            for (int i = 0; i < Mathf.Min(selectedFaces.Length, newSelectedFaces.Length); i++)
            {
                newSelectedFaces[i] = selectedFaces[i];
            }
        }
        selectedFaces = newSelectedFaces;
    }

    private float getValueFromMouseAxisPosition(Vector3 axes, Vector3 origin, Vector2 mouseOrigin, Vector2 mousePosition, bool asRatio)
    {
        axes = axes.normalized;
        float l = 1;
        Vector3 p0 = origin + axes * (l);

        Vector2 p2D0 = HandleUtility.WorldToGUIPoint(p0);
        Vector2 o = HandleUtility.WorldToGUIPoint(origin);
        Vector2 axes2D = p2D0 - o;

        Vector2 b = MeshEdit.closestPoint(o, p2D0, mousePosition, false);
        Vector2 a = MeshEdit.closestPoint(o, p2D0, mouseOrigin, false);
        float bRatio = Vector2.Dot(b - o, axes2D.normalized) / axes2D.magnitude;
        float aRatio = Vector2.Dot(a - o, axes2D.normalized) / axes2D.magnitude;
        Vector3 aAt3DPos = origin + axes * (aRatio * l);
        Vector3 bAt3DPos = origin + axes * (bRatio * l);
        float dot = Vector3.Dot(aAt3DPos - origin, axes);
        Debug.DrawLine(origin + axes * 10000, origin - axes * 0);
        Debug.DrawLine(aAt3DPos, bAt3DPos, Color.red);
        float value = Vector3.Distance(aAt3DPos, bAt3DPos) * Mathf.Sign(bRatio - aRatio);

        Debug.DrawLine(aAt3DPos, aAt3DPos + value * axes, Color.green);

        if (Vector3.Dot(value * axes, bAt3DPos - aAt3DPos) < 0)
        {
            value *= -1;
        }

        if (!asRatio)
        {
            return value;
        }

        float distanceToOrigin = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, origin);

        Vector3 projectedMouseOrigin = HandleUtility.GUIPointToWorldRay(a).GetPoint(distanceToOrigin);

        Debug.DrawLine(HandleUtility.GUIPointToWorldRay(b).GetPoint(distanceToOrigin), origin, Color.red);

        Vector3 outv = HandleUtility.GUIPointToWorldRay(b).GetPoint(distanceToOrigin) - projectedMouseOrigin;

        Vector3 targetPoint = HandleUtility.GUIPointToWorldRay(b).GetPoint(
            Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, origin));

        float d = Vector3.Dot(outv, axes.normalized);
        if (Mathf.Sign(d) == Mathf.Sign(dot))
        {
            d *= -1;
        }

        return (d);
        // Vector3.Distance(targetPoint, origin)) * Mathf.Sign(dot);// ((b - o).magnitude / (a - o).magnitude));
    }

    private bool[] affectedVerts;
    private bool[] affectedFaces;
    private void getAffectedVerts(MeshEdit solid)
    {
        affectedVerts = new bool[selectedVerts.Length];
        affectedFaces = new bool[selectedFaces.Length];

        for (int i = 0; i < selectedVerts.Length; i++)
        {
            affectedVerts[i] = selectedVerts[i];
        }

        for (int i = 0; i < selectedFaces.Length; i++)
        {
            bool areAnyVertsActive = false;
            for (int ii = 0; ii < 4; ii++)
            {
                if (selectedVerts[i * 4 + ii])
                {
                    areAnyVertsActive = true;
                    break;
                }
            }
            if (areAnyVertsActive)
            {
                affectedVerts[i * 4 + 0] = true;
                affectedVerts[i * 4 + 1] = true;
                affectedVerts[i * 4 + 2] = true;
                affectedVerts[i * 4 + 3] = true;
                affectedFaces[i] = true;
            }
        }
        for (int i = 0; i < affectedVerts.Length; i++)
        {
            for (int j = 0; j < solid.connectedVerts[i].Count; j++)
            {
                affectedVerts[solid.connectedVerts[i].list[j]] = true;
            }

            /*
            for (int ii = 0; ii < affectedVerts.Length; ii++)
            {
                if (i != ii &&
                    selectedVerts[i])
                {
                    if (solid.verts[i] == solid.verts[ii])
                    {
                        affectedVerts[i] = true;
                    }
                }
            }*/
        }

    }
    private void selectionAddTouchingVerts(MeshEdit solid)
    {

        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVerts[i])
            {
                for (int j = 0; j < solid.connectedVerts[i].Count; j++)
                {
                    selectedVerts[solid.connectedVerts[i].list[j]] = true;
                }
            }
        }
    }

    private void selectionConvertToVerts(MeshEdit solid)
    {
        // Convert faces into verts
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            for (int ii = 0; ii < 4; ii++)
            {
                selectedVerts[i * 4 + ii] = selectedFaces[i];
            }
        }
    }

    private void selectionConvertToFaces(MeshEdit solid)
    {
        // Convert verts into faces
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedVerts[solid.quads[i * 4 + 0]] &&
                selectedVerts[solid.quads[i * 4 + 1]] &&
                selectedVerts[solid.quads[i * 4 + 2]] &&
                selectedVerts[solid.quads[i * 4 + 3]])
            {
                selectedFaces[i] = true;
            }
        }
    }

    private void selectVert(MeshEdit solid, int vert, bool isAdditive)
    {
        selectedVerts[vert] = !isAdditive || !selectedVerts[vert];
        // Also select the second vert that is hidden by the originally selected vert.
        for (int i = 0; i < solid.connectedVerts[vert].Count; i++)
        {
            selectedVerts[solid.connectedVerts[vert].list[i]] = selectedVerts[vert];
        }
        /*
         Position based
        for (int i = 0; i < solid.verts.Count; i++)
        {
            if (i != vert)
            {
                if (solid.verts[i] == solid.verts[vert])
                {
                    selectedVerts[i] = selectedVerts[vert];
                }
            }
        }*/
    }
    private void setSelectVert(MeshEdit solid, int vert, bool state)
    {
        selectedVerts[vert] = state;
        // Also select the second vert that is hidden by the originally selected vert.
        for (int i = 0; i < solid.connectedVerts[vert].Count; i++)
        {
            selectedVerts[solid.connectedVerts[vert].list[i]] = state;
        }
    }

    private void activateTransformMode(MeshEdit solid, int transformMode)
    {
        if (this.transformMode == 0 && !transformDimensionsExtrude)
        {

            Undo.RegisterCompleteObjectUndo(solid, "Mesh Transformation");


            //Undo.RegisterCompleteObjectUndo(solid, "Mesh Transformation");
            //Undo.RecordObject(solid.gameObject, "Mesh Transformation");
            transformDimensions = Vector3.zero;
            transformDimensionsPlanar = false;
        }
        else if (this.transformMode != transformMode && this.transformMode != 0)
        {
            solid.verts = oldVerts;
            solid.updateMeshVerts();
        }

        this.transformMode = transformMode;
        getAffectedVerts(solid);
        oldVerts = new List<Vector3>();

        int c = 0;
        anchorCenter = Vector3.zero;
        for (int i = 0; i < solid.verts.Count; i++)
        {
            if (selectedVerts[i])
            {
                anchorCenter += solid.verts[i];
                c++;
            }
            oldVerts.Add(solid.verts[i]);
        }
        anchorCenter /= c;

        if (transformDimensions.sqrMagnitude < 0.9f)
        {
            transformDimensions = Vector3.one;
            transformDimensionsPlanar = false;
        }
    }

    Rect fullGUIRectangle = Rect.zero;
    private void guiHeader(MeshEdit solid)
    {
        Handles.BeginGUI();
        GUI.skin = skin;

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(uiHeaderPosition.x, uiHeaderPosition.y, 340, 800));

        string labelText = ray.origin.ToString("f4");

        Rect rect = EditorGUILayout.BeginVertical();
        GUI.Box(rect, GUIContent.none);

        GUI.color = Color.white;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical();
        GUILayout.Space(6);
        int oldEditMode = editMode;

        int newEditMode = EditorGUILayout.Popup(editMode, editModes);
        updateEditMode(solid, newEditMode);

        GUILayout.Space(6);
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // Catches clicks on box
        GUI.Button(rect, "", GUIStyle.none);
        GUILayout.EndArea();

    }

    private void updateEditMode(MeshEdit solid, int newEditMode)
    {
        //Debug.Log("Currrent Tool: " + Tools.current.ToString() + ",   View: " + Tools.viewTool.ToString());
        if (newEditMode != editMode)
        {
            editOperation = MeshEditOperation.Standard;

            editMode = newEditMode;
            solid.currentEditMode = editMode;
            if (editMode != 0)
            {
                solid.verifyMeshData();
                lastUsedEditMode = editMode;
                //lastTool = Tools.current;
                // Tools.current = Tool.None;
                Tools.hidden = true;

                if (solid.transformsApplied)
                {
                    //Debug.Log("De-Transform");
                    solid.transformsApplied = false;
                    solid.pushNewGeometry();
                }
            }
            else if (editMode == 0)
            {
                solid.checkTransforms();
                if (Tools.hidden)
                {
                    Tools.hidden = false;
                    // Tools.current = Tool.Move;
                }
                else
                {
                    // Tools.current = lastTool;
                }
            }
            
            if (solid.lastTilesetUsed < tilesetsAvailable.Count)
            {
                selectedTileset = solid.lastTilesetUsed;
            }
            else
            {
                selectedTileset = 0;
                solid.lastTilesetUsed = 0;
            }
            if (editMode == 2)
            {

                if (!solid.hasDefaultUVs)
                {
                    loadTilesets();
                }
            }
            if (editMode == 1)
            {
                selectedEdges = new bool[0];
                selectedFaces = new bool[solid.quads.Count / 4];
                selectedVerts = new bool[solid.verts.Count];

            }
        }
    }
    bool copyTexturesOnExport = true;
    Vector2 helpModeScrollPosition = Vector2.zero;
    private void guiMeshEditing(MeshEdit solid, int controlId)
    {
        GUILayout.BeginArea(new Rect(uiPosition.x, uiPosition.y, 340, 800));

        Rect editRect = EditorGUILayout.BeginVertical();
        GUI.Box(editRect, GUIContent.none);

        // Dropdown selector
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical();
        GUILayout.Space(6);
        int oldVertMode = vertMode;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Select: ", GUILayout.Width(80));
        vertMode = EditorGUILayout.Popup(vertMode, new string[] { "Vertices", "Faces" }, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (oldVertMode != vertMode)
        {

            if (vertMode == 0)
            {
                // To verts 
                if (vertMode == 1)
                {
                    selectionConvertToVerts(solid);
                }
                selectionAddTouchingVerts(solid);
                selectedFaces = new bool[selectedFaces.Length];
            }
            else if (vertMode == 1)
            {
                // To faces
                selectionConvertToFaces(solid);
                selectedVerts = new bool[selectedVerts.Length];
            }

            saveSettings();
        }
        if (selectedFaces != null && selectedVerts != null)
        {
            GUILayout.Label("Faces: " + selectedFaces.Length + " Verts: " + selectedVerts.Length);
        }
        if (solid.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("Quads: " + (solid.quads == null ? "null" : solid.quads.Count.ToString()));
        }
        if (solid.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("ConnectedVerts: " + (solid.connectedVerts == null ? "null" : solid.connectedVerts.Count.ToString()));
        }
        if (solid.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("AdjacentFaces: " + (solid.adjacentFaces == null ? "null" : solid.adjacentFaces.Count.ToString()));
        }
        /*
        if (GUILayout.Button("Re-calculate mesh data from source."))
        {
            solid.createTrianglesFromWorldMesh(true);
        }*/
        GUILayout.BeginHorizontal();
        GUILayout.Label("Normals: ", GUILayout.Width(80));
        solid.drawNormals = EditorGUILayout.Popup(solid.drawNormals, new string[] { "None", "Faces", "Verts", "Triangles" }, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Normal Length: ", GUILayout.Width(100));
        solid.normalLength = GUILayout.HorizontalSlider(solid.normalLength, 0.01f, 5.0f, GUILayout.Width(200));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(12);
        GUILayout.Label("Add Primitive:");
        Vector3 centerPos = SceneView.lastActiveSceneView.pivot;

        int faceCount = solid.faceNormals.Count;
        int vertCount = solid.verts.Count;
        bool wasChanged = false;
        if (skinDefault != null)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Plane", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
            {
                Undo.RegisterCompleteObjectUndo(solid, "Add Plane");
                solid.addMesh(plane(), centerPos);

                wasChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cube", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
            {
                Undo.RegisterCompleteObjectUndo(solid, "Add Cube");
                solid.addMesh(cube(), centerPos);

                wasChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Circle", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
            {
                Undo.RegisterCompleteObjectUndo(solid, "Add Circle");
                solid.addMesh(circle(circleVerticesSelected), centerPos);

                wasChanged = true;
            }
            circleVerticesSelected = EditorGUILayout.Popup(circleVerticesSelected, circleVertices, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cylinder", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
            {
                Undo.RegisterCompleteObjectUndo(solid, "Add Circle");
                solid.addMesh(cylinder(circleVerticesSelected), centerPos);

                wasChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(GUILayout.Width(200));

            if (GUILayout.Button("Cut", skinDefault.GetStyle("Button"), GUILayout.Width(60)))
            {
                cut(solid);
            }
            if (GUILayout.Button("Copy", skinDefault.GetStyle("Button"), GUILayout.Width(60)))
            {
                copy(solid);
            }
            if (GUILayout.Button("Paste", skinDefault.GetStyle("Button"), GUILayout.Width(60)))
            {
                paste(solid);
            }
            EditorGUILayout.EndHorizontal();
        }
        if (wasChanged)
        {
            updateSelectionArray(solid.verts.Count);

            for (int i = 0; i < solid.faceNormals.Count; i++)
            {
                selectedFaces[i] = (i >= faceCount);
            }
            for (int i = 0; i < solid.verts.Count; i++)
            {
                selectedVerts[i] = (i >= vertCount);
            }
        }

        GUILayout.Space(6);

        #region Help Mode
        if (helpMode)
        {

            GUILayout.Space(6);

            helpModeScrollPosition = GUILayout.BeginScrollView(helpModeScrollPosition,GUILayout.Width(320), GUILayout.Height(240));

            if (editOperation == MeshEditOperation.SelectCircle)
            {
                GUILayout.Label("Mode: Circle Select");
                if (vertMode == 0)
                {
                    GUILayout.Label("LMB - Select a vert");
                    GUILayout.Label("MMB - Deselect a vert");
                }
                else
                {
                    GUILayout.Label("LMB - Select a face");
                    GUILayout.Label("MMB - Deselect a face");
                }
                GUILayout.Label("C/LMB - Exit circle-select mode");
                GUILayout.Label("Scroll - Change circle size");
            }
            else if (editOperation == MeshEditOperation.LoopCut)
            {
                GUILayout.Label("Mode: Loop Cut");
                GUILayout.Label("LMB - Exit loop-cut mode");
                GUILayout.Label("RMB - Cut");
                GUILayout.Label("Scroll - Change number of cuts");
            }
            else if (transformMode > 0)
            {
                if (transformMode == 1)
                {
                    GUILayout.Label("Mode: Transform - Move");
                }
                else if (transformMode == 2)
                {
                    GUILayout.Label("Mode: Transform - Rotate");
                }
                else if (transformMode == 3)
                {
                    GUILayout.Label("Mode: Transform - Scale");
                }

                GUILayout.Label("LMB - Confirm transform");
                GUILayout.Label("RMB - Undo transform");
                GUILayout.Label("X, Y, Z - Constrain transform to axis");
            }
            else
            {
                GUILayout.Label("Mode: Default");
                if (vertMode == 0)
                {
                    GUILayout.Label("RMB - Select a vert");
                }
                else
                {
                    GUILayout.Label("RMB - Select a face");
                }

                GUILayout.Label("C - Activate circle-select mode");
                GUILayout.Label("A - Select all");
                GUILayout.Label("G - Move selection");
                GUILayout.Label("R - Rotate selection");
                GUILayout.Label("S - Scale selection");
                GUILayout.Label("E - Extrude selected section");
                GUILayout.Label("Shift + R - Activate loop-cut mode");
                GUILayout.Label("F - If four vertices are selected, create a new face between them");
                GUILayout.Label("F - Flip selected faces");
                GUILayout.Label("D - Flip saddling on selected faces");
                GUILayout.Label("Z - Toggle mesh visibility");
                GUILayout.Label("L - Select all connected faces under the mouse");

            }

            GUILayout.EndScrollView();
            GUILayout.Space(6);
        }
        #endregion

        EditorGUILayout.EndVertical();

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        // Catches clicks on box
        GUI.Button(editRect, "", GUIStyle.none);
        GUILayout.EndArea();
    }






    private void guiDefault(MeshEdit solid, int controlId)
    {
        GUILayout.BeginArea(new Rect(uiPosition.x, uiPosition.y, 340, 800));

        Rect editRect = EditorGUILayout.BeginVertical();
        GUILayout.Space(6);
        GUI.Box(editRect, GUIContent.none);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);

        #region Button - Recenter Pivot
        if (GUILayout.Button("Re-Center Pivot", skinDefault.GetStyle("Button"), GUILayout.Width(120)))
        {
            solid.recenterPivot(new Vector3(0.5f, 0.5f, 0.5f));
        }
        #endregion
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        #region Button - Export to OBJ
        if (GUILayout.Button("Export to .obj", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
        {
            exportObj(solid);
        }
        GUILayout.Space(16);
        //GUILayout.Label("Copy textures on export");
        bool oldCopyTextureOnExport = copyTexturesOnExport;
        copyTexturesOnExport = GUILayout.Toggle(copyTexturesOnExport, "Copy textures on export", GUILayout.Height(24));
        if (oldCopyTextureOnExport != copyTexturesOnExport)
        {
            saveSettings();
        }
        #endregion
        
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        #region Button - Export to FBX
        if (GUILayout.Button("Import .obj", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
        {
            importObj();
        }
        #endregion
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(16);

        if (GUILayout.Button("Show the tutorial", skinDefault.GetStyle("Button"), GUILayout.Width(180)))
        {
            Application.OpenURL("www.jamierollo.com/MeshEdit/Tutorial/index.html");
        }

        bool oldHelpMode = helpMode;
        helpMode = GUILayout.Toggle(helpMode, "Show shortcuts", GUILayout.Height(24));
        if (oldHelpMode != helpMode)
        {
            saveSettings();
        }


        if (helpMode)
        {
            GUILayout.Label("Shortcuts for all edit modes:");
            GUILayout.Label("Tab - Switch edit mode");
            GUILayout.Label("Numpad5 - Toggle ortho perspective");
            GUILayout.Label("Numpad7 - View top");
            GUILayout.Label("Numpad1 - View front");
            GUILayout.Label("Numpad3 - View side");
        }

        GUILayout.Space(6);
        if (solid.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            #region Button - Rebuild verts
            if (GUILayout.Button("Rebuild Connected Verts", skinDefault.GetStyle("Button"), GUILayout.Width(180)))
            {
                solid.getConnectedVertsBasedOnPosition();
            }
            if (GUILayout.Button("Rebuild Adjacent Faces", skinDefault.GetStyle("Button"), GUILayout.Width(180)))
            {
                solid.rebuildAdjacentFaces();
            }
            GUILayout.Space(6);
        }
        #endregion
        EditorGUILayout.EndVertical();
        // Catches clicks on box
        GUI.Button(editRect, "", GUIStyle.none);
        GUILayout.EndArea();
    }
    private void exportObj(MeshEdit solid)
    {
        // TODO: Allow the user toggle applying modifiers to exported objects
        string path = EditorUtility.SaveFilePanel(
            "Export model as a .obj file",
            "",
            target.name + ".obj",
            "obj");

        if (path.Length > 0)
        {
            int slashIndex = path.LastIndexOf('/') + 1;
            string name = path.Remove(0, slashIndex);
            name = name.Remove(name.Length - 4);

            path = path.Remove(slashIndex);

            Debug.Log("Exporting " + target.name + " to " + path + name + ".obj");

            Material mat = solid.gameObject.GetComponent<Renderer>().sharedMaterial;
            string matName = "Material0";
            #region .obj
            string data = "# Material" + Environment.NewLine;
            data += "mtllib " + name + ".mtl" + Environment.NewLine;
            data += "usemtl " + matName + Environment.NewLine;
            data += "s " + "1" + Environment.NewLine;

            data += "# Object" + Environment.NewLine;
            data += "o " + name + Environment.NewLine;
            MeshFilter mf = solid.gameObject.GetComponent<MeshFilter>();
            Mesh m = mf.sharedMesh;

            string f6 = "f6";
            string f4 = "f4";
            CultureInfo c = CultureInfo.InvariantCulture;


            // Construct hash table for vertices








            int[] vertMap;
            List<int> fewestVertices;
            List<Vector3> fewestNormals;

            vertMap = new int[m.vertices.Length];
            fewestVertices = new List<int>();
            fewestNormals = new List<Vector3>();



            bool[] mappedVerts = new bool[solid.verts.Count];
            int vertIndex = 0;
            for (int i = 0; i < solid.connectedVerts.Count; i++)
            {
                if (!mappedVerts[i])
                {
                    Vector3 averageNormal;
                    List<int> added = new List<int>();
                    // "Added" is just for redundancy, in case a connected vert is counted twice by mistake, or in case the format changes.
                    added.Add(i);

                    averageNormal = m.normals[i].normalized;
                    // Get the full list of connected verts and the average normal of one of the main vertices
                    for (int j = 0; j < solid.connectedVerts[i].Count; j++)
                    {
                        if (!added.Contains(solid.connectedVerts[i].list[j]))
                        {
                            added.Add(solid.connectedVerts[i].list[j]);
                            averageNormal += m.normals[solid.connectedVerts[i].list[j]].normalized;
                        }
                    }

                    bool isValid = true;
                    for (int j = 0; j < added.Count; j++)
                    {
                        if (mappedVerts[added[j]])
                        {
                            isValid = false;

                            // Fill in all spots that have connected vertices
                            for (int jj = 0; jj < added.Count; jj++)
                            {
                                mappedVerts[added[jj]] = true;
                                vertMap[added[jj]] = vertIndex;
                            }

                            // Add this to the list in the order it was found
                            vertIndex++;

                            break;
                        }
                    }

                    if (isValid)
                    {
                        averageNormal = averageNormal.normalized;
                        fewestVertices.Add(i);
                        fewestNormals.Add(averageNormal);

                        for (int j = 0; j < added.Count; j++)
                        {
                            mappedVerts[added[j]] = true;
                            vertMap[added[j]] = vertIndex;
                        }

                        vertIndex++;
                    }
                }
            }


            data += "# Vertices" + Environment.NewLine;
            for (int i = 0; i < fewestVertices.Count; i++)
            {
                data += "v ";
                data += m.vertices[fewestVertices[i]].x.ToString(f6, c) + " ";
                data += m.vertices[fewestVertices[i]].y.ToString(f6, c) + " ";
                data += m.vertices[fewestVertices[i]].z.ToString(f6, c);
                data += Environment.NewLine;
            }
            data += "# UV Coordinates" + Environment.NewLine;

            for (int i = 0; i < m.uv.Length; i++)
            {
                data += "vt ";
                data += m.uv[i].x.ToString(f6, c) + " ";
                data += m.uv[i].y.ToString(f6, c);
                data += Environment.NewLine;
            }
            data += "# Vertex Normals" + Environment.NewLine;

            for (int i = 0; i < fewestNormals.Count; i++)
            {
                Vector3 averageNormal = fewestNormals[i];

                data += "vn ";
                data += averageNormal.x.ToString(f4, c) + " ";
                data += averageNormal.y.ToString(f4, c) + " ";
                data += averageNormal.z.ToString(f4, c);
                data += Environment.NewLine;
            }

            data += "# Faces" + Environment.NewLine;
            int[] compoundedVertIndexes = new int[solid.connectedVerts.Count];

            for (int i = 0; i < solid.connectedVerts.Count; i++)
            {
                List<int> choices = new List<int>();
                choices.Add(i);
                for (int j = 0; j < solid.connectedVerts[i].Count; j++)
                {
                    choices.Add(solid.connectedVerts[i].list[j]);
                }

                bool doesVertExist = false;

                for (int j = 0; j < i; j++)
                {
                    if (choices.Contains(compoundedVertIndexes[j]))
                    {
                        doesVertExist = true;
                        compoundedVertIndexes[i] = compoundedVertIndexes[j];
                        break;
                    }
                }

                if (!doesVertExist)
                {
                    compoundedVertIndexes[i] = choices[0];
                }
            }

            for (int i = 0; i < solid.quads.Count; i += 4)
            {
                Vector3 faceNormal =
                    (fewestNormals[vertMap[solid.quads[i + 0]]] +
                    fewestNormals[vertMap[solid.quads[i + 1]]] +
                    fewestNormals[vertMap[solid.quads[i + 2]]] +
                    fewestNormals[vertMap[solid.quads[i + 3]]]) / 4.0f;

                Vector3 ta = m.vertices[fewestVertices[vertMap[solid.quads[i + 1]]]];
                Vector3 tb = m.vertices[fewestVertices[vertMap[solid.quads[i + 0]]]];
                Vector3 tc = m.vertices[fewestVertices[vertMap[solid.quads[i + 2]]]];

                Vector3 ab = tb - ta;
                Vector3 ac = tc - ta;
                Vector3 proofNormal = Vector3.Cross(ab, ac);

                data += "f ";

                // Obj file format gets the face direction based on the standard cross product of it's triangles.
                // Because of this it expects the face to list the points in a clockwise/counterclockwise fashion.
                if (Vector3.Dot(faceNormal, proofNormal) > 0)
                {
                    data += (vertMap[solid.quads[i + 1]] + 1) + "/" + (solid.quads[i + 1] + 1) + "/" + (vertMap[solid.quads[i + 1]] + 1) + " ";
                    data += (vertMap[solid.quads[i + 0]] + 1) + "/" + (solid.quads[i + 0] + 1) + "/" + (vertMap[solid.quads[i + 0]] + 1) + " ";
                    data += (vertMap[solid.quads[i + 2]] + 1) + "/" + (solid.quads[i + 2] + 1) + "/" + (vertMap[solid.quads[i + 2]] + 1) + " ";
                    data += (vertMap[solid.quads[i + 3]] + 1) + "/" + (solid.quads[i + 3] + 1) + "/" + (vertMap[solid.quads[i + 3]] + 1);
                }
                else
                {
                    // Flipped face
                    data += (vertMap[solid.quads[i + 2]] + 1) + "/" + (solid.quads[i + 2] + 1) + "/" + (vertMap[solid.quads[i + 2]] + 1) + " ";
                    data += (vertMap[solid.quads[i + 0]] + 1) + "/" + (solid.quads[i + 0] + 1) + "/" + (vertMap[solid.quads[i + 0]] + 1) + " ";
                    data += (vertMap[solid.quads[i + 1]] + 1) + "/" + (solid.quads[i + 1] + 1) + "/" + (vertMap[solid.quads[i + 1]] + 1) + " ";
                    data += (vertMap[solid.quads[i + 3]] + 1) + "/" + (solid.quads[i + 3] + 1) + "/" + (vertMap[solid.quads[i + 3]] + 1);
                }

                data += Environment.NewLine;
            }

            File.WriteAllText(path + name + ".obj", data);
            #endregion
            string resourceName = solid.uvMaps[solid.currentUVMap].name;
            string textureName = solid.uvMaps[solid.currentUVMap].name.Replace(' ', '_');

            #region .mtl
            data = "";
            // TODO Repeat for multiple materials
            data += "newmtl " + matName + Environment.NewLine;


            Color ambientCol = Color.white; ;
            if (mat.HasProperty("_Color"))
            {
                ambientCol = mat.GetColor("_Color");
            }

            Color specularCol = Color.white;
            if (mat.HasProperty("_SpecColor"))
            {
                specularCol = mat.GetColor("_SpecColor");
            }

            float specularExp = 0.0f;
            if (mat.HasProperty("_Shininess"))
            {
                specularExp = mat.GetFloat("_Shininess");
            }

            data += "Ka " + ambientCol.r + " " + ambientCol.g + " " + ambientCol.b + Environment.NewLine;
            data += "Kd 1.000 1.000 1.000" + Environment.NewLine;
            data += "Ks " + specularCol.r + " " + specularCol.g + " " + specularCol.b + Environment.NewLine;
            data += "Ns " + specularExp + Environment.NewLine;
            data += "d 1.0" + Environment.NewLine;
            data += "Tr 1.0" + Environment.NewLine;
            data += "s 0.0" + Environment.NewLine;
            data += "illum 0" + Environment.NewLine;
            data += "map_Kd " + textureName + ".png" + Environment.NewLine;
            File.WriteAllText(path + name + ".mtl", data);
            #endregion

            #region Textures
            if (copyTexturesOnExport)
            {
                File.Copy(
                    Application.dataPath + "/Resources/Tilesets/Constructed/" + resourceName + ".png",
                    path + textureName + ".png",
                    true);
            }
            #endregion
        }


    }

    public static void importObj()
    {
        string path = EditorUtility.OpenFilePanel(
            "Export model as a .obj file",
            Application.dataPath + "/Resources",
            "obj");

        if (path != null && path.Length > 0)
        {
            Debug.Log("Importing " + path);

            string data;

            using (StreamReader reader = new StreamReader(path))
            {
                data = reader.ReadToEnd();
            }

            // Repeat for each "o" object in the file that corresponds to a mesh and create a separate mesh edit object for each.
            // TODO: Set up texturing tab to show the loaded texture, or a warning before initialising a new tiled texture.

            string[] lines = data.Split('\r', '\n');

            string materialPath = "";
            string materialName = "";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> vertNormals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int o = 0; o < lines.Length; o++)
            {
                if (lines[o].Trim(' ').StartsWith("mtllib"))
                {
                    materialPath = lines[o].Trim(' ').Remove(0, 6).Trim(' ');
                }
                if (lines[o].Trim(' ').StartsWith("usemtl"))
                {
                    materialName = lines[o].Trim(' ').Remove(0, 6).Trim(' ');
                }

                if (lines[o].Trim(' ').StartsWith("o"))
                {
                    #region Load Mesh
                    List<List<int>> faceVertices = new List<List<int>>();
                    List<List<int>> faceUvs = new List<List<int>>();
                    List<List<int>> faceNormals = new List<List<int>>();
                    string texturePath = "";
                    string name = "";

                    name = lines[o].Trim(' ').Remove(0, 1).Trim(' ');

                    Debug.Log("Loading new mesh: " + name);
                    
                    for (int i = o; i < lines.Length; i++)
                    {
                        // Empty
                        if (lines[i].Length == 0)
                        {
                            continue;
                        }

                        // Comment
                        if (lines[i].StartsWith("#"))
                        {
                            continue;
                        }

                        // New object, break from current object.
                        if (lines[i].Trim(' ').StartsWith("o") && i != o)
                        {
                            o = i - 1;
                            break;
                        }

                        if (lines[i].Trim(' ').StartsWith("mtllib"))
                        {
                            materialPath = lines[i].Trim(' ').Remove(0, 6).Trim(' ');
                        }
                        if (lines[i].Trim(' ').StartsWith("usemtl"))
                        {
                            materialName = lines[i].Trim(' ').Remove(0, 6).Trim(' ');
                        }

                        // Vert
                        Vector3 vert;
                        Vector2 tex;
                        if (parseVector3(lines[i], out vert, "v# # #"))
                        {
                            vertices.Add(vert);
                        }
                        // Normal
                        else if (parseVector3(lines[i], out vert, "vn# # #"))
                        {
                            vertNormals.Add(vert);
                        }
                        // Uv
                        else if (parseVector2(lines[i], out tex, "vt# #"))
                        {
                            uvs.Add(tex);
                        }
                        // Face
                        else if (lines[i].TrimStart(' ').StartsWith("f"))
                        {
                            string face = lines[i].TrimStart(' ').Remove(0, 1);

                            string[] elements = face.Split(' ');

                            List<int> newFaceVertices = new List<int>();
                            List<int> newFaceUvs = new List<int>();
                            List<int> newFaceNormals = new List<int>();

                            int vertsAdded = 0;
                            for (int j = 0; j < elements.Length; j++)
                            {
                                string faceElement = elements[j].Trim(' ');
                                if (faceElement.Length > 0)
                                {
                                    string[] arguments = faceElement.Split('/');
                                    if (arguments.Length == 1)
                                    {
                                        int vertex;
                                        if (int.TryParse(arguments[0], out vertex))
                                        {
                                            vertsAdded++;
                                            newFaceVertices.Add(vertex - 1);
                                        }
                                    }
                                    else if (arguments.Length == 2)
                                    {
                                        int vertex;
                                        if (int.TryParse(arguments[0], out vertex))
                                        {
                                            vertsAdded++;
                                            newFaceVertices.Add(vertex - 1);
                                        }
                                        int uv;
                                        if (int.TryParse(arguments[1], out uv))
                                        {
                                            newFaceUvs.Add(uv - 1);
                                        }
                                    }
                                    else if (arguments.Length == 3)
                                    {
                                        int vertex;
                                        if (int.TryParse(arguments[0], out vertex))
                                        {
                                            vertsAdded++;
                                            newFaceVertices.Add(vertex - 1);
                                        }
                                        int uv;
                                        if (int.TryParse(arguments[1], out uv))
                                        {
                                            newFaceUvs.Add(uv - 1);
                                        }
                                        int normal;
                                        if (int.TryParse(arguments[2], out normal))
                                        {
                                            newFaceNormals.Add(normal - 1);
                                        }
                                    }
                                }

                            }
                            // Only add if there are exactly four elements in the face. 
                            // Quads only topology in the current Mesh Edit system
                            if (Mathf.Max(newFaceVertices.Count, newFaceUvs.Count, newFaceNormals.Count) == 4)
                            {
                                faceVertices.Add(newFaceVertices);
                                faceUvs.Add(newFaceUvs);
                                faceNormals.Add(newFaceNormals);
                            }
                            
                        }
                    }


                    int fV = faceVertices.Count;
                    int fU = faceUvs.Count;
                    int fN = faceNormals.Count;

                    if (fU < fV)
                    {
                        fU = 0;
                    }
                    if (fN < fV)
                    {
                        fN = 0;
                    }

                    Mesh mesh = new Mesh();
                    List<int> quads = new List<int>();
                    List<int> tris = new List<int>();

                    List<Vector3> meshVertices = new List<Vector3>();
                    List<Vector3> meshVertNormals = new List<Vector3>();


                    MeshEdit.ListWrapper[] connectedVerts = new MeshEdit.ListWrapper[faceVertices.Count * 4];

                    for (int i = 0; i < connectedVerts.Length; i++)
                    {
                        connectedVerts[i] = new MeshEdit.ListWrapper();
                    }

                    // The remap is intented to counteract the way the verts are shuffled around in the mesh-building section
                    int[] remap = { 1, 3, 2, 0 };
                    for (int i = 0; i < faceVertices.Count; i++)
                    {

                        for (int ii = 0; ii < faceVertices[i].Count; ii++)
                        {

                            for (int j = 0; j < faceVertices.Count; j++)
                            {
                                // As long as they're not the same face
                                if (i != j)
                                {

                                    for (int jj = 0; jj < faceVertices[j].Count; jj++)
                                    {
                                        if (faceVertices[j][jj] == faceVertices[i][ii])
                                        {
                                            connectedVerts[j * 4 + remap[jj]].Add(i * 4 + remap[ii]);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Mesh building
                    List<Vector2> meshUvs = new List<Vector2>();

                    for (int i = 0; i < faceVertices.Count; i++)
                    {
                        meshVertices.Add(vertices[faceVertices[i][3]]);
                        meshVertices.Add(vertices[faceVertices[i][0]]);
                        meshVertices.Add(vertices[faceVertices[i][2]]);
                        meshVertices.Add(vertices[faceVertices[i][1]]);

                        Vector3 ab = vertices[faceVertices[i][1]] - vertices[faceVertices[i][0]];
                        Vector3 ac = vertices[faceVertices[i][2]] - vertices[faceVertices[i][0]];
                        Vector3 defaultNormal = Vector3.Cross(ab, ac);

                        if (fN > 0)
                        {
                            if (faceNormals != null && faceNormals.Count > i && faceNormals[i].Count == 4)
                            {
                                meshVertNormals.Add(vertNormals[faceNormals[i][3]]);
                                meshVertNormals.Add(vertNormals[faceNormals[i][0]]);
                                meshVertNormals.Add(vertNormals[faceNormals[i][2]]);
                                meshVertNormals.Add(vertNormals[faceNormals[i][1]]);
                            }
                            else
                            {
                                meshVertNormals.Add(defaultNormal);
                                meshVertNormals.Add(defaultNormal);
                                meshVertNormals.Add(defaultNormal);
                                meshVertNormals.Add(defaultNormal);
                            }
                        }
                        else
                        {
                            meshVertNormals.Add(defaultNormal);
                            meshVertNormals.Add(defaultNormal);
                            meshVertNormals.Add(defaultNormal);
                            meshVertNormals.Add(defaultNormal);
                        }
                        if (fU > 0)
                        {
                            if (faceUvs != null && faceUvs.Count > i && faceUvs[i].Count == 4)
                            {
                                meshUvs.Add(uvs[faceUvs[i][3]]);
                                meshUvs.Add(uvs[faceUvs[i][0]]);
                                meshUvs.Add(uvs[faceUvs[i][2]]);
                                meshUvs.Add(uvs[faceUvs[i][1]]);
                            }
                            else
                            {
                                meshUvs.Add(Vector2.zero);
                                meshUvs.Add(Vector2.zero);
                                meshUvs.Add(Vector2.zero);
                                meshUvs.Add(Vector2.zero);
                            }
                        }
                        else
                        {
                            meshUvs.Add(Vector2.zero);
                            meshUvs.Add(Vector2.zero);
                            meshUvs.Add(Vector2.zero);
                            meshUvs.Add(Vector2.zero);
                        }
                        
                        tris.Add(i * 4 + 0); 
                        tris.Add(i * 4 + 1); 
                        tris.Add(i * 4 + 2); 

                        tris.Add(i * 4 + 3); 
                        tris.Add(i * 4 + 2); 
                        tris.Add(i * 4 + 1); 

                        quads.Add(i * 4 + 0);
                        quads.Add(i * 4 + 1);
                        quads.Add(i * 4 + 2);
                        quads.Add(i * 4 + 3);
                    }

                    GameObject go = new GameObject(name);
                    go.transform.position = SceneView.lastActiveSceneView.pivot;

                    Color[] colours = new Color[meshVertices.Count];
                    for (int i = 0; i < colours.Length; i++)
                    {
                        colours[i] = Color.white;
                    }

                    mesh.vertices = meshVertices.ToArray();
                    mesh.triangles = tris.ToArray();
                    mesh.colors = colours;
                    if (fU > 0)
                    {
                        mesh.uv = meshUvs.ToArray();
                    }
                    else
                    {
                        mesh.uv = new Vector2[meshVertices.Count];
                    }

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    // Add MeshFilter
                    MeshFilter mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    // Add MeshRenderer
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    mr.material = new Material(Shader.Find("Particles/Standard Unlit"));

                    // Add MeshEdit script
                    MeshEdit me = go.AddComponent<MeshEdit>();

                    // Full MeshEdit initialisation
                    me.deepCopyMesh(ref mesh, ref me._mesh);

                    me.verts = meshVertices;
                    me.vertNormals = meshVertNormals;
                    me.colours = colours.ToList();
                    me.quads = quads;
                    me.tris = tris;

                    me.connectedVerts = connectedVerts.ToList();
                    me.rebuildAdjacentFaces();

                    //
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
                    //

                    me.recalculateNormals(me._mesh);
                    for (int i = 0; i < meshVertices.Count; i++)
                    {
                        meshVertices[i] += go.transform.position;
                    }

                    #endregion

                    #region Load textures and materials
                    string matPath = path.Remove(path.LastIndexOf('/')) + "/" + materialPath;

                    using (StreamReader stream = new StreamReader(matPath))
                    {
                        data = stream.ReadToEnd();
                    }

                    string[] mtlLines = data.Split('\r', '\n');

                    bool isReadingCorrectMaterial = false;

                    for (int i = 0; i < mtlLines.Length; i++)
                    {
                        if (mtlLines[i].Trim(' ').StartsWith("newmtl"))
                        {
                            string currentName = mtlLines[i].Trim(' ').Remove(0, 6).Trim(' ');

                            isReadingCorrectMaterial = (currentName == materialName);
                            
                        }

                        if (isReadingCorrectMaterial &&
                            mtlLines[i].Trim(' ').ToLower().StartsWith("map_kd"))
                        {
                            string textureName = mtlLines[i].Trim(' ').Remove(0, 6).Trim(' ');
                            texturePath = path.Remove(path.LastIndexOf('/')) + "/" + textureName;


                            // Import the texture and apply it to the mesh
                            string importPath = Application.dataPath + "/Resources/" + textureName;

                            File.Copy(
                                texturePath,
                                importPath,
                                true);

                            AssetDatabase.ImportAsset("Assets/Resources/" + textureName);
                            AssetDatabase.Refresh();

                            string loadName = textureName.Remove(textureName.LastIndexOf('.'));
                            Texture tex = Resources.Load<Texture>(loadName);
                            Renderer r = go.GetComponent<Renderer>();
                            r.sharedMaterial.SetTexture("_MainTex", tex);

                            
                            me.hasDefaultUVs = true;
                            me.defaultUVs = meshUvs.ToArray();
                            me.uvMaps = new List<MeshEdit.UVData>();
                            //me.uvMaps.Add(new MeshEdit.UVBasic(uvs, tex));


                        }
                    }

                    #endregion

                    GameObject[] objects = new GameObject[1];
                    objects[0] = me.gameObject;
                    Selection.objects = objects;
                    Selection.activeGameObject = me.gameObject;

                    SceneView sceneView = (SceneView)SceneView.sceneViews[0];
                    sceneView.Focus();

                }
            }
        }
    }


    
    public static bool parseVector3(string text, out Vector3 vector, string format = "# # #")
    {
        int formatPosition = 0;

        string[] vectorElements = new string[3];
        int vectorElement = 0;

        for (int i = 0; i < text.Length; i++)
        {

            // Extract all characters until the next character in the format is found
            // # represents a number
            if (format[formatPosition] == '#')
            {
                vectorElements[vectorElement] += text[i];

                if (text.Length > i + 1 &&
                    format.Length > formatPosition + 1 && 
                    text[i + 1] == format[formatPosition + 1])
                {
                    vectorElement++;
                    formatPosition++;
                }
            }
            else if (format[formatPosition] == text[i])
            {
                formatPosition++;
            }
            else
            {
                continue;
            }
        }

        float a, b, c;

        if (float.TryParse(vectorElements[0], NumberStyles.Any, CultureInfo.InvariantCulture, out a) &&
            float.TryParse(vectorElements[1], NumberStyles.Any, CultureInfo.InvariantCulture, out b) &&
            float.TryParse(vectorElements[2], NumberStyles.Any, CultureInfo.InvariantCulture, out c))
        {

            vector = new Vector3(a, b, c);

            return true;
        }

        vector = Vector3.zero;

        return false;
    }

    public static bool parseVector2(string text, out Vector2 vector, string format = "# #")
    {
        int formatPosition = 0;

        string[] vectorElements = new string[2];
        int vectorElement = 0;

        for (int i = 0; i < text.Length; i++)
        {

            // Extract all characters until the next character in the format is found
            // # represents a number
            if (format[formatPosition] == '#')
            {
                vectorElements[vectorElement] += text[i];

                if (text.Length > i + 1 &&
                    format.Length > formatPosition + 1 && 
                    text[i + 1] == format[formatPosition + 1])
                {
                    vectorElement++;
                    formatPosition++;
                }
            }
            else if (format[formatPosition] == text[i])
            {
                formatPosition++;
            }
            else
            {
                continue;
            }
        }

        float a, b;

        if (float.TryParse(vectorElements[0], NumberStyles.Any, CultureInfo.InvariantCulture, out a) &&
            float.TryParse(vectorElements[1], NumberStyles.Any, CultureInfo.InvariantCulture, out b))
        {

            vector = new Vector2(a, b);

            return true;
        }

        vector = Vector2.zero;

        return false;
    }

    private void exportFbx(MeshEdit solid, string path, string name)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Create(path + name + ".fbx")))
        {
            writer.Write("Kaydara FBX Binary  \x00"); // Header
            writer.Write((byte)0x1A); // ???
            writer.Write((byte)0x00); // ???
            writer.Write((uint)7300); // Version number
        }
    }

    float tileZoom = 24;
    Vector2 scrollPosition = Vector2.zero;

    List<Vector2> uvCoord = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
    List<Vector2> uvCoordSnapped = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
    int uvCoordSelected = -1;
    int uvCoordEdgeSelected = -1;
    bool uvCoordBodySelected = false;
    bool useCustomUVCoords = true;
    bool snapUVsToPixel = true;
    Vector2 uvRectLastMouseClickPosition;
    float uvEdgeSelectionDistance = 12.0f;


    private void guiTextureTiling(MeshEdit solid, int controlId)
    {
        GUILayout.BeginArea(new Rect(uiPosition.x, uiPosition.y, 340, 800));

        Rect editRect = EditorGUILayout.BeginVertical();
        GUI.Box(editRect, GUIContent.none);
        // Dropdown selector

        if (solid.hasDefaultUVs)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("This mesh has been imported with its own texture and UV coordinates. Once you apply a tileset to this mesh, you will have to manually apply the original texture to revert it, and the current UV data may be changed irreperably.");

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Click this button to convert the UVs to a tileset.");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            if (GUILayout.Button("Convert to Tileset", skinDefault.GetStyle("Button"), GUILayout.Width(160)))
            {
                solid.hasDefaultUVs = false;
                loadTilesets();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(16);
            GUILayout.EndVertical();
        }
        else if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(6);

            int oldTileset = selectedTileset;
            selectedTileset = EditorGUILayout.Popup(selectedTileset, tilesetTexturesAvailable.ToArray());
            if (selectedTileset != oldTileset || tiles == null || solid.isTilesetRefreshRequired)
            {
                solid.lastTilesetUsed = selectedTileset;
                
                if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
                {

                    loadTilesFromTileset(selectedTileset);

                    solid.isTilesetRefreshRequired = false;

                    string name = tilesetsAvailable[selectedTileset].tilesetName;
                    int indexOfCurrent = solid.uvMaps.FindIndex(map => map.name == name);

                    if (indexOfCurrent == -1)
                    {
                        solid.uvMaps.Add(
                            new MeshEdit.UVData(
                                tilesetsAvailable[selectedTileset].tilesetName,
                                tilesetsAvailable[selectedTileset].texturePage.width,
                                tilesetsAvailable[selectedTileset].texturePage.height,
                                tilesetsAvailable[selectedTileset].tileWidth,
                                tilesetsAvailable[selectedTileset].tileHeight,
                                tilesetsAvailable[selectedTileset].tileOutline,
                                solid.verts.Count,
                                solid.defaultUVs));

                        indexOfCurrent = solid.uvMaps.Count - 1;
                    }
                    else
                    {
                        solid.currentUVMap = indexOfCurrent;

                        //solid.uvMaps[solid.currentUVMap].resizeUVSpace(texturePage.width, texturePage.height, tileWidth, tileHeight, tilesetsAvailable[solid.currentUVMap]);
                    }

                    solid.currentUVMap = indexOfCurrent;

                    Renderer r = solid.gameObject.GetComponent<Renderer>();
                    r.sharedMaterial.SetTexture("_MainTex", texturePage);


                    updateMeshUVs(solid);


                    solid.uvMaps[solid.currentUVMap].tileWidth = tileWidth;
                    solid.uvMaps[solid.currentUVMap].tileHeight = tileHeight;
                    solid.uvMaps[solid.currentUVMap].tileOutline = tileOutline;
                }
            }


            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            
                // Tile picker
                if (tiles != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(6);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Zoom");
                tileZoom = GUILayout.HorizontalSlider(tileZoom, 16, 128, GUILayout.Width(100));
                EditorGUILayout.EndVertical();
                // Chosen tile and toolbar
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(4);


                tilesPerRow = tiles.GetLength(0);
                tilesPerColumn = tiles.GetLength(1);

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(320), GUILayout.Height(240));

                for (int y = 0; y < tilesPerColumn; y++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    GUI.backgroundColor = new Color(0.5f, 0.5f, 1.0f);

                    for (int x = 0; x < tilesPerRow; x++)
                    {
                        Rect selectRect = EditorGUILayout.BeginHorizontal(GUILayout.Width(tileZoom + 2), GUILayout.Height(tileZoom + 2));
                        skin.box.normal.background = (Texture2D)tileDirectionTexture[0];

                        if (y * tilesPerRow + x == selectedTile) { GUI.Box(selectRect, GUIContent.none); }
                        skin.button.normal.background = tiles[x, y];
                        if (GUILayout.Button("", GUILayout.Width(tileZoom), GUILayout.Height(tileZoom)))
                        {
                            selectedTile = y * tilesPerRow + x;
                            //Debug.Log("Picked tile " + (selectedTile % tilesPerRow).ToString() + ", " + (selectedTile / tilesPerRow).ToString());
                            GUIUtility.hotControl = controlId;

                            Event.current.Use();
                        }

                        Rect r = GUILayoutUtility.GetLastRect();
                        Color col = Color.white;

                        if (r.Contains(Event.current.mousePosition))
                        {
                            col = Color.red;
                        }
                        if (selectedTile == x + y * tilesPerRow)
                        {
                            col = Color.blue;
                        }
                        GUI.DrawTexture(r, tiles[x, y], ScaleMode.ScaleToFit, true, 0.0f, col, 0, 0);
                        
                        EditorGUILayout.EndHorizontal();
                        skin.box.normal.background = texWindow;
                    }
                    EditorGUILayout.EndHorizontal();

                }



                GUILayout.EndScrollView();
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                if (tiles != null && selectedTile >= 0 && selectedTile < tiles.Length)
                {
                    //skin.box.normal.background = tiles[selectedTile % tilesPerRow, selectedTile / tilesPerRow];
                    bool queueClick = false;

                    drawBox(208, 208);

                    Rect activeRect = new Rect(dumRectangle.x - 4, dumRectangle.y - 4, 208, 208);
                    //if (activeRect.Contains(Event.current.mousePosition))
                    {
                        Vector2 mousePos = Event.current.mousePosition;

                        if (uvCoordEdgeSelected >= 0 || uvCoordSelected >= 0 || uvCoordBodySelected)
                        {
                            Vector2 newMousePos = mousePos - dumRectangle;

                            Vector2 difference = (newMousePos - uvRectLastMouseClickPosition);

                            // Move
                            difference.x = difference.x / 200.0f;
                            difference.y = difference.y / 200.0f;

                            if (uvCoordSelected >= 0)
                            {
                                uvCoord[uvCoordSelected] += difference;
                            }
                            else if (uvCoordEdgeSelected >= 0)
                            {
                                uvCoord[uvCoordEdgeSelected] += difference;
                                uvCoord[(uvCoordEdgeSelected + 1) % uvCoord.Count] += difference;
                            }
                            else if (uvCoordBodySelected)
                            {
                                for (int i = 0; i < uvCoord.Count; i++)
                                {
                                    uvCoord[i] += difference;
                                }
                            }

                            snapUVCoords();

                            uvRectLastMouseClickPosition = newMousePos;

                        }
                        else
                        {
                            if (Event.current.type == EventType.MouseDown &&
                                (activeRect.Contains(Event.current.mousePosition)))
                            {
                                uvRectLastMouseClickPosition = mousePos - dumRectangle;

                                float sqrDist = uvEdgeSelectionDistance * uvEdgeSelectionDistance;

                                int closestUVCoord = -1;
                                float distToClosestUVCoord = float.MaxValue;
                                for (int i = 0; i < uvCoord.Count; i++)
                                {
                                    Vector2 uvCoordPixelPosition = new Vector2(uvCoord[i].x * 200, uvCoord[i].y * 200) + dumRectangle;
                                    float d = (uvCoordPixelPosition - mousePos).sqrMagnitude;
                                    if (d <= sqrDist && d < distToClosestUVCoord)
                                    {
                                        closestUVCoord = i;
                                        distToClosestUVCoord = d;
                                    }
                                }
                                if (closestUVCoord != -1)
                                {
                                    uvCoordBodySelected = false;
                                    uvCoordEdgeSelected = -1;
                                    uvCoordSelected = closestUVCoord;
                                }
                                else
                                {
                                    for (int i = 0; i < uvCoord.Count; i++)
                                    {
                                        Vector2 uvCoordPixelPositionA = new Vector2(uvCoord[i].x * 200, uvCoord[i].y * 200) + dumRectangle;
                                        Vector2 uvCoordPixelPositionB = new Vector2(uvCoord[(i + 1) % uvCoord.Count].x * 200, uvCoord[(i + 1) % uvCoord.Count].y * 200) + dumRectangle;
                                        float d = (closestPoint(uvCoordPixelPositionA, uvCoordPixelPositionB, mousePos) - mousePos).sqrMagnitude;
                                        if (d <= sqrDist && d < distToClosestUVCoord)
                                        {
                                            closestUVCoord = i;
                                            distToClosestUVCoord = d;
                                        }
                                    }
                                    if (closestUVCoord != -1)
                                    {
                                        uvCoordBodySelected = false;
                                        uvCoordEdgeSelected = closestUVCoord;
                                        uvCoordSelected = -1;
                                    }
                                    else
                                    {
                                        uvCoordBodySelected = true;
                                    }
                                }

                                // Push snapped verts into main vert list

                                for (int i = 0; i < uvCoord.Count; i++)
                                {
                                    uvCoord[i] = uvCoordSnapped[i];
                                }
                            }
                        }
                    }

                    if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow)
                    {
                        uvCoordBodySelected = false;
                        uvCoordEdgeSelected = -1;
                        uvCoordSelected = -1;
                    }

                    GUILayout.Space(6);
                    Color col = Color.white; 
                    GUI.DrawTexture(new Rect(dumRectangle.x, dumRectangle.y, 200, 200), tiles[selectedTile % tilesPerRow, selectedTile / tilesPerRow], ScaleMode.ScaleToFit, true, 0.0f, col, 0, 0);
                    glDrawUVs();
                    for (int i = 0; i < uvCoord.Count; i++)
                    {
                        if (solid.debugMode != MeshEdit.DebugShowInfo.Off)
                        {
                            GUI.DrawTexture(
                                new Rect(
                                    dumRectangle.x + uvCoordSnapped[i].x * 200 - 4,
                                    dumRectangle.y + uvCoordSnapped[i].y * 200 - 4,
                                    8,
                                    8),
                                texPixel, ScaleMode.ScaleToFit, true, 0.0f, new Color(i / 3.0f, i / 3.0f, i / 3.0f, 1.0f), 0, 0);
                        }
                        else
                        {
                            GUI.DrawTexture(
                                new Rect(
                                    dumRectangle.x + uvCoordSnapped[i].x * 200 - 4,
                                    dumRectangle.y + uvCoordSnapped[i].y * 200 - 4,
                                    8,
                                    8),
                                texPixel, ScaleMode.ScaleToFit, true, 0.0f, Color.cyan, 0, 0);
                        }
                    }


                    EditorGUILayout.BeginVertical();


                    // skin.box.normal.background = texWindow;
                    // skin.button.normal.background = texWindow;
                    //skin.button.
                    skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Editor/MeshEdit/EditorTools.guiskin");
                    GUI.skin = skin;
                    GUI.skin.box.normal.background = skin.box.normal.background;
                    if (GUILayout.Button("Reset", skinDefault.GetStyle("Button"), GUILayout.Width(100)))
                    {
                        uvCoord = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
                        uvCoordSnapped = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
                    }
                    bool temp = useCustomUVCoords;
                    useCustomUVCoords = GUILayout.Toggle(useCustomUVCoords, "Use custom UVs");
                    if (temp != useCustomUVCoords)
                    {
                        saveSettings();
                    }
                    temp = snapUVsToPixel;
                    snapUVsToPixel = GUILayout.Toggle(snapUVsToPixel, "Snap to pixel");
                    if (temp != snapUVsToPixel)
                    {
                        saveSettings();
                    }
                    GUILayout.Space(8);
                    skin.box.normal.background = texWindow;
                    if (GUILayout.Button(tileDirectionTexture[tileDirection], GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        tileDirection++;
                    }
                    GUILayout.Label("Move UVs");
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("-90", skinDefault.GetStyle("Button"), GUILayout.Width(50)))
                    {
                        rotateUVCoordsCCW();
                        snapUVCoords();
                    }
                    if (GUILayout.Button("+90", skinDefault.GetStyle("Button"), GUILayout.Width(50)))
                    {
                        rotateUVCoordsCW();
                        snapUVCoords();
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                GUILayout.Space(16); 

                EditorGUILayout.EndHorizontal();
                if (helpMode)
                {
                    GUILayout.Space(8);
                    GUILayout.Label("W, A, S, D - Select a tile from the tileset");
                    GUILayout.Label("Q, E - Rotate the tile");
                    GUILayout.Label("To create new tilesets, open the Tilesets window under \"Window/Tilesets.\"");
                }
                GUILayout.Space(16);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("No tilesets found, create one using \"Window/Tilesets\"");
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        // Catches clicks on box
        GUI.Button(editRect, "", GUIStyle.none);
        GUILayout.EndArea();
    }

    private void glDrawUVs()
    {
        
        #region GLDrawing
        if (Event.current.type == EventType.Repaint)
        {
            CreateLineMaterial();
            // Apply the line material

            GL.PushMatrix();
            GL.Clear(true, false, Color.black);

            lineMat.SetPass(0);

            GL.Begin(GL.QUADS);

            GL.Color(new Color(1.0f, 1.0f, 1.0f, 0.25f));

            for (int i = 0; i < uvCoord.Count; i++)
            {
                Vector2 uvCoordPixelPosition = new Vector2(uvCoordSnapped[i].x * 200, uvCoordSnapped[i].y * 200) + dumRectangle;
                GL.Vertex3(uvCoordPixelPosition.x, uvCoordPixelPosition.y, 0.0f);

            }

            GL.End();
            GL.Begin(GL.LINE_STRIP);

            GL.Color(new Color(0.0f, 0.0f, 0.0f, 1.0f));

            for (int i = 0; i < uvCoord.Count + 1; i++)
            {
                Vector2 uvCoordPixelPosition = new Vector2(uvCoordSnapped[i % uvCoord.Count].x * 200, uvCoordSnapped[i % uvCoord.Count].y * 200) + dumRectangle;
                GL.Vertex3(uvCoordPixelPosition.x, uvCoordPixelPosition.y, 0.0f);

            }

            GL.End();
            GL.PopMatrix();
        }
        #endregion
    }

    private void rotateUVCoordsCCW()
    {
        Vector2 origin = new Vector2(0.5f, 0.5f);
        Vector2 tempVector = rotate(uvCoord[0], -90, origin);
        uvCoord[0] = rotate(uvCoord[1], -90, origin);
        uvCoord[1] = rotate(uvCoord[2], -90, origin);
        uvCoord[2] = rotate(uvCoord[3], -90, origin);
        uvCoord[3] = tempVector;
    }

    private void rotateUVCoordsCW()
    {
        Vector2 origin = new Vector2(0.5f, 0.5f);

        Vector2 tempVector = rotate(uvCoord[3], 90, origin);
        uvCoord[3] = rotate(uvCoord[2], 90, origin);
        uvCoord[2] = rotate(uvCoord[1], 90, origin);
        uvCoord[1] = rotate(uvCoord[0], 90, origin);
        uvCoord[0] = tempVector;
    }

    private void snapUVCoords()
    {

        // Constrain & snap
        float pixelUnitWidth = 1.0f / tilesetsAvailable[selectedTileset].tileWidth;
        float pixelUnitHeight = 1.0f / tilesetsAvailable[selectedTileset].tileHeight;

        for (int i = 0; i < uvCoord.Count; i++)
        {
            uvCoord[i] = new Vector2(
                Mathf.Clamp(uvCoord[i].x, 0.0f, 1.0f),
                Mathf.Clamp(uvCoord[i].y, 0.0f, 1.0f));

            Vector2 snappedCoord = uvCoord[i];

            if (snapUVsToPixel)
            {
                snappedCoord = new Vector2(
                    Mathf.Round(uvCoord[i].x / pixelUnitWidth) * pixelUnitWidth,
                    Mathf.Round(uvCoord[i].y / pixelUnitHeight) * pixelUnitWidth);

            }
            uvCoordSnapped[i] = snappedCoord;
        }
    }

    public static Vector2 rotate(Vector2 point, float degree, Vector2 origin)
    {
        point -= origin;
        degree = Mathf.Deg2Rad * degree;

        float cos = Mathf.Cos(degree);
        float sin = Mathf.Sin(degree);

        return new Vector2(
            cos * point.x - sin * point.y,
            sin * point.x + cos * point.y) + origin;
    }

    void drawBox(int w, int h)
    {
        Texture2D pixel = new Texture2D(1, 1);

        pixel.SetPixel(0, 0, Color.black);
        pixel.Apply();

        GUIStyle style = new GUIStyle();
        style.normal.background = pixel;
        //GUI.skin.box.normal.background = pixel;
        GUILayout.Box(GUIContent.none, style, GUILayout.Width(w), GUILayout.Height(h));
    }

    public static Material lineMat;
    public static void CreateLineMaterial()
    {
        if (lineMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMat = new Material(shader);
            lineMat.hideFlags = HideFlags.HideAndDontSave;

            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMat.SetInt("_ZWrite", 0);
        }
    }


    public static void loadAllUVMaps(MeshEdit solid)
    {
        List<Tileset> tempTilesetsAvailable = new List<Tileset>();
        List<string> tempTexturesAvailable = new List<string>();
        loadTilesets();

        if (solid.uvMaps == null)
        {
            solid.uvMaps = new List<MeshEdit.UVData>();
        }

        if (tempTilesetsAvailable != null && tempTilesetsAvailable.Count > 0)
        {
            for (int i = 0; i < tempTilesetsAvailable.Count; i++)
            {
                string name = tempTilesetsAvailable[i].tilesetName;
                int indexOfCurrent = solid.uvMaps.FindIndex(map => map.name == name);

                if (indexOfCurrent == -1)
                {
                    solid.uvMaps.Add(
                    new MeshEdit.UVData(
                        tilesetsAvailable[i].tilesetName,
                        tilesetsAvailable[i].texturePage.width,
                        tilesetsAvailable[i].texturePage.height,
                        tilesetsAvailable[i].tileWidth,
                        tilesetsAvailable[i].tileHeight,
                        tilesetsAvailable[i].tileOutline,
                        solid.verts.Count,
                        solid.defaultUVs));
                }

            }
        }
    }


    private void operationsTextureTiling(MeshEdit solid, int controlId)
    {
        if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            GameObject obj = Selection.activeTransform.gameObject;
            //Solid solid = obj.GetComponent<Solid>();
            // meshFilter = obj.GetComponent<MeshFilter>();
            //Mesh meshCopy = Mesh.Instantiate(meshFilter.sharedMesh) as Mesh;
            MeshEdit meshEdit = obj.GetComponent<MeshEdit>();// meshFilter.sharedMesh;

            solid.checkMeshValidity();

            if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && Event.current.button == 0)
            {
                selectedTri = -1;
                checkFrame = 0;

                ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition); //Camera.current.ScreenPointToRay(Input.mousePosition);


                if (meshEdit.mesh != null)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    Vector3 colPoint = Vector3.zero;
                    float d = float.MaxValue;
                    for (int i = 0; i < solid.tris.Count; i += 3)
                    {
                        t = new MeshEdit.Triangle(
                            solid.verts[solid.tris[i + 0]],
                            solid.verts[solid.tris[i + 1]],
                            solid.verts[solid.tris[i + 2]]);

                        if (rayIntersectsTriangle(ray.origin, ray.direction, t, ref colPoint))
                        {
                            float dd = Vector3.SqrMagnitude(colPoint - ray.origin);
                            if (dd < d)
                            {
                                d = dd;
                                selectedTri = i;
                                selectedQuad = i / 6;
                            }
                        }
                    }
                }
            }
            
            if (Event.current.type == EventType.MouseUp )
            {
                canTextureFaces = true;
            }
            if (selectedTri >= 0)
            {
                if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && Event.current.button == 0)
                {
                    if (Event.current.modifiers == EventModifiers.Alt || Event.current.modifiers == EventModifiers.Command || Event.current.modifiers == EventModifiers.Control)
                    {
                        useCustomUVCoords = true;
                        
                        canTextureFaces = false;

                        getUVsOfSelectedQuads(solid, selectedQuad);
                    }
                    else if (canTextureFaces)
                    {
                        // Optimisation:
                        // Chec to see if any change is nevessary
                        // Try to eliminate the deep copy

                        //Undo.RecordObject(meshFilter.mesh, "Model Mesh");
                        Undo.RegisterCompleteObjectUndo(solid, "Model Mesh Solid");

                        solid.uvMaps[solid.currentUVMap].newUvs = new Vector2[meshEdit.mesh.vertices.Length];
                        //Vector2[] newUVs = solid.uvMaps[solid.currentUVMap].newUvs;
                        Vector2[] newUVs = solid.uvMaps[solid.currentUVMap].uvs;

                        var watch = System.Diagnostics.Stopwatch.StartNew();

                        if (solid.uvMaps[solid.currentUVMap].uvs != null &&
                            solid.uvMaps[solid.currentUVMap].uvs.Length == meshEdit.mesh.uv.Length)
                        {
                            /*
                            for (int i = 0; i < mesh.vertices.Length; i++)
                            {
                                newUVs[i] = solid.uvMaps[solid.currentUVMap].uvs[i];
                            }*/
                        }
                        else
                        {
                            for (int i = 0; i < meshEdit.mesh.vertices.Length; i++)
                            {
                                newUVs[i] = meshEdit.mesh.uv[i];
                            }
                        }

                        watch.Stop();
                        long elapsedMs = watch.ElapsedMilliseconds;

                        int[] coords = { -1, -1, -1, -1 };
                        /*coords[0] = mesh.triangles[selectedTri];
                        coords[1] = mesh.triangles[selectedTri + 1];
                        coords[2] = mesh.triangles[selectedTri + 2];
                        coords[3] = findAdjacentTriPoint(mesh.triangles, mesh.vertices, ref coords[0], ref coords[1], ref coords[2]);
                        */
                        coords[0] = solid.quads[selectedQuad * 4 + 0];
                        coords[1] = solid.quads[selectedQuad * 4 + 1];
                        coords[2] = solid.quads[selectedQuad * 4 + 2];
                        coords[3] = solid.quads[selectedQuad * 4 + 3];


                        /*
                        int[] coords = solid.getQuadFromTriTable(selectedTri);*/

                        int topRight = -1;
                        int topLeft = -1;
                        int bottomRight = -1;
                        int bottomLeft = -1;

                        #region reorganise the quad vertices so that they represent the correct tile rotation.

                        Vector2[] screenCoords = new Vector2[4];
                        screenCoords[0] = HandleUtility.WorldToGUIPoint(solid.verts[coords[0]]);
                        screenCoords[1] = HandleUtility.WorldToGUIPoint(solid.verts[coords[1]]);
                        screenCoords[2] = HandleUtility.WorldToGUIPoint(solid.verts[coords[2]]);
                        screenCoords[3] = HandleUtility.WorldToGUIPoint(solid.verts[coords[3]]);


                        float min1 = Mathf.Min(screenCoords[0].y, screenCoords[1].y, screenCoords[2].y, screenCoords[3].y);
                        for (int i = 0; i < 4; i++)
                        {
                            if (Mathf.Abs(screenCoords[i].y - min1) <= float.Epsilon)
                            {
                                screenCoords[i].y = float.MaxValue;
                                topRight = i;
                                break;
                            }
                        }
                        float min2 = Mathf.Min(screenCoords[0].y, screenCoords[1].y, screenCoords[2].y, screenCoords[3].y);
                        for (int i = 0; i < 4; i++)
                        {
                            if (Mathf.Abs(screenCoords[i].y - min2) <= float.Epsilon && topLeft == -1)
                            {
                                screenCoords[i].y = float.MaxValue;
                                topLeft = i;
                            }
                            else if (screenCoords[i].y < float.MaxValue)
                            {
                                if (bottomRight == -1) { bottomRight = i; } else { bottomLeft = i; }
                            }
                        }
                        if (screenCoords[topLeft].x > screenCoords[topRight].x)
                        {
                            int temp = topLeft;
                            topLeft = topRight;
                            topRight = temp;

                        }
                        if (screenCoords[bottomLeft].x > screenCoords[bottomRight].x)
                        {
                            int temp = bottomLeft;
                            bottomLeft = bottomRight;
                            bottomRight = temp;
                        }

                        rotate(tileDirection, ref topLeft, ref topRight, ref bottomRight, ref bottomLeft);
                        #endregion

                        Color col = Color.white;
                        float outline = tilesetsAvailable[selectedTileset].tileOutline;

                        float epsilonX = outline / texturePage.width;
                        float epsilonY = outline / texturePage.height;
                        // Convert from the tileset selected tile to the appropriate tile on the texture page.
                        Vector2 targetTile = new Vector2(selectedTile % tilesPerRow, selectedTile / tilesPerRow);
                        int indexOfTarget = tilesetsAvailable[selectedTileset].tilesetMappedPoints.IndexOf(targetTile);

                        if (indexOfTarget >= 0)
                        {
                            targetTile = tilesetsAvailable[selectedTileset].editorMappedPoints[indexOfTarget];
                        }


                        int tx = (int)targetTile.x;
                        int tPC = texturePage.height / (tileHeight + (int)outline * 2);
                        int ty = (tPC - 1) - (int)targetTile.y;
                        float txx = tx / (float)tilesPerRow;
                        float tyy = ty / (float)tPC;
                        float ww = 1.0f / tilesPerRow;
                        float hh = 1.0f / tPC;

                        if (!useCustomUVCoords)
                        {
                            newUVs[coords[topLeft]] = new Vector2(txx + epsilonX, tyy + hh - epsilonY);
                            newUVs[coords[topRight]] = new Vector2(txx + ww - epsilonX, tyy + hh - epsilonY);
                            newUVs[coords[bottomLeft]] = new Vector2(txx + epsilonX, tyy + epsilonY);
                            newUVs[coords[bottomRight]] = new Vector2(txx + ww - epsilonX, tyy + epsilonY);
                        }
                        else
                        {
                            List<Vector2> uvsInEpsilon = new List<Vector2>();
                            for (int i = 0; i < uvCoord.Count; i++)
                            {
                                Vector2 coord = new Vector2(
                                    (uvCoordSnapped[i].x * tileWidth) / (tileWidth + outline * 2),
                                    (uvCoordSnapped[i].y * tileHeight) / (tileHeight + outline * 2));

                                uvsInEpsilon.Add(coord);
                            }

                            txx = (targetTile.x + ((uvsInEpsilon[0].x))) / tilesPerRow;
                            tyy = 1.0f - (targetTile.y + uvsInEpsilon[0].y) / tPC;
                            newUVs[coords[topLeft]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                            txx = (targetTile.x + uvsInEpsilon[1].x) / tilesPerRow;
                            tyy = 1.0f - (targetTile.y + uvsInEpsilon[1].y) / tPC;
                            newUVs[coords[topRight]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                            txx = (targetTile.x + uvsInEpsilon[2].x) / tilesPerRow;
                            tyy = 1.0f - (targetTile.y + uvsInEpsilon[2].y) / tPC;
                            newUVs[coords[bottomRight]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                            txx = (targetTile.x + uvsInEpsilon[3].x) / tilesPerRow;
                            tyy = 1.0f - (targetTile.y + uvsInEpsilon[3].y) / tPC;
                            newUVs[coords[bottomLeft]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                        }
                        solid.uvMaps[solid.currentUVMap].vertCount = meshEdit.mesh.uv.Length;

                        int animationLength = tilesetsAvailable[selectedTileset].getAnimationLength(selectedTile);
                        // Debug.Log("Animation Length of selected tile = " + animationLength);
                        solid.uvMaps[solid.currentUVMap].uvAnimationLength[coords[topLeft]] = animationLength;
                        solid.uvMaps[solid.currentUVMap].uvAnimationLength[coords[topRight]] = animationLength;
                        solid.uvMaps[solid.currentUVMap].uvAnimationLength[coords[bottomLeft]] = animationLength;
                        solid.uvMaps[solid.currentUVMap].uvAnimationLength[coords[bottomRight]] = animationLength;

                        solid.uvMaps[solid.currentUVMap].uvAnimationIndex[coords[topLeft]] = 0;
                        solid.uvMaps[solid.currentUVMap].uvAnimationIndex[coords[topRight]] = 0;
                        solid.uvMaps[solid.currentUVMap].uvAnimationIndex[coords[bottomLeft]] = 0;
                        solid.uvMaps[solid.currentUVMap].uvAnimationIndex[coords[bottomRight]] = 0;

                        int startPos = tx + ty * tilesPerRow;
                        solid.uvMaps[solid.currentUVMap].uvAnimationStartPosition[coords[topLeft]] = startPos;
                        solid.uvMaps[solid.currentUVMap].uvAnimationStartPosition[coords[topRight]] = startPos;
                        solid.uvMaps[solid.currentUVMap].uvAnimationStartPosition[coords[bottomLeft]] = startPos;
                        solid.uvMaps[solid.currentUVMap].uvAnimationStartPosition[coords[bottomRight]] = startPos;

                        solid.uvMaps[solid.currentUVMap].uvAnimationQuadPoint[coords[topLeft]] = 0;
                        solid.uvMaps[solid.currentUVMap].uvAnimationQuadPoint[coords[topRight]] = 1;
                        solid.uvMaps[solid.currentUVMap].uvAnimationQuadPoint[coords[bottomLeft]] = 3;
                        solid.uvMaps[solid.currentUVMap].uvAnimationQuadPoint[coords[bottomRight]] = 2;

                        solid.uvMaps[solid.currentUVMap].uvs = newUVs;
                        // Debug.Log("Adding tile to mesh...");
                        //meshFilter.sharedMesh.uv = newUVs;
                        meshEdit.mesh.uv = newUVs;
                        meshEdit.pushLocalMeshToGameObject();
                        // Tell the UI your event is the main one to use, it override the selection in  the scene view
                    }
                    GUIUtility.hotControl = controlId;

                    Event.current.Use();
                }
            }
        }
    }

    bool canTextureFaces = true;

    private void getUVsOfSelectedQuads(MeshEdit solid, int selectedQuad)
    {
        // This function doesn't take into account quads that aren't planar, and won't deform the uv to accurately map the space a saddled quad will occupy
        // This isn't the hardest thing to fix but I'm lazy :)

        Vector2 origin = new Vector2(0.5f, 0.5f);

        if (Event.current.modifiers == EventModifiers.Command || Event.current.modifiers == EventModifiers.Control)
        {
            Vector3 up = -Vector3.up;
            Vector3 planeNormal = solid.faceNormals[selectedQuad];

            if (Vector3.Cross(up, planeNormal).sqrMagnitude < 0.000001f)
            {

                up = SceneView.lastActiveSceneView.camera.transform.up; //

                up.y = 0;
                up = up.normalized;
                up = roundNormalToAxis(up);

                if (Vector3.Cross(up, planeNormal).sqrMagnitude < 0.000001f)
                {
                    new Vector3(0.0f, 0.0f, 1.0f);
                }
            }
            uvCoord[0] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 0]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;
            uvCoord[1] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 1]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;
            uvCoord[2] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 3]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;
            uvCoord[3] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 2]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;

        }
        else
        {

            {
                uvCoord[0] = solid.uvMaps[solid.currentUVMap].uvs[solid.quads[selectedQuad * 4 + 0]];
                uvCoord[1] = solid.uvMaps[solid.currentUVMap].uvs[solid.quads[selectedQuad * 4 + 1]];
                uvCoord[2] = solid.uvMaps[solid.currentUVMap].uvs[solid.quads[selectedQuad * 4 + 3]];
                uvCoord[3] = solid.uvMaps[solid.currentUVMap].uvs[solid.quads[selectedQuad * 4 + 2]];
                float w = solid.uvMaps[solid.currentUVMap].texWidth;
                float h = solid.uvMaps[solid.currentUVMap].texHeight;
                float tw = solid.uvMaps[solid.currentUVMap].tileWidth;
                float th = solid.uvMaps[solid.currentUVMap].tileHeight;
                float outline = solid.uvMaps[solid.currentUVMap].tileOutline;

                for (int i = 0; i < uvCoord.Count; i++)
                {
                    float x = uvCoord[i].x * w;
                    float y = uvCoord[i].y * h;
                    
                    x = x % (tw + outline * 2) - outline;
                    y = y % (th + outline * 2) - outline;
                    uvCoord[i] = new Vector2(
                        x / tw,
                        1.0f - y / th);
                }


            }
        }

        // Ensure that the uv poly is clockwise


        float direction = 0;
        for (int i = 0; i < uvCoord.Count; i++)
        {
            direction += (uvCoord[(i + 1) % uvCoord.Count].x - uvCoord[i].x) * (uvCoord[(i + 1) % uvCoord.Count].y + uvCoord[i].y);
        }

        if (direction > 0)
        {
            // Flip the uv-shape so that it's clockwise!
            Vector2 temp = uvCoord[3];
            uvCoord[3] = uvCoord[0];
            uvCoord[0] = temp;
            temp = uvCoord[2];
            uvCoord[2] = uvCoord[1];
            uvCoord[1] = temp;
        }

        // Find the most suitable top-left coord

        int closestCorner = -1;
        float dTL = float.MaxValue;
        for (int i = 0; i < uvCoord.Count; i++)
        {
            float dd = uvCoord[i].sqrMagnitude;
            if (dd < dTL)
            {
                dTL = dd;
                closestCorner = i;
            }
        }

        while (closestCorner > 0)
        {
            Vector2 temp = uvCoord[0];
            uvCoord[0] = uvCoord[1];
            uvCoord[1] = uvCoord[2];
            uvCoord[2] = uvCoord[3];
            uvCoord[3] = temp;
            //rotateUVCoordsCW();
            closestCorner--;
        }
        
        // TODO: Constrain to the accurate size?

        if (snapUVsToPixel)
        {
            Vector2 difference = uvCoord[0];
            snapUVCoords();
            difference = uvCoordSnapped[0] - difference;
            for (int i = 0; i < uvCoord.Count; i++)
            {
                uvCoord[i] += difference;
            }
        }

        snapUVCoords();
    }

    private Vector3 roundNormalToAxis(Vector3 normal)
    {
        float max = Mathf.Max(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (max == Mathf.Abs(normal.x))
        {
            return new Vector3(1.0f * Mathf.Sign(normal.x), 0.0f, 0.0f);
        }
        else if (max == Mathf.Abs(normal.y))
        {
            return new Vector3(0.0f, 1.0f * Mathf.Sign(normal.y), 0.0f);
        }
        else
        {
            return new Vector3(0.0f, 0.0f, 1.0f * Mathf.Sign(normal.z));
        }
    }

    private Vector2 easyProjection(Vector3 point, Vector3 planeNormal, Vector3 planeOrigin, Vector3 up)
    {
        planeNormal = planeNormal.normalized;

        // Make up perpendicular to the normal
        up = (up - planeNormal * Vector3.Dot(planeNormal, up)).normalized;

        Vector3 side = Vector3.Cross(up, planeNormal).normalized;

        point = point - planeOrigin;
        point -= planeNormal * Vector3.Dot(planeNormal, point);

        float x = Vector3.Dot(point, side);
        float y = Vector3.Dot(point, up);

        return new Vector2(x, y);
    }

    [SerializeField, HideInInspector]
    Color paintColour;
    
    int paintMode = 0;

    Vector2 uiHeaderPosition = new Vector2(20, 20);
    Vector2 uiPosition = new Vector2(20, 60);
    Vector2 dumRectangle = new Vector2(20 - 10, 60 + 269);


    private void guiColourEditing(MeshEdit solid, int controlId)
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        GUILayout.BeginArea(new Rect(uiPosition.x, uiPosition.y, 340, 800));

        Rect editRect = EditorGUILayout.BeginVertical();
        GUI.Box(editRect, GUIContent.none);
        EditorGUILayout.BeginVertical();

        GUILayout.Space(16);
        GUILayout.BeginHorizontal();
        GUILayout.Space(6);

        int oldPaintMode = paintMode;
        GUILayout.Label("Paint Mode: ", GUILayout.Width(80));
        paintMode = EditorGUILayout.Popup(paintMode, new string[] { "Vertices", "Faces" }, GUILayout.Width(100));
        if (oldPaintMode != paintMode)
        {
            saveSettings();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(6);
        GUILayout.Label("Paint Colour: ", GUILayout.Width(80));
        
        paintColour = EditorGUILayout.ColorField(paintColour, GUILayout.Width(100));

        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        if (colourHistory == null)
        {
            colourHistory = new List<Color>();
        }
        skin.button.normal.background = null;
        for (int j = 0; j < maxColours / 10; j++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            Color c = Color.black;
            for (int i = j * 10; i < maxColours && i < (j + 1) * 10; i++)
            {
                if (i < colourHistory.Count)
                {
                    c = colourHistory[i];
                }
                else
                {
                    c = Color.black;
                }

                GUI.color = c;

                if (GUILayout.Button(texColourSwatch, GUILayout.Width(32), GUILayout.Width(22)))
                {
                    paintColour = c;
                }
            }

            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        if (helpMode)
        {
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.BeginVertical();
            if (paintMode == 0)
            {
                GUILayout.Label("LMB - Paint vert");
                GUILayout.Label("MMB - Remove colour on vert");
            }
            else if (paintMode == 1)
            {
                GUILayout.Label("LMB - Paint face");
                GUILayout.Label("MMB - Remove colour on face");
            }
            GUILayout.Label("Scroll - Change paintbrush size");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        // Catches clicks on box
        GUI.Button(editRect, "", GUIStyle.none);
        GUILayout.EndArea();
    }

    private void operationsColourEdit(MeshEdit solid, int controlId)
    {
        Vector2 mousePos = Vector2.zero;
        mousePos = Event.current.mousePosition;
        mousePos = constrainToScreenSize(mousePos);

        #region Selection Mode
        if (Event.current.type != EventType.Used)
        {
            if (Event.current.type == EventType.ScrollWheel && Event.current.modifiers == EventModifiers.Shift)
            {
                float s = Event.current.delta.y;
                selectionCircleRadius += s;
                if (selectionCircleRadius < 1)
                {
                    selectionCircleRadius = 1;
                }
                else if (selectionCircleRadius > 100)
                {
                    selectionCircleRadius = 100;
                }
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }

            GUI.color = Color.white;
            Graphics.DrawTexture(
                new Rect(
                    mousePos.x - selectionCircleRadius,
                    mousePos.y - selectionCircleRadius,
                    selectCircleTexture.width,
                    selectCircleTexture.width),
                selectCircleTexture);

            bool wasChanged = false;
            bool wasErasing = false;

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                Undo.RecordObject(solid, "Colouring");

                if (paintMode == 0)
                {
                    if (Event.current.button == 0)
                    {
                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (solid.colours[i] != paintColour &&
                                    !solid.isVertCovered(i) || solid.isMeshTransparent)
                                {
                                    solid.colours[i] = paintColour;
                                    for (int ii = 0; ii < solid.connectedVerts[i].Count; ii++)
                                    {
                                        solid.colours[solid.connectedVerts[i].list[ii]] = paintColour;
                                    }
                                    wasChanged = true;
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (solid.colours[i] != Color.white &&
                                    !solid.isVertCovered(i) || solid.isMeshTransparent)
                                {
                                    solid.colours[i] = Color.white;
                                    for (int ii = 0; ii < solid.connectedVerts[i].Count; ii++)
                                    {
                                        solid.colours[solid.connectedVerts[i].list[ii]] = Color.white;
                                    }
                                    wasChanged = true;
                                    wasErasing = true;
                                }
                            }
                        }
                    }
                }
                else if (paintMode == 1)
                {
                    if (Event.current.button == 0)
                    {
                        for (int i = 0; i < solid.faceNormals.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                {
                                    for (int ii =0; ii < 4; ii++)
                                    {

                                        if (solid.colours[i * 4 + ii] != paintColour)
                                        {
                                            solid.colours[i * 4 + ii] = paintColour;
                                            wasChanged = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        for (int i = 0; i < solid.faceNormals.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                {
                                    for (int ii = 0; ii < 4; ii++)
                                    {

                                        if (solid.colours[i * 4 + ii] != Color.white)
                                        {
                                            solid.colours[i * 4 + ii] = Color.white;
                                            wasChanged = true;
                                            wasErasing = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (wasChanged)
                {
                    if (!wasErasing)
                    {
                        setLatestColour();
                    }

                    solid.pushColour();
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                else if (Event.current.button == 2)
                {
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }
        }
        #endregion
    }

    private void setLatestColour()
    {
        if (colourHistory == null)
        {
            colourHistory = new List<Color>();
        }

        colourHistory.Insert(0, paintColour);

        for (int i = 1; i < colourHistory.Count; i++)
        {
            if (paintColour == colourHistory[i] ||
                i >= maxColours)
            {
                colourHistory.RemoveAt(i);
                i--;
            }
        }

        saveSettings();
    }

    private Vector3 maskVector(Vector3 v, Vector3 mask)
    {
        return new Vector3(
            v.x * mask.x,
            v.y * mask.y,
            v.z * mask.z);
    }

    List<Vector3> loopDebugMemory;
    List<Vector3> loopDebugMemory2;
    List<Vector3> loopDebugMemory3;
    List<Vector3> loopDebugMemory4;
    private List<int> getEdgeLoop(MeshEdit solid, int faceA, int vertAFromFaceA, int vertBFromFaceA)
    {
        loopDebugMemory = new List<Vector3>();
        loopDebugMemory2 = new List<Vector3>();
        loopDebugMemory3 = new List<Vector3>();
        List<int> vertsInLoopPartial = new List<int>();
        // Get the opposite side to begin the edge tracking
        int sideOfEdgeOnFaceA = -1;

        for (int i = 0; i < MeshEdit.quadEdgePatternClockwise.Length; i += 2)
        {
            if ((solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 0]] == vertAFromFaceA &&
                solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 1]] == vertBFromFaceA) ||
                (solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 0]] == vertBFromFaceA &&
                solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 1]] == vertAFromFaceA))
            {
                sideOfEdgeOnFaceA = i / 2;
                break;
            }
        }

        // The inputs were not accurate
        if (sideOfEdgeOnFaceA == -1)
        {
            return null;
        }


        int faceB = solid.adjacentFaces[faceA].list[sideOfEdgeOnFaceA];
        int sideOfEdgeOnFaceB = -1;
        if (faceB != -1)
        {
            sideOfEdgeOnFaceB = solid.adjacentFaces[faceB].list.IndexOf(faceA);
        }

        // Sync the "isNSCut" variables for each of the opposite faces. They must be defined so that they will move in the same direction
        bool isNSCutA = (sideOfEdgeOnFaceA == 1 || sideOfEdgeOnFaceA == 3);
        bool isNSCutB = false;

        if (faceB != -1)
        {
            isNSCutB = (sideOfEdgeOnFaceB == 1 || sideOfEdgeOnFaceB == 3);
        }
        
      
        // Face A is always "Real" as in it can't be -1
        // TODO: When we move to a system of polys instead of quads this method will need to be re-thought
        List<int> addedFacesA = new List<int>();
        List<int> addedFacesB = new List<int>();
        List<bool> addedFaceDirectionA = new List<bool>();
        List<bool> addedFaceDirectionB = new List<bool>();
        List<int> partialVertList = new List<int>();

        bool ignoreASide = (faceA == -1);
        bool ignoreBSide = (faceB == -1);

        if (ignoreBSide)

        {
            loopDebugMemory.Add(solid.quadCenter(faceA));

            loopDebugMemory.Add(solid.quadCenter(faceA) + Vector3.up);
        }
        else
        {
            loopDebugMemory.Add(solid.quadCenter(faceA));

            loopDebugMemory.Add(solid.quadCenter(faceB));
        }

        addedFacesA.Add(faceA);
        addedFacesB.Add(faceB);
        addedFaceDirectionA.Add(!isNSCutA);
        addedFaceDirectionB.Add(!isNSCutB);

        vertsInLoopPartial.Add(vertAFromFaceA);
        vertsInLoopPartial.Add(vertBFromFaceA);


        loopDebugMemory3.Add(solid.verts[vertAFromFaceA]);
        loopDebugMemory3.Add(solid.verts[vertBFromFaceA]);


        // keep adding quads to cut until you encounter a quad with no adjacent face to cut, OR if you encounter a quad that has already been cut in the SAME DIRECTION that you try to cut it
        // Repeat this loop forward and backward from the selected face
        // A cut along the North-West (NW) axis refers to the cut travelling from edge 0 to edge 1 (The edges created by verts 0-1 and 3-2)

        // A cut on an edge loop is similar, but it uses TWO, ADJACENT FACES instead of a lone one for the quad loop-select
        // Now, an additional condition is added. If the two face loops ever stop being adjacent in the same direction, the loop ends.
        // The original loop termination conditions apply for each face loop as well

        // There is also the contingency of searching for an edge loop on an edge with no adjacent face. In this case, the relevant side of the edge shouldn't be considered

        // TODO: Figure out why this won't flip-flop

        int edgeTheFirstCutAEnteredFrom = 0;

        int edgeTheFirstCutBEnteredFrom = 0;

        if (!isNSCutA)
        {
            edgeTheFirstCutAEnteredFrom = 1;
        }

        if (!isNSCutB)
        {
            edgeTheFirstCutBEnteredFrom = 1;
        }

        // Find the sides for FaceA and FaceB where one (and only one) vert on the shared edge are shared.
        {
            int vAA = vertAFromFaceA;
            int vAB = vertBFromFaceA;

            int sharedVert = vAA;

            // Find the correct side to start from on Face B
            if (!ignoreBSide)
            {
                for (int ii = 0; ii < 4; ii++)
                {
                    int vBA = solid.quads[faceB * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 0]];
                    int vBB = solid.quads[faceB * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 1]];

                    int sharedCount = 0;

                    if (solid.isShared(vAA, vBA))
                    {
                        sharedVert = vAA;
                        sharedCount++;
                    }
                    if (solid.isShared(vAB, vBA))
                    {
                        sharedVert = vAB;
                        sharedCount++;
                    }
                    if (solid.isShared(vAA, vBB))
                    {
                        sharedVert = vAA;
                        sharedCount++;
                    }
                    if (solid.isShared(vAB, vBB))
                    {
                        sharedVert = vAB;
                        sharedCount++;
                    }

                    if (sharedCount == 1)
                    {
                        edgeTheFirstCutBEnteredFrom = ii;
                        break;
                    }
                }
            }

            // Find the correct side to start from for Face A
            // This is the side that touches the shared vert, but does not face the edge that's added to the selection
            int checkedVert = -1;

            for (int ii = 0; ii < 4; ii++)
            {
                vAA = solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 0]];
                vAB = solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 1]];

                int sharedCount = 0;

                if (vAA == vertAFromFaceA)
                {
                    checkedVert = vAA;
                    sharedCount++;
                }
                if (vAB == vertAFromFaceA)
                {
                    checkedVert = vAB;
                    sharedCount++;
                }
                if (vAA == vertBFromFaceA)
                {
                    checkedVert = vAA;
                    sharedCount++;
                }
                if (vAB == vertBFromFaceA)
                {
                    checkedVert = vAB;
                    sharedCount++;
                }

                if (sharedCount == 1 && (checkedVert == sharedVert || ignoreBSide))
                {
                    edgeTheFirstCutAEnteredFrom = ii;
                    break;
                }
            }
        }

        for (int i = 0; i < 2; i++)
        {
            int edgeTheNextCutAEnteredFrom = edgeTheFirstCutAEnteredFrom;

            int edgeTheNextCutBEnteredFrom = edgeTheFirstCutBEnteredFrom;

            if (i > 0)
            {
                edgeTheNextCutAEnteredFrom = (edgeTheNextCutAEnteredFrom + 2) % 4;
                edgeTheNextCutBEnteredFrom = (edgeTheNextCutBEnteredFrom + 2) % 4;
            }

            int lastFaceA = faceA;
            int lastFaceB = faceB;

            int currentEdgeA = vertAFromFaceA;
            int currentEdgeB = vertBFromFaceA;

            bool isConnectionFound = true;

            int cc = 0;
            while (isConnectionFound)
            {
                cc++;
                if (cc > 10000)
                {
                    Console.WriteLine("Error");
                    break;
                }

                int nextFaceA = solid.adjacentFaces[lastFaceA].list[(edgeTheNextCutAEnteredFrom + 2) % 4];

                if (nextFaceA == -1)
                {
                    break;
                }

                edgeTheNextCutAEnteredFrom = solid.adjacentFaces[nextFaceA].list.IndexOf(lastFaceA);

                int nextFaceB = -1;
                if (!ignoreBSide)
                {
                    nextFaceB = solid.adjacentFaces[lastFaceB].list[(edgeTheNextCutBEnteredFrom + 2) % 4];
                    if (nextFaceB == -1)
                    {
                        break;
                    }
                    edgeTheNextCutBEnteredFrom = solid.adjacentFaces[nextFaceB].list.IndexOf(lastFaceB);

                }

                if (i > 0)
                {

                    loopDebugMemory4.Add(solid.quadCenter(lastFaceA));
                    loopDebugMemory4.Add(solid.quadCenter(nextFaceA));
                    if (!ignoreBSide)
                    {
                        loopDebugMemory4.Add(solid.quadCenter(lastFaceB));
                        loopDebugMemory4.Add(solid.quadCenter(nextFaceB));
                    }
                }
                else
                {
                    loopDebugMemory.Add(solid.quadCenter(lastFaceA));
                    loopDebugMemory.Add(solid.quadCenter(nextFaceA));

                    if (!ignoreBSide)
                    {
                        loopDebugMemory.Add(solid.quadCenter(lastFaceB));
                        loopDebugMemory.Add(solid.quadCenter(nextFaceB));
                    }
                }
                // The North-South variable isn't absolute, and a series of NS cuts may make a loop that cuts perpendicular along the loop. It's only necessary to compare a quad to itself in the case of a crossover in the loop.
                bool isNextAAnNSCut = (edgeTheNextCutAEnteredFrom == 1 || edgeTheNextCutAEnteredFrom == 3);
                bool isNextBAnNSCut = (edgeTheNextCutBEnteredFrom == 1 || edgeTheNextCutBEnteredFrom == 3);

                // Make sure "nextFace" hasn't already been cut in the same way
                if (!ignoreASide)
                {
                    int repeatIndexA = addedFacesA.IndexOf(nextFaceA);
                    if (repeatIndexA >= 0)
                    {
                        if (addedFaceDirectionA[repeatIndexA] == isNextAAnNSCut)
                        {
                            isConnectionFound = false;
                        }
                    }
                }
                if (!ignoreBSide)
                {
                    int repeatIndexB = addedFacesB.IndexOf(nextFaceB);

                    if (repeatIndexB >= 0)
                    {
                        if (addedFaceDirectionB[repeatIndexB] == isNextBAnNSCut)
                        {
                            isConnectionFound = false;
                        }
                    }
                }

                if (!ignoreASide && !ignoreBSide)
                {
                    // Check to see if the two sides stop being adjacent
                    if (solid.areFacesAdjacent(nextFaceA, nextFaceB))
                    {
                        // No discrepancy found
                    }
                    else
                    {
                        isConnectionFound = false;
                    }
                }

                if (isConnectionFound)
                {
                    bool foundVerts = false;
                    // Pick the correct two verts to add to the partial list

                    for (int j = 0; j < 4; j ++)
                    {
                        //if (j / 2 != edgeTheNextCutAEnteredFrom &&
                         //   j / 2 != (edgeTheNextCutAEnteredFrom + 2) % 4)
                        {
                            //if (j == solid.adjacentFaces[nextFaceA].list.IndexOf(nextFaceB))
                            if (solid.adjacentFaces[nextFaceA].list[j] == nextFaceB ||
                                (ignoreBSide && solid.adjacentFaces[nextFaceA].list[j] == -1))
                            {
                                int nextEdgeA = solid.quads[nextFaceA * 4 + MeshEdit.quadEdgePatternClockwise[j * 2 + 0]];
                                int nextEdgeB = solid.quads[nextFaceA * 4 + MeshEdit.quadEdgePatternClockwise[j * 2 + 1]];

                                if (nextFaceB >= 0 ||
                                    (
                                    solid.isShared(nextEdgeA, currentEdgeA) ||
                                    solid.isShared(nextEdgeA, currentEdgeB) ||
                                    solid.isShared(nextEdgeB, currentEdgeA) ||
                                    solid.isShared(nextEdgeB, currentEdgeB)))
                                {
                                    /*
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceA));
                                    loopDebugMemory2.Add(solid.quadCenter(solid.adjacentFaces[nextFaceA].list[edgeTheNextCutAEnteredFrom]));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceB));
                                    loopDebugMemory2.Add(solid.quadCenter(solid.adjacentFaces[nextFaceB].list[edgeTheNextCutBEnteredFrom]));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceA));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceB));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceA));
                                    loopDebugMemory2.Add((solid.verts[nextEdgeA] + solid.verts[nextEdgeB]) / 2);
                                    */
                                    vertsInLoopPartial.Add(nextEdgeA);
                                    vertsInLoopPartial.Add(nextEdgeB);

                                    currentEdgeA = nextEdgeA;
                                    currentEdgeB = nextEdgeB;
                                    foundVerts = true;

                                    break;
                                }
                            }
                        }
                    }

                    if (!foundVerts)
                    {
                        //Debug.Log("Error!! Could not find correct side");
                        isConnectionFound = false;
                    }
                    else
                    {
                        lastFaceA = nextFaceA;
                        lastFaceB = nextFaceB;

                        // A
                        if (isNextAAnNSCut)
                        {
                            addedFaceDirectionA.Add(true);
                        }
                        else
                        {
                            addedFaceDirectionA.Add(false);
                        }
                        // B
                        if (isNextBAnNSCut)
                        {
                            addedFaceDirectionB.Add(true);
                        }
                        else
                        {
                            addedFaceDirectionB.Add(false);
                        }
                    }
                }

            }
        }

        return vertsInLoopPartial;
    }

    private void clearSelected(MeshEdit solid)
    {
        selectedEdges = new bool[0];
        selectedFaces = new bool[solid.quads.Count / 4];
        selectedVerts = new bool[solid.verts.Count];
    }

    public void updateMeshUVs(MeshEdit solid)
    {
        GameObject obj = Selection.activeTransform.gameObject;
        //MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

        MeshEdit meshEdit = obj.GetComponent<MeshEdit>();
        //Mesh mesh = meshFilter.sharedMesh;



        solid.uvMaps[solid.currentUVMap].newUvs = new Vector2[meshEdit.mesh.vertices.Length];
        Vector2[] newUVs = solid.uvMaps[solid.currentUVMap].newUvs;

        if (solid.uvMaps[solid.currentUVMap].uvs != null &&
            solid.uvMaps[solid.currentUVMap].uvs.Length == meshEdit.mesh.uv.Length)
        {
           // Debug.Log("Loading UVs from UVMap: " + solid.uvMaps[solid.currentUVMap].name);
            for (int i = 0; i < meshEdit.mesh.vertices.Length; i++)
            {
                newUVs[i] = solid.uvMaps[solid.currentUVMap].uvs[i];
            }
            meshEdit.mesh.uv = solid.uvMaps[solid.currentUVMap].uvs;
        }
        else
        {
            // Create a new set of uvs from scratch
            // Debug.Log("Creating new UVs for UVMap: " + solid.uvMaps[solid.currentUVMap].name);
            //Debug.Log("newUVs.Length: " + newUVs.Length);
            //Debug.Log(" meshEdit.mesh.uvs.Length: " + meshEdit.mesh.uv.Length);
            for (int i = 0; i < meshEdit.mesh.vertices.Length; i++)
            {

                newUVs[i] = meshEdit.mesh.uv[i];
            }
            
            solid.uvMaps[solid.currentUVMap].uvs = newUVs;
            meshEdit.mesh.uv = newUVs;
        }

        meshEdit.pushLocalMeshToGameObject();
    }

    public static int fileCount(DirectoryInfo d)
    {
        int i = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            if (fi.Extension.Contains("png"))
            {
                i++;
            }
        }
        return i;
    }
    public static List<string> getFileNames(DirectoryInfo d, string extension)
    {

        List<string> files = new List<string>();
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            if (fi.Extension.Contains(extension))
            {
                files.Add(fi.Name);
            }
        }
        return files;
    }

    public static void rotate(int rotations, ref int a, ref int b, ref int c, ref int d)
    {
        while (rotations > 0)
        {
            int temp = a;
            a = b;
            b = c;
            c = d;
            d = temp;
            rotations--;
        }
    }
    public static bool intersectionRayTriangle(Ray r, MeshEdit.Triangle t)
    {
        // if (Vector3.Dot(r.direction, t.n) > 0) { return false; }
        Vector3 q = Vector3.Dot(t.a - r.origin, t.n) * t.n;
        q = r.origin - q;

        if (Vector3.Dot(Vector3.Cross((t.b - t.a), (q - t.a)), t.n) < 0) { return false; }
        if (Vector3.Dot(Vector3.Cross((t.c - t.b), (q - t.b)), t.n) < 0) { return false; }
        if (Vector3.Dot(Vector3.Cross((t.a - t.c), (q - t.c)), t.n) < 0) { return false; }

        return true;
    }
    public static bool rayIntersectsTriangle(Vector3 rayOrigin,
                          Vector3 rayVector,
                          MeshEdit.Triangle inTriangle, ref Vector3 collisionPoint)
    {
        const float EPSILON = 0.0000001f;
        Vector3 vertex0 = inTriangle.a;
        Vector3 vertex1 = inTriangle.b;
        Vector3 vertex2 = inTriangle.c;
        Vector3 edge1, edge2, h, s, q;
        float a, f, u, v;
        edge1 = vertex1 - vertex0;
        edge2 = vertex2 - vertex0;
        h = Vector3.Cross(rayVector, edge2);
        a = Vector3.Dot(edge1, h);

        if (a > -EPSILON && a < EPSILON)
        {
            return false;
        }
        f = 1.0f / a;
        s = rayOrigin - vertex0;
        u = f * (Vector3.Dot(s, h));
        if (u < 0.0 || u > 1.0)
        {
            return false;
        }
        q = Vector3.Cross(s, edge1);
        v = f * Vector3.Dot(rayVector, q);
        if (v < 0.0 || u + v > 1.0)
        {
            return false;
        }
        // At this stage we can compute t to find out where the intersection point is on the line.
        float t = f * Vector3.Dot(edge2, q);
        if (t > EPSILON) // ray intersection
        {
            collisionPoint = rayOrigin + rayVector * t;
            return true;
        }
        else // This means that there is a line intersection but not a ray intersection.
        {
            return false;
        }
    }

    private int findAdjacentTriPoint(int[] triangles, Vector3[] verts, ref int a, ref int b, ref int cOther, int checkIndex = 0)
    {
        int tri = 0;
        bool aFound = false;
        bool bFound = false;
        bool cFound = false;
        int c = -1;

        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] == a)
            {
                aFound = true;
            }
            else if (triangles[i] == b)
            {
                bFound = true;
            }
            else if (triangles[i] != cOther)
            {
                cFound = true;
                c = triangles[i];
            }

            if (aFound && bFound && cFound)
            {
                break;
            }

            tri++;
            if (tri > 2)
            {
                tri = 0;

                aFound = false;
                bFound = false;
                cFound = false;

                c = -1;
            }
        }
        if (c == -1 && checkIndex < 2)
        {
            int temp = cOther;
            cOther = a;
            a = b;
            b = temp;

            c = findAdjacentTriPoint(triangles, verts, ref a, ref b, ref cOther, checkIndex + 1);
        }
        return c;
    }

    public static void checkEditorFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor/EditorResources"))
        {
            AssetDatabase.CreateFolder("Assets/Editor", "EditorResources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Editor/EditorResources/Tilesets"))
        {
            AssetDatabase.CreateFolder("Assets/Editor/EditorResources", "Tilesets");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Tilesets"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Tilesets");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Tilesets/Constructed"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Tilesets", "Constructed");
        }
    }

    private void loadTilesFromTileset(int index)
    {
        checkEditorFoldersExist();

        if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
        { 
            tileset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/EditorResources/Tilesets/" + tilesetsAvailable[index].tilesetName + ".png");
        

            tileWidth = tilesetsAvailable[index].tileWidth;
            tileHeight = tilesetsAvailable[index].tileHeight;
            tileOutline = tilesetsAvailable[index].tileOutline;

            string path = "Tilesets/Constructed/" + tilesetsAvailable[index].tilesetName;

            texturePage = Resources.Load<Texture2D>(path);

            tilesPerRow = tileset.width / tileWidth;
            tilesPerColumn = tileset.height / tileHeight;
            tiles = new Texture2D[tilesPerRow, tilesPerColumn];
            for (int y = 0; y < (tileset.height / tileHeight) * tileHeight; y += tileHeight)
            {
                for (int x = 0; x < (tileset.width / tileWidth) * tileWidth; x += tileWidth)
                {
                    Texture2D newTile = new Texture2D(tileWidth, tileHeight, TextureFormat.RGB24, false);
                    
                    for (int cy = 0; cy < tileHeight; cy++)
                    {
                        for (int cx = 0; cx < tileWidth; cx++)
                        {

                            Color pixel = tileset.GetPixel(cx + x, cy + y);
                            if (pixel.a < 0.1f)
                            {
                                pixel = Color.black;
                            }
                            newTile.SetPixel(cx, cy, pixel);
                        }
                    }

                    newTile.filterMode = FilterMode.Point;
                    newTile.Apply(false);
                    tiles[x / tileWidth, (tilesPerColumn - 1) - y / tileHeight] = newTile;
                }
            }

        }
    }
    
    private static void loadTilesets()
    {
        tilesetsAvailable = new List<Tileset>();
        tilesetTexturesAvailable = new List<string>();

        checkEditorFoldersExist();

        DirectoryInfo d = new DirectoryInfo(Application.dataPath + "/Editor/EditorResources/Tilesets");
        FileInfo[] files = d.GetFiles();

        foreach (FileInfo xmlFile in files)
        {
            if (xmlFile.Extension.EndsWith("xml"))
            {
                Tileset tileset = TilesetManager.DeSerializeObject<Tileset>(xmlFile.FullName);
                tileset.loadTexturesFromAssets();
                //tileset.packTextures();

                // If either file has been manually renamed then make sure no asset is loaded, since it would cause a FileNotFound error when loading the texture page.

                tilesetsAvailable.Add(tileset);
                tilesetTexturesAvailable.Add(tileset.tilesetName);

            }
        }
    }

    












    public static Mesh cube()
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

        return customCube;
    }
    public static Mesh plane()
    {

        Mesh customPlane = new Mesh();
        customPlane.name = "Plane";
        Vector3[] verts = {
            new Vector3(-0.5f, 0.0f, 0.5f), // Top
            new Vector3(0.5f, 0.0f, 0.5f),
            new Vector3(-0.5f, 0.0f, -0.5f),
            new Vector3(0.5f, 0.0f, -0.5f) };


        int[] tris = {
            0, 1, 2,
            3, 2, 1 };

        Color col = Color.white;

        Color[] colours = {
            col, col, col, col };

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);
        Vector2[] uv = {
                uv00, uv01, uv10, uv11};

        customPlane.vertices = verts;
        customPlane.triangles = tris;
        customPlane.colors = colours;
        customPlane.uv = uv;
        customPlane.RecalculateBounds();
        customPlane.RecalculateNormals();

        return customPlane;
    }
    public static Mesh circle(int selectedCircle)
    {
        if (circleVerticesCount[selectedCircle] == 6)
        {
            return circle6();
        }
        else if (circleVerticesCount[selectedCircle] > 6 &&
                 circleVerticesCount[selectedCircle] % 2 == 0)
        {
            return circleDivTwo(circleVerticesCount[selectedCircle] / 2);
        }

        else return (circleDivTwo(16));
    }

    public static Mesh cylinder(int selectedCircle)
    {
        if (circleVerticesCount[selectedCircle] == 6)
        {
            return cylinder6();
        }
        else if (circleVerticesCount[selectedCircle] > 6 &&
                 circleVerticesCount[selectedCircle] % 2 == 0)
        {
            return cylinderDivTwo(circleVerticesCount[selectedCircle] / 2);
        }

        else return (cylinderDivTwo(8));
    }

    public static Mesh circle6()
    {

        Mesh customCircle6 = new Mesh();
        customCircle6.name = "Circle6";
        float x = 0.4330127019f;
        float y = 0.25f;

        bool isLookingDown = SceneView.lastActiveSceneView.camera.transform.forward.y > 0;

        if (isLookingDown)
        {

            Vector3[] verts = {
            new Vector3(0.0f, 0.0f, 0.5f), // A
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(x, 0.0f, y),
            new Vector3(x, 0.0f, -y),
            new Vector3(0.0f, 0.0f, 0.5f), // B
            new Vector3(-x, 0.0f, y),
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(-x, 0.0f, -y)
         };



            int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5
        };

            Color col = Color.white;

            Color[] colours = {
            col, col, col, col,
            col, col, col, col};

            Vector2 uv00 = new Vector2(0, 0);
            Vector2 uv01 = new Vector2(0, 1);
            Vector2 uv10 = new Vector2(1, 0);
            Vector2 uv11 = new Vector2(1, 1);
            Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11};

            customCircle6.vertices = verts;
            customCircle6.triangles = tris;
            customCircle6.colors = colours;
            customCircle6.uv = uv;
            customCircle6.RecalculateBounds();
            customCircle6.RecalculateNormals();
        }
        else
        {
            Vector3[] verts = {
            new Vector3(0.0f, 0.0f, 0.5f), // A
            new Vector3(x, 0.0f, y),
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(x, 0.0f, -y),
            new Vector3(0.0f, 0.0f, 0.5f), // B
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(-x, 0.0f, y),
            new Vector3(-x, 0.0f, -y)
         };


            int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5
        };

            Color col = Color.white;

            Color[] colours = {
            col, col, col, col,
            col, col, col, col};

            Vector2 uv00 = new Vector2(0, 0);
            Vector2 uv01 = new Vector2(0, 1);
            Vector2 uv10 = new Vector2(1, 0);
            Vector2 uv11 = new Vector2(1, 1);
            Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11};

            customCircle6.vertices = verts;
            customCircle6.triangles = tris;
            customCircle6.colors = colours;
            customCircle6.uv = uv;
            customCircle6.RecalculateBounds();
            customCircle6.RecalculateNormals();
        }
        return customCircle6;
    }
    
    public static Mesh cylinder6()
    {

        Mesh customCylinder6 = new Mesh();
        customCylinder6.name = "Cylinder6";
        float x = 0.4330127019f;
        float y = 0.25f;

        Vector3[] verts = {
            new Vector3(0.0f, 0.5f, -0.5f), // A Bottom
            new Vector3(0.0f, 0.5f, 0.5f),
            new Vector3(x, 0.5f, -y),
            new Vector3(x, 0.5f, y),
            new Vector3(0.0f, 0.5f, 0.5f), // B Bottom
            new Vector3(0.0f, 0.5f, -0.5f),
            new Vector3(-x, 0.5f, y),
            new Vector3(-x, 0.5f, -y),

            new Vector3(0.0f, -0.5f, 0.5f), // A Top
            new Vector3(0.0f, -0.5f, -0.5f),
            new Vector3(x, -0.5f, y),
            new Vector3(x, -0.5f, -y),
            new Vector3(0.0f, -0.5f, -0.5f), // B Top
            new Vector3(0.0f, -0.5f, 0.5f),
            new Vector3(-x, -0.5f, -y),
            new Vector3(-x, -0.5f, y),

            // Sides
            new Vector3(0.0f, 0.5f, 0.5f), // 1
            new Vector3(0.0f, -0.5f, 0.5f),
            new Vector3(x, 0.5f, y),
            new Vector3(x, -0.5f, y),

            new Vector3(x, 0.5f, y), // 2
            new Vector3(x, -0.5f, y),
            new Vector3(x, 0.5f, -y),
            new Vector3(x, -0.5f, -y),

            new Vector3(x, 0.5f, -y), // 3
            new Vector3(x, -0.5f, -y),
            new Vector3(0.0f, 0.5f, -0.5f),
            new Vector3(0.0f, -0.5f, -0.5f),

            new Vector3(0.0f, 0.5f, -0.5f), // 4
            new Vector3(0.0f, -0.5f, -0.5f),
            new Vector3(-x, 0.5f, -y),
            new Vector3(-x, -0.5f, -y),

            new Vector3(-x, 0.5f, -y), // 5
            new Vector3(-x, -0.5f, -y),
            new Vector3(-x, 0.5f, y),
            new Vector3(-x, -0.5f, y),

            new Vector3(-x, 0.5f, y), // 6
            new Vector3(-x, -0.5f, y),
            new Vector3(0.0f, 0.5f, 0.5f), 
            new Vector3(0.0f, -0.5f, 0.5f)

        };


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
            23, 22, 21,

            24, 25, 26,
            27, 26, 25,

            28, 29, 30,
            31, 30, 29,

            32, 33, 34,
            35, 34, 33,

            36, 37, 38,
            39, 38, 37
        };

        Color col = Color.white;

        Color[] colours = {
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col ,
            col, col, col, col };

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);
        Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11 ,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11 ,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11 ,
                uv00, uv01, uv10, uv11};

        customCylinder6.vertices = verts;
        customCylinder6.triangles = tris;
        customCylinder6.colors = colours;
        customCylinder6.uv = uv;
        customCylinder6.RecalculateBounds();
        customCylinder6.RecalculateNormals();

        return customCylinder6;
    }

    public static Mesh circleDivTwo(int steps)
    {

        Mesh customCircle = new Mesh();
        customCircle.name = "Circle" + (steps * 2);

        Color col = Color.white;

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colour = new List<Color>();
        List<Vector2> uv = new List<Vector2>();

        float degrees = Mathf.Deg2Rad * (360.0f / (4.0f * steps)) * 2;

        float x, y;

        bool isLookingDown = SceneView.lastActiveSceneView.camera.transform.forward.y > 0;

        for (int i = 0; i < steps * 2; i += 2)
        {
            if (isLookingDown)
            {
                x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                verts.Add(new Vector3(0.0f, 0.0f, 0.0f));

                x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                x = Mathf.Cos(degrees * i) * 0.5f;
                y = Mathf.Sin(degrees * i) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));
            }
            else
            {
                x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                verts.Add(new Vector3(0.0f, 0.0f, 0.0f));

                x = Mathf.Cos(degrees * i) * 0.5f;
                y = Mathf.Sin(degrees * i) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));
            }




            tris.Add(i * 2 + 0);
            tris.Add(i * 2 + 1);
            tris.Add(i * 2 + 2);

            tris.Add(i * 2 + 3);
            tris.Add(i * 2 + 2);
            tris.Add(i * 2 + 1);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);

        }

        customCircle.vertices = verts.ToArray();
        customCircle.triangles = tris.ToArray();
        customCircle.colors = colour.ToArray();
        customCircle.uv = uv.ToArray();
        customCircle.RecalculateBounds();
        customCircle.RecalculateNormals();

        return customCircle;
    }
    public static Mesh cylinderDivTwo(int steps)
    {

        Mesh customCylinder = new Mesh();
        customCylinder.name = "Cylinder" + (steps * 2);

        Color col = Color.white;

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colour = new List<Color>();
        List<Vector2> uv = new List<Vector2>();

        float degrees = Mathf.Deg2Rad * (360.0f / (4.0f * steps)) * 2;

        float x, y, z;
        for (int i = 0; i < steps * 2; i += 2)
        {
            // Top face
            z = 0.5f;
            x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            verts.Add(new Vector3(0.0f, z, 0.0f));


            x = Mathf.Cos(degrees * i) * 0.5f;
            y = Mathf.Sin(degrees * i) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            tris.Add(i * 8 + 0);
            tris.Add(i * 8 + 1);
            tris.Add(i * 8 + 2);

            tris.Add(i * 8 + 3);
            tris.Add(i * 8 + 2);
            tris.Add(i * 8 + 1);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);

            // Bottom face
            z = -0.5f;
            x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            verts.Add(new Vector3(0.0f, z, 0.0f));

            x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            x = Mathf.Cos(degrees * i) * 0.5f;
            y = Mathf.Sin(degrees * i) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            tris.Add(i * 8 + 4);
            tris.Add(i * 8 + 5);
            tris.Add(i * 8 + 6);

            tris.Add(i * 8 + 7);
            tris.Add(i * 8 + 6);
            tris.Add(i * 8 + 5);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);

            // Side1

            z = -0.5f;
            verts.Add(verts[i * 8 + 0]);
            verts.Add(verts[i * 8 + 4]);
            verts.Add(verts[i * 8 + 1]);
            verts.Add(verts[i * 8 + 6]);

            tris.Add(i * 8 + 8);
            tris.Add(i * 8 + 9);
            tris.Add(i * 8 + 10);

            tris.Add(i * 8 + 11);
            tris.Add(i * 8 + 10);
            tris.Add(i * 8 + 9);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);
            // Side2

            z = -0.5f;
            verts.Add(verts[i * 8 + 1]);
            verts.Add(verts[i * 8 + 6]);
            verts.Add(verts[i * 8 + 3]);
            verts.Add(verts[i * 8 + 7]);

            tris.Add(i * 8 + 12);
            tris.Add(i * 8 + 13);
            tris.Add(i * 8 + 14);

            tris.Add(i * 8 + 15);
            tris.Add(i * 8 + 14);
            tris.Add(i * 8 + 13);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);
        }


        customCylinder.vertices = verts.ToArray();
        customCylinder.triangles = tris.ToArray();
        customCylinder.colors = colour.ToArray();
        customCylinder.uv = uv.ToArray();
        customCylinder.RecalculateBounds();
        customCylinder.RecalculateNormals();

        return customCylinder;
    }








}