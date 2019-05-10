using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TxtUI : MonoBehaviour {

    public Text subjectLine;
    public Text dateLine;
    public Text fromLine;
    public Text contentArea;

    public void Populate(string date, string from, string subj, string content) {
        subjectLine.text = subj;
        dateLine.text = date;
        fromLine.text = from;
        contentArea.text = content;
    }
}
