using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PhuneUI : MonoBehaviour, InputListener {

    public Vector3 appearOffset;
    public float snapTime = 0.2f;

    private Vector3 originalPosition;

    public void Awake() {
        this.originalPosition = this.transform.localPosition;
        Global.Instance().Input.PushListener(this);
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType == InputManager.Event.Up) {
            switch (command) {
                case (InputManager.Command.Menu):
                case (InputManager.Command.Cancel):
                    StartCoroutine(HideRoutine());
                    break;
            }
        }
        return true;
    }

    public IEnumerator ShowRoutine() {
        Global.Instance().Input.PushListener(this);
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => transform.localPosition = newPos,
            originalPosition + appearOffset,
            snapTime)
            .SetOptions(true));
    }

    public IEnumerator HideRoutine() {
        Global.Instance().Input.RemoveListener(this);
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => transform.localPosition = newPos,
            originalPosition,
            snapTime)
            .SetOptions(true));
    }
}
