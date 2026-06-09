using System.Collections.Generic;
using PaperShift.Domain;
using PaperShift.Model;
using PaperShift.Runtime;
using UnityEngine;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftInheritanceScreenBinder : PaperShiftScreenBinderBase
    {
        public PaperShiftInheritanceViewReferences View;

        private int selectedHeirIndex;

        private void Reset()
        {
            Screen = PaperShiftScreen.Inheritance;
            View = GetComponent<PaperShiftInheritanceViewReferences>();
        }

        public override void BindActions()
        {
            ResolveView();
            if (View != null && View.HeirCards != null)
            {
                for (var i = 0; i < View.HeirCards.Length; i++)
                {
                    var index = i;
                    var card = View.HeirCards[i];
                    if (card != null)
                    {
                        Bind(card.Button, () =>
                        {
                            selectedHeirIndex = index;
                            RefreshView();
                        });
                    }
                }
            }

            Bind(View == null ? null : View.ContinueButton, () =>
            {
                Presenter.StartNextGeneration(selectedHeirIndex);
                RefreshAll();
            });
        }

        public override void RefreshView()
        {
            if (State == null || View == null)
            {
                ResolveView();
            }

            if (State == null || View == null)
            {
                return;
            }

            var later = State.LaterLife;
            var totalScore = later != null && later.TotalScore > 0
                ? later.TotalScore
                : HireSettlementScoreCalculator.Calculate(State, Database).TotalPoints;
            Set(View.CoinText, totalScore.ToString("N0"));
            RefreshSummary();
            RefreshHeirs();
        }

        private void RefreshSummary()
        {
            var later = State.LaterLife;
            var jobTitle = string.IsNullOrEmpty(State.Retirement.FinalJobTitle)
                ? State.CurrentJob.JobTitle
                : State.Retirement.FinalJobTitle;
            if (string.IsNullOrEmpty(jobTitle))
            {
                jobTitle = "这份工作";
            }

            Set(View.SummaryTexts, "job", later == null || string.IsNullOrEmpty(later.FinalCareer) ? jobTitle : later.FinalCareer);
            Set(View.SummaryTexts, "years", later == null || later.WorkYears <= 0 ? "-" : later.WorkYears + "年");
            Set(View.SummaryTexts, "pressure", later == null || string.IsNullOrEmpty(later.PressureLabel) ? "-" : later.PressureLabel);
            Set(View.SummaryTexts, "prospect", later == null || string.IsNullOrEmpty(later.ProspectLabel) ? "-" : later.ProspectLabel);
            Set(View.SummaryTexts, "story", later == null || string.IsNullOrEmpty(later.StoryText) ? "后来的人生还没有被推演。" : later.StoryText);
            RefreshMilestones();
        }

        private void RefreshMilestones()
        {
            for (var i = 0; i < 6; i++)
            {
                var milestone = LaterLifeMilestoneAt(i);
                SetMilestoneSlot(i, milestone);
                SetNumberedMilestone(i, milestone);
            }

            SetLegacyMilestone("marriage", 0);
            SetLegacyMilestone("house", 1);
            SetLegacyMilestone("child", 2);
        }

        private void SetLegacyMilestone(string legacyIdPrefix, int index)
        {
            var milestone = LaterLifeMilestoneAt(index);
            Set(View.SummaryTexts, legacyIdPrefix + "Age", milestone == null ? "-" : milestone.Age + "岁");
            Set(View.SummaryTexts, legacyIdPrefix + "Text", milestone == null ? "未发生" : MilestoneText(milestone));
        }

        private void SetMilestoneSlot(int index, LaterLifeMilestone milestone)
        {
            if (View.Milestones == null || index < 0 || index >= View.Milestones.Length || View.Milestones[index] == null)
            {
                return;
            }

            View.Milestones[index].Set(milestone == null ? "-" : milestone.Age + "岁", milestone == null ? string.Empty : MilestoneText(milestone));
        }

        private void SetNumberedMilestone(int index, LaterLifeMilestone milestone)
        {
            var id = "milestone" + (index + 1);
            Set(View.SummaryTexts, id + "Age", milestone == null ? "-" : milestone.Age + "岁");
            Set(View.SummaryTexts, id + "Text", milestone == null ? string.Empty : MilestoneText(milestone));
        }

        private static string MilestoneText(LaterLifeMilestone milestone)
        {
            if (milestone == null)
            {
                return string.Empty;
            }

            return CompactMilestoneLine(milestone.Title, 4) + "\n" + CompactMilestoneLine(milestone.Body, 8);
        }

        private static string CompactMilestoneLine(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var normalized = value.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        private LaterLifeMilestone LaterLifeMilestoneAt(int index)
        {
            var later = State == null ? null : State.LaterLife;
            if (later == null || later.Milestones == null || index < 0 || index >= later.Milestones.Count)
            {
                return null;
            }

            return later.Milestones[index];
        }

        private void RefreshHeirs()
        {
            var heirs = State.Worker.Heirs;
            if (selectedHeirIndex < 0 || selectedHeirIndex >= heirs.Count)
            {
                selectedHeirIndex = 0;
            }

            if (View.HeirCards != null)
            {
                for (var i = 0; i < View.HeirCards.Length; i++)
                {
                    var card = View.HeirCards[i];
                    if (card == null)
                    {
                        continue;
                    }

                    var hasHeir = i < heirs.Count;
                    card.gameObject.SetActive(hasHeir);
                    if (!hasHeir)
                    {
                        continue;
                    }

                    var heir = heirs[i];
                    Set(card.NameText, ShortName(heir.Name));
                    Set(card.MetaText, DisplayHeirAge(heir, i) + "岁");
                    if (card.SelectedState != null)
                    {
                        card.SelectedState.SetActive(i == selectedHeirIndex);
                    }
                }
            }

            if (heirs.Count == 0)
            {
                Set(View.SelectedHeirTexts, "name", "暂无");
                Set(View.SelectedHeirTexts, "gender", "-");
                Set(View.SelectedHeirTexts, "age", "-");
                Set(View.SelectedHeirTexts, "family", "-");
                Set(View.SelectedHeirTexts, "education", "-");
                Set(View.SelectedHeirTexts, "appearance", "-");
                Set(View.SelectedHeirTexts, "ability", "-");
                Set(View.SelectedHeirTexts, "height", "-");
                Set(View.SelectedHeirTexts, "personality", "-");
                Set(View.SelectedHeirTexts, "income", "0元");
                SetInitialTags(new[] { "没有后代", "游戏结束" });
                if (View.ContinueButton != null)
                {
                    View.ContinueButton.interactable = false;
                }
                return;
            }

            if (View.ContinueButton != null)
            {
                View.ContinueButton.interactable = true;
            }

            var selected = heirs[selectedHeirIndex];
            Set(View.SelectedHeirTexts, "name", selected.Name);
            Set(View.SelectedHeirTexts, "gender", selected.Gender);
            Set(View.SelectedHeirTexts, "age", DisplayHeirAge(selected, selectedHeirIndex) + "岁");
            Set(View.SelectedHeirTexts, "family", PaperShiftWorkerAttributes.FamilyLabel(HeirStat(selected, PaperShiftWorkerAttributes.Family, State.Worker.GetStat(PaperShiftWorkerAttributes.Family))));
            Set(View.SelectedHeirTexts, "education", PaperShiftWorkerAttributes.EducationLabel(HeirStat(selected, PaperShiftWorkerAttributes.Education, State.Worker.GetStat(PaperShiftWorkerAttributes.Education))));
            Set(View.SelectedHeirTexts, "appearance", PaperShiftWorkerAttributes.DisplayValue(BuildPreviewWorker(selected, selectedHeirIndex), PaperShiftWorkerAttributes.Appearance));
            Set(View.SelectedHeirTexts, "ability", PaperShiftWorkerAttributes.AbilityLabel(HeirStat(selected, PaperShiftWorkerAttributes.Ability, State.Worker.GetStat(PaperShiftWorkerAttributes.Ability))));
            Set(View.SelectedHeirTexts, "height", PaperShiftWorkerAttributes.DisplayValue(BuildPreviewWorker(selected, selectedHeirIndex), PaperShiftWorkerAttributes.Height));
            Set(View.SelectedHeirTexts, "personality", HeirPersonality(selected, selectedHeirIndex));
            Set(View.SelectedHeirTexts, "income", "0元");
            SetInitialTags(BuildInitialTags(selected));
        }

        private void SetInitialTags(IList<string> tags)
        {
            if (View.InitialTagTexts == null)
            {
                return;
            }

            for (var i = 0; i < View.InitialTagTexts.Length; i++)
            {
                var text = View.InitialTagTexts[i];
                if (text == null)
                {
                    continue;
                }

                var hasTag = tags != null && i < tags.Count && !string.IsNullOrEmpty(tags[i]);
                text.transform.parent.gameObject.SetActive(hasTag);
                if (hasTag)
                {
                    text.text = tags[i];
                }
            }
        }

        private string[] BuildInitialTags(HeirProfile heir)
        {
            var tags = new List<string>();
            if (heir.Tags != null)
            {
                for (var i = 0; i < heir.Tags.Count && tags.Count < 2; i++)
                {
                    if (!string.IsNullOrEmpty(heir.Tags[i].DisplayName))
                    {
                        tags.Add(heir.Tags[i].DisplayName);
                    }
                }
            }

            var later = State == null ? null : State.LaterLife;
            if (later != null && later.IndustryInsight >= 65)
            {
                tags.Add("行业见识");
            }

            if (later != null && later.FamilyStability >= 65)
            {
                tags.Add("稳定家庭");
            }

            if (later != null && later.LifePressure >= 70)
            {
                tags.Add("压力遗产");
            }

            if (later != null && later.SpecialOpportunity >= 35)
            {
                tags.Add("特殊机遇");
            }

            tags.Add(HeirPersonality(heir, selectedHeirIndex) + "做事");

            while (tags.Count > 4)
            {
                tags.RemoveAt(tags.Count - 1);
            }

            return tags.ToArray();
        }

        private WorkerProfile BuildPreviewWorker(HeirProfile heir, int index)
        {
            var preview = new WorkerProfile
            {
                LastName = State.Worker.LastName,
                FirstName = ShortName(heir.Name),
                Gender = heir.Gender,
                Personality = HeirPersonality(heir, index),
                Age = DisplayHeirAge(heir, index),
                Stress = Mathf.Clamp(heir.Stress, 0, 100)
            };

            preview.SetStat(PaperShiftWorkerAttributes.Height, HeirStat(heir, PaperShiftWorkerAttributes.Height, heir.Gender == "男" ? 172 : 162));
            preview.SetStat(PaperShiftWorkerAttributes.Appearance, HeirStat(heir, PaperShiftWorkerAttributes.Appearance, 50));
            preview.SetStat(PaperShiftWorkerAttributes.Family, HeirStat(heir, PaperShiftWorkerAttributes.Family, State.Worker.GetStat(PaperShiftWorkerAttributes.Family)));
            preview.SetStat(PaperShiftWorkerAttributes.Education, HeirStat(heir, PaperShiftWorkerAttributes.Education, State.Worker.GetStat(PaperShiftWorkerAttributes.Education)));
            preview.SetStat(PaperShiftWorkerAttributes.Ability, HeirStat(heir, PaperShiftWorkerAttributes.Ability, State.Worker.GetStat(PaperShiftWorkerAttributes.Ability)));
            return preview;
        }

        private static int HeirStat(HeirProfile heir, string statId, int fallback)
        {
            if (heir == null || heir.Stats == null)
            {
                return fallback;
            }

            for (var i = 0; i < heir.Stats.Count; i++)
            {
                if (heir.Stats[i].Id == statId)
                {
                    return heir.Stats[i].Value;
                }
            }

            return fallback;
        }

        private static string ShortName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return "孩子";
            }

            return fullName.Length > 3 ? fullName.Substring(fullName.Length - 2) : fullName;
        }

        private static int DisplayHeirAge(HeirProfile heir, int index)
        {
            if (heir == null)
            {
                return 18;
            }

            return Mathf.Clamp(heir.Age, 18, 40);
        }

        private static string HeirPersonality(HeirProfile heir, int index)
        {
            if (heir != null && !string.IsNullOrEmpty(heir.Personality))
            {
                return heir.Personality;
            }

            var options = new[] { "谨慎", "开朗", "沉稳", "灵活" };
            return options[Mathf.Abs(index) % options.Length];
        }

        private void ResolveView()
        {
            if (View == null)
            {
                View = GetComponent<PaperShiftInheritanceViewReferences>();
            }
        }
    }
}
