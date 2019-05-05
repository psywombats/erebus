using UnityEngine;
using System.Collections;

public class TrackerCam3D : MapCamera3D {
    
    public Vector3 targetOffset;

    void Start() {
        MemorizePosition();
    }

    public override void ManualUpdate() {
        transform.position = target.transform.position + targetOffset;
        base.ManualUpdate();
    }

    public void MemorizePosition() {
        targetOffset = transform.position - target.transform.position;
    }
}
