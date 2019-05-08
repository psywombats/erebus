using UnityEngine;
using System.Collections;

public class Oscillator : MonoBehaviour {

    public enum OscillationOffsetMode {
        StartsAtMiddle,
        StartsAtExtreme
    };

    public enum OscillationMovementMode {
        Sinusoidal,
        Linear,
        Loop,
    };

    public float durationSeconds = 1.0f;
    public Vector3 maxOffset;
    public OscillationMovementMode movementMode = OscillationMovementMode.Sinusoidal;
    public OscillationOffsetMode offsetMode = OscillationOffsetMode.StartsAtMiddle;
    public bool scaleMode;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float elapsed;

    public void Start() {
        originalPosition = gameObject.transform.localPosition;
        originalScale = gameObject.transform.localScale;
        Reset();
    }

    public void OnEnable() {
        Reset();
    }

    public void Update() {
        elapsed += Time.deltaTime;
        while (elapsed >= durationSeconds) {
            elapsed -= durationSeconds;
        }

        float completed = (elapsed / durationSeconds);
        if (offsetMode == OscillationOffsetMode.StartsAtMiddle) {
            completed += 0.5f;
            if (completed > 1.0f) {
                completed -= 1.0f;
            }
        }

        float vectorMultiplier = 1.0f;
        switch (movementMode) {
            case OscillationMovementMode.Sinusoidal:
                vectorMultiplier = Mathf.Sin(completed * 2.0f * Mathf.PI);
                break;
            case OscillationMovementMode.Linear:
                vectorMultiplier = (completed * 2.0f) - 1.0f;
                if (vectorMultiplier < -0.5f) {
                    vectorMultiplier = (vectorMultiplier * -1) - 1.0f;
                }
                if (vectorMultiplier > 0.5f) {
                    vectorMultiplier = (vectorMultiplier * -1) + 1.0f;
                }
                vectorMultiplier *= 2.0f;
                break;
            case OscillationMovementMode.Loop:
                vectorMultiplier = completed;
                break;
        }

        if (scaleMode) {
            gameObject.transform.localScale = originalScale + maxOffset * vectorMultiplier;
        } else {
            gameObject.transform.localPosition = originalPosition + maxOffset * vectorMultiplier;
        }
    }

    private void Reset() {
        elapsed = 0;
    }
}
