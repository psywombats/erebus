using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MapEvent3D))]
public class CollectibleEvent : MonoBehaviour {

    public PhuneUplinkData program;

    public void Start() {
        GetComponent<Dispatch>().RegisterListener(MapEvent.EventInteract, OnInteract);
    }

    public void Awake() {
        if (Global.Instance().Memory.GetSwitch(SwitchName())) {
            gameObject.SetActive(false);
        }
    }

    public void OnInteract(object payload) {
        PhuneUI phune = FindObjectOfType<PhuneUI>();
        foreach (SlowFlashBehavior alerter in phune.tabsToTurnOff) {
            alerter.disable = false;
        }
        phune.AddData(program);
        phune.SetDefaultTab(program.category);
        StartCoroutine(phune.ShowRoutine());
        Global.Instance().Memory.SetSwitch(SwitchName(), true);
        gameObject.SetActive(false);
    }

    private string SwitchName() {
        return program.title;
    }
}
