using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class AlphaProximityBehavior : MonoBehaviour {

    public MapEvent anchor;
    public float minAlpha = 0.0f;
    public float maxAlpha = 1.0f;
    public float minDist = 1.0f;
    public float maxDist = 5.0f;

    // Update is called once per frame
    void Update() {
        float d = Vector3.Distance(anchor.positionPx, Global.Instance().Maps.avatar.GetComponent<MapEvent>().positionPx);
        float a = (Mathf.Clamp(d, minDist, maxDist) - minDist) / (maxDist - minDist);
        a = a * (maxAlpha - minAlpha) + minAlpha;
        Color c = GetComponent<SpriteRenderer>().color;
        c = new Color(c.r, c.g, c.b, a);
        GetComponent<SpriteRenderer>().color = c;
    }
}
