using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class LightSoundBehavior : SoundEmitterBehavior {

    float lastIntensity;

    public override Color GetHue() {
        return GetLight().color;
    }

    public override float GetIntensity() {
        return GetLight().intensity;
    }

    public override void SetHue(Color c) {
        GetLight().color = c;
    }

    public override void SetIntensity(float f) {
        lastIntensity = f;
        GetLight().intensity = (f + lastIntensity / 2.0f);
    }

    private Light GetLight() {
        return GetComponent<Light>();
    }
}
