using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class PhuneUI : MonoBehaviour, InputListener {

    private const float subsOffset = 300f;
    private const float subsDuration = 1.2f;

    public GameObject attachPoint;
    public Vector3 appearOffset;
    public float snapTime = 0.2f;
    public GameObject standardContent;
    public GameObject subContent;
    [Space]
    public PhuneHeaderCell headerPrefab;
    public PhuneEntryCell entryPrefab;
    public PhuneMessageCell textPrefab;
    [Space]
    public TxtUI uiText;
    [Space]
    public List<SlowFlashBehavior> tabsToTurnOff;
    [Space]
    public List<PhuneEntryData> dataModel;
    [Space]
    public GameObject invertLightsPane;

    private Vector3 originalPosition;
    private bool shown = false;
    private bool subselectionMode;

    private Dictionary<PhuneHeaderCell, List<PhuneEntryCell>> entries;
    private List<PhuneEntryData> tempData;
    private List<PhuneCell> allCells;
    private PhuneCell selection;

    private static HashSet<PhuneEntryData> readMessages = new HashSet<PhuneEntryData>();

    public void Awake() {
        originalPosition = transform.localPosition;
        Global.Instance().Input.PushListener(this);

        entries = new Dictionary<PhuneHeaderCell, List<PhuneEntryCell>>();
        tempData = new List<PhuneEntryData>();
        allCells = new List<PhuneCell>();
        ReloadData();

        DontDestroyOnLoad(transform.parent.gameObject);
    }

    public void AddTempData(PhuneEntryData data) {
        dataModel.Add(data);
        tempData.Add(data);
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
            } else if (subselectionMode) {
                switch (command) {
                    case InputManager.Command.Cancel:
                    case InputManager.Command.Menu:
                    case InputManager.Command.Confirm:
                    case InputManager.Command.Left:
                        SelectEntry(true);
                        break;
                }
            } else {
                switch (command) {
                    case InputManager.Command.Cancel:
                        if (!(selection is PhuneHeaderCell)) {
                            CollapseEntry();
                        } else {
                            StartCoroutine(HideRoutine());
                        }
                        break;
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
                    case InputManager.Command.Confirm:
                        if (selection is PhuneHeaderCell) {
                            CollapseEntry();
                        } else {
                            selection.Execute();
                        }
                        break;
                    case InputManager.Command.Left:
                        CollapseEntry();
                        break;
                }
            }

        }
        return true;
    }

    public void SelectEntry(bool deselect = false) {
        subselectionMode = !deselect;
        Vector3 to = new Vector3(deselect ? 0 : -subsOffset, 0.0f, 0.0f);
        var tween = DOTween.To(
            () => standardContent.GetComponent<RectTransform>().anchoredPosition,
            (Vector3 newPos) => {
                Vector3 newTo;
                if (newPos != to) {
                    newTo = new Vector3(
                        newPos.x - (newPos.x % 16),
                        newPos.y,
                        newPos.z);
                } else {
                    newTo = newPos;
                }
                standardContent.GetComponent<RectTransform>().anchoredPosition = newTo;
                subContent.GetComponent<RectTransform>().anchoredPosition = new Vector3(
                    newTo.x + subsOffset,
                    newTo.y,
                    newTo.z);
            },
            to,
            subsDuration);
        tween.SetOptions(true);
        //tween.SetEase(Ease.Linear);

        StartCoroutine(CoUtils.RunWithCallback(CoUtils.RunTween(tween), () => {
            if (deselect) {
                uiText.gameObject.SetActive(false);
            }
        }));
    }

    public bool IsMessageRead(PhuneTxtData data) {
        bool result = readMessages.Contains(data);
        return result;
    }

    public void MarkMessageRead(PhuneTxtData data) {
        readMessages.Add(data);
    }

    public IEnumerator ShowRoutine() {
        ReloadData();
        foreach (SlowFlashBehavior flash in tabsToTurnOff) {
            flash.disable = true;
        }
        Global.Instance().Input.PushListener(this);
        yield return CoUtils.RunWithCallback(CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => GetComponent<RectTransform>().localPosition = newPos,
            originalPosition + appearOffset,
            snapTime)
            .SetOptions(true)), 
            () => {
                shown = true;
            });
    }

    public IEnumerator HideRoutine() {
        shown = false;
        Global.Instance().Input.RemoveListener(this);
        foreach (PhuneEntryData data in tempData) {
            dataModel.Remove(data);
        }
        tempData.Clear();
        yield return CoUtils.RunTween(DOTween.To(
            () => GetComponent<RectTransform>().localPosition,
            (Vector3 newPos) => GetComponent<RectTransform>().localPosition = newPos,
            originalPosition,
            snapTime)
            .SetOptions(true));
    }

    private void ReloadData() {
        allCells.Clear();
        foreach (Transform child in attachPoint.transform) {
            Destroy(child.gameObject);
        }

        Dictionary<PhuneCategoryData, List<PhuneEntryCell>> entriesByCategory =
                new Dictionary<PhuneCategoryData, List<PhuneEntryCell>>();

        foreach (PhuneEntryData data in dataModel) {
            if (!entriesByCategory.ContainsKey(data.category)) {
                List<PhuneEntryCell> newSiblings = new List<PhuneEntryCell>();
                entriesByCategory[data.category] = newSiblings;
            }
            List<PhuneEntryCell> siblings = entriesByCategory[data.category];
            siblings.Add(GenerateEntry(data));
        }

        foreach (PhuneCategoryData category in entriesByCategory.Keys) {
            AddSection(category.GetSummary(), entriesByCategory[category]);
        }

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

    private PhuneEntryCell GenerateEntry(PhuneEntryData data) {
        if (data is PhuneTxtData) {
            return GenerateMessage((PhuneTxtData)data);
        } else if (data is PhuneUplinkData) {
            PhuneEntryCell cell = Instantiate(entryPrefab);
            cell.Populate(false, data.GetSummary(), () => {
                SelectEntry();
                PhuneUplinkData uplink = (PhuneUplinkData)data;
                switch (uplink.program) {
                    case PhuneProgramType.ProgramInvertLights:
                        invertLightsPane.SetActive(true);
                        break;
                }
            });
            return cell;
        } else {
            PhuneEntryCell cell = Instantiate(entryPrefab);
            cell.Populate(false, data.GetSummary(), () => { });
            return cell;
        }
    }

    private PhuneMessageCell GenerateMessage(PhuneTxtData data) {
        PhuneMessageCell cell = Instantiate(textPrefab);
        if (data.preread) readMessages.Add(data);
        cell.Populate(this, data, false, IsMessageRead(data));
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
