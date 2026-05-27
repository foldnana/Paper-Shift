using UnityEngine;

namespace PaperShift.Presenter
{
    public static class PaperShiftTheme
    {
        public const float ReferenceWidth = 565f;
        public const float ReferenceHeight = 980f;

        public static readonly Color Sky = Hex("#93d3f8");
        public static readonly Color PageBlue = Hex("#93d3f8");
        public static readonly Color PagePurple = Hex("#d9bbff");
        public static readonly Color Paper = Hex("#f7fcff");
        public static readonly Color White = Hex("#ffffff");
        public static readonly Color Ink = Hex("#303238");
        public static readonly Color MutedInk = Hex("#50545b");
        public static readonly Color Blue = Hex("#249ee8");
        public static readonly Color BlueLight = Hex("#8fd0f5");
        public static readonly Color BlueTicket = Hex("#75c8f3");
        public static readonly Color Purple = Hex("#8c22ee");
        public static readonly Color PurpleTicket = Hex("#d7b8ff");
        public static readonly Color Gold = Hex("#fed54a");
        public static readonly Color Green = Hex("#16b86f");
        public static readonly Color Pink = Hex("#ff6aa0");
        public static readonly Color Orange = Hex("#ff8a00");
        public static readonly Color Red = Hex("#dd315b");
        public static readonly Color GrayButton = Hex("#c8c8c8");
        public static readonly Color Slot = Hex("#e9eef2");
        public static readonly Color Line = Hex("#eef3f6");

        public static Color Hex(string html, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(html, out var color))
            {
                color = Color.white;
            }

            color.a = alpha;
            return color;
        }
    }
}
