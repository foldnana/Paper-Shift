using PaperShift.Domain;
using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;
using static PaperShift.Presenter.PaperShiftUiFormatter;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftResumeScreenBinder : PaperShiftScreenBinderBase
    {
        public Text CoinText;
        public Text HeaderNameText;
        public Text GenerationText;
        public Text RiskText;
        public PaperShiftSelectableTextBinding[] IntentButtons = new PaperShiftSelectableTextBinding[0];
        public PaperShiftResumeLineBinding[] ResumeLines = new PaperShiftResumeLineBinding[0];
        public Transform TagPoolRoot;
        public Button SendResumeButton;

        private readonly PaperShiftResumeTagListView resumeTagListView = new PaperShiftResumeTagListView();

        private void Reset()
        {
            Screen = PaperShiftScreen.Resume;
        }

        public override void BindActions()
        {
            Bind(SendResumeButton, () =>
            {
                Presenter.FindInterviewAndShow();
                RefreshAll();
            });
        }

        public override void RefreshView()
        {
            if (State == null)
            {
                return;
            }

            Set(CoinText, State.Worker.Money.ToString("N0"));
            Set(HeaderNameText, State.Worker.LastName + " " + State.Worker.FirstName + "\n<size=18>" + State.Worker.Gender + " " + State.Worker.Age + " 岁 " + State.Worker.EraName + "</size>");
            Set(GenerationText, "第" + State.Generation + "代");
            Set(RiskText, "<size=40>" + State.Resume.DeceptionRisk + "%</size>  <size=13>识破风险</size>");
            RefreshIntentButtons();
            RefreshResumeLines();
            RefreshTagPool();
        }

        private void RefreshIntentButtons()
        {
            for (var i = 0; i < IntentButtons.Length; i++)
            {
                var binding = IntentButtons[i];
                if (binding == null || string.IsNullOrEmpty(binding.Id))
                {
                    continue;
                }

                var active = State.Resume.IntentTagIds.Contains(binding.Id);
                Set(binding.Label, IntentLabel(binding.Id));
                if (binding.Background != null)
                {
                    binding.Background.color = active ? PaperShiftTheme.BlueLight : PaperShiftTheme.Hex("#f4f8fb");
                }

                if (binding.Outline != null)
                {
                    binding.Outline.effectColor = active ? PaperShiftTheme.Blue : PaperShiftTheme.Hex("#d7e0e6");
                }

                var intentId = binding.Id;
                Bind(binding.Button, () =>
                {
                    Presenter.ToggleResumeIntent(intentId);
                    RefreshView();
                });
            }
        }

        private void RefreshResumeLines()
        {
            for (var lineIndex = 0; lineIndex < ResumeLines.Length; lineIndex++)
            {
                var line = ResumeLines[lineIndex];
                if (line == null || string.IsNullOrEmpty(line.FieldId))
                {
                    continue;
                }

                var choice = State.Resume.GetOrCreateChoice(line.FieldId);
                Set(line.Value, ResumeFieldValue(line.FieldId));
                var actualIndex = ActualResumeOptionIndex(line.FieldId, line.Options == null ? 0 : line.Options.Length);
                for (var optionIndex = 0; line.Options != null && optionIndex < line.Options.Length; optionIndex++)
                {
                    var option = line.Options[optionIndex];
                    if (option == null)
                    {
                        continue;
                    }

                    var selectedIndex = optionIndex;
                    var selectedMode = ResumeModeFromComparison(optionIndex, actualIndex);
                    ApplyResumeChipStyle(option, optionIndex, actualIndex, choice.OptionIndex == optionIndex);
                    Bind(option.Button, () =>
                    {
                        Presenter.SetResumePackaging(line.FieldId, selectedMode, selectedIndex);
                        RefreshView();
                    });
                }
            }
        }

        private void RefreshTagPool()
        {
            if (TagPoolRoot == null)
            {
                return;
            }

            resumeTagListView.TagPrefab = Host == null ? null : Host.ResumeTagPrefab;
            resumeTagListView.Refresh(TagPoolRoot, Presenter, RefreshView, () => ShowBanner("最多只能隐藏 3 个标签"));
        }

        private string ResumeFieldValue(string fieldId)
        {
            switch (fieldId)
            {
                case "education":
                    return EducationLabel(State.Worker);
                case "experience":
                    return ResumeExperienceLabel();
                case "ability":
                    return "逻辑" + State.Worker.GetStat("logic") + " / 社交" + State.Worker.GetStat("social");
                case "tags":
                    return WorkerTagSummary(State.Worker);
                case "salary":
                    return ExpectedSalaryLabel();
                default:
                    return string.Empty;
            }
        }

        private int ActualResumeOptionIndex(string fieldId, int optionCount)
        {
            if (optionCount <= 0)
            {
                return 0;
            }

            int score;
            switch (fieldId)
            {
                case "education":
                    score = State.Worker.GetStat("education");
                    break;
                case "experience":
                    score = Mathf.RoundToInt((State.Worker.GetStat("logic") + State.Worker.GetStat("social") + State.Worker.GetStat("body")) / 3f);
                    break;
                case "ability":
                    score = Mathf.RoundToInt((State.Worker.GetStat("logic") + State.Worker.GetStat("social")) / 2f);
                    break;
                case "tags":
                    score = Mathf.Clamp(State.Worker.Tags.Count * 22, 0, 100);
                    break;
                case "salary":
                    score = Mathf.RoundToInt((State.Worker.GetStat("logic") + State.Worker.GetStat("social") + State.Worker.GetStat("education")) / 3f);
                    break;
                default:
                    score = 50;
                    break;
            }

            return Mathf.Clamp(score * optionCount / 101, 0, optionCount - 1);
        }

        private static ResumePackagingMode ResumeModeFromComparison(int optionIndex, int actualIndex)
        {
            var delta = optionIndex - actualIndex;
            if (delta == 0)
            {
                return ResumePackagingMode.Normal;
            }

            if (delta < 0)
            {
                return ResumePackagingMode.Hide;
            }

            return delta >= 2 ? ResumePackagingMode.Fake : ResumePackagingMode.Exaggerate;
        }

        private static void ApplyResumeChipStyle(PaperShiftResumeOptionBinding option, int optionIndex, int actualIndex, bool selected)
        {
            var palette = PaperShiftResumeStyle.PaletteByComparison(optionIndex, actualIndex, selected);
            if (option.Background != null)
            {
                option.Background.color = palette.Background;
            }

            if (option.Outline != null)
            {
                option.Outline.effectColor = palette.Border;
            }

            if (option.Label != null)
            {
                option.Label.color = palette.Text;
            }
        }
    }
}
