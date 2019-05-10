using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PhuneMessageCell : PhuneEntryCell {
    
    public Image readIcon;
    public Image unreadIcon;

    private bool read;
    private string subj, date, from, content;
    private PhuneUI presenter;

    public void Populate(PhuneUI presenter, bool on, bool read, string date, string from, string subj, string content) {
        this.read = read;
        Populate(on, subj, ShowText);
        this.presenter = presenter;
        this.subj = subj;
        this.date = date;
        this.from = from;
        this.content = content;
    }

    public override void Populate(bool on) {
        base.Populate(on);
        readIcon.enabled = read;
        unreadIcon.enabled = !read;
    }

    private void ShowText() {
        presenter.SelectEntry();
        presenter.uiText.Populate(date, from, subj, content);
        presenter.uiText.gameObject.SetActive(true);
        presenter.MarkMessageRead(date);
        read = true;
        Populate(on);
    }
}
