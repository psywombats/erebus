using UnityEngine;
using System.Collections;

public class RotationBehavior : MonoBehaviour {

    public Vector3 degreesPerSecond;

    public void Update() {
        transform.eulerAngles += degreesPerSecond * Time.deltaTime;
    }
}
