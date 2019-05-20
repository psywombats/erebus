using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MapEvent))]
public class WanderBehavior : MonoBehaviour {

    // Use this for initialization
    void Start() {
        StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine() {
        MapEvent ev = GetComponent<MapEvent>();
        
        while (true) {
            OrthoDir dir = (OrthoDir)Random.Range(0, 4);
            int count = Random.Range(2, 6);
            for (int i = 0; i < count; i += 1) {
                yield return ev.TryStepRoutine(dir);
            }
            yield return CoUtils.Wait(1.0f);
        }
        
    }
}
