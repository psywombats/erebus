using UnityEngine;
using System.Collections;

public class ProgramLights : MonoBehaviour, InputListener {

    private bool toggling;

    public GameObject onObject;
    public GameObject offObject;

    public void OnEnable() {
        Global.Instance().Input.PushListener(this);
        onObject.SetActive(!Global.Instance().IsLightsOutMode());
        offObject.SetActive(Global.Instance().IsLightsOutMode());
    }

    public void OnDisable() {
        Global.Instance().Input.RemoveListener(this);
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType != InputManager.Event.Up) {
            return true;
        }
        switch (command) {
            case InputManager.Command.Confirm:
                if (!toggling) {
                    StartCoroutine(ToggleRoutine());
                }
                break;
            case InputManager.Command.Cancel:
            case InputManager.Command.Left:
                if (!toggling) {
                    return false;
                }
                break;
            default:
                return true;
        }
        return true;
    }

    private IEnumerator ToggleRoutine() {
        bool newMode = !Global.Instance().IsLightsOutMode();

        onObject.SetActive(!newMode);
        offObject.SetActive(newMode);

        toggling = true;
        Global.Instance().Audio.PlaySFX("lightswitch");
        float granule = 0.05f;
        for (float elapsed = 0.0f; elapsed < 0.5f; elapsed += granule) {
            Global.Instance().Memory.SetSwitch("lights_out", !Global.Instance().IsLightsOutMode());
            yield return CoUtils.Wait(granule);
        }
        Global.Instance().Memory.SetSwitch("lights_out", newMode);
        toggling = false;
    }
}
