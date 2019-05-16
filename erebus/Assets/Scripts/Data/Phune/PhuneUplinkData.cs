using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PhuneUplinkData", menuName = "Data/Phune/PhuneUplinkData")]
public class PhuneUplinkData : PhuneEntryData {

    public string title;
    public PhuneProgramType program;

    public override string GetSummary() {
        return title;
    }
}
