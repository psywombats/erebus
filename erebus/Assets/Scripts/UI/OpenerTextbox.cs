using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class OpenerTextbox : MonoBehaviour, InputListener {

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
        yield return TypeRoutine("a place that is not here, now");
        yield return TypeRoutine("that will never be here, again");
        yield return TypeRoutine("never.");
        yield return TypeRoutine("but instead");
        yield return TypeRoutine("a 'here' and 'now' that can be restored");
        yield return TypeRoutine("how?");
        yield return TypeRoutine("'how' is the task at hand, the focus");
        yield return TypeRoutine("focus.");
        yield return TypeRoutine("remove the emotion");
        yield return TypeRoutine("jettison the ego");
        yield return TypeRoutine("abandon the self");
        yield return TypeRoutine("destroy the conscience");
        yield return TypeRoutine("you are your action and you are your purpose");
        yield return TypeRoutine("perform. your. mission.");

        FadeImageEffect fader = FindObjectOfType<FadeImageEffect>();
        yield return CoUtils.Wait(0.5f);
        TransitionData data = Global.Instance().Database.Transitions.GetData(MainMenu.TitleTransitionTag);
        var tween = GetComponent<CanvasGroup>().DOFade(0.0f, data.GetFadeOut().delay);
        yield return CoUtils.RunParallel(new IEnumerator[] {
            CoUtils.RunTween(tween),
            fader.FadeRoutine(data.GetFadeIn()),
            Global.Instance().Audio.FadeOutRoutine(data.GetFadeOut().delay),
        }, Global.Instance());
        SceneManager.LoadScene("Subway", LoadSceneMode.Single);
        fader = FindObjectOfType<FadeImageEffect>();
        yield return fader.FadeRoutine(data.GetFadeOut(), true);
        yield return CoUtils.Wait(1.0f);
        yield return FindObjectOfType<PhuneUI>().ShowRoutine();
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
