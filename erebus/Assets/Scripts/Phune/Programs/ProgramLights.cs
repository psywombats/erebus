using UnityEngine;
using System.Collections;

public class ProgramLights : PhuneProgram {

    private bool toggling;

    public GameObject onObject;
    public GameObject offObject;

    public override void OnEnable() {
        base.OnEnable();
        onObject.SetActive(!Global.Instance().IsLightsOutMode());
        offObject.SetActive(Global.Instance().IsLightsOutMode());
    }

    protected override bool InternalHandleCommand(InputManager.Command command) {
        switch (command) {
            case InputManager.Command.Confirm:
                if (!toggling) {
                    StartCoroutine(ToggleRoutine());
                }
                break;
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
