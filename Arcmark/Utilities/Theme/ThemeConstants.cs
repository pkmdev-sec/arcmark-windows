namespace Arcmark.Utilities.Theme;

using System;
using System.Windows;
using System.Windows.Media;

public static class ThemeConstants
{
    public static class Colors
    {
        // #141414 - Primary dark color
        public static readonly Color DarkGray = Color.FromRgb(20, 20, 20);
        public static readonly SolidColorBrush DarkGrayBrush;

        public static readonly Color White = System.Windows.Media.Colors.White;
        public static readonly SolidColorBrush WhiteBrush;

        // #E5E7EB - Settings background
        public static readonly Color SettingsBackground = Color.FromRgb(229, 231, 235);
        public static readonly SolidColorBrush SettingsBackgroundBrush;

        static Colors()
        {
            DarkGrayBrush = new SolidColorBrush(DarkGray);
            DarkGrayBrush.Freeze();

            WhiteBrush = new SolidColorBrush(White);
            WhiteBrush.Freeze();

            SettingsBackgroundBrush = new SolidColorBrush(SettingsBackground);
            SettingsBackgroundBrush.Freeze();
        }
    }

    public static class Opacity
    {
        public const double Full        = 1.0;
        public const double High        = 0.8;
        public const double Medium      = 0.6;
        public const double Low         = 0.4;
        public const double Subtle      = 0.15;
        public const double ExtraSubtle = 0.10;
        public const double Minimal     = 0.06;
    }

    public static class Fonts
    {
        public const double BodySize   = 14;
        public const string FontFamily = "Segoe UI"; // Windows system font

        public static readonly FontWeight Regular  = FontWeights.Regular;
        public static readonly FontWeight SemiBold = FontWeights.SemiBold;
        public static readonly FontWeight Medium   = FontWeights.Medium;
        public static readonly FontWeight Bold     = FontWeights.Bold;
    }

    public static class Spacing
    {
        public const double Tiny       = 4;
        public const double Small      = 6;
        public const double Medium     = 8;
        public const double Regular    = 10;
        public const double Large      = 14;
        public const double ExtraLarge = 16;
        public const double Huge       = 20;
    }

    public static class CornerRadius
    {
        public static readonly System.Windows.CornerRadius Small  = new(6);
        public static readonly System.Windows.CornerRadius Medium = new(8);
        public static readonly System.Windows.CornerRadius Large  = new(12);
    }

    public static class Sizing
    {
        public const double IconSmall          = 14;
        public const double IconMedium         = 18;
        public const double IconLarge          = 22;
        public const double IconExtraLarge     = 26;
        public const double ButtonHeight       = 32;
        public const double RowHeight          = 44;
        public const double PinnedTileHeight   = 50;
        public const double ScrollShadowHeight = 32;
        public const double ScrollShadowWidth  = 32;
        public const int    PinnedTileColumns  = 4;
        public const int    PinnedTileMaxRows  = 3;
    }

    public static class Animation
    {
        public static readonly Duration DurationFast   = new(TimeSpan.FromSeconds(0.15));
        public static readonly Duration DurationNormal = new(TimeSpan.FromSeconds(0.2));
        public static readonly Duration DurationSlow   = new(TimeSpan.FromSeconds(0.3));
    }
}
