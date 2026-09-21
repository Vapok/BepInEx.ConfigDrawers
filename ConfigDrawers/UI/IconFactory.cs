using UnityEngine;

namespace BepInEx.ConfigDrawers.UI;

public static class IconFactory
{
    private static Sprite? _syncSprite;
    private static Sprite? _lockSprite;
    private static Sprite? _textPadSprite;

    public static Sprite GetSyncIcon()
    {
        if (_syncSprite != null)
        {
            return _syncSprite;
        }

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0f, 0f, 0f, 0f);
        var white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        // Top arrow (pointing right)
        for (int x = 3; x <= 12; x++) tex.SetPixel(x, 11, white);
        tex.SetPixel(11, 12, white);
        tex.SetPixel(10, 13, white);
        tex.SetPixel(11, 10, white);
        tex.SetPixel(10, 9, white);

        // Bottom arrow (pointing left)
        for (int x = 3; x <= 12; x++) tex.SetPixel(x, 4, white);
        tex.SetPixel(4, 5, white);
        tex.SetPixel(5, 6, white);
        tex.SetPixel(4, 3, white);
        tex.SetPixel(5, 2, white);

        tex.Apply();
        _syncSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _syncSprite;
    }

    public static Sprite GetLockIcon()
    {
        if (_lockSprite != null)
        {
            return _lockSprite;
        }

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0f, 0f, 0f, 0f);
        var white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        // Arch
        for (int x = 6; x <= 9; x++) tex.SetPixel(x, 13, white);
        tex.SetPixel(5, 12, white); tex.SetPixel(10, 12, white);
        tex.SetPixel(5, 11, white); tex.SetPixel(10, 11, white);
        tex.SetPixel(5, 10, white); tex.SetPixel(10, 10, white);
        tex.SetPixel(5, 9, white); tex.SetPixel(10, 9, white);

        // Body
        for (int y = 2; y <= 8; y++)
        {
            for (int x = 4; x <= 11; x++)
            {
                tex.SetPixel(x, y, white);
            }
        }

        // Keyhole cut
        tex.SetPixel(7, 5, clear); tex.SetPixel(8, 5, clear);
        tex.SetPixel(7, 4, clear); tex.SetPixel(8, 4, clear);

        tex.Apply();
        _lockSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _lockSprite;
    }

    public static Sprite GetTextPadIcon()
    {
        if (_textPadSprite != null)
        {
            return _textPadSprite;
        }

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0f, 0f, 0f, 0f);
        var white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        // Clip / top binding
        for (int x = 6; x <= 9; x++) tex.SetPixel(x, 14, white);

        // Pad outline
        for (int x = 3; x <= 12; x++)
        {
            tex.SetPixel(x, 13, white);
            tex.SetPixel(x, 1, white);
        }
        for (int y = 1; y <= 13; y++)
        {
            tex.SetPixel(3, y, white);
            tex.SetPixel(12, y, white);
        }

        // Ruled text lines
        for (int x = 5; x <= 10; x++) tex.SetPixel(x, 10, white);
        for (int x = 5; x <= 10; x++) tex.SetPixel(x, 7, white);
        for (int x = 5; x <= 8; x++) tex.SetPixel(x, 4, white);

        tex.Apply();
        _textPadSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _textPadSprite;
    }

    private static Sprite? _undoSprite;
    private static Sprite? _redoSprite;
    private static Sprite? _formatSprite;
    private static Sprite? _revertSprite;
    private static Sprite? _saveSprite;

    public static Sprite GetUndoIcon()
    {
        if (_undoSprite != null)
        {
            return _undoSprite;
        }

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        tex.SetPixel(3, 9, white);
        tex.SetPixel(4, 10, white);
        tex.SetPixel(4, 8, white);
        tex.SetPixel(5, 11, white);
        tex.SetPixel(5, 7, white);

        for (int x = 5; x <= 10; x++) tex.SetPixel(x, 9, white);
        for (int x = 5; x <= 10; x++) tex.SetPixel(x, 8, white);
        for (int y = 5; y <= 8; y++)
        {
            tex.SetPixel(11, y, white);
            tex.SetPixel(12, y, white);
        }

        tex.Apply();
        _undoSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _undoSprite;
    }

    public static Sprite GetRedoIcon()
    {
        if (_redoSprite != null)
        {
            return _redoSprite;
        }

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        tex.SetPixel(12, 9, white);
        tex.SetPixel(11, 10, white);
        tex.SetPixel(11, 8, white);
        tex.SetPixel(10, 11, white);
        tex.SetPixel(10, 7, white);

        for (int x = 5; x <= 10; x++) tex.SetPixel(x, 9, white);
        for (int x = 5; x <= 10; x++) tex.SetPixel(x, 8, white);
        for (int y = 5; y <= 8; y++)
        {
            tex.SetPixel(3, y, white);
            tex.SetPixel(4, y, white);
        }

        tex.Apply();
        _redoSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _redoSprite;
    }

    public static Sprite GetFormatIcon()
    {
        if (_formatSprite != null)
        {
            return _formatSprite;
        }

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        tex.SetPixel(5, 12, white);
        tex.SetPixel(4, 11, white);
        tex.SetPixel(4, 9, white);
        tex.SetPixel(3, 8, white);
        tex.SetPixel(4, 7, white);
        tex.SetPixel(4, 5, white);
        tex.SetPixel(5, 4, white);

        tex.SetPixel(10, 12, white);
        tex.SetPixel(11, 11, white);
        tex.SetPixel(11, 9, white);
        tex.SetPixel(12, 8, white);
        tex.SetPixel(11, 7, white);
        tex.SetPixel(11, 5, white);
        tex.SetPixel(10, 4, white);

        tex.Apply();
        _formatSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _formatSprite;
    }

    public static Sprite GetRevertIcon()
    {
        if (_revertSprite != null)
        {
            return _revertSprite;
        }

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        tex.SetPixel(3, 11, white);
        tex.SetPixel(4, 12, white);
        tex.SetPixel(3, 10, white);
        tex.SetPixel(2, 10, white);

        for (int x = 5; x <= 9; x++) tex.SetPixel(x, 12, white);
        tex.SetPixel(10, 11, white);
        tex.SetPixel(11, 10, white);
        for (int y = 6; y <= 9; y++) tex.SetPixel(12, y, white);
        tex.SetPixel(11, 5, white);
        tex.SetPixel(10, 4, white);
        for (int x = 6; x <= 9; x++) tex.SetPixel(x, 3, white);
        tex.SetPixel(5, 4, white);
        tex.SetPixel(4, 5, white);

        tex.Apply();
        _revertSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _revertSprite;
    }

    public static Sprite GetSaveIcon()
    {
        if (_saveSprite != null)
        {
            return _saveSprite;
        }

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        for (int x = 3; x <= 11; x++) tex.SetPixel(x, 13, white);
        tex.SetPixel(12, 12, white);
        for (int y = 2; y <= 11; y++) tex.SetPixel(12, y, white);
        for (int x = 3; x <= 12; x++) tex.SetPixel(x, 2, white);
        for (int y = 2; y <= 13; y++) tex.SetPixel(3, y, white);

        for (int x = 5; x <= 9; x++)
        {
            tex.SetPixel(x, 12, white);
            tex.SetPixel(x, 11, white);
            tex.SetPixel(x, 10, white);
        }
        tex.SetPixel(8, 11, clear);
        tex.SetPixel(8, 12, clear);

        for (int x = 5; x <= 10; x++)
        {
            for (int y = 4; y <= 7; y++)
            {
                tex.SetPixel(x, y, white);
            }
        }

        tex.Apply();
        _saveSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _saveSprite;
    }
}
