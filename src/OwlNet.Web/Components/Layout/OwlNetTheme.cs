using MudBlazor;

namespace OwlNet.Web.Components.Layout;

/// <summary>
/// Defines the OwlNet application theme — a modern violet/indigo Material Design theme
/// inspired by project management dashboard aesthetics with a deep violet sidebar,
/// rounded cards, and the Poppins typeface.
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
    //  Brand colors — single source of truth for the violet/indigo identity
    // -----------------------------------------------------------------------

    /// <summary>Vibrant violet primary used in light mode — strong brand presence on light surfaces.</summary>
    private const string PrimaryViolet = "#6C3FC5";

    /// <summary>Darker violet for hover/active states in light mode.</summary>
    private const string PrimaryVioletDarken = "#5A2DB5";

    /// <summary>Lighter violet for subtle highlights in light mode.</summary>
    private const string PrimaryVioletLighten = "#8B6AD0";

    /// <summary>Brighter, desaturated violet for dark mode — maintains contrast on dark surfaces.</summary>
    private const string PrimaryVioletDark = "#B39DDB";

    /// <summary>Darker variant for hover states in dark mode.</summary>
    private const string PrimaryVioletDarkDarken = "#9575CD";

    /// <summary>Lighter variant for highlights in dark mode.</summary>
    private const string PrimaryVioletDarkLighten = "#D1C4E9";

    /// <summary>Teal accent — complements violet as a secondary color across both modes.</summary>
    private const string SecondaryTeal = "#00897B";

    /// <summary>Brighter teal for dark mode to maintain readability.</summary>
    private const string SecondaryTealDark = "#4DB6AC";

    // -----------------------------------------------------------------------
    //  Drawer / sidebar colors — deep violet sidebar for the dashboard look
    // -----------------------------------------------------------------------

    /// <summary>Deep violet background for the navigation drawer in light mode.</summary>
    private const string DrawerBgLight = "#2D1B69";

    /// <summary>Slightly lighter deep violet for the drawer in dark mode.</summary>
    private const string DrawerBgDark = "#1E1245";

    // -----------------------------------------------------------------------
    //  Semantic status colors — Material Design standard, tuned for violet harmony
    // -----------------------------------------------------------------------

    private const string SuccessLight = "#43A047";
    private const string SuccessDark = "#66BB6A";

    private const string WarningLight = "#FB8C00";
    private const string WarningDark = "#FFA726";

    private const string ErrorLight = "#E53935";
    private const string ErrorDark = "#EF5350";

    private const string InfoLight = "#1E88E5";
    private const string InfoDark = "#42A5F5";

    // -----------------------------------------------------------------------
    //  Shared font stack — Poppins loaded via Google Fonts in App.razor
    // -----------------------------------------------------------------------

    private static readonly string[] FontStack = ["Poppins", "Helvetica", "Arial", "sans-serif"];

    /// <summary>
    /// The OwlNet custom <see cref="MudTheme"/> instance.
    /// Configured with light and dark palettes, Poppins typography, and rounded layout
    /// properties for a clean, modern project management dashboard appearance.
    /// </summary>
    public static MudTheme Theme { get; } = new()
    {
        // -- Light mode palette ------------------------------------------------
        PaletteLight = new PaletteLight
        {
            // Absolute black/white anchors
            Black = "#212121",
            White = "#FFFFFF",

            // Brand — vibrant violet shifted to match the dashboard reference
            Primary = PrimaryViolet,
            PrimaryDarken = PrimaryVioletDarken,
            PrimaryLighten = PrimaryVioletLighten,
            PrimaryContrastText = "#FFFFFF",

            Secondary = SecondaryTeal,
            SecondaryDarken = "#00695C",
            SecondaryLighten = "#26A69A",
            SecondaryContrastText = "#FFFFFF",

            Tertiary = "#7C4DFF",
            TertiaryDarken = "#651FFF",
            TertiaryLighten = "#B388FF",
            TertiaryContrastText = "#FFFFFF",

            // Semantic status
            Info = InfoLight,
            InfoDarken = "#1565C0",
            InfoLighten = "#42A5F5",
            InfoContrastText = "#FFFFFF",

            Success = SuccessLight,
            SuccessDarken = "#2E7D32",
            SuccessLighten = "#66BB6A",
            SuccessContrastText = "#FFFFFF",

            Warning = WarningLight,
            WarningDarken = "#EF6C00",
            WarningLighten = "#FFA726",
            WarningContrastText = "#FFFFFF",

            Error = ErrorLight,
            ErrorDarken = "#C62828",
            ErrorLighten = "#EF5350",
            ErrorContrastText = "#FFFFFF",

            // Dark color (used for MudChip Dark variant, etc.)
            Dark = "#424242",
            DarkDarken = "#303030",
            DarkLighten = "#616161",
            DarkContrastText = "#FFFFFF",

            // Text hierarchy
            TextPrimary = "#212121",
            TextSecondary = "#757575",
            TextDisabled = "#BDBDBD",

            // Interactive element colors
            ActionDefault = "#616161",
            ActionDisabled = "#BDBDBD",
            ActionDisabledBackground = "#E0E0E0",

            // Backgrounds — blue-tinted light gray base gives a modern, airy feel
            // while white surface cards feel elevated against it
            Background = "#F4F6FA",
            BackgroundGray = "#EEF0F5",
            Surface = "#FFFFFF",

            // App bar — clean white bar with dark text for a minimal header
            AppbarBackground = "#FFFFFF",
            AppbarText = "#424242",

            // Navigation drawer — deep violet sidebar for the dashboard look;
            // light text and icons ensure readability on the dark background
            DrawerBackground = DrawerBgLight,
            DrawerText = "#E0E0E0",
            DrawerIcon = "#B0B0B0",

            // Lines and dividers
            LinesDefault = "#E0E0E0",
            LinesInputs = "#BDBDBD",
            Divider = "#E0E0E0",
            DividerLight = "#EEEEEE",

            // Table
            TableLines = "#E0E0E0",
            TableStriped = "#F5F5F5",
            TableHover = "#EEEEEE",

            // Skeleton loading placeholder
            Skeleton = "#E0E0E0",

            // Overlay — semi-transparent backdrop for dialogs
            OverlayDark = "rgba(33,33,33,0.4)",
            OverlayLight = "rgba(255,255,255,0.4)",

            // Gray scale (used internally by MudBlazor for contrast calculations)
            GrayDefault = "#9E9E9E",
            GrayLight = "#BDBDBD",
            GrayLighter = "#E0E0E0",
            GrayDark = "#616161",
            GrayDarker = "#424242",

            // Interaction opacity — slightly reduced for a subtler hover/ripple effect
            HoverOpacity = 0.06,
            RippleOpacity = 0.1,
            RippleOpacitySecondary = 0.08,
            BorderOpacity = 0.12,
        },

        // -- Dark mode palette -------------------------------------------------
        PaletteDark = new PaletteDark
        {
            // Absolute black/white anchors
            Black = "#000000",
            White = "#FFFFFF",

            // Brand — brighter violet to pop against dark surfaces
            Primary = PrimaryVioletDark,
            PrimaryDarken = PrimaryVioletDarkDarken,
            PrimaryLighten = PrimaryVioletDarkLighten,
            PrimaryContrastText = "#1E1E1E",

            Secondary = SecondaryTealDark,
            SecondaryDarken = "#00897B",
            SecondaryLighten = "#80CBC4",
            SecondaryContrastText = "#1E1E1E",

            Tertiary = "#B388FF",
            TertiaryDarken = "#7C4DFF",
            TertiaryLighten = "#D1C4E9",
            TertiaryContrastText = "#1E1E1E",

            // Semantic status — slightly brighter for dark background readability
            Info = InfoDark,
            InfoDarken = "#1E88E5",
            InfoLighten = "#64B5F6",
            InfoContrastText = "#1E1E1E",

            Success = SuccessDark,
            SuccessDarken = "#43A047",
            SuccessLighten = "#81C784",
            SuccessContrastText = "#1E1E1E",

            Warning = WarningDark,
            WarningDarken = "#FB8C00",
            WarningLighten = "#FFB74D",
            WarningContrastText = "#1E1E1E",

            Error = ErrorDark,
            ErrorDarken = "#E53935",
            ErrorLighten = "#E57373",
            ErrorContrastText = "#1E1E1E",

            // Dark color
            Dark = "#E0E0E0",
            DarkDarken = "#BDBDBD",
            DarkLighten = "#EEEEEE",
            DarkContrastText = "#1E1E1E",

            // Text hierarchy — high-contrast white text on dark surfaces
            TextPrimary = "#E0E0E0",
            TextSecondary = "#9E9E9E",
            TextDisabled = "#616161",

            // Interactive element colors
            ActionDefault = "#B0B0B0",
            ActionDisabled = "#616161",
            ActionDisabledBackground = "#333333",

            // Backgrounds — true dark (#121212) per Material Design dark theme spec
            Background = "#121212",
            BackgroundGray = "#181818",
            Surface = "#1E1E1E",

            // App bar — blends with the dark surface for a seamless look
            AppbarBackground = "#1E1E1E",
            AppbarText = "#E0E0E0",

            // Navigation drawer — slightly lighter deep violet to maintain the
            // branded sidebar feel while fitting the dark mode context
            DrawerBackground = DrawerBgDark,
            DrawerText = "#E0E0E0",
            DrawerIcon = "#B0B0B0",

            // Lines and dividers — subtle so they don't overpower the dark UI
            LinesDefault = "#333333",
            LinesInputs = "#424242",
            Divider = "#333333",
            DividerLight = "#2A2A2A",

            // Table
            TableLines = "#333333",
            TableStriped = "#242424",
            TableHover = "#2A2A2A",

            // Skeleton loading placeholder
            Skeleton = "#333333",

            // Overlay
            OverlayDark = "rgba(0,0,0,0.5)",
            OverlayLight = "rgba(255,255,255,0.06)",

            // Gray scale (inverted relative to light mode for dark backgrounds)
            GrayDefault = "#757575",
            GrayLight = "#616161",
            GrayLighter = "#424242",
            GrayDark = "#9E9E9E",
            GrayDarker = "#B0B0B0",

            // Interaction opacity — slightly higher on dark backgrounds for visibility
            HoverOpacity = 0.08,
            RippleOpacity = 0.12,
            RippleOpacitySecondary = 0.10,
            BorderOpacity = 0.15,
        },

        // -- Typography --------------------------------------------------------
        // Uses the Poppins family (loaded via Google Fonts in App.razor).
        // Headings use semibold/medium weights for a clean dashboard hierarchy.
        // Buttons use sentence-case (TextTransform "none") per the design reference.
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontStack,
                FontSize = ".875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = ".00938em",
            },
            H1 = new H1Typography
            {
                FontFamily = FontStack,
                FontSize = "3rem",
                FontWeight = "300",
                LineHeight = "1.167",
                LetterSpacing = "-.01562em",
            },
            H2 = new H2Typography
            {
                FontFamily = FontStack,
                FontSize = "2.125rem",
                FontWeight = "300",
                LineHeight = "1.2",
                LetterSpacing = "-.00833em",
            },
            // H3 — semibold for page titles like "Projects", "Dashboard"
            H3 = new H3Typography
            {
                FontFamily = FontStack,
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.267",
                LetterSpacing = "0",
            },
            // H4 — semibold for section headings and card titles
            H4 = new H4Typography
            {
                FontFamily = FontStack,
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.35",
                LetterSpacing = ".00735em",
            },
            // H5 — medium weight for sub-section headings
            H5 = new H5Typography
            {
                FontFamily = FontStack,
                FontSize = "1.1rem",
                FontWeight = "500",
                LineHeight = "1.4",
                LetterSpacing = "0",
            },
            // H6 is used in the AppBar title — weight 500 for a clean, medium-emphasis look
            H6 = new H6Typography
            {
                FontFamily = FontStack,
                FontSize = "1rem",
                FontWeight = "500",
                LineHeight = "1.5",
                LetterSpacing = ".0125em",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = FontStack,
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.75",
                LetterSpacing = ".00938em",
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = FontStack,
                FontSize = ".875rem",
                FontWeight = "500",
                LineHeight = "1.57",
                LetterSpacing = ".00714em",
            },
            Body1 = new Body1Typography
            {
                FontFamily = FontStack,
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = ".00938em",
            },
            Body2 = new Body2Typography
            {
                FontFamily = FontStack,
                FontSize = ".875rem",
                FontWeight = "400",
                LineHeight = "1.43",
                LetterSpacing = ".01071em",
            },
            // Button — semibold, sentence-case (no uppercase) per the design reference
            Button = new ButtonTypography
            {
                FontFamily = FontStack,
                FontSize = ".875rem",
                FontWeight = "600",
                LineHeight = "1.75",
                LetterSpacing = ".02857em",
                TextTransform = "none",
            },
            Caption = new CaptionTypography
            {
                FontFamily = FontStack,
                FontSize = ".75rem",
                FontWeight = "400",
                LineHeight = "1.66",
                LetterSpacing = ".03333em",
            },
            Overline = new OverlineTypography
            {
                FontFamily = FontStack,
                FontSize = ".75rem",
                FontWeight = "400",
                LineHeight = "2.66",
                LetterSpacing = ".08333em",
                TextTransform = "uppercase",
            },
        },

        // -- Layout properties -------------------------------------------------
        // Increased border radius (8px -> 14px) for a softer, more modern
        // card/button appearance matching the rounded dashboard design reference.
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px",
        },
    };
}
