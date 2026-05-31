using System.Collections.Generic;
using PaperShift.Domain;
using PaperShift.Model;
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

            Set(View.CoinText, State.Retirement.FinalSavings.ToString("N0"));
            RefreshSummary();
            RefreshHeirs();
        }

        private void RefreshSummary()
        {
            var jobTitle = string.IsNullOrEmpty(State.Retirement.FinalJobTitle)
                ? State.CurrentJob.JobTitle
                : State.Retirement.FinalJobTitle;
            if (string.IsNullOrEmpty(jobTitle))
            {
                jobTitle = "这份工作";
            }

            var pressure = EstimatePressure();
            var years = EstimateWorkYears(pressure);
            var prospect = EstimateProspect();
            var marriageAge = Mathf.Clamp(State.Worker.Age + 4 + pressure / 40, State.Worker.Age + 1, 45);
            var houseAge = Mathf.Clamp(marriageAge + 4 + (State.Worker.GetStat(PaperShiftWorkerAttributes.Family) < 45 ? 2 : 0), marriageAge + 1, 55);
            var childAge = Mathf.Clamp(marriageAge + 6, marriageAge + 1, 56);

            Set(View.SummaryTexts, "job", jobTitle);
            Set(View.SummaryTexts, "years", years + "年");
            Set(View.SummaryTexts, "pressure", PressureLabel(pressure));
            Set(View.SummaryTexts, "prospect", ProspectLabel(prospect));
            Set(View.SummaryTexts, "story", BuildStory(jobTitle, pressure, prospect));
            Set(View.SummaryTexts, "marriageAge", marriageAge + "岁");
            Set(View.SummaryTexts, "marriageText", "结婚\n生活合并账本");
            Set(View.SummaryTexts, "houseAge", houseAge + "岁");
            Set(View.SummaryTexts, "houseText", "买房\n家境变得稳定");
            Set(View.SummaryTexts, "childAge", childAge + "岁");
            Set(View.SummaryTexts, "childText", "生子\n下一代登场");
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
                SetInitialTags(new[] { "等待下一代" });
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

            var finalJobTitle = State.Retirement.FinalJobTitle ?? string.Empty;
            var currentJobTitle = State.CurrentJob.JobTitle ?? string.Empty;
            if (finalJobTitle.Contains("AI") || currentJobTitle.Contains("AI"))
            {
                tags.Add("AI耳濡目染");
            }

            if (HeirStat(heir, PaperShiftWorkerAttributes.Family, 0) >= 55)
            {
                tags.Add("小康家庭");
            }

            if (HeirStat(heir, PaperShiftWorkerAttributes.Education, 0) >= 65)
            {
                tags.Add("受过教育");
            }

            if (EstimatePressure() >= 55)
            {
                tags.Add("父母期望");
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
                Age = DisplayHeirAge(heir, index)
            };

            preview.SetStat(PaperShiftWorkerAttributes.Height, heir.Gender == "男" ? 172 + index * 2 : 162 + index * 2);
            preview.SetStat(PaperShiftWorkerAttributes.Appearance, Mathf.Clamp(48 + State.Worker.GetStat(PaperShiftWorkerAttributes.Appearance) / 3 + index * 3, 0, 100));
            preview.SetStat(PaperShiftWorkerAttributes.Family, HeirStat(heir, PaperShiftWorkerAttributes.Family, State.Worker.GetStat(PaperShiftWorkerAttributes.Family)));
            preview.SetStat(PaperShiftWorkerAttributes.Education, HeirStat(heir, PaperShiftWorkerAttributes.Education, State.Worker.GetStat(PaperShiftWorkerAttributes.Education)));
            preview.SetStat(PaperShiftWorkerAttributes.Ability, HeirStat(heir, PaperShiftWorkerAttributes.Ability, State.Worker.GetStat(PaperShiftWorkerAttributes.Ability)));
            return preview;
        }

        private int EstimatePressure()
        {
            var intensity = State.CurrentJob.Intensity > 0 ? State.CurrentJob.Intensity : State.Worker.Stress;
            return Mathf.Clamp(intensity, 20, 90);
        }

        private int EstimateProspect()
        {
            var salary = State.CurrentJob.Salary;
            var ability = State.Worker.GetStat(PaperShiftWorkerAttributes.Ability);
            return Mathf.Clamp(45 + salary / 500 + ability / 5 - EstimatePressure() / 6, 20, 95);
        }

        private int EstimateWorkYears(int pressure)
        {
            if (State.Retirement.WorkYears > 0)
            {
                return State.Retirement.WorkYears;
            }

            return Mathf.Clamp(10 + EstimateProspect() / 8 + (100 - pressure) / 12 + Mathf.Max(0, 45 - State.Worker.Age) / 3, 6, 32);
        }

        private string BuildStory(string jobTitle, int pressure, int prospect)
        {
            var pressureText = pressure >= 65 ? "压力不小" : pressure >= 45 ? "压力适中" : "节奏还算平稳";
            var prospectText = prospect >= 68 ? "行业还在往上走" : prospect >= 45 ? "行业没有太差，也不算轻松" : "行业前景开始变窄";
            return "他把「" + jobTitle + "」干成了长期饭碗。" + prospectText + "，所以日子比刚入职时稳了许多；只是" + pressureText + "，后来的人生一直带着一点“不能停下”的习惯。";
        }

        private static string PressureLabel(int pressure)
        {
            if (pressure >= 65)
            {
                return "偏高";
            }

            return pressure >= 45 ? "适中" : "较低";
        }

        private static string ProspectLabel(int prospect)
        {
            if (prospect >= 68)
            {
                return "上升";
            }

            return prospect >= 45 ? "平稳" : "收缩";
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

            return Mathf.Clamp(heir.Age < 18 ? 22 + index : heir.Age, 18, 30);
        }

        private static string HeirPersonality(HeirProfile heir, int index)
        {
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
