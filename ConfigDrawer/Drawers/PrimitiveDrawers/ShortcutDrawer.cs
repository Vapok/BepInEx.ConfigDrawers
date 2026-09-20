using System;
using System.Collections;
using BepInEx.Configuration;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class ShortcutDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        GameObject? btnObj = null;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            var cur = entry.ConfigEntry.BoxedValue as KeyboardShortcut? ?? new KeyboardShortcut(KeyCode.None);
            if (btnObj != null)
            {
                var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = cur.MainKey != KeyCode.None ? cur.MainKey.ToString() : "None";
                    tmp.color = CyberPalette.ColorIceBlueBright;
                }
            }
        });

        var shortcut = entry.ConfigEntry.BoxedValue as KeyboardShortcut? ?? new KeyboardShortcut(KeyCode.None);
        var labelText = shortcut.MainKey != KeyCode.None ? shortcut.MainKey.ToString() : "None";

        btnObj = UiFactory.CreateCyberButton(valueArea, "ShortcutBtn", labelText, () =>
        {
            var mono = parent.GetComponentInParent<MonoBehaviour>();
            if (mono != null && btnObj != null)
            {
                mono.StartCoroutine(RecordRoutine(entry, btnObj));
            }
        }, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 70f, 22f);

        btnObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static IEnumerator RecordRoutine(SettingEntry entry, GameObject btnObj)
    {
        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = "Press key...";
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
            tmp.text = updated.MainKey != KeyCode.None ? updated.MainKey.ToString() : "None";
            tmp.color = CyberPalette.ColorIceBlueBright;
        }
    }
}
