using System;
using BepInEx.ConfigDrawers.Models;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers;

public class SettingChangeTracker : MonoBehaviour
{
    private SettingEntry? _entry;
    private Action? _callback;

    public void Init(SettingEntry entry, Action callback)
    {
        _entry = entry;
        _callback = callback;
        _entry.OnSettingValueChanged += OnSettingChanged;
    }

    private void OnSettingChanged()
    {
        _callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (_entry != null)
        {
            _entry.OnSettingValueChanged -= OnSettingChanged;
            _entry = null;
        }
        _callback = null;
    }
}
