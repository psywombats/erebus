using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public abstract class PhuneCell : MonoBehaviour {

    public Image chevronOn;
    public Image chevronOff;

    protected virtual void Populate(bool on) {
        chevronOn.enabled = on;
        chevronOff.enabled = !on;
    }
}
