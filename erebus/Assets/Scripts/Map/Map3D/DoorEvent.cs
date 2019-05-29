using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MapEvent3D))]
public class DoorEvent : MonoBehaviour {

    public OrthoDir dir;

    public SimpleSpriteAnimator animator;
    [Space]
    public string mapName;
    public string targetEventName;
    public bool requiresLightsOff;
    public bool requiresGlitch;

    public virtual IEnumerator TeleportRoutine(AvatarEvent avatar) {
        if (avatar.GetComponent<CharaEvent>().facing != dir) {
            yield break;
        }
        if (!GetComponent<MapEvent>().switchEnabled) {
            yield break;
        }
        if (requiresGlitch && !Global.Instance().Memory.GetSwitch("glitch_on")) {
            Global.Instance().Audio.PlaySFX("locked");
            yield break;
        }
        if (requiresLightsOff && !Global.Instance().IsLightsOutMode()) {
            Global.Instance().Audio.PlaySFX("locked");
            yield break;
        }

        avatar.PauseInput();
        while (avatar.GetComponent<MapEvent>().tracking) {
            yield return null;
        }
        yield return animator.PlayRoutine();
        yield return CoUtils.Wait(animator.frameDuration * 2);
        Vector3 targetPx = avatar.GetComponent<MapEvent>().positionPx + avatar.GetComponent<CharaEvent>().facing.Px3D();
        yield return CoUtils.RunParallel(new IEnumerator[] {
            avatar.GetComponent<MapEvent>().LinearStepRoutine(targetPx),
            avatar.GetComponent<CharaEvent>().FadeRoutine(1.0f / avatar.GetComponent<MapEvent>().tilesPerSecond * 0.75f)
        }, this);
        yield return CoUtils.Wait(animator.frameDuration * 2);
        yield return Global.Instance().Maps.TeleportRoutine(mapName, targetEventName);
        yield return avatar.GetComponent<MapEvent>().StepRoutine(avatar.GetComponent<CharaEvent>().facing);
        avatar.UnpauseInput();
    }
}
