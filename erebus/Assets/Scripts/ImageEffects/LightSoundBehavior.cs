using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class LightSoundBehavior : SoundEmitterBehavior {

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
        GetLight().intensity = f;
    }

    private Light GetLight() {
        return GetComponent<Light>();
    }
}
