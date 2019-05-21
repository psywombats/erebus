using UnityEngine;
using System.Collections;

public class AreaLocator : MonoBehaviour {

    public string bgm;
    public string areaName;

    public void Start() {
        StartCoroutine(CoUtils.RunAfterDelay(0.0f, () => {
            Global.Instance().Audio.PlayBGM(bgm);
        }));
    }
}
