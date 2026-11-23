using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UiFactory
{
    public static readonly Color ButtonColor = new(0.18f, 0.32f, 0.24f, 0.95f);
    public static readonly Color Gold = new(1f, 0.84f, 0.35f);

    public static Canvas Canvas(string name, int order = 0)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static EventSystem EventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        return go.GetComponent<EventSystem>();
    }

    public static void EventSystemIfMissing()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null) EventSystem();
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static Image Image(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = Rect(name, parent, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.3f;
        }
        return img;
    }

    public static Image FullScreenImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var rt = Stretch(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
        Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = true)
    {
        var rt = Rect(name, parent, anchor, pos, box);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = new Color32(15, 20, 18, 255);
        return tmp;
    }

    public static Button Button(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var img = Image(name, parent, ArtGenerator.UiPanel, Color.white, anchor, pos, size);
        img.raycastTarget = true;
        var button = img.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.30f, 0.55f, 0.36f);
        colors.selectedColor = new Color(0.36f, 0.62f, 0.40f);
        colors.pressedColor = new Color(0.85f, 0.65f, 0.25f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = img;

        var text = Text("Label", img.transform, label, size.y * 0.5f, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        img.gameObject.AddComponent<ButtonSounds>();
        return button;
    }

    public static void ChainVertical(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length],
                selectOnDown = buttons[(i + 1) % buttons.Length],
            };
            buttons[i].navigation = nav;
        }
    }

    public static Slider Slider(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, out Image fill)
    {
        var rt = Rect(name, parent, anchor, pos, size);
        var slider = rt.gameObject.AddComponent<Slider>();

        var bg = Image("Background", rt, ArtGenerator.UiPanel, new Color(0.1f, 0.1f, 0.12f, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero);
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

        var area = Stretch("FillArea", rt);
        area.offsetMin = new Vector2(6, 6);
        area.offsetMax = new Vector2(-6, -6);

        fill = Image("Fill", area, ArtGenerator.UiPanel, new Color(0.3f, 0.85f, 0.35f), Vector2.zero, Vector2.zero, Vector2.zero);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = null;
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }
}
