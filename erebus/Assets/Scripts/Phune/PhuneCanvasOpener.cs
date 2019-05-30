using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PhuneCanvasOpener : MonoBehaviour {

    private bool on;
    private bool next;

    void Start() {
        StartCoroutine(CoUtils.RunAfterDelay(1.0f, () => {
            StartCoroutine(FindObjectOfType<PhuneUI>().ShowRoutine());
            StartCoroutine(CoUtils.RunAfterDelay(1.0f, () => {
                on = true;
            }));
        }));
    }

    public void Update() {

        if (on && !FindObjectOfType<PhuneUI>().shown && !next) {
            next = true;
            Global.Instance().StartCoroutine(NextRoutine());
        }
    }

    private IEnumerator NextRoutine() {
        FadeImageEffect fader = FindObjectOfType<FadeImageEffect>();
        TransitionData data = Global.Instance().Database.Transitions.GetData(MainMenu.TitleTransitionTag);
        var tween = GetComponent<CanvasGroup>().DOFade(0.0f, data.GetFadeOut().delay);
        yield return CoUtils.RunParallel(new IEnumerator[] {
            CoUtils.RunTween(tween),
            fader.FadeRoutine(data.GetFadeIn()),
        }, Global.Instance());
        Global.Instance().Audio.PlaySFX("sub_arrives");
        yield return CoUtils.Wait(7.0f);
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
        fader = FindObjectOfType<FadeImageEffect>();
        yield return fader.FadeRoutine(data.GetFadeIn(), true);
        yield return CoUtils.Wait(1.0f);
        yield return FindObjectOfType<PhuneUI>().ShowRoutine();
    }
}
