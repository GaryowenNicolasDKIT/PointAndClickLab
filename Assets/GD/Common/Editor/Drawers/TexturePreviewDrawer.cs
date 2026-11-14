using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TexturePreviewAttribute))]
public class TexturePreviewDrawer : PropertyDrawer
{
    private float Size => ((TexturePreviewAttribute)attribute).Size;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight;
        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue != null)
        {
            h += EditorGUIUtility.standardVerticalSpacing + Size;
        }
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.LabelField(position, label.text, "[TexturePreview] requires a Texture or Sprite field");
            return;
        }

        var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(lineRect, property, label);
        EditorGUI.EndProperty();

        var obj = property.objectReferenceValue;
        if (obj == null) return;

        // Align the thumbnail with the field column, not the label.
        float indent = EditorGUIUtility.labelWidth;
        var area = new Rect(position.x + indent,
                            lineRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                            Mathf.Min(Size, position.width - indent),
                            Size);

        switch (obj)
        {
            case Sprite sprite when sprite.texture != null:
                DrawSprite(area, sprite);
                break;
            case Texture tex:
                EditorGUI.DrawTextureTransparent(area, tex, ScaleMode.ScaleToFit);
                break;
        }
    }

    private static void DrawSprite(Rect area, Sprite sprite)
    {
        var tex = sprite.texture;
        var r = sprite.textureRect;
        var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);

        var fitted = Fit(area, r.width / r.height);
        EditorGUI.DrawTextureTransparent(fitted, Texture2D.blackTexture, ScaleMode.StretchToFill);
        GUI.DrawTextureWithTexCoords(fitted, tex, uv, true);
    }

    private static Rect Fit(Rect area, float aspect)
    {
        if (area.width / area.height > aspect)
            return new Rect(area.x, area.y, area.height * aspect, area.height);

        float h = area.width / aspect;
        return new Rect(area.x, area.y + (area.height - h) * 0.5f, area.width, h);
    }
}
