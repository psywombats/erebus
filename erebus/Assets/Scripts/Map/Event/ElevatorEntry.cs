using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MapEvent3D))]
public class ElevatorEntry : DoorEvent {

    public int floor;

    public override IEnumerator TeleportRoutine(AvatarEvent avatar) {
        if (avatar.GetComponent<CharaEvent>().facing != dir) {
            yield break;
        }

        if (Global.Instance().Memory.GetSwitch("lift_" + floor)) {
            yield return base.TeleportRoutine(avatar);
        } else {
            string oldName = gameObject.name;
            string origName = GetComponent<MapEvent>().parent.name;
            mapName = "ElevatorShaft";
            targetEventName = "target";

            yield return base.TeleportRoutine(avatar);
            avatar.PauseInput();
            yield return CoUtils.Wait(2.0f);
            yield return CoUtils.RunParallel(new IEnumerator[] {
                avatar.GetComponent<MapEvent>().StepRoutine(OrthoDir.South),
                CoUtils.Wait(0.4f)
            }, Global.Instance());
            
            targetEventName = oldName;
            yield return Global.Instance().Maps.TeleportRoutine(origName, targetEventName);
            avatar.UnpauseInput();
        }
    }
}