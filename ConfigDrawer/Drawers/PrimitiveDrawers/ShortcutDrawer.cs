using System;
using System.Collections;
using BepInEx.Configuration;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class ShortcutDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var rowObj = CreateRowContainer(parent, entry);
        var valueArea = rowObj.transform.Find("ValueArea");
        var targetParent = valueArea != null ? valueArea : rowObj.transform;

        var shortcut = entry.ConfigEntry.BoxedValue as KeyboardShortcut? ?? new KeyboardShortcut(KeyCode.None);
        var labelText = shortcut.MainKey != KeyCode.None ? $"[ {shortcut.MainKey} ]" : "[ NONE ]";

        var btn = UiFactory.CreateCyberButton(targetParent, "ShortcutBtn", labelText, () =>
        {
            var mono = parent.GetComponentInParent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(RecordRoutine(entry, targetParent));
            }
        }, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 110f, 24f);

        return rowObj;
    }

    private static IEnumerator RecordRoutine(SettingEntry entry, Transform container)
    {
        var tmp = container.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = "[ PRESS KEY ]";
            tmp.color = CyberPalette.ColorWarningAmber;
        }

        var recording = true;
        while (recording)
        {
            yield return null;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(code) && code != KeyCode.Escape)
                {
                    entry.SetValue(new KeyboardShortcut(code));
                    recording = false;
                    break;
                }
            }
        }

        if (tmp != null)
        {
            var updated = entry.ConfigEntry.BoxedValue as KeyboardShortcut? ?? new KeyboardShortcut(KeyCode.None);
            tmp.text = updated.MainKey != KeyCode.None ? $"[ {updated.MainKey} ]" : "[ NONE ]";
            tmp.color = CyberPalette.ColorIceBlueBright;
        }
    }

    private static GameObject CreateRowContainer(Transform parent, SettingEntry entry)
    {
        var row = UiFactory.CreatePanel(parent, $"Row_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 32f;
        layout.preferredHeight = 32f;

        var hover = row.AddComponent<HoverCardHandler>();
        hover.Bind(entry);

        var fill = row.transform.Find("Fill");
        var target = fill != null ? fill : row.transform;

        var label = UiFactory.CreateLabel(target, "Label", entry.DispName, entry.EntryColor, 11f);
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.55f, 1f);
        labelRT.offsetMin = new Vector2(10f, 0f);
        labelRT.offsetMax = Vector2.zero;

        var valueArea = new GameObject("ValueArea", typeof(RectTransform));
        valueArea.transform.SetParent(target, false);
        var valueRT = valueArea.GetComponent<RectTransform>();
        valueRT.anchorMin = new Vector2(0.55f, 0f);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.offsetMin = Vector2.zero;
        valueRT.offsetMax = new Vector2(-10f, 0f);

        return row;
    }
}
