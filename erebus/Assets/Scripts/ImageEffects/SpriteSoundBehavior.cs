using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSoundBehavior : SoundEmitterBehavior {

    public bool useOldHard = false;

    public override Color GetHue() {
        return GetSprite().color;
    }

    public override float GetIntensity() {
        return GetSprite().color.a;
    }

    public override void SetHue(Color c) {
        GetSprite().color = c;
    }

    public override void SetIntensity(float f) {
        float f2 = .5f + f / 2.0f;
        Color c = GetSprite().color;
        GetSprite().color = new Color(c.r, c.g, c.b, useOldHard ? f : f2);
    }

    private SpriteRenderer GetSprite() {
        return GetComponent<SpriteRenderer>();
    }
}
