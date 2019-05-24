using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class MapManager : MonoBehaviour {

    private static readonly string DefaultTransitionTag = "default";

    public Map activeMap { get; set; }
    public AvatarEvent avatar { get; set; }
    public SceneBlendController blendController { get; set; }

    private MapCamera _camera;
    public new MapCamera camera {
        get {
            if (_camera == null) {
                _camera = FindObjectOfType<MapCamera>();
            }
            return _camera;
        }
        set {
            _camera = value;
        }
    }

    public IEnumerator TeleportRoutine(string mapName, string targetEventName) {
        TransitionData data = Global.Instance().Database.Transitions.GetData(DefaultTransitionTag);
        
        yield return camera.GetComponent<FadeImageEffect>().FadeRoutine(data.GetFadeOut());
        StartCoroutine(avatar.GetComponent<CharaEvent>().FadeRoutine(0.1f, true));
        RawTeleport(mapName, targetEventName);
        camera = activeMap.GetComponentInChildren<MapCamera>();
        yield return camera.GetComponent<FadeImageEffect>().FadeRoutine(data.GetFadeIn(), true);
    }
    
    private void RawTeleport(string mapName, Vector2Int location) {
        Assert.IsNotNull(activeMap);
        Map newMapInstance = InstantiateMap(mapName);
        RawTeleport(newMapInstance, location);
    }

    private void RawTeleport(string mapName, string targetEventName) {
        Assert.IsNotNull(activeMap);
        Map newMapInstance = InstantiateMap(mapName);
        Assert.IsNotNull(newMapInstance, "No new map!!");
        MapEvent target = newMapInstance.GetEventNamed(targetEventName);
        RawTeleport(newMapInstance, target.position);
    }

    private void RawTeleport(Map map, Vector2Int location) {
        Assert.IsNotNull(activeMap);
        Assert.IsNotNull(avatar);

        avatar.transform.SetParent(map.objectLayer.transform, false);

        activeMap.OnTeleportAway();
        Destroy(activeMap.gameObject.transform.parent.gameObject);
        activeMap = map;
        activeMap.OnTeleportTo();
        avatar.GetComponent<MapEvent>().SetLocation(location);
    }

    private Map InstantiateMap(string mapName) {
        GameObject newMapObject = null;
        if (activeMap != null) {
            string localPath = Map.ResourcePath + mapName;
            newMapObject = Resources.Load<GameObject>(localPath);
        }
        if (newMapObject == null) {
            newMapObject = Resources.Load<GameObject>(mapName);
        }
        Assert.IsNotNull(newMapObject);
        return Instantiate(newMapObject).GetComponentInChildren<Map>();
    }
}
