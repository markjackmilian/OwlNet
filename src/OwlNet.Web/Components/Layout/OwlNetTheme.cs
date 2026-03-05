using MudBlazor;

namespace OwlNet.Web.Components.Layout;

/// <summary>
/// Defines the OwlNet application theme — a clean, minimalist design inspired by
/// modern analytics dashboards with a light sidebar, warm neutral tones, and
/// green/orange/violet accent colors paired with the Inter typeface.
/// <para>
/// Provides fully specified <see cref="PaletteLight"/> and <see cref="PaletteDark"/> palettes
/// so that every visible color is intentional and no MudBlazor defaults leak through.
/// </para>
/// <para>
/// Usage in MainLayout.razor:
/// <code>
/// &lt;MudThemeProvider Theme="@OwlNetTheme.Theme" @bind-IsDarkMode="_isDarkMode" /&gt;
/// </code>
/// </para>
/// </summary>
public static class OwlNetTheme
{
    // -----------------------------------------------------------------------
    //  Brand colors — warm minimalist palette
    //  Inspired by the analytics dashboard reference with green active states,
    //  orange chart accents, and violet badge/icon accents.
    // -----------------------------------------------------------------------

    // Primary — deep forest green (used for active nav items, primary actions)
    private const string PrimaryGreen = "#2D6A4F";
    private const string PrimaryGreenDarken = "#1B4332";
    private const string PrimaryGreenLighten = "#52B788";

    // Dark-mode primary — brighter green for contrast on dark surfaces
    private const string PrimaryGreenDark = "#74C69D";
    private const string PrimaryGreenDarkDarken = "#52B788";
    private const string PrimaryGreenDarkLighten = "#B7E4C7";

    // Secondary — warm orange (used for chart highlights, CTAs, badges)
    private const string SecondaryOrange = "#E76F51";
    private const string SecondaryOrangeDark = "#F4A261";

    // Tertiary — muted violet (used for icon accents, decorative badges)
    private const string TertiaryViolet = "#7C3AED";
    private const string TertiaryVioletDark = "#A78BFA";

    // -----------------------------------------------------------------------
    //  Drawer / sidebar colors — light, clean sidebar
    // -----------------------------------------------------------------------

    /// <summary>Light warm off-white for the sidebar in light mode.</summary>
    private const string DrawerBgLight = "#FAFAF8";

    /// <summary>Dark surface for the sidebar in dark mode.</summary>
    private const string DrawerBgDark = "#1A1A1A";

    // -----------------------------------------------------------------------
    //  Semantic status colors — softer tones for a minimalist feel
    // -----------------------------------------------------------------------

    private const string SuccessLight = "#2D6A4F";
    private const string SuccessDark = "#74C69D";

    private const string WarningLight = "#D97706";
    private const string WarningDark = "#FBBF24";

    private const string ErrorLight = "#D62828";
    private const string ErrorDark = "#EF6351";

    private const string InfoLight = "#457B9D";
    private const string InfoDark = "#81B4D8";

    // -----------------------------------------------------------------------
    //  Shared font stack — Inter loaded via Google Fonts in App.razor
    // -----------------------------------------------------------------------

    private static readonly string[] FontStack = ["Inter", "system-ui", "-apple-system", "Segoe UI", "Helvetica", "Arial", "sans-serif"];

