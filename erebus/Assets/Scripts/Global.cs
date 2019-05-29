using UnityEngine;

public class Global : MonoBehaviour {

    private static Global instance;
    //private static bool destructing;
    
    public InputManager Input { get; private set; }
    public MapManager Maps { get; private set; }
    public MemoryManager Memory { get; private set; }
    public AudioManager Audio { get; private set; }
    public SettingsCollection Settings { get; private set; }

    private IndexDatabase database;
    public IndexDatabase Database {
        get {
            if (database == null) {
                database = IndexDatabase.Instance();
            }
            return database;
        }
    }

    public static Global Instance() {
        if (instance == null) {
            GameObject globalObject = new GameObject("Globals");
            // debug-ish and we don't serialize scenes
            // globalObject.hideFlags = HideFlags.HideAndDontSave;
            instance = globalObject.AddComponent<Global>();
            instance.InstantiateManagers();
        }

        return instance;
    }

    public void Update() {
        SetFullscreenMode();
    }

    public void Awake() {
        DontDestroyOnLoad(gameObject);
        MoonSharp.Interpreter.UserData.RegisterAssembly();
    }

    public void OnDestroy() {
        //destructing = true;
    }

    public bool IsLightsOutMode() {
        return Memory.GetSwitch("lights_out");
    }

    public bool IsSoundMode() {
        return (Maps.activeMap != null &&
            Maps.activeMap.name.Contains("Top")) || Memory.GetSwitch("sound_on");
    }

    public bool IsGlitchOn() {
        return Memory.GetSwitch("glitch_on");
    }

    private void InstantiateManagers() {
        Settings = gameObject.AddComponent<SettingsCollection>();
        Input = gameObject.AddComponent<InputManager>();
        Maps = gameObject.AddComponent<MapManager>();
        Memory = gameObject.AddComponent<MemoryManager>();
        Audio = gameObject.AddComponent<AudioManager>();
    }

    private void SetFullscreenMode() {
        // not sure if this "check" is necessary
        // actually performing this here is kind of a hack
        if (Settings != null && Screen.fullScreen != Settings.GetBoolSetting(SettingsConstants.Fullscreen).Value) {
            Screen.fullScreen = Settings.GetBoolSetting(SettingsConstants.Fullscreen).Value;
        }
    }
}
