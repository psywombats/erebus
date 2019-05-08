using UnityEngine;
using System.Collections;

public class SubJitter : MonoBehaviour {

    public float resolution = 10f;
    public float chance = 0.1f;
    public Vector3 offset;
    
    private float elapsed;
    private bool on;
    private Vector3 originalPosition;

    public void Start() {
        originalPosition = transform.localPosition;
    }

    public void Update() {
        elapsed += Time.deltaTime;
        if (elapsed > resolution) {
            elapsed -= resolution;
            on = Random.Range(0.0f, 1.0f) < chance;
        }
        if (on) {
            transform.localPosition = originalPosition + offset;
        } else {
            transform.localPosition = originalPosition;
        }
    }
}
