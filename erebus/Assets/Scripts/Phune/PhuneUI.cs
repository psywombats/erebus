using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class PhuneUI : MonoBehaviour, InputListener {

    public GameObject attachPoint;
    public Vector3 appearOffset;
    public float snapTime = 0.2f;
    [Space]
    public PhuneHeaderCell headerPrefab;
    public PhuneEntryCell entryPrefab;
    public PhuneMessageCell textPrefab;

    private Vector3 originalPosition;
    private bool shown = true;

    private Dictionary<PhuneHeaderCell, List<PhuneEntryCell>> entries;
    private List<PhuneCell> allCells;
    private PhuneCell selection;

    public void Awake() {
        originalPosition = transform.localPosition;
        Global.Instance().Input.PushListener(this);

        entries = new Dictionary<PhuneHeaderCell, List<PhuneEntryCell>>();
        allCells = new List<PhuneCell>();
        ReloadData();
    }

    public bool OnCommand(InputManager.Command command, InputManager.Event eventType) {
        if (eventType == InputManager.Event.Up) {
            if (!shown) {
                if (command == InputManager.Command.Menu) {
                    StartCoroutine(ShowRoutine());
                    return true;
                } else {
                    return false;
                }
            } else {
                switch (command) {
                    case InputManager.Command.Cancel:
                    case InputManager.Command.Menu:
                        StartCoroutine(HideRoutine());
                        break;
                    case InputManager.Command.Up:
                        MoveSelection(-1);
                        break;
                    case InputManager.Command.Down:
                        MoveSelection(1);
                        break; 
                    case InputManager.Command.Right:
                    case InputManager.Command.Left:
                        CollapseEntry();
                        break;
                }
            }

        }
        return true;
    }

    public IEnumerator ShowRoutine() {
        shown = true;
        Global.Instance().Input.PushListener(this);
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => transform.localPosition = newPos,
            originalPosition + appearOffset,
            snapTime)
            .SetOptions(true));
    }

    public IEnumerator HideRoutine() {
        shown = false;
        Global.Instance().Input.RemoveListener(this);
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => transform.localPosition = newPos,
            originalPosition,
            snapTime)
            .SetOptions(true));
    }

    private void ReloadData() {
        foreach (Transform child in attachPoint.transform) {
            Destroy(child.gameObject);
        }

        List<PhuneEntryCell> entries = new List<PhuneEntryCell>();

        entries.Add(GenerateEntry("yabba", null));
        entries.Add(GenerateEntry("yabba", null));
        entries.Add(GenerateEntry("yeek", null));
        entries.Add(GenerateEntry("yeek", null));
        AddSection("Yeeks", entries);
        entries.Clear();

        entries.Add(GenerateEntry("orf", null));
        entries.Add(GenerateEntry("blorf", null));
        entries.Add(GenerateEntry("gibborf", null));
        AddSection("Metasyntactic variables", entries);
        entries.Clear();

        selection = allCells[0];
        selection.Populate(true);
        UpdateSelection();
    }

    private void AddCell(PhuneCell cell) {
        cell.transform.SetParent(attachPoint.transform);
        allCells.Add(cell);
    }

    private void AddSection(string headerText, List<PhuneEntryCell> entries) {
        PhuneHeaderCell header = Instantiate(headerPrefab);
        header.Populate(false, false, headerText);
        AddCell(header);
        this.entries[header] = entries;
        foreach (PhuneEntryCell entry in entries) {
            AddCell(entry);
        }
    }

    private PhuneEntryCell GenerateEntry(string text, Action action) {
        PhuneEntryCell cell = Instantiate(entryPrefab);
        cell.Populate(false, text, action);
        return cell;
    }

    private void MoveSelection(int delta) {
        int index = allCells.IndexOf(selection);
        if (index + delta >= allCells.Count || index + delta < 0) return;
        PhuneCell next = allCells[index + delta];

        if (next is PhuneHeaderCell && !(selection is PhuneHeaderCell)) return;

        if (selection is PhuneHeaderCell) {
            while (!(next is PhuneHeaderCell)) {
                index += delta;
                if (index >= allCells.Count || index == -1) return;
                next = allCells[index];
            }
        }

        selection.Populate(false);
        next.Populate(true);
        selection = next;

        UpdateSelection();
    }

    private void CollapseEntry() {
        if (selection is PhuneHeaderCell) {
            PhuneHeaderCell header = (PhuneHeaderCell)selection;
            header.expanded = true;
            header.Populate(false);

            int index = allCells.IndexOf(selection);
            selection = allCells[index + 1];
            selection.Populate(true);
        } else {
            selection.Populate(false);

            PhuneHeaderCell header = null;
            foreach (PhuneCell cell in allCells) {
                if (cell is PhuneHeaderCell) {
                    header = (PhuneHeaderCell)cell;
                }
                if (cell == selection) {
                    header.expanded = false;
                    header.Populate(true);
                    selection = header;
                    break;
                }
            }
        }
        UpdateSelection();
    }

    private void UpdateSelection() {
        PhuneHeaderCell header = null;
        foreach (PhuneCell cell in allCells) {
            if (cell is PhuneHeaderCell) {
                header = (PhuneHeaderCell)cell;
            } else {
                cell.gameObject.SetActive(header.expanded);
            }
        }
    }
}
