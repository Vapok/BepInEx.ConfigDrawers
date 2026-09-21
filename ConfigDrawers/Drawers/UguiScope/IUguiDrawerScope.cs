using System;
using UnityEngine;

namespace BepInEx.ConfigDrawers;

public interface IUguiDrawerScope
{
    Transform Container { get; }
    bool IsReadOnly { get; }
    IDisposable Horizontal(float spacing = 4f);
    IDisposable Vertical(float spacing = 4f);
    IDisposable Box(Color? backgroundColor = null, float padding = 4f, float spacing = 4f);
    IDisposable Row(Color? backgroundColor = null, float padding = 4f, float spacing = 4f);
    void Label(string text, float width = -1f);
    void TextField(string value, Action<string> onCommit, float width = -1f);
    void Button(string text, Action onClick, float width = 50f);
    void Button(string text, Action onClick, float width, string? tooltip);
    void Slider(float value, float min, float max, Action<float> onChanged, float width = 120f, string? format = null);
    void Toggle(bool value, string label, Action<bool> onChanged);
    void Space(float pixels);
    void Separator(float height = 1f, Color? color = null);
}