    /// <summary>
    /// The OwlNet custom <see cref="MudTheme"/> instance.
    /// Configured with light and dark palettes, Inter typography, and rounded layout
    /// properties for a clean, minimalist dashboard appearance.
    /// </summary>
    public static MudTheme Theme { get; } = new()
    {
        // -- Light mode palette ------------------------------------------------
        PaletteLight = new PaletteLight
        {
            // Absolute anchors
            Black = "#1A1A1A",
            White = "#FFFFFF",

            // Brand — deep forest green as primary
            Primary = PrimaryGreen,
            PrimaryDarken = PrimaryGreenDarken,
            PrimaryLighten = PrimaryGreenLighten,
            PrimaryContrastText = "#FFFFFF",

            // Secondary — warm orange accent
            Secondary = SecondaryOrange,
            SecondaryDarken = "#C85A3E",
            SecondaryLighten = "#F4A261",
            SecondaryContrastText = "#FFFFFF",

            // Tertiary — muted violet
            Tertiary = TertiaryViolet,
            TertiaryDarken = "#6D28D9",
            TertiaryLighten = "#A78BFA",
            TertiaryContrastText = "#FFFFFF",

            // Semantic status
            Info = InfoLight,
            InfoDarken = "#2C5F7C",
            InfoLighten = "#81B4D8",
            InfoContrastText = "#FFFFFF",

            Success = SuccessLight,
            SuccessDarken = "#1B4332",
            SuccessLighten = "#52B788",
            SuccessContrastText = "#FFFFFF",

            Warning = WarningLight,
            WarningDarken = "#B45309",
            WarningLighten = "#FCD34D",
            WarningContrastText = "#FFFFFF",

            Error = ErrorLight,
            ErrorDarken = "#A61F1F",
            ErrorLighten = "#EF6351",
            ErrorContrastText = "#FFFFFF",

            // Dark variant
            Dark = "#3D3D3D",
            DarkDarken = "#2A2A2A",
            DarkLighten = "#5C5C5C",
            DarkContrastText = "#FFFFFF",

            // Text hierarchy — warm dark tones, not pure black
            TextPrimary = "#1A1A1A",
            TextSecondary = "#6B7280",
            TextDisabled = "#B0B0B0",

            // Interactive element colors
            ActionDefault = "#6B7280",
            ActionDisabled = "#B0B0B0",
            ActionDisabledBackground = "#E5E5E5",

            // Backgrounds — warm off-white base with white surface cards
            Background = "#F5F3F0",
            BackgroundGray = "#EDEAE6",
            Surface = "#FFFFFF",

            // App bar — clean white bar with dark text
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1A1A1A",

            // Navigation drawer — light warm sidebar with dark text
            DrawerBackground = DrawerBgLight,
            DrawerText = "#1A1A1A",
            DrawerIcon = "#6B7280",

            // Lines and dividers — subtle warm gray
            LinesDefault = "#E5E2DE",
            LinesInputs = "#D1CEC9",
            Divider = "#E5E2DE",
            DividerLight = "#F0EDE9",

            // Table
            TableLines = "#E5E2DE",
            TableStriped = "#FAF9F7",
            TableHover = "#F0EDE9",

            // Skeleton loading placeholder
            Skeleton = "#E5E2DE",

            // Overlay
            OverlayDark = "rgba(26,26,26,0.35)",
            OverlayLight = "rgba(255,255,255,0.4)",

            // Gray scale
            GrayDefault = "#9CA3AF",
            GrayLight = "#B0B0B0",
            GrayLighter = "#E5E5E5",
            GrayDark = "#6B7280",
            GrayDarker = "#3D3D3D",

            // Interaction opacity — very subtle for a clean, minimal look
            HoverOpacity = 0.04,
            RippleOpacity = 0.08,
            RippleOpacitySecondary = 0.06,
            BorderOpacity = 0.10,
        },

        // -- Dark mode palette -------------------------------------------------
        PaletteDark = new PaletteDark
        {
            // Absolute anchors
            Black = "#000000",
            White = "#FFFFFF",

            // Brand — brighter green for dark surfaces
            Primary = PrimaryGreenDark,
            PrimaryDarken = PrimaryGreenDarkDarken,
            PrimaryLighten = PrimaryGreenDarkLighten,
            PrimaryContrastText = "#1A1A1A",

            // Secondary — warm orange (brighter variant)
            Secondary = SecondaryOrangeDark,
            SecondaryDarken = "#E76F51",
            SecondaryLighten = "#F7C59F",
            SecondaryContrastText = "#1A1A1A",

            // Tertiary — lighter violet for dark backgrounds
            Tertiary = TertiaryVioletDark,
            TertiaryDarken = "#7C3AED",
            TertiaryLighten = "#C4B5FD",
            TertiaryContrastText = "#1A1A1A",

            // Semantic status
            Info = InfoDark,
            InfoDarken = "#457B9D",
            InfoLighten = "#A8D0E6",
            InfoContrastText = "#1A1A1A",

            Success = SuccessDark,
            SuccessDarken = "#2D6A4F",
            SuccessLighten = "#B7E4C7",
            SuccessContrastText = "#1A1A1A",

            Warning = WarningDark,
            WarningDarken = "#D97706",
            WarningLighten = "#FDE68A",
            WarningContrastText = "#1A1A1A",

            Error = ErrorDark,
            ErrorDarken = "#D62828",
            ErrorLighten = "#F49B8F",
            ErrorContrastText = "#1A1A1A",

            // Dark variant
            Dark = "#E0E0E0",
            DarkDarken = "#B0B0B0",
            DarkLighten = "#F0F0F0",
            DarkContrastText = "#1A1A1A",

            // Text hierarchy — high-contrast for dark surfaces
            TextPrimary = "#E8E6E3",
            TextSecondary = "#9CA3AF",
            TextDisabled = "#555555",

            // Interactive element colors
            ActionDefault = "#9CA3AF",
            ActionDisabled = "#555555",
            ActionDisabledBackground = "#2A2A2A",

            // Backgrounds — warm dark tones
            Background = "#111110",
            BackgroundGray = "#181817",
            Surface = "#1E1E1D",

            // App bar — blends with dark surface
            AppbarBackground = "#1E1E1D",
            AppbarText = "#E8E6E3",

            // Navigation drawer — dark sidebar matching the dark theme
            DrawerBackground = DrawerBgDark,
            DrawerText = "#E8E6E3",
            DrawerIcon = "#9CA3AF",

            // Lines and dividers
            LinesDefault = "#2E2E2D",
            LinesInputs = "#3D3D3C",
            Divider = "#2E2E2D",
            DividerLight = "#252524",

            // Table
            TableLines = "#2E2E2D",
            TableStriped = "#222221",
            TableHover = "#282827",

            // Skeleton loading placeholder
            Skeleton = "#2E2E2D",

            // Overlay
            OverlayDark = "rgba(0,0,0,0.5)",
            OverlayLight = "rgba(255,255,255,0.06)",

            // Gray scale
            GrayDefault = "#6B7280",
            GrayLight = "#555555",
            GrayLighter = "#3D3D3D",
            GrayDark = "#9CA3AF",
            GrayDarker = "#B0B0B0",

            // Interaction opacity
            HoverOpacity = 0.06,
            RippleOpacity = 0.10,
            RippleOpacitySecondary = 0.08,
            BorderOpacity = 0.12,
        },

        // -- Typography --------------------------------------------------------
        // Uses Inter family (loaded via Google Fonts in App.razor).
        // Clean, geometric sans-serif with excellent readability at small sizes.
        // Slightly tighter letter-spacing for a modern, compact feel.
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontStack,
                FontSize = ".875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "-.011em",
            },
            H1 = new H1Typography
            {
                FontFamily = FontStack,
                FontSize = "2.5rem",
                FontWeight = "600",
                LineHeight = "1.2",
                LetterSpacing = "-.025em",
            },
            H2 = new H2Typography
            {
                FontFamily = FontStack,
                FontSize = "2rem",
                FontWeight = "600",
                LineHeight = "1.25",
                LetterSpacing = "-.025em",
            },
            H3 = new H3Typography
            {
                FontFamily = FontStack,
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.3",
                LetterSpacing = "-.02em",
            },
            H4 = new H4Typography
            {
                FontFamily = FontStack,
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.35",
                LetterSpacing = "-.015em",
            },
            H5 = new H5Typography
            {
                FontFamily = FontStack,
                FontSize = "1.05rem",
                FontWeight = "500",
                LineHeight = "1.4",
                LetterSpacing = "-.01em",
            },
            H6 = new H6Typography
            {
                FontFamily = FontStack,
                FontSize = ".95rem",
                FontWeight = "500",
                LineHeight = "1.5",
                LetterSpacing = "-.005em",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = FontStack,
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.65",
                LetterSpacing = "-.011em",
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = FontStack,
                FontSize = ".875rem",
                FontWeight = "500",
                LineHeight = "1.57",
                LetterSpacing = "-.006em",
            },
            Body1 = new Body1Typography
            {
                FontFamily = FontStack,
                FontSize = ".9375rem",
                FontWeight = "400",
                LineHeight = "1.6",
                LetterSpacing = "-.011em",
            },
            Body2 = new Body2Typography
            {
                FontFamily = FontStack,
                FontSize = ".8125rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "-.006em",
            },
            Button = new ButtonTypography
            {
                FontFamily = FontStack,
                FontSize = ".8125rem",
                FontWeight = "500",
                LineHeight = "1.75",
                LetterSpacing = "-.011em",
                TextTransform = "none",
            },
            Caption = new CaptionTypography
            {
                FontFamily = FontStack,
                FontSize = ".75rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "0",
            },
            Overline = new OverlineTypography
            {
                FontFamily = FontStack,
                FontSize = ".6875rem",
                FontWeight = "500",
                LineHeight = "2.4",
                LetterSpacing = ".06em",
                TextTransform = "uppercase",
            },
        },

        // -- Layout properties -------------------------------------------------
        // Moderate border radius (12px) for a clean but friendly appearance.
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
        },
    };
}
