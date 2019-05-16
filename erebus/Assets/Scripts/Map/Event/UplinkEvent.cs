using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MapEvent3D))]
public class UplinkEvent : MonoBehaviour {

    public PhuneUplinkData uplink;

    public void Start() {
        GetComponent<Dispatch>().RegisterListener(MapEvent.EventInteract, OnInteract);
    }

    public void OnInteract(object payload) {
        PhuneUI phune = FindObjectOfType<PhuneUI>();
        phune.AddTempData(uplink);
        StartCoroutine(phune.ShowRoutine());
    }
}
