using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class TextDrawer
{
    private static readonly string ArrowColorHex = ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint);

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var rawInitial = entry.EditBuffer ?? string.Empty;
        var isExpandable = IsExpandable(entry, rawInitial);

        GameObject? inputRootObj = null;
        TMP_InputField? inputFieldObj = null;
        GameObject? subpanelObj = null;
        TMP_InputField? multilineInputObj = null;
        GameObject? multilineRootObj = null;
        TextMeshProUGUI? labelTmp = null;
        RectTransform? iconRT = null;
        var isExpanded = false;

        void UpdateLabel()
        {
            if (labelTmp != null)
            {
                if (isExpandable)
                {
                    var arrow = isExpanded ? "▼" : "▶";
                    labelTmp.richText = true;
                    labelTmp.text = $"<color=#{ArrowColorHex}><b>{arrow}</b></color>  {entry.DispName}";
                    labelTmp.ForceMeshUpdate();
                    if (iconRT != null)
                    {
                        iconRT.anchoredPosition = new Vector2(labelTmp.preferredWidth + 6f, 0f);
                    }
                }
                else
                {
                    labelTmp.text = entry.DispName;
                }
            }
        }

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            var text = entry.EditBuffer;
            if (inputFieldObj != null)
            {
                inputFieldObj.text = FormatDisplay(text);
            }
            if (multilineInputObj != null)
            {
                multilineInputObj.text = text ?? string.Empty;
            }
            if (inputRootObj != null)
            {
                UpdateVisuals(inputRootObj, entry);
            }
            if (multilineRootObj != null)
            {
                UpdateVisuals(multilineRootObj, entry);
            }
        });

        labelTmp = rowObj.transform.Find("Fill/LeftArea/Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp != null)
        {
            labelTmp.raycastTarget = false;
        }
        UpdateLabel();

        if (isExpandable)
        {
            DrawerDispatcher.SetRightControlWidth(rowObj, 0f);
            var leftArea = rowObj.transform.Find("Fill/LeftArea");

            if (leftArea != null && labelTmp != null)
            {
                iconRT = UiFactory.AttachTextPadIcon(leftArea, labelTmp);
            }

            Action toggleAction = () =>
            {
                isExpanded = !isExpanded;
                if (subpanelObj != null)
                {
                    subpanelObj.SetActive(isExpanded);
                }
                UpdateLabel();
                if (parent is RectTransform pRT)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
                }
            };

            DrawerDispatcher.AttachBarToggle(rowObj, toggleAction);

            subpanelObj = new GameObject($"SubText_{entry.Key}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            subpanelObj.transform.SetParent(parent, false);
            subpanelObj.transform.SetSiblingIndex(rowObj.transform.GetSiblingIndex() + 1);

            var bgImg = subpanelObj.GetComponent<Image>();
            bgImg.color = CyberPalette.ColorVoidBlack;

            var vlg = subpanelObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(16, 12, 6, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = subpanelObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = subpanelObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            (multilineRootObj, multilineInputObj) = UiFactory.CreateInputField(subpanelObj.transform, "MultilineInput", rawInitial, text =>
            {
                entry.UpdateBuffer(text);
                entry.CommitBuffer();
                if (multilineRootObj != null)
                {
                    UpdateVisuals(multilineRootObj, entry);
                }
            }, -1f, 68f, "", true);

            multilineInputObj.interactable = entry.CanEdit;
            multilineInputObj.onValueChanged.AddListener(val =>
            {
                entry.UpdateBuffer(val);
                if (multilineRootObj != null)
                {
                    UpdateVisuals(multilineRootObj, entry);
                }
            });

            subpanelObj.SetActive(false);
        }
        else
        {
            DrawerDispatcher.SetRightControlWidth(rowObj, 190f);
            (inputRootObj, inputFieldObj) = UiFactory.CreateInputField(valueArea, "Input", FormatDisplay(entry.EditBuffer), text =>
            {
                var raw = UnformatDisplay(text);
                entry.UpdateBuffer(raw);
                entry.CommitBuffer();
                if (inputRootObj != null)
                {
                    UpdateVisuals(inputRootObj, entry);
                }
            }, 190f, 22f, "");

            inputFieldObj.interactable = entry.CanEdit;
            inputFieldObj.onValueChanged.AddListener(val =>
            {
                var raw = UnformatDisplay(val);
                entry.UpdateBuffer(raw);
                if (inputRootObj != null)
                {
                    UpdateVisuals(inputRootObj, entry);
                }
            });

            inputRootObj.transform.SetAsFirstSibling();
        }

        return rowObj;
    }

    private static bool IsExpandable(SettingEntry entry, string rawVal)
    {
        if (entry == null)
        {
            return false;
        }

        var key = entry.Key.ToLowerInvariant();
        if (key.Contains("format") || key.Contains("template") || key.Contains("pattern"))
        {
            return true;
        }

        if (string.IsNullOrEmpty(rawVal))
        {
            return false;
        }

        return rawVal.Contains("{0}") ||
               rawVal.Contains("{1}") ||
               rawVal.Contains("\n") ||
               rawVal.Contains("\r") ||
               rawVal.Contains("<size") ||
               rawVal.Contains("<color") ||
               rawVal.Length > 28;
    }

    private static string FormatDisplay(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text!.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
    }

    private static string UnformatDisplay(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text!.Replace("\\n", "\n").Replace("\\r", "\r");
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
            img.color = CyberPalette.ColorInputGroove;
        }
    }
}
