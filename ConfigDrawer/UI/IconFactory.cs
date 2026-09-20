using UnityEngine;

namespace BepInEx.ConfigDrawers.UI;

public static class IconFactory
{
    private static Sprite? _syncSprite;
    private static Sprite? _lockSprite;

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
}
