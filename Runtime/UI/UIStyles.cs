using UnityEngine;
using UnityEngine.UIElements;

namespace WiseTwin.UI
{
    /// <summary>
    /// Design system centralisé pour toutes les UI runtime WiseTwin.
    /// Contient les tokens de design (couleurs, typographie, espacement, rayons)
    /// et des méthodes builder pour appliquer des styles cohérents.
    /// </summary>
    public static class UIStyles
    {
        // =====================================================================
        // COLOR PALETTE
        // =====================================================================

        // Brand / Accent
        public static readonly Color Accent = new Color(0.18f, 0.78f, 0.58f, 1f);       // #2EC795 - teal green
        public static readonly Color AccentHover = new Color(0.24f, 0.85f, 0.65f, 1f);   // lighter on hover
        public static readonly Color AccentSubtle = new Color(0.18f, 0.78f, 0.58f, 0.15f);

        // Semantic
        public static readonly Color Success = new Color(0.18f, 0.82f, 0.45f, 1f);       // green
        public static readonly Color SuccessBg = new Color(0.1f, 0.5f, 0.3f, 0.25f);
        public static readonly Color Danger = new Color(0.85f, 0.25f, 0.25f, 1f);        // red
        public static readonly Color DangerBg = new Color(0.5f, 0.1f, 0.1f, 0.25f);
        public static readonly Color Warning = new Color(0.92f, 0.7f, 0.2f, 1f);         // amber
        public static readonly Color WarningBg = new Color(0.8f, 0.5f, 0.1f, 0.15f);
        public static readonly Color Info = new Color(0.3f, 0.6f, 0.95f, 1f);            // blue
        public static readonly Color InfoBg = new Color(0.2f, 0.4f, 0.6f, 0.15f);

        // Surfaces
        public static readonly Color BgDeep = new Color(0.04f, 0.04f, 0.06f, 0.95f);     // fullscreen backdrops
        public static readonly Color BgBase = new Color(0.07f, 0.07f, 0.10f, 0.98f);     // cards / panels
        public static readonly Color BgElevated = new Color(0.10f, 0.10f, 0.14f, 1f);    // raised elements
        public static readonly Color BgInput = new Color(0.14f, 0.14f, 0.18f, 1f);       // inputs, options
        public static readonly Color BgInputHover = new Color(0.18f, 0.18f, 0.22f, 1f);

        // Overlay / Backdrop
        public static readonly Color BackdropLight = new Color(0f, 0f, 0f, 0.25f);
        public static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.82f);
        public static readonly Color BackdropHeavy = new Color(0f, 0f, 0f, 0.92f);

        // Borders
        public static readonly Color BorderSubtle = new Color(0.22f, 0.22f, 0.26f, 0.4f);
        public static readonly Color BorderDefault = new Color(0.28f, 0.28f, 0.32f, 0.7f);
        public static readonly Color BorderStrong = new Color(0.35f, 0.35f, 0.40f, 1f);

