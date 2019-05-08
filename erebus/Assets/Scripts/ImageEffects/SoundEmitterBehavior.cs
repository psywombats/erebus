using UnityEngine;
using System.Collections;

public abstract class SoundEmitterBehavior : MonoBehaviour {

    public bool useAlpha = true;
    public bool useColor = true;
    public bool swapBands = false;
    public bool invert = false;

    public abstract float GetIntensity();
    public abstract Color GetHue();
    public abstract void SetIntensity(float f);
    public abstract void SetHue(Color c);

    private float baseIntensity;
    private Color baseHue;

    public void Start() {
        baseIntensity = GetIntensity();
        baseHue = GetHue();
    }

    public void Update() {
        float band1 = Global.Instance().Audio.GetWaveSource().GetLowBand();
        float band2 = Global.Instance().Audio.GetWaveSource().GetHighBand();

        if (invert) {
            band1 = 1.0f - band1;
            band2 = 1.0f - band2;
        }

        if (swapBands) {
            float temp = band1;
            band1 = band2;
            band2 = temp;
        }

        if (useColor) {
            SetHue(new Color(
                Rebase(baseHue.r, band2),
                Rebase(baseHue.g, 1.0f - band2),
                baseHue.b));
        }
        if (useAlpha) {
            SetIntensity(Rebase(baseIntensity, band1, 0.9f));
        }
    }

    private float Rebase(float baseVal, float adjVal, float pct = 0.5f) {
        return baseVal * (1.0f - pct) + adjVal * pct;
    }
}
