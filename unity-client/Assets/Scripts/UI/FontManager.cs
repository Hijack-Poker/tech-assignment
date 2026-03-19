using TMPro;
using UnityEngine;

public static class FontManager
{
    private static TMP_FontAsset _regular;

    public static TMP_FontAsset Regular
    {
        get
        {
            if (_regular != null) return _regular;

            var font = Resources.Load<Font>("Fonts/Fredoka-Regular");
            if (font != null)
            {
                _regular = TMP_FontAsset.CreateFontAsset(font);
                _regular.TryAddCharacters("\u2665\u2666\u2663\u2660"); // ♥♦♣♠
            }

            return _regular;
        }
    }
}
