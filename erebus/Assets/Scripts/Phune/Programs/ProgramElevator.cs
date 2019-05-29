using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProgramElevator : PhuneProgram {

    private static bool done;
    
    public List<GameObject> options;
    
    private int selection;

    public override void OnEnable() {
        base.OnEnable();
        selection = Global.Instance().Memory.GetSwitch("finale_elevator") ? 0 : 1;
        MoveSelection(0);
    }

    protected override bool InternalHandleCommand(InputManager.Command command) {
        switch (command) {
            case InputManager.Command.Confirm:
                CallLift();
                break;
            case InputManager.Command.Up:
                MoveSelection(-1);
                break;
            case InputManager.Command.Down:
                MoveSelection(1);
                break;
        }
        return true;
    }

    private void CallLift() {
        if (selection == 0 && !Global.Instance().Memory.GetSwitch("finale_elevator")) {
            Global.Instance().Memory.SetSwitch("finale_elevator", true);
            Global.Instance().Memory.SetSwitch("sound_on", true);
            StartCoroutine(ElevatorRoutine());
        }
    }

    private void MoveSelection(int delta) {
        selection += delta;
        if (selection < 0) selection = options.Count - 1;
        if (selection >= options.Count) selection = 0;

        foreach (GameObject obj in options) {
            obj.SetActive(obj == options[selection]);
        }
    }

    private IEnumerator ElevatorRoutine() {
        if (working) {
            yield break;
        }
        working = true;
        Global.Instance().Maps.avatar.PauseInput();
        PhuneUI ui = FindObjectOfType<PhuneUI>();
        yield return ui.HideRoutine();
        yield return CoUtils.Wait(0.8f);
        SubJitter jitter = FindObjectOfType<SubJitter>();
        jitter.enabled = true;
        yield return CoUtils.Wait(5.0f);
        jitter.enabled = false;
        Global.Instance().Maps.avatar.UnpauseInput();
        working = false;
    }
}