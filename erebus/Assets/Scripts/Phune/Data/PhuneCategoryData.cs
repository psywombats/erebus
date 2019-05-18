using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PhuneCategory", menuName = "Data/Phune/PhuneCategory")]
public class PhuneCategoryData : PhuneData {

    public string headerName;
    [Tooltip("Lower numbers are higher on the phune")]
    public int priority;

    public override string GetSummary() {
        return headerName;
    }
}
