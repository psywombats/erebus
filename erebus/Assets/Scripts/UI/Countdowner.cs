using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Countdowner : MonoBehaviour {

    public List<Text> textboxes;

    public float value;

    public void OnEnable() {
        foreach (Text text in textboxes) {
            text.enabled = true;
        }
    }

    public void OnDisable() {
        foreach (Text text in textboxes) {
            text.enabled = false;
        }
    }

    public void Update() {
        value -= Time.deltaTime;
        foreach (Text text in textboxes) {
            text.text = (Mathf.Round(value * 100f) / 100f).ToString();
        }
    }
}
