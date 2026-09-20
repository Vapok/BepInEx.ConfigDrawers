using System;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class NumberDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        TMP_InputField? input = null;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            if (input != null)
            {
                input.text = entry.EditBuffer;
            }
        });

        var (inputRoot, inputField) = UiFactory.CreateInputField(valueArea, "Input", entry.EditBuffer, text =>
        {
            entry.UpdateBuffer(text);
            entry.CommitBuffer();
        }, 75f, 22f);

        input = inputField;
        inputField.onValueChanged.AddListener(val =>
        {
            entry.UpdateBuffer(val);
        });

        inputRoot.transform.SetAsFirstSibling();

        return rowObj;
    }
}
