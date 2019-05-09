using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

public class PhuneUI : MonoBehaviour, InputListener {

    public GameObject attachPoint;
    public Vector3 appearOffset;
    public float snapTime = 0.2f;
    [Space]
    public PhuneHeaderCell headerPrefab;
    public PhuneEntryCell entryPrefab;
    public PhuneMessageCell textPrefab;

    private Vector3 originalPosition;

    private Dictionary<PhuneHeaderCell, List<PhuneEntryCell>> entries;
    private List<PhuneCell> allCells;
    private int index, subindex;

    public void Awake() {
        originalPosition = transform.localPosition;
        Global.Instance().Input.PushListener(this);

        index = 0;
        subindex = 0;
        entries = new Dictionary<PhuneHeaderCell, List<PhuneEntryCell>>();
        allCells = new List<PhuneCell>();
        ReloadData();
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType == InputManager.Event.Up) {
            switch (command) {
                case (InputManager.Command.Menu):
                case (InputManager.Command.Cancel):
                    StartCoroutine(HideRoutine());
                    break;
            }
        }
        return true;
    }

    public IEnumerator ShowRoutine() {
        Global.Instance().Input.PushListener(this);
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => transform.localPosition = newPos,
            originalPosition + appearOffset,
            snapTime)
            .SetOptions(true));
    }

    public IEnumerator HideRoutine() {
        Global.Instance().Input.RemoveListener(this);
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => transform.localPosition = newPos,
            originalPosition,
            snapTime)
            .SetOptions(true));
    }

    private void ReloadData() {
        foreach (Transform child in transform) {
            Destroy(child);
        }
    }

    private void AddCell(PhuneCell cell) {
        cell.transform.parent = attachPoint.transform;
        allCells.Add(cell);
    }

    private void AddHeader(string text, List<PhuneEntryCell> entries) {
        PhuneHeaderCell header = Instantiate(headerPrefab);
        header.Populate(false, false, text);
        this.entries[header] = entries;
        foreach (PhuneEntryCell entry in entries) {
            AddCell(entry);
        }
    }
}
