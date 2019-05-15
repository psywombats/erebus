using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PhuneCategory", menuName = "Data/Phune/PhuneCategory")]
public class PhuneCategoryData : PhuneEntryData {

    public string headerName;

    public override string GetSummary() {
        return headerName;
    }
}
