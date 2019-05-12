using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class LightOscillator : Oscillator {
    
    public float rangeOffset;
    public float intensityOffset;

    private float originalIntensity;
    private float originalRange;

    public override void Start() {
        base.Start();

        Light light = GetComponent<Light>();
        originalIntensity = light.intensity;
        originalRange = light.range;
    }

    public override void Update() {
        float vectorMult = CalcVectorMult();
        Light light = GetComponent<Light>();

        light.intensity = originalIntensity + intensityOffset * vectorMult;
        light.range = originalRange + rangeOffset * vectorMult;
    }
}
