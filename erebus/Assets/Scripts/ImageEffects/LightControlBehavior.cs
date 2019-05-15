using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class LightControlBehavior : MonoBehaviour {

    public static bool lightsOutMode = true;
    public static bool soundMode = false;
    public List<GameObject> lightsOnOnly = new List<GameObject>();
    public List<GameObject> lightsOffOnly = new List<GameObject>();
    public List<GameObject> soundOnly = new List<GameObject>();

    public void Update() {
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
