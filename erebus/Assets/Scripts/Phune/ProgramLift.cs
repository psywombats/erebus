using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProgramLift : MonoBehaviour, InputListener {
    
    public List<GameObject> options;
    public GameObject errorMessage;
    public GameObject powerError;

    private int selection;

    public void OnEnable() {
        Global.Instance().Input.PushListener(this);
        selection = 0;
        for (int i = 0; i < options.Count; i += 1) {
            if (Global.Instance().Memory.GetSwitch("lift_" + i)) {
                selection = i;
            }
        }
        MoveSelection(0);
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType != InputManager.Event.Up) {
            return true;
        }
        switch (command) {
            case InputManager.Command.Confirm:
                CallLift();
                break;
            case InputManager.Command.Cancel:
            case InputManager.Command.Left:
                Global.Instance().Input.RemoveListener(this);
                FindObjectOfType<PhuneUI>().SelectEntry(true);
                return true;
            case InputManager.Command.Up:
                MoveSelection(-1);
                break;
            case InputManager.Command.Down:
                MoveSelection(1);
                break;
            default:
                return true;
        }
        return true;
    }

    private void CallLift() {
        MemoryManager mem = Global.Instance().Memory;
        if (Global.Instance().IsLightsOutMode()) {
            powerError.SetActive(true);
        } else if (selection == 0) {
            errorMessage.SetActive(true);
        } else {
            if (!mem.GetSwitch("lift_" + selection)) {
                for (int i = 0; i < options.Count; i += 1) {
                    mem.SetSwitch("lift_" + i, i == selection);
                }
                if (Global.Instance().Maps.avatar.GetComponent<MapEvent>().parent.name == "ElevatorRide") {
                    StartCoroutine(ElevatorRoutine());
                }
            }
        }
    }

    private void MoveSelection(int delta) {
        selection += delta;
        if (selection < 0) selection = options.Count - 1;
        if (selection >= options.Count) selection = 0;

        foreach (GameObject obj in options) {
            obj.SetActive(obj == options[selection]);
        }
        errorMessage.SetActive(false);
        powerError.SetActive(false);
    }

    private IEnumerator ElevatorRoutine() {
        Global.Instance().Maps.avatar.PauseInput();
        PhuneUI ui = FindObjectOfType<PhuneUI>();
        yield return ui.HideRoutine();
        yield return CoUtils.Wait(0.8f);
        SubJitter jitter = FindObjectOfType<SubJitter>();
        jitter.enabled = true;
        yield return CoUtils.Wait(3.5f);
        jitter.enabled = false;
        Global.Instance().Maps.avatar.UnpauseInput();
    }
}