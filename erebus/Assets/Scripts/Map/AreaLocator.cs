using UnityEngine;
using System.Collections;

public class AreaLocator : MonoBehaviour {

    public static string currentAreaName;
    public static string currentBgmName;
    public static string currentBgmComposer;

    public string bgm;
    public string areaName;
    public string bgmName;
    public string bgmComposer;

    public void Start() {
        StartCoroutine(CoUtils.RunAfterDelay(0.0f, () => {
            if (bgm != null && bgm.Length > 0) {
                Global.Instance().Audio.PlayBGM(bgm);
            }
            currentAreaName = areaName;
            if (currentBgmName != null && currentBgmName.Length > 0) {
                currentBgmName = bgmName;
                currentBgmComposer = bgmComposer;
            }
        }));
    }
}
