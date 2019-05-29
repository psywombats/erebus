using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProgramFruit : PhuneProgram {

    public List<GameObject> options;
    public PhuneEntryData data;
    public GameObject disableMe;

    private int selection;

    public override void OnEnable() {
        base.OnEnable();
        selection = 0;
        MoveSelection(0);
        
        disableMe.SetActive(Global.Instance().Memory.GetSwitch("take2"));

    }

    protected override bool InternalHandleCommand(InputManager.Command command) {
        switch (command) {
            case InputManager.Command.Confirm:
                Confirm();
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

    private void Confirm() {
        if (selection == 0) {
            Global.Instance().StartCoroutine(MessageRoutine());
        } else {
            Global.Instance().StartCoroutine(OblivionRoutine());
        }
    }

    private IEnumerator OblivionRoutine() {
        Global.Instance().Memory.SetSwitch("glitch_on", true);
        PhuneUI ui = FindObjectOfType<PhuneUI>();
        Global.Instance().Maps.avatar.PauseInput();
        GlitchImageEffect glitch = FindObjectOfType<Camera>().GetComponent<GlitchImageEffect>();
        Countdowner count = ui.gameObject.transform.parent.GetComponentInChildren<Countdowner>(true);

        yield return ui.HideRoutine();
        yield return CoUtils.Wait(0.5f);
        Global.Instance().Audio.PlaySFX("lightswitch");

        Global.Instance().Audio.FadeOutRoutine(0.6f);
        for (int i = 0; i < 20; i += 1) {
            glitch.enabled = !glitch.enabled;
            Global.Instance().Memory.SetSwitch("ghoulie", glitch.enabled);
            yield return CoUtils.Wait(Random.Range(0.025f, 0.05f));
            
        }
        Global.Instance().Memory.SetSwitch("ghoulie", false);
        glitch.enabled = true;
        CoUtils.Wait(1.0f);
        Global.Instance().Audio.PlayBGM("silvereyes");

        //count.enabled = true;
        //count.value = 60;
        Global.Instance().Maps.avatar.UnpauseInput();
        
    }

    private IEnumerator MessageRoutine() {
        PhuneUI ui = FindObjectOfType<PhuneUI>();
        Global.Instance().Input.RemoveListener(this);
        ui.SelectEntry(true);
        yield return ui.HideRoutine();
        yield return CoUtils.Wait(0.5f);
        ui.AddDrop(data);
        Global.Instance().Memory.SetSwitch("take2", true);
    }

    private void MoveSelection(int delta) {
        selection += delta;
        if (selection < 0) selection = options.Count - 1;
        if (selection >= options.Count) selection = 0;
        if (!Global.Instance().Memory.GetSwitch("take2")) selection = 0;

        foreach (GameObject obj in options) {
            obj.SetActive(obj == options[selection]);
        }
    }
}
