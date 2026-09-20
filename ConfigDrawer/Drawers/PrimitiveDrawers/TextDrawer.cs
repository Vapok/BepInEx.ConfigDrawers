using System;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class TextDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        GameObject? inputRootObj = null;
        TMP_InputField? inputFieldObj = null;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            if (inputFieldObj != null)
            {
                inputFieldObj.text = entry.EditBuffer;
            }
            if (inputRootObj != null)
            {
                UpdateVisuals(inputRootObj, entry);
            }
        });

        (inputRootObj, inputFieldObj) = UiFactory.CreateInputField(valueArea, "Input", entry.EditBuffer, text =>
        {
            entry.UpdateBuffer(text);
            entry.CommitBuffer();
            if (inputRootObj != null)
            {
                UpdateVisuals(inputRootObj, entry);
            }
        }, 85f, 22f);

        inputFieldObj.onValueChanged.AddListener(val =>
        {
            entry.UpdateBuffer(val);
            if (inputRootObj != null)
            {
                UpdateVisuals(inputRootObj, entry);
            }
        });

        inputRootObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static void UpdateVisuals(GameObject inputRoot, SettingEntry entry)
    {
        var img = inputRoot.GetComponent<Image>();
        if (img == null)
        {
            return;
        }

        if (!entry.IsValid)
        {
            img.color = CyberPalette.ColorErrorRed;
        }
        else if (entry.IsDirty)
        {
            img.color = CyberPalette.ColorWarningAmber;
        }
        else
        {
            img.color = CyberPalette.ColorBorderSubtle;
        }
    }
}
