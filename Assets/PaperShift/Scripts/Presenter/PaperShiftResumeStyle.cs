using PaperShift.Domain;
using UnityEngine;

namespace PaperShift.Presenter
{
    public static class PaperShiftResumeStyle
    {
        public static readonly Color Actual = PaperShiftTheme.BlueLight;
        public static readonly Color ActualBorder = PaperShiftTheme.Blue;
        public static readonly Color ActualSelected = PaperShiftTheme.Hex("#0b78b6");
        public static readonly Color ActualSelectedBorder = PaperShiftTheme.Hex("#075f91");

        public static readonly Color Conservative = PaperShiftTheme.Hex("#dff6e8");
        public static readonly Color ConservativeBorder = PaperShiftTheme.Green;
        public static readonly Color ConservativeText = PaperShiftTheme.Hex("#17663a");

        public static readonly Color Exaggerated = PaperShiftTheme.Hex("#ffd6dc");
        public static readonly Color ExaggeratedBorder = PaperShiftTheme.Red;
        public static readonly Color ExaggeratedText = PaperShiftTheme.Hex("#8c1f36");

        public static readonly Color Fake = PaperShiftTheme.Hex("#f3a6b5");
        public static readonly Color FakeBorder = PaperShiftTheme.Hex("#9f1736");
        public static readonly Color FakeText = PaperShiftTheme.Hex("#5b0f20");

        public static readonly Color Default = PaperShiftTheme.White;
        public static readonly Color DefaultBorder = PaperShiftTheme.Hex("#d7e0e6");
        public static readonly Color DefaultText = PaperShiftTheme.Hex("#58626c");
        public static readonly Color SelectedText = Color.white;

        public static ResumeOptionPalette PaletteByComparison(int optionIndex, int actualIndex, bool selected)
        {
            if (!selected)
            {
                return optionIndex == actualIndex
                    ? new ResumeOptionPalette(Actual, ActualBorder, SelectedText)
                    : new ResumeOptionPalette(Default, DefaultBorder, DefaultText);
            }

            var delta = optionIndex - actualIndex;
            if (delta == 0)
            {
                return new ResumeOptionPalette(ActualSelected, ActualSelectedBorder, SelectedText);
            }

            if (delta < 0)
            {
                return new ResumeOptionPalette(ConservativeBorder, ConservativeBorder, SelectedText);
            }

            return delta >= 2
                ? new ResumeOptionPalette(FakeBorder, FakeBorder, SelectedText)
                : new ResumeOptionPalette(ExaggeratedBorder, ExaggeratedBorder, SelectedText);
        }

        public static ResumeOptionPalette Palette(ResumePackagingMode mode, bool selected)
        {
            if (mode == ResumePackagingMode.Normal)
            {
                return selected
                    ? new ResumeOptionPalette(ActualSelected, ActualSelectedBorder, SelectedText)
                    : new ResumeOptionPalette(Actual, ActualBorder, SelectedText);
            }

            if (mode == ResumePackagingMode.Hide)
            {
                return selected
                    ? new ResumeOptionPalette(ConservativeBorder, ConservativeBorder, SelectedText)
                    : new ResumeOptionPalette(Conservative, ConservativeBorder, ConservativeText);
            }

            if (mode == ResumePackagingMode.Exaggerate)
            {
                return selected
                    ? new ResumeOptionPalette(ExaggeratedBorder, ExaggeratedBorder, SelectedText)
                    : new ResumeOptionPalette(Exaggerated, ExaggeratedBorder, ExaggeratedText);
            }

            return selected
                ? new ResumeOptionPalette(FakeBorder, FakeBorder, SelectedText)
                : new ResumeOptionPalette(Fake, FakeBorder, FakeText);
        }
    }

    public readonly struct ResumeOptionPalette
    {
        public readonly Color Background;
        public readonly Color Border;
        public readonly Color Text;

        public ResumeOptionPalette(Color background, Color border, Color text)
        {
            Background = background;
            Border = border;
            Text = text;
        }
    }
}
