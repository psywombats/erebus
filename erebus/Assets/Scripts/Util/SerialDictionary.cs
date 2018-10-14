﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerialDictionary<K, V> {

    public List<K> keys;
    public List<V> values;

    public SerialDictionary(Dictionary<K, V> dictionary) {
        if (dictionary == null) {
            keys = new List<K>();
            values = new List<V>();
        } else {
            keys = new List<K>(dictionary.Keys);
            values = new List<V>(dictionary.Values);
        }
    }

    public Dictionary<K, V> ToDictionary() {
        Dictionary<K, V> result = new Dictionary<K, V>();
        if (keys != null) {
            for (int i = 0; i < keys.Count; i += 1) {
                result[keys[i]] = values[i];
            }
        }
        return result;
    }
}
