using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PhuneMessageCell : PhuneEntryCell {
    
    public Image readIcon;
    public Image unreadIcon;

    public PhuneMessageCell(bool on, string subject, bool read) {
        Populate(on, subject, ShowText);
        readIcon.enabled = read;
        unreadIcon.enabled = !read;
    }

    private void ShowText() {

    }
}
