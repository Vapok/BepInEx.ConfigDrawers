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
    private const float Width2D = 104f;
    private const float Width3D = 154f;
    private const float LabelWidth = 10f;

    public static bool CanDraw(SettingEntry entry)
    {
        if (entry == null || entry.SettingType == null)
        {
            return false;
        }

        var t = entry.SettingType;
        return t == typeof(Vector2) || t == typeof(Vector3) || t.Name == "Vector2i" || t.Name == "Vector3i";
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var is3D = Is3DVector(entry.SettingType);
        var isInteger = IsIntegerVector(entry.SettingType);
        var totalWidth = is3D ? Width3D : Width2D;
        var inputWidth = is3D ? 36f : 38f;

        TMP_InputField? inputX = null;
        TMP_InputField? inputY = null;
        TMP_InputField? inputZ = null;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            var (cx, cy, cz) = ReadVector(entry.ConfigEntry.BoxedValue);
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

        var (xVal, yVal, zVal) = ReadVector(entry.ConfigEntry.BoxedValue);

        var vectorContainer = new GameObject("VectorControls", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        vectorContainer.transform.SetParent(valueArea, false);

        var vRT = vectorContainer.GetComponent<RectTransform>();
        vRT.sizeDelta = new Vector2(totalWidth, 22f);

        var vLe = vectorContainer.GetComponent<LayoutElement>();
        vLe.minWidth = totalWidth;
        vLe.preferredWidth = totalWidth;
        vLe.flexibleWidth = 0f;
        vLe.minHeight = 22f;
        vLe.preferredHeight = 22f;
        vLe.flexibleHeight = 0f;

        var vHlg = vectorContainer.GetComponent<HorizontalLayoutGroup>();
        vHlg.spacing = 2.5f;
        vHlg.childAlignment = TextAnchor.MiddleRight;
        vHlg.childControlWidth = false;
        vHlg.childControlHeight = false;
        vHlg.childForceExpandWidth = false;
        vHlg.childForceExpandHeight = false;

        void TriggerCommit()
        {
            var sx = inputX != null ? inputX.text : FormatFloat(xVal);
            var sy = inputY != null ? inputY.text : FormatFloat(yVal);
            var sz = inputZ != null ? inputZ.text : FormatFloat(zVal);
            CommitChanges(entry, sx, sy, sz);
        }

        var xLabel = UiFactory.CreateLabel(vectorContainer.transform, "LabelX", "X", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.MidlineRight);
        var xlRT = xLabel.GetComponent<RectTransform>();
        xlRT.sizeDelta = new Vector2(LabelWidth, 20f);
        var xlLe = xLabel.gameObject.AddComponent<LayoutElement>();
        xlLe.minWidth = LabelWidth;
        xlLe.preferredWidth = LabelWidth;
        xlLe.flexibleWidth = 0f;

        var (_, inX) = UiFactory.CreateInputField(vectorContainer.transform, "InputX", FormatFloat(xVal), _ => TriggerCommit(), inputWidth, 20f);
        inputX = inX;
        inputX.interactable = entry.CanEdit;
        inputX.contentType = isInteger ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

        var yLabel = UiFactory.CreateLabel(vectorContainer.transform, "LabelY", "Y", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.MidlineRight);
        var ylRT = yLabel.GetComponent<RectTransform>();
        ylRT.sizeDelta = new Vector2(LabelWidth, 20f);
        var ylLe = yLabel.gameObject.AddComponent<LayoutElement>();
        ylLe.minWidth = LabelWidth;
        ylLe.preferredWidth = LabelWidth;
        ylLe.flexibleWidth = 0f;

        var (_, inY) = UiFactory.CreateInputField(vectorContainer.transform, "InputY", FormatFloat(yVal), _ => TriggerCommit(), inputWidth, 20f);
        inputY = inY;
        inputY.interactable = entry.CanEdit;
        inputY.contentType = isInteger ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

        if (is3D)
        {
            var zLabel = UiFactory.CreateLabel(vectorContainer.transform, "LabelZ", "Z", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.MidlineRight);
            var zlRT = zLabel.GetComponent<RectTransform>();
            zlRT.sizeDelta = new Vector2(LabelWidth, 20f);
            var zlLe = zLabel.gameObject.AddComponent<LayoutElement>();
            zlLe.minWidth = LabelWidth;
            zlLe.preferredWidth = LabelWidth;
            zlLe.flexibleWidth = 0f;

            var (_, inZ) = UiFactory.CreateInputField(vectorContainer.transform, "InputZ", FormatFloat(zVal), _ => TriggerCommit(), inputWidth, 20f);
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
            var type = boxed.GetType();
            var fx = type.GetField("x", BindingFlags.Instance | BindingFlags.Public) ?? (MemberInfo?)type.GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var fy = type.GetField("y", BindingFlags.Instance | BindingFlags.Public) ?? (MemberInfo?)type.GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            var fz = type.GetField("z", BindingFlags.Instance | BindingFlags.Public) ?? (MemberInfo?)type.GetProperty("z", BindingFlags.Instance | BindingFlags.Public);

            float x = 0f, y = 0f, z = 0f;

            if (fx is FieldInfo fldX) x = Convert.ToSingle(fldX.GetValue(boxed), CultureInfo.InvariantCulture);
            else if (fx is PropertyInfo propX) x = Convert.ToSingle(propX.GetValue(boxed, null), CultureInfo.InvariantCulture);

            if (fy is FieldInfo fldY) y = Convert.ToSingle(fldY.GetValue(boxed), CultureInfo.InvariantCulture);
            else if (fy is PropertyInfo propY) y = Convert.ToSingle(propY.GetValue(boxed, null), CultureInfo.InvariantCulture);

            if (fz is FieldInfo fldZ) z = Convert.ToSingle(fldZ.GetValue(boxed), CultureInfo.InvariantCulture);
            else if (fz is PropertyInfo propZ) z = Convert.ToSingle(propZ.GetValue(boxed, null), CultureInfo.InvariantCulture);

            return (x, y, z);
        }
        catch
        {
            return (0f, 0f, 0f);
        }
    }

    private static void CommitChanges(SettingEntry entry, string strX, string strY, string strZ)
    {
        if (!entry.CanEdit)
        {
            return;
        }

        if (!float.TryParse(strX, NumberStyles.Float, CultureInfo.InvariantCulture, out var fx))
        {
            return;
        }

        if (!float.TryParse(strY, NumberStyles.Float, CultureInfo.InvariantCulture, out var fy))
        {
            return;
        }

        float.TryParse(strZ, NumberStyles.Float, CultureInfo.InvariantCulture, out var fz);

        var t = entry.SettingType;
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
            var ix = Mathf.RoundToInt(fx);
            var iy = Mathf.RoundToInt(fy);
            var iz = Mathf.RoundToInt(fz);

            var ctor3 = t.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
            if (ctor3 != null)
            {
                var newObj = ctor3.Invoke(new object[] { ix, iy, iz });
                entry.SetValue(newObj);
                return;
            }

            var ctor3F = t.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            if (ctor3F != null)
            {
                var newObj = ctor3F.Invoke(new object[] { fx, fy, fz });
                entry.SetValue(newObj);
                return;
            }

            var ctor2 = t.GetConstructor(new[] { typeof(int), typeof(int) });
            if (ctor2 != null)
            {
                var newObj = ctor2.Invoke(new object[] { ix, iy });
                entry.SetValue(newObj);
                return;
            }

            var ctor2F = t.GetConstructor(new[] { typeof(float), typeof(float) });
            if (ctor2F != null)
            {
                var newObj = ctor2F.Invoke(new object[] { fx, fy });
                entry.SetValue(newObj);
                return;
            }

            var inst = Activator.CreateInstance(t);
            var fxFld = t.GetField("x", BindingFlags.Instance | BindingFlags.Public);
            var fyFld = t.GetField("y", BindingFlags.Instance | BindingFlags.Public);
            var fzFld = t.GetField("z", BindingFlags.Instance | BindingFlags.Public);

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
        catch
        {
            // Soft failure ignore
        }
    }

    private static string FormatFloat(float val)
    {
        return val % 1 == 0 ? ((int)val).ToString(CultureInfo.InvariantCulture) : val.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
