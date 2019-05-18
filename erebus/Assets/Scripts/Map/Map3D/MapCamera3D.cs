using UnityEngine;

public class MapCamera3D : MapCamera {

    public bool fixedZ;
    public bool constrain;
    public Vector3 minValues = new Vector3(0, 0, 0);
    public Vector3 maxValues = new Vector3(100, 100, 0);

    private float lastFixedZ;

    public void Awake() {
        lastFixedZ = transform.position.z;
        //GetComponent<Camera>().transparencySortMode = TransparencySortMode.Orthographic;
    }

    public void LateUpdate() {
        ManualUpdate();
    }

    public override void ManualUpdate() {
        base.ManualUpdate();
        if (fixedZ) {
            transform.position = new Vector3(transform.position.x, transform.position.y, lastFixedZ);
        }
        if (constrain) {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, minValues.x, maxValues.x),
                Mathf.Clamp(transform.position.y, minValues.y, maxValues.y),
                transform.position.z);
        }
    }
}
