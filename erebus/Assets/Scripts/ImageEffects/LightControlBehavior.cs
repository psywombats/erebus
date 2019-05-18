using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightControlBehavior : MonoBehaviour {

    public List<GameObject> lightsOnOnly = new List<GameObject>();
    public List<GameObject> lightsOffOnly = new List<GameObject>();
    public List<GameObject> soundOnly = new List<GameObject>();

    public void Update() {
        bool lightsOutMode = Global.Instance().IsLightsOutMode();
        bool soundMode = Global.Instance().IsSoundMode();
        if (lightsOnOnly != null) {
            foreach (GameObject go in lightsOnOnly) {
                go.SetActive(!lightsOutMode);
            }
        }
        if (lightsOffOnly != null) {
            foreach (GameObject go in lightsOffOnly) {
                go.SetActive(lightsOutMode);
            }
        }
        if (soundOnly != null) {
            foreach (GameObject go in soundOnly) {
                go.SetActive(soundMode && lightsOutMode);
            }
        }
    }
}
