using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class FrameSoundEffect : MonoBehaviour {

    public Sprite frame0;
    public Sprite frame1;

    public bool useHighBand;
    public float thresh;

    public void Update() {
        WaveSource source = Global.Instance().Audio.GetWaveSource();
        float val = useHighBand ? source.GetHighBand() : source.GetLowBand();
        Sprite sprite = val < thresh ? frame0 : frame1;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
