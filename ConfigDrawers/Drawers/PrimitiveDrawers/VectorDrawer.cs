using System;
using System.Globalization;
using System.Reflection;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class VectorDrawer
{
    private const float Width2D = 137f;
    private const float Width3D = 207f;
    private const float LabelWidth = 10f;
    private const float InputWidth = 54f;
    private const float Spacing = 3f;

    public static bool CanDraw(SettingEntry entry)
    {
        if (entry == null || entry.SettingType == null)
        {
            return false;
        }

        Type t = entry.SettingType;
        return t == typeof(Vector2) || t == typeof(Vector3) || t.Name == "Vector2i" || t.Name == "Vector3i";
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        bool is3D = Is3DVector(entry.SettingType);
        bool isInteger = IsIntegerVector(entry.SettingType);
        float totalWidth = is3D ? Width3D : Width2D;

        TMP_InputField? inputX = null;
        TMP_InputField? inputY = null;
        TMP_InputField? inputZ = null;

        GameObject rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out Transform valueArea, () =>
        {
            (float cx, float cy, float cz) = ReadVector(entry.ConfigEntry.BoxedValue);
            if (inputX != null)
            {
                inputX.text = FormatFloat(cx);
            }
            if (inputY != null)
            {
                inputY.text = FormatFloat(cy);
            }
            if (inputZ != null)
            {
                inputZ.text = FormatFloat(cz);
            }
        }, totalWidth);

        (float xVal, float yVal, float zVal) = ReadVector(entry.ConfigEntry.BoxedValue);

        GameObject vectorContainer = new GameObject("VectorControls", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        vectorContainer.transform.SetParent(valueArea, false);

        RectTransform vRT = vectorContainer.GetComponent<RectTransform>();
        vRT.sizeDelta = new Vector2(totalWidth, 22f);

        LayoutElement vLe = vectorContainer.GetComponent<LayoutElement>();
        vLe.minWidth = totalWidth;
        vLe.preferredWidth = totalWidth;
        vLe.flexibleWidth = 0f;
        vLe.minHeight = 22f;
        vLe.preferredHeight = 22f;
        vLe.flexibleHeight = 0f;

        HorizontalLayoutGroup vHlg = vectorContainer.GetComponent<HorizontalLayoutGroup>();
        vHlg.spacing = Spacing;
        vHlg.childAlignment = TextAnchor.MiddleRight;
        vHlg.childControlWidth = false;
        vHlg.childControlHeight = false;
        vHlg.childForceExpandWidth = false;
        vHlg.childForceExpandHeight = false;

        void TriggerCommit()
        {
            string sx = inputX != null ? inputX.text : FormatFloat(xVal);
            string sy = inputY != null ? inputY.text : FormatFloat(yVal);
            string sz = inputZ != null ? inputZ.text : FormatFloat(zVal);
            CommitChanges(entry, sx, sy, sz);
        }

        TextMeshProUGUI xLabel = UiFactory.CreateLabel(vectorContainer.transform, "LabelX", "X", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.MidlineRight);
        RectTransform xlRT = xLabel.GetComponent<RectTransform>();
        xlRT.sizeDelta = new Vector2(LabelWidth, 20f);
        LayoutElement xlLe = xLabel.gameObject.AddComponent<LayoutElement>();
        xlLe.minWidth = LabelWidth;
        xlLe.preferredWidth = LabelWidth;
        xlLe.flexibleWidth = 0f;

        (_, TMP_InputField inX) = UiFactory.CreateInputField(vectorContainer.transform, "InputX", FormatFloat(xVal), _ => TriggerCommit(), InputWidth, 20f, horizontalPadding: 4f);
        inputX = inX;
        inputX.interactable = entry.CanEdit;
        inputX.contentType = isInteger ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

        TextMeshProUGUI yLabel = UiFactory.CreateLabel(vectorContainer.transform, "LabelY", "Y", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.MidlineRight);
        RectTransform ylRT = yLabel.GetComponent<RectTransform>();
        ylRT.sizeDelta = new Vector2(LabelWidth, 20f);
        LayoutElement ylLe = yLabel.gameObject.AddComponent<LayoutElement>();
        ylLe.minWidth = LabelWidth;
        ylLe.preferredWidth = LabelWidth;
        ylLe.flexibleWidth = 0f;

        (_, TMP_InputField inY) = UiFactory.CreateInputField(vectorContainer.transform, "InputY", FormatFloat(yVal), _ => TriggerCommit(), InputWidth, 20f, horizontalPadding: 4f);
        inputY = inY;
        inputY.interactable = entry.CanEdit;
        inputY.contentType = isInteger ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

        if (is3D)
        {
            TextMeshProUGUI zLabel = UiFactory.CreateLabel(vectorContainer.transform, "LabelZ", "Z", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.MidlineRight);
            RectTransform zlRT = zLabel.GetComponent<RectTransform>();
            zlRT.sizeDelta = new Vector2(LabelWidth, 20f);
            LayoutElement zlLe = zLabel.gameObject.AddComponent<LayoutElement>();
            zlLe.minWidth = LabelWidth;
            zlLe.preferredWidth = LabelWidth;
            zlLe.flexibleWidth = 0f;

            (_, TMP_InputField inZ) = UiFactory.CreateInputField(vectorContainer.transform, "InputZ", FormatFloat(zVal), _ => TriggerCommit(), InputWidth, 20f, horizontalPadding: 4f);
            inputZ = inZ;
            inputZ.interactable = entry.CanEdit;
            inputZ.contentType = isInteger ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
        }

        vectorContainer.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static bool Is3DVector(Type t)
    {
        return t == typeof(Vector3) || t.Name == "Vector3i";
    }

    private static bool IsIntegerVector(Type t)
    {
        return t.Name == "Vector2i" || t.Name == "Vector3i";
    }

    private static (float X, float Y, float Z) ReadVector(object? boxed)
    {
        if (boxed == null)
        {
            return (0f, 0f, 0f);
        }

        if (boxed is Vector2 v2)
        {
            return (v2.x, v2.y, 0f);
        }

        if (boxed is Vector3 v3)
        {
            return (v3.x, v3.y, v3.z);
        }

        try
        {
            Type type = boxed.GetType();
            MemberInfo? fx = type.GetField("x", BindingFlags.Instance | BindingFlags.Public) ?? (MemberInfo?)type.GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            MemberInfo? fy = type.GetField("y", BindingFlags.Instance | BindingFlags.Public) ?? (MemberInfo?)type.GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            MemberInfo? fz = type.GetField("z", BindingFlags.Instance | BindingFlags.Public) ?? (MemberInfo?)type.GetProperty("z", BindingFlags.Instance | BindingFlags.Public);

            float x = 0f, y = 0f, z = 0f;

            if (fx is FieldInfo fldX) x = Convert.ToSingle(fldX.GetValue(boxed), CultureInfo.InvariantCulture);
            else if (fx is PropertyInfo propX) x = Convert.ToSingle(propX.GetValue(boxed, null), CultureInfo.InvariantCulture);

            if (fy is FieldInfo fldY) y = Convert.ToSingle(fldY.GetValue(boxed), CultureInfo.InvariantCulture);
            else if (fy is PropertyInfo propY) y = Convert.ToSingle(propY.GetValue(boxed, null), CultureInfo.InvariantCulture);

            if (fz is FieldInfo fldZ) z = Convert.ToSingle(fldZ.GetValue(boxed), CultureInfo.InvariantCulture);
            else if (fz is PropertyInfo propZ) z = Convert.ToSingle(propZ.GetValue(boxed, null), CultureInfo.InvariantCulture);

            return (x, y, z);
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed reading vector components: {ex.Message}");
            return (0f, 0f, 0f);
        }
    }

    private static void CommitChanges(SettingEntry entry, string strX, string strY, string strZ)
    {
        if (!entry.CanEdit)
        {
            return;
        }

        if (!float.TryParse(strX, NumberStyles.Float, CultureInfo.InvariantCulture, out float fx))
        {
            return;
        }

        if (!float.TryParse(strY, NumberStyles.Float, CultureInfo.InvariantCulture, out float fy))
        {
            return;
        }

        float.TryParse(strZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float fz);

        Type t = entry.SettingType;
        if (t == typeof(Vector2))
        {
            entry.SetValue(new Vector2(fx, fy));
            return;
        }

        if (t == typeof(Vector3))
        {
            entry.SetValue(new Vector3(fx, fy, fz));
            return;
        }

        try
        {
            int ix = Mathf.RoundToInt(fx);
            int iy = Mathf.RoundToInt(fy);
            int iz = Mathf.RoundToInt(fz);

            ConstructorInfo? ctor3 = t.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
            if (ctor3 != null)
            {
                object? newObj = ctor3.Invoke(new object[] { ix, iy, iz });
                entry.SetValue(newObj);
                return;
            }

            ConstructorInfo? ctor3F = t.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            if (ctor3F != null)
            {
                object? newObj = ctor3F.Invoke(new object[] { fx, fy, fz });
                entry.SetValue(newObj);
                return;
            }

            ConstructorInfo? ctor2 = t.GetConstructor(new[] { typeof(int), typeof(int) });
            if (ctor2 != null)
            {
                object? newObj = ctor2.Invoke(new object[] { ix, iy });
                entry.SetValue(newObj);
                return;
            }

            ConstructorInfo? ctor2F = t.GetConstructor(new[] { typeof(float), typeof(float) });
            if (ctor2F != null)
            {
                object? newObj = ctor2F.Invoke(new object[] { fx, fy });
                entry.SetValue(newObj);
                return;
            }

            object? inst = Activator.CreateInstance(t);
            FieldInfo? fxFld = t.GetField("x", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo? fyFld = t.GetField("y", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo? fzFld = t.GetField("z", BindingFlags.Instance | BindingFlags.Public);

            if (fxFld != null && fyFld != null)
            {
                fxFld.SetValue(inst, fxFld.FieldType == typeof(int) ? ix : fx);
                fyFld.SetValue(inst, fyFld.FieldType == typeof(int) ? iy : fy);
                if (fzFld != null)
                {
                    fzFld.SetValue(inst, fzFld.FieldType == typeof(int) ? iz : fz);
                }
                entry.SetValue(inst);
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed committing vector value for {entry.Key}: {ex.Message}");
        }
    }

    private static string FormatFloat(float val)
    {
        return val % 1 == 0 ? ((int)val).ToString(CultureInfo.InvariantCulture) : val.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
