using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PhuneMessageCell : PhuneEntryCell {
    
    public Image readIcon;
    public Image unreadIcon;

    private PhuneUI presenter;
    private PhuneTxtData data;
    private bool read;
    private string subj, date, from, content;

    public void Populate(PhuneUI presenter, PhuneTxtData data, bool on, bool read) {
        this.data = data;
        this.read = read;
        Populate(on, data.subject, ShowText);
        this.presenter = presenter;
        subj = data.subject;
        date = data.date;
        from = data.from;
        content = data.contents;
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
        presenter.MarkMessageRead(data);
        read = true;
        Populate(on);
    }
}
