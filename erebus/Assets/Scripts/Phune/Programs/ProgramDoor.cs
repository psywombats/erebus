using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProgramDoor : PhuneProgram {

    public Text cancelText, successText;
    public List<Text> digits;

    public override void OnEnable() {
        if (Global.Instance().Memory.GetSwitch("sec")) {
            foreach (Text digit in digits) {
                digit.text = Random.Range(0, 10).ToString();
            }
            StartCoroutine(CoUtils.RunAfterDelay(1.0f, () => {
                Global.Instance().Audio.PlaySFX("success");
            }));
            successText.enabled = true;
            cancelText.enabled = false;
            Global.Instance().Memory.SetSwitch("door_open", true);
        } else {
            successText.enabled = false;
            cancelText.enabled = true;
        }

    }
}
