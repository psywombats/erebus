using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public abstract class PhuneCell : MonoBehaviour {

    public Image chevronOn;
    public Image chevronOff;

    public bool on;

    public virtual void Populate(bool on) {
        this.on = on;
        chevronOn.enabled = on;
        chevronOff.enabled = !on;
    }
}
