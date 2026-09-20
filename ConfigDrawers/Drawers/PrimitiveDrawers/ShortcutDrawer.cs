using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class ShortcutDrawer
{
    private static readonly KeyCode[] ModifierKeys = new[]
    {
        KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftCommand, KeyCode.RightCommand,
        KeyCode.LeftWindows, KeyCode.RightWindows
    };

    public static string FormatShortcut(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None)
        {
            return "None";
        }

        var text = shortcut.ToString();
        return string.IsNullOrEmpty(text) ? "None" : text;
    }

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
                    tmp.text = FormatShortcut(cur);
                    tmp.color = CyberPalette.ColorIceBlueBright;
                }
            }
        }, 120f);

        var shortcut = entry.ConfigEntry.BoxedValue as KeyboardShortcut? ?? new KeyboardShortcut(KeyCode.None);
        var labelText = FormatShortcut(shortcut);

        btnObj = UiFactory.CreateCyberButton(valueArea, "ShortcutBtn", labelText, () =>
        {
            var mono = parent.GetComponentInParent<MonoBehaviour>();
            if (mono != null && btnObj != null)
            {
                mono.StartCoroutine(RecordRoutine(entry, btnObj));
            }
        }, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 120f, 22f);

        var btnTmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnTmp != null)
        {
            btnTmp.enableAutoSizing = true;
            btnTmp.fontSizeMin = 8f;
            btnTmp.fontSizeMax = 10f;
        }

        btnObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static IEnumerator RecordRoutine(SettingEntry entry, GameObject btnObj)
    {
        yield return null;
        while (Input.GetKey(KeyCode.Mouse0))
        {
            yield return null;
        }

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

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            {
                entry.SetValue(new KeyboardShortcut(KeyCode.None));
                recording = false;
                break;
            }

            var heldModifiers = new List<KeyCode>();
            foreach (var mod in ModifierKeys)
            {
                if (Input.GetKey(mod))
                {
                    heldModifiers.Add(mod);
                }
            }

            if (heldModifiers.Count > 0 && tmp != null)
            {
                tmp.text = $"{string.Join(" + ", heldModifiers)} + ...";
            }

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (code == KeyCode.None || code == KeyCode.Escape || code == KeyCode.Backspace || code == KeyCode.Delete)
                {
                    continue;
                }

                if (Array.IndexOf(ModifierKeys, code) >= 0)
                {
                    continue;
                }

                if (Input.GetKeyDown(code))
                {
                    entry.SetValue(new KeyboardShortcut(code, heldModifiers.ToArray()));
                    recording = false;
                    break;
                }
            }

            if (recording && heldModifiers.Count > 0)
            {
                foreach (var mod in heldModifiers)
                {
                    if (Input.GetKeyUp(mod))
                    {
                        entry.SetValue(new KeyboardShortcut(mod));
                        recording = false;
                        break;
                    }
                }
            }
        }

        if (tmp != null)
        {
            var updated = entry.ConfigEntry.BoxedValue as KeyboardShortcut? ?? new KeyboardShortcut(KeyCode.None);
            tmp.text = FormatShortcut(updated);
            tmp.color = CyberPalette.ColorIceBlueBright;
        }
    }
}
