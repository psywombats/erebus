using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ProgramSecurity : PhuneProgram {

    private const float PerTick = 6.0f;

    public List<Text> digits;
    public Text countdowner;

    private float togo;

    public override void OnEnable() {
        base.OnEnable();
        Global.Instance().Memory.SetSwitch("sec", true);
        togo = 0.0f;
    }

    public void Update() {
        togo -= Time.deltaTime;
        if (togo < 0) {
            togo = PerTick;
            foreach (Text digit in digits) {
                digit.text = Random.Range(0, 10).ToString();
            }
        }
        countdowner.text = (Mathf.FloorToInt(togo * 100.0f) / 100.0f).ToString();
    }
}
