using TMPro;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>Creates text that lives in the 3D world (screens, ad boards, the blimp).</summary>
    public static class WorldText
    {
        /// <param name="towardsViewer">Direction from the text to whoever should read it.</param>
        public static TextMeshPro Create(Transform parent, string name, Vector3 localPosition, Vector3 towardsViewer,
            Vector2 size, string text, float fontSize, Color colour)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);

            TextMeshPro label = holder.AddComponent<TextMeshPro>();
            label.rectTransform.localPosition = localPosition;
            // TextMeshPro is read from its -Z side, so it looks away from the viewer.
            label.rectTransform.localRotation = Quaternion.LookRotation(-towardsViewer, Vector3.up);
            label.rectTransform.sizeDelta = size;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = colour;
            return label;
        }

        /// <summary>A colour as a rich-text tag, e.g. &lt;color=#3F80FF&gt;.</summary>
        public static string ColourTag(Color colour)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(colour)}>";
        }
    }
}
