using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PhuneEntry", menuName = "Data/Phune/PhuneTxtEntry")]
public class PhuneTxtData : PhuneEntryData {

    public string subject;
    public string from;
    public string date;
    [Space]
    public bool preread;
    [TextArea(15, 30)]
    public string contents;

    public override string GetSummary() {
        return subject;
    }
}
