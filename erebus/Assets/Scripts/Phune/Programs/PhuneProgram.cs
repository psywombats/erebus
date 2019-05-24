using UnityEngine;
using System.Collections;

public class PhuneProgram : MonoBehaviour, InputListener {

    protected bool working;

    public virtual void OnEnable() {
        Global.Instance().Input.PushListener(this);
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType != InputManager.Event.Up || working) {
            return true;
        }
        switch (command) {
            case InputManager.Command.Cancel:
            case InputManager.Command.Left:
                Global.Instance().Input.RemoveListener(this);
                FindObjectOfType<PhuneUI>().SelectEntry(true);
                return true;
            default:
                return InternalHandleCommand(command);
        }
    }

    protected virtual bool InternalHandleCommand(InputManager.Command command) {
        return true;
    }
}
