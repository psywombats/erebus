using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AlphaSet", menuName = "Data/AlphaSet")]
public class AlphaSet : ScriptableObject {

    public int firstIndex = 65;

    public List<Sprite> characters;
}
