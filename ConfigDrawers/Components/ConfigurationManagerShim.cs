using System;
using BepInEx.ConfigDrawers;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using UnityEngine;

namespace ConfigurationManager;

public class ConfigurationManager : MonoBehaviour
{
    public static ConfigurationManager? Instance { get; private set; }

    public bool DisplayingWindow
    {
        get => ConfigDrawerWindow.Instance != null && ConfigDrawerWindow.Instance.IsVisible;
        set
        {
            if (value)
            {
                ConfigDrawers.OpenWindow();
            }
            else
            {
                ConfigDrawers.CloseWindow();
            }
        }
    }

    public bool OverrideHotkey { get; set; }

    public int RightColumnWidth => 350;

    public void SetRightColumnWidth(int value)
    {
    }

    private void Awake()
    {
        Instance = this;
    }

    public void BuildSettingList()
    {
        ConfigRegistry.Instance.Refresh();
    }
}
