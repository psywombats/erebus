using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SlowFlashImageBehavior : SlowFlashBehavior {

    protected override float GetAlpha() {
        return GetComponent<Image>().color.a;
    }

    protected override void SetAlpha(float alpha) {
        Image img = GetComponent<Image>();
        img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
    }
}
