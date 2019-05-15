using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SlowFlashTextBehavior : SlowFlashBehavior {

    protected override float GetAlpha() {
        return GetComponent<Text>().color.a;
    }

    protected override void SetAlpha(float alpha) {
        Text text = GetComponent<Text>();
        text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
    }
}
