using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AlphaPanelBehavior : MonoBehaviour {

    public string message;
    public float charasPerSecond = 2.0f;
    public AlphaSet alphas;
    public List<SpriteRenderer> slots;

    private int offset;
    private float elapsed;

    public void Update() {
        elapsed += Time.deltaTime;
        if (ShouldAdvanceThisFrame()) {
            offset += 1;
            for (int i = 0; i < slots.Count; i += 1) {
                slots[i].sprite = AlphaForSlot(i);
            }
        }
    }

    private Sprite AlphaForSlot(int slot) {
        int index = (offset + slot) % (message.Length + 2);
        if (index >= message.Length) return null;
        int chara = message[index] - alphas.firstIndex;
        if (chara < 0 || chara >= 26) {
            return null;
        } else {
            return alphas.characters[chara];
        }
    }

    private bool ShouldAdvanceThisFrame() {
        return offset < elapsed * charasPerSecond;
    }
}
