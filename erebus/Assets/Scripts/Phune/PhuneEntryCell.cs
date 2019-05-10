using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class PhuneEntryCell : PhuneCell {

    public Text text;

    private Action selectAction;

    public void Populate(bool on, string text, Action selectAction) {
        Populate(on);
        this.selectAction = selectAction;
        this.text.text = text;
    }

    public override void Execute() {
        base.Execute();
        selectAction();
    }
}
