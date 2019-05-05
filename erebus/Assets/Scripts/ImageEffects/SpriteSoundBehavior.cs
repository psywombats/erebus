using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSoundBehavior : SoundEmitterBehavior {

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
        Color c = GetSprite().color;
        GetSprite().color = new Color(c.r, c.g, c.b, f);
    }

    private SpriteRenderer GetSprite() {
        return GetComponent<SpriteRenderer>();
    }
}
