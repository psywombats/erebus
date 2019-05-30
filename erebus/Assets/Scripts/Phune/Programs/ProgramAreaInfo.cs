using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProgramAreaInfo : PhuneProgram {

    public Text areaName;
    public Text bgmName;
    public Text composerName;
    public List<Image> bands;

    public override void OnEnable() {
        base.OnEnable();
        areaName.text = AreaLocator.currentAreaName;
        bgmName.text = AreaLocator.currentBgmName;
        composerName.text = AreaLocator.currentBgmComposer;
    }

    public void Update() {
        for (int i = 0; i < bands.Count; i += 1) {
            WaveSource source = Global.Instance().Audio.GetWaveSource();
            bands[i].GetComponent<RectTransform>().localScale = new Vector3(1f, source.GetBand(i) * 5.0f);
        }
    }
}
