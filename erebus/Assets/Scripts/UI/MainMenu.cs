using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class MainMenu : MonoBehaviour, InputListener {

    public const string TitleTransitionTag = "title";

    public GameObject startSelection, quitSelection;
    public FadeImageEffect fader;
    private bool selectionToggle;

    public void OnEnable() {
        Global.Instance().Input.PushListener(this);
    }

    public void OnDisable() {
        Global.Instance().Input.RemoveListener(this);
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType != InputManager.Event.Up) {
            return true;
        }
        switch (command) {
            case InputManager.Command.Up:
            case InputManager.Command.Down:
                ToggleSelection();
                break;
            case InputManager.Command.Confirm:
                Select();
                break;
        }
        return true;
    }

    private void ToggleSelection() {
        selectionToggle = !selectionToggle;
        startSelection.SetActive(!selectionToggle);
        quitSelection.SetActive(selectionToggle);
    }

    private void Select() {
        if (selectionToggle) {
            Application.Quit();
        } else {
            Global.Instance().StartCoroutine(NewRoutine());
        }
    }
    
    private IEnumerator NewRoutine() {
        TransitionData data = Global.Instance().Database.Transitions.GetData(TitleTransitionTag);
        var tween = GetComponent<CanvasGroup>().DOFade(0.0f, data.GetFadeOut().delay);
        yield return CoUtils.RunParallel(new IEnumerator[] {
            CoUtils.RunTween(tween),
            fader.FadeRoutine(data.GetFadeIn()),
            Global.Instance().Audio.FadeOutRoutine(data.GetFadeOut().delay),
        }, Global.Instance());
        SceneManager.LoadScene("Opener", LoadSceneMode.Single);
        fader = FindObjectOfType<FadeImageEffect>();
        yield return fader.FadeRoutine(data.GetFadeOut(), true);
    }
}
