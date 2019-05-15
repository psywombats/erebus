using UnityEngine;
using System.Collections;
using DG.Tweening;

public abstract class SlowFlashBehavior : MonoBehaviour {

    public float period = 1.0f;
    public float transitionDuration = 0.1f;

    public bool disable { get; set; }

    private float originalAlpha;
    private float elapsed;
    private bool off;

    public void Start() {
        originalAlpha = GetAlpha();
    }

    public void Update() {
        elapsed += Time.deltaTime;
        if (elapsed > period / 2.0f) {
            if (off) {
                var tween = DOTween.To(GetAlpha, SetAlpha, 0.0f, transitionDuration);
                StartCoroutine(CoUtils.RunTween(tween));
            } else {
                if (!disable) {
                    var tween = DOTween.To(GetAlpha, SetAlpha, originalAlpha, transitionDuration);
                    StartCoroutine(CoUtils.RunTween(tween));
                }
            }
            off = !off;
            elapsed -= period / 2.0f;

        }
    }

    protected abstract float GetAlpha();
    protected abstract void SetAlpha(float alpha);
}
