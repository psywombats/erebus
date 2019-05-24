using UnityEngine;
using System.Collections;

public class TimedDrop : MonoBehaviour {

    public PhuneEntryData data;
    public float delay = 3.0f;

    public void OnEnable() {
        StartCoroutine(CoUtils.RunAfterDelay(0.1f, () => {
            if (!Global.Instance().Memory.GetSwitch(SwitchName())) {
                Global.Instance().Memory.SetSwitch(SwitchName(), true);
                Global.Instance().StartCoroutine(DropRoutine());
            }
        }));
    }

    private string SwitchName() {
        return Global.Instance().Maps.activeMap.name + "_drop";
    }

    private IEnumerator DropRoutine() {
        yield return CoUtils.Wait(delay);
        FindObjectOfType<PhuneUI>().AddDrop(data);
    }
}
