using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class SecondHeadBehavior : MonoBehaviour {

    private const int AmortizedCount = 16;

    public CharaEvent parent;
    [Space]
    public List<Sprite> sprites;

    private float[] amort;
    private Dictionary<string, Sprite> spritesByName;

    public void Awake() {
        spritesByName = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in sprites) {
            spritesByName.Add(sprite.name, sprite);
        }

        amort = new float[AmortizedCount];
    }

    // Update is called once per frame
    void Update() {
        MapEvent avatar = Global.Instance().Maps.avatar.GetComponent<MapEvent>();
        OrthoDir facing = parent.GetComponent<MapEvent>().DirectionTo(avatar);
        int y = facing.Ordinal();
        int x = Mathf.FloorToInt(parent.moveTime * CharaEvent.StepsPerSecond) % 4;
        if (x == 3) x = 1;
        if (!parent.stepping) x = 1;
        string name = CharaEvent.NameForFrame("ghoul_head", x, y);
        GetComponent<SpriteRenderer>().sprite = spritesByName[name];
        
        float val = 0.0f;
        for (int i = 0; i < AmortizedCount; i += 1) {
            val += amort[i];
            if (i == 0) {
                amort[i] = Global.Instance().Audio.GetWaveSource().GetLowBand();
            } else {
                amort[i] = amort[i - 1];
            }
        }
        val /= AmortizedCount;
        GetComponent<SpriteRenderer>().color = new Color(val, val, val, GetComponent<SpriteRenderer>().color.a);
    }
}
