using System;
using BepInEx.ConfigDrawers.Models;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers.LegacyBridge;

public class LegacyImguiBridge : MonoBehaviour
{
    private SettingEntry? _entry;

    public void Bind(SettingEntry entry)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    private void OnGUI()
    {
        if (_entry?.CustomDrawer == null)
        {
            return;
        }

        try
        {
            _entry.CustomDrawer(_entry.ConfigEntry);
        }
        catch (Exception ex)
        {
            GUILayout.Label($"[CustomDrawer Error]: {ex.Message}");
        }
    }
}
