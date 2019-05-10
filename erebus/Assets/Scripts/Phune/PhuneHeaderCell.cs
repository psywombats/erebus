using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PhuneHeaderCell : PhuneCell {

    public Text text;
    public Image chevronDownOn;
    public Image chevronDownOff;

    public bool expanded;

    public void Populate(bool on, bool expanded, string text) {
        Populate(on);
        this.expanded = expanded;
        this.text.text = text;
    }

    public override void Populate(bool on) {
        chevronDownOn.enabled = expanded && on;
        chevronDownOff.enabled = expanded && !on;
        chevronOff.enabled = !expanded && !on;
        chevronOn.enabled = !expanded && on;
    }
}