        // Text
        public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.97f, 1f);
        public static readonly Color TextSecondary = new Color(0.78f, 0.78f, 0.82f, 1f);
        public static readonly Color TextMuted = new Color(0.55f, 0.55f, 0.60f, 1f);
        public static readonly Color TextOnAccent = Color.white;

        // =====================================================================
        // TYPOGRAPHY (font sizes)
        // =====================================================================

        public const int FontXS = 12;
        public const int FontSM = 14;
        public const int FontBase = 16;
        public const int FontMD = 18;
        public const int FontLG = 20;
        public const int FontXL = 24;
        public const int Font2XL = 28;
        public const int Font3XL = 36;
        public const int Font4XL = 42;

        // =====================================================================
        // SPACING
        // =====================================================================

        public const int SpaceXS = 4;
        public const int SpaceSM = 8;
        public const int SpaceMD = 12;
        public const int SpaceLG = 16;
        public const int SpaceXL = 24;
        public const int Space2XL = 32;
        public const int Space3XL = 40;
        public const int Space4XL = 48;

        // =====================================================================
        // BORDER RADIUS
        // =====================================================================

        public const int RadiusSM = 6;
        public const int RadiusMD = 10;
        public const int RadiusLG = 14;
        public const int RadiusXL = 20;
        public const int Radius2XL = 26;
        public const int RadiusPill = 999;

        // =====================================================================
        // BUILDER METHODS - Surfaces
        // =====================================================================

        /// <summary>
        /// Applique le style de carte standard (fond sombre, coins arrondis, bordure subtile).
        /// </summary>
        public static void ApplyCardStyle(VisualElement el, int radius = RadiusXL)
        {
            el.style.backgroundColor = BgBase;
            SetBorderRadius(el, radius);
            SetBorderWidth(el, 1);
            SetBorderColor(el, BorderSubtle);
        }

        /// <summary>
        /// Applique le style de carte élevée (plus claire, bordure visible).
        /// </summary>
        public static void ApplyElevatedCardStyle(VisualElement el, int radius = RadiusLG)
        {
            el.style.backgroundColor = BgElevated;
            SetBorderRadius(el, radius);
            SetBorderWidth(el, 1);
            SetBorderColor(el, BorderDefault);
        }

        /// <summary>
        /// Applique le style de fond de modal plein écran.
        /// </summary>
        public static void ApplyBackdropStyle(VisualElement el)
        {
            el.style.position = Position.Absolute;
            el.style.left = 0;
            el.style.top = 0;
            el.style.width = Length.Percent(100);
            el.style.height = Length.Percent(100);
            el.style.backgroundColor = Backdrop;
            el.style.alignItems = Align.Center;
            el.style.justifyContent = Justify.Center;
            el.pickingMode = PickingMode.Position;
        }

        /// <summary>
        /// Applique le style de fond de modal heavy (complétion, dialogue).
        /// </summary>
        public static void ApplyBackdropHeavyStyle(VisualElement el)
        {
            ApplyBackdropStyle(el);
            el.style.backgroundColor = BackdropHeavy;
        }

        // =====================================================================
        // BUILDER METHODS - Buttons
        // =====================================================================

        /// <summary>
        /// Crée un bouton primaire (accent color, arrondi, bold).
        /// </summary>
        public static Button CreatePrimaryButton(string text, System.Action onClick = null)
        {
            var btn = new Button(onClick);
            btn.text = text;
            btn.style.height = 52;
            btn.style.fontSize = FontMD;
            btn.style.backgroundColor = Accent;
            btn.style.color = TextOnAccent;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorderRadius(btn, RadiusMD);
            SetBorderWidth(btn, 0);
            btn.style.paddingLeft = SpaceXL;
            btn.style.paddingRight = SpaceXL;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;

            // Hover
            btn.RegisterCallback<MouseEnterEvent>(evt =>
            {
                btn.style.backgroundColor = AccentHover;
                btn.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
            });
            btn.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = Accent;
                btn.style.scale = new Scale(Vector3.one);
            });

            return btn;
        }

        /// <summary>
        /// Crée un bouton secondaire (fond sombre, bordure).
        /// </summary>
        public static Button CreateSecondaryButton(string text, System.Action onClick = null)
        {
            var btn = new Button(onClick);
            btn.text = text;
            btn.style.height = 48;
            btn.style.fontSize = FontBase;
            btn.style.backgroundColor = BgElevated;
            btn.style.color = TextSecondary;
            SetBorderRadius(btn, RadiusMD);
            SetBorderWidth(btn, 1);
            SetBorderColor(btn, BorderDefault);
            btn.style.paddingLeft = SpaceXL;
            btn.style.paddingRight = SpaceXL;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;

            btn.RegisterCallback<MouseEnterEvent>(evt =>
            {
                btn.style.backgroundColor = BgInputHover;
                btn.style.color = TextPrimary;
                SetBorderColor(btn, Accent);
            });
            btn.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = BgElevated;
                btn.style.color = TextSecondary;
                SetBorderColor(btn, BorderDefault);
            });

            return btn;
        }

        /// <summary>
        /// Crée un bouton danger (rouge).
        /// </summary>
        public static Button CreateDangerButton(string text, System.Action onClick = null)
        {
            var btn = new Button(onClick);
            btn.text = text;
            btn.style.height = 48;
            btn.style.fontSize = FontBase;
            btn.style.backgroundColor = Danger;
            btn.style.color = TextOnAccent;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorderRadius(btn, RadiusMD);
            SetBorderWidth(btn, 0);
            btn.style.paddingLeft = SpaceXL;
            btn.style.paddingRight = SpaceXL;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;

            btn.RegisterCallback<MouseEnterEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.92f, 0.30f, 0.30f, 1f);
                btn.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
            });
            btn.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = Danger;
                btn.style.scale = new Scale(Vector3.one);
            });

            return btn;
        }

        /// <summary>
        /// Crée un petit bouton icône rond (ex: restart, close).
        /// </summary>
        public static Button CreateIconButton(string icon, int size, Color bgColor, System.Action onClick = null)
        {
            var btn = new Button(onClick);
            btn.text = icon;
            btn.style.width = size;
            btn.style.height = size;
            btn.style.fontSize = size * 0.55f;
            btn.style.backgroundColor = bgColor;
            btn.style.color = TextOnAccent;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorderRadius(btn, RadiusPill);
            SetBorderWidth(btn, 0);
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;

            return btn;
        }

        // =====================================================================
        // BUILDER METHODS - Text
        // =====================================================================

        /// <summary>
        /// Crée un label titre (grand, bold, accent).
        /// </summary>
        public static Label CreateTitle(string text, int fontSize = Font2XL)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = Accent;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        /// <summary>
        /// Crée un label de sous-titre.
        /// </summary>
        public static Label CreateSubtitle(string text, int fontSize = FontLG)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = TextSecondary;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        /// <summary>
        /// Crée un label de corps de texte.
        /// </summary>
        public static Label CreateBodyText(string text, int fontSize = FontBase)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = TextPrimary;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        /// <summary>
        /// Crée un label de texte muted.
        /// </summary>
        public static Label CreateMutedText(string text, int fontSize = FontSM)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = TextMuted;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        // =====================================================================
        // BUILDER METHODS - UI Elements
        // =====================================================================

        /// <summary>
        /// Crée un séparateur horizontal.
        /// </summary>
        public static VisualElement CreateSeparator(int marginVertical = SpaceMD)
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = BorderSubtle;
            sep.style.marginTop = marginVertical;
            sep.style.marginBottom = marginVertical;
            return sep;
        }

        /// <summary>
        /// Crée une barre de progression avec fill.
        /// Retourne (container, fill) pour pouvoir animer le fill.
        /// </summary>
        public static (VisualElement bar, VisualElement fill) CreateProgressBar(
            int height = 8, int radius = SpaceXS, Color? fillColor = null)
        {
            var bar = new VisualElement();
            bar.style.height = height;
            bar.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 0.6f);
            SetBorderRadius(bar, radius);

            var fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(0);
            fill.style.backgroundColor = fillColor ?? Accent;
            SetBorderRadius(fill, radius);
            // Smooth transition via USS is not available, but width is updated per-frame anyway
            bar.Add(fill);

            return (bar, fill);
        }

        /// <summary>
        /// Crée un conteneur de type "option sélectionnable" (pour choix, réponses QCM).
        /// </summary>
        public static VisualElement CreateSelectableOption(int radius = RadiusMD)
        {
            var option = new VisualElement();
            option.style.backgroundColor = BgInput;
            SetBorderRadius(option, radius);
            SetBorderWidth(option, 2);
            SetBorderColor(option, BorderDefault);
            option.style.paddingTop = SpaceLG;
            option.style.paddingBottom = SpaceLG;
            option.style.paddingLeft = SpaceXL;
            option.style.paddingRight = SpaceXL;
            option.style.marginBottom = SpaceSM;
            option.style.flexDirection = FlexDirection.Row;
            option.style.alignItems = Align.Center;

            return option;
        }

        /// <summary>
        /// Applique le hover interactif sur une option sélectionnable.
        /// </summary>
        public static void ApplyOptionHover(VisualElement option, System.Func<bool> isInteractable = null)
        {
            option.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (isInteractable != null && !isInteractable()) return;
                option.style.backgroundColor = BgInputHover;
                SetBorderColor(option, new Color(Info.r, Info.g, Info.b, 0.5f));
            });
            option.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (isInteractable != null && !isInteractable()) return;
                option.style.backgroundColor = BgInput;
                SetBorderColor(option, BorderDefault);
            });
        }

        /// <summary>
        /// Applique un style "correct" sur un élément (bordure verte, fond vert subtil).
        /// </summary>
        public static void ApplyCorrectStyle(VisualElement el)
        {
            el.style.backgroundColor = SuccessBg;
            SetBorderColor(el, Success);
        }

        /// <summary>
        /// Applique un style "incorrect" sur un élément (bordure rouge, fond rouge subtil).
        /// </summary>
        public static void ApplyIncorrectStyle(VisualElement el)
        {
            el.style.backgroundColor = DangerBg;
            SetBorderColor(el, Danger);
        }

        /// <summary>
        /// Applique un style "sélectionné / neutre" (bleu).
        /// </summary>
        public static void ApplySelectedStyle(VisualElement el)
        {
            el.style.backgroundColor = InfoBg;
            SetBorderColor(el, Info);
        }

        /// <summary>
        /// Réinitialise le style d'une option au défaut.
        /// </summary>
        public static void ResetOptionStyle(VisualElement el)
        {
            el.style.backgroundColor = BgInput;
            SetBorderColor(el, BorderDefault);
        }

        // =====================================================================
        // BUILDER METHODS - Scrollbar
        // =====================================================================

        /// <summary>
        /// Applique un style de scrollbar minimaliste sur un ScrollView.
        /// À appeler après AttachToPanel ou GeometryChanged.
        /// </summary>
        public static void ApplyMinimalScrollbar(ScrollView scrollView, int width = 4)
        {
            var scroller = scrollView.verticalScroller;
            if (scroller == null) return;

            scroller.style.position = Position.Absolute;
            scroller.style.right = SpaceSM;
            scroller.style.top = SpaceSM;
            scroller.style.bottom = SpaceSM;
            scroller.style.width = width;

            // Hide scroller arrow buttons (RepeatButton, not Button)
            scroller.lowButton.style.display = DisplayStyle.None;
            scroller.lowButton.style.width = 0;
            scroller.lowButton.style.height = 0;
            scroller.highButton.style.display = DisplayStyle.None;
            scroller.highButton.style.width = 0;
            scroller.highButton.style.height = 0;

            // Tracker invisible
            var tracker = scroller.Q<VisualElement>("unity-tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = Color.clear;
                SetBorderWidth(tracker, 0);
            }

            // Dragger minimal
            var dragger = scroller.Q<VisualElement>("unity-dragger");
            if (dragger != null)
            {
                dragger.style.backgroundColor = new Color(0.4f, 0.4f, 0.45f, 0.35f);
                SetBorderWidth(dragger, 0);
                SetBorderRadius(dragger, width / 2);
                dragger.style.width = width;
                dragger.style.marginLeft = 0;
                dragger.style.marginRight = 0;
                dragger.style.minHeight = 30;
            }

            // Slider track transparent
            var slider = scroller.Q<VisualElement>("unity-slider");
            if (slider != null)
            {
                slider.style.backgroundColor = Color.clear;
                SetBorderWidth(slider, 0);
            }
        }

        // =====================================================================
        // BUILDER METHODS - Layout
        // =====================================================================

        /// <summary>
        /// Crée un conteneur centré avec largeur max responsive.
        /// </summary>
        public static VisualElement CreateCenteredContainer(int width = 650, float maxWidthPercent = 90f)
        {
            var container = new VisualElement();
            container.style.width = width;
            container.style.maxWidth = Length.Percent(maxWidthPercent);
            container.style.alignSelf = Align.Center;
            return container;
        }

        /// <summary>
        /// Crée un tag / badge coloré.
        /// </summary>
        public static VisualElement CreateBadge(string text, Color bgColor, Color textColor)
        {
            var badge = new VisualElement();
            badge.style.backgroundColor = bgColor;
            SetBorderRadius(badge, RadiusPill);
            badge.style.paddingLeft = SpaceMD;
            badge.style.paddingRight = SpaceMD;
            badge.style.paddingTop = SpaceXS;
            badge.style.paddingBottom = SpaceXS;
            badge.style.alignSelf = Align.FlexStart;

            var label = new Label(text);
            label.style.fontSize = FontXS;
            label.style.color = textColor;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.Add(label);

            return badge;
        }

        // =====================================================================
        // UTILITY HELPERS
        // =====================================================================

        /// <summary>
        /// Applique un borderRadius uniforme.
        /// </summary>
        public static void SetBorderRadius(VisualElement el, int radius)
        {
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }

        /// <summary>
        /// Applique une borderWidth uniforme.
        /// </summary>
        public static void SetBorderWidth(VisualElement el, int width)
        {
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
        }

        /// <summary>
        /// Applique une borderColor uniforme.
        /// </summary>
        public static void SetBorderColor(VisualElement el, Color color)
        {
            el.style.borderTopColor = color;
            el.style.borderBottomColor = color;
            el.style.borderLeftColor = color;
            el.style.borderRightColor = color;
        }

        /// <summary>
        /// Applique un padding uniforme.
        /// </summary>
        public static void SetPadding(VisualElement el, int padding)
        {
            el.style.paddingTop = padding;
            el.style.paddingBottom = padding;
            el.style.paddingLeft = padding;
            el.style.paddingRight = padding;
        }

        /// <summary>
        /// Applique un margin uniforme.
        /// </summary>
        public static void SetMargin(VisualElement el, int margin)
        {
            el.style.marginTop = margin;
            el.style.marginBottom = margin;
            el.style.marginLeft = margin;
            el.style.marginRight = margin;
        }
    }
}
