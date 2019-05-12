using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MapEvent3D))]
public class DoorEvent : MonoBehaviour {

    public SimpleSpriteAnimator animator;
    [Space]
    public string mapName;
    public string targetEventName;

    public IEnumerator TeleportRoutine(AvatarEvent avatar) {
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
        avatar.UnpauseInput();
    }
}
