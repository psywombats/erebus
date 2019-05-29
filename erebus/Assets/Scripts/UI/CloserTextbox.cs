using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class CloserTextbox : MonoBehaviour, InputListener {

    public float charsPerSecond = 4.0f;
    public Text textbox;
    public GameObject advanceArrow;

    private bool awaitingConfirm = false;

    private bool confirmed = false;

    public void Start() {
        StartCoroutine(OpenerRoutine());
        advanceArrow.SetActive(false);
    }

    private IEnumerator OpenerRoutine() {
        yield return TypeRoutine("do you remember a place?");
        yield return TypeRoutine("a person, time, space, state of mind?");
        yield return TypeRoutine("that will never be here, again");
        yield return TypeRoutine("capture it. what made it what it was?");
        yield return TypeRoutine("I think I realize now -");
        yield return TypeRoutine("that 'here' that cannot be restored -");
        yield return TypeRoutine("it was the only place without the constant eyes");
        yield return TypeRoutine("so there was no need to hide.");
        yield return TypeRoutine("knowing that");
        yield return TypeRoutine("it is possible to return from oblivion.");

        FadeImageEffect fader = FindObjectOfType<FadeImageEffect>();
        yield return CoUtils.Wait(0.5f);
        TransitionData data = Global.Instance().Database.Transitions.GetData(MainMenu.TitleTransitionTag);
        var tween = GetComponent<CanvasGroup>().DOFade(0.0f, data.GetFadeOut().delay);
        yield return CoUtils.RunParallel(new IEnumerator[] {
            CoUtils.RunTween(tween),
            fader.FadeRoutine(data.GetFadeIn()),
            Global.Instance().Audio.FadeOutRoutine(data.GetFadeOut().delay),
        }, Global.Instance());
        SceneManager.LoadScene("Title", LoadSceneMode.Single);
    }

    private IEnumerator TypeRoutine(string text) {
        float elapsed = 0.0f;
        float total = text.Length / charsPerSecond;
        while (elapsed <= total) {
            elapsed += Time.deltaTime;
            int charsToShow = Mathf.FloorToInt(elapsed * charsPerSecond);
            int cutoff = charsToShow > text.Length ? text.Length : charsToShow;
            textbox.text = text.Substring(0, cutoff);
            textbox.text += "<color=#00000000>";
            textbox.text += text.Substring(cutoff);
            textbox.text += "</color>";
            yield return null;
        }
        awaitingConfirm = true;
        textbox.text = text;
        Global.Instance().Input.PushListener(this);

        confirmed = false;
        advanceArrow.SetActive(true);
        while (!confirmed) {
            yield return null;
        }
        awaitingConfirm = false;
        advanceArrow.SetActive(false);
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType != InputManager.Event.Up) {
            return false;
        }

        if (awaitingConfirm && command == InputManager.Command.Confirm) {
            confirmed = true;
            awaitingConfirm = false;
        }

        return false;
    }
}
