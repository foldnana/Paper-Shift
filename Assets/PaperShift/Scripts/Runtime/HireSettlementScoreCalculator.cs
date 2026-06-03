using System;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public enum HireSettlementScoreTier
    {
        Normal,
        Rare,
        SuperRare
    }

    public sealed class HireSettlementScore
    {
        public HireSettlementScoreItem[] Items;
        public int TotalPoints;

        public HireSettlementScoreItem Find(string id)
        {
            if (Items == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var i = 0; i < Items.Length; i++)
            {
                if (Items[i] != null && Items[i].Id == id)
                {
                    return Items[i];
                }
            }

            return null;
        }
    }

    public sealed class HireSettlementScoreItem
    {
        public string Id;
        public string Label;
        public string ValueText;
        public int Score;
        public int Points;
        public HireSettlementScoreTier Tier;

        public string TierText
        {
            get
            {
                switch (Tier)
                {
                    case HireSettlementScoreTier.SuperRare:
                        return "★★超稀有!";
                    case HireSettlementScoreTier.Rare:
                        return "★ 稀有!";
                    default:
                        return "普通";
                }
            }
        }
    }

    public static class HireSettlementScoreCalculator
    {
        public const string Job = "job";
        public const string Income = "income";
        public const string Prospect = "prospect";
        public const string Efficiency = "efficiency";
        public const string Fit = "fit";

        private const int PointsPerScore = 10;

        public static HireSettlementScore Calculate(PaperShiftRunState state, PaperShiftDatabase database)
        {
            database = EnsureDatabase(database);
            var company = FindCompany(state, database);
            var job = FindJob(state, database, company);
            var salary = CurrentSalary(state);
            var elapsedMonths = GenerationElapsedMonths(state);

            var items = new[]
            {
                JobItem(state, company, job),
                IncomeItem(database, salary),
                ProspectItem(state, company, job),
                EfficiencyItem(elapsedMonths),
                FitItem(state, database, job)
            };

            var total = 0;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    total += items[i].Points;
                }
            }

            return new HireSettlementScore
            {
                Items = items,
                TotalPoints = total
            };
        }

        private static HireSettlementScoreItem JobItem(PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var title = CurrentJobTitle(state, job);
            var score = 45;
            if (job != null)
            {
                score += job.Difficulty / 2;
                score += (job.OfferThreshold - 70) / 2;
                score += Count(job.TagIds) * 3;
                if (job.PromotionBase >= 12)
                {
                    score += 5;
                }
            }

            if (company != null)
            {
                score += Count(company.TagIds) * 2;
            }

            score = Clamp(score, 0, 100);
            return Item(Job, "岗位", score, title + " " + score + "分");
        }

        private static HireSettlementScoreItem IncomeItem(PaperShiftDatabase database, int salary)
        {
            var score = SalaryScore(database, salary);
            var value = salary <= 0 ? "收入未知 " + score + "分" : "月薪 " + salary.ToString("N0") + "元 " + score + "分";
            return Item(Income, "收入", score, value);
        }

        private static HireSettlementScoreItem ProspectItem(PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var score = 42;
            if (job != null)
            {
                score += job.PromotionBase * 4;
                score += HasTag(job.TagIds, "ai") ? 8 : 0;
                score += HasTag(job.TagIds, "tooling") ? 5 : 0;
                score += HasTag(job.TagIds, "remote") ? 2 : 0;
                score += HasTag(job.TagIds, "fast_change") ? 3 : 0;
                score -= HasTag(job.TagIds, "physical") ? 4 : 0;
                score -= HasTag(job.TagIds, "local") ? 2 : 0;
            }

            if (company != null)
            {
                score += HasTag(company.TagIds, "ai") ? 4 : 0;
                var months = CompanyAgeMonths(state, company);
                if (months > 0 && months <= 36)
                {
                    score += 5;
                }
                else if (months >= 72)
                {
                    score += 3;
                }
            }

            score = Clamp(score, 0, 100);
            return Item(Prospect, "前景", score, ProspectLabel(score) + " " + score + "分");
        }

        private static HireSettlementScoreItem EfficiencyItem(int elapsedMonths)
        {
            var score = 100;
            if (elapsedMonths > 0)
            {
                score = 92 - Math.Max(0, elapsedMonths - 5) * 7;
            }

            score = Clamp(score, 20, 100);
            var value = elapsedMonths <= 0 ? "当月入职 " + score + "分" : "耗时 " + elapsedMonths + "个月 " + score + "分";
            return Item(Efficiency, "效率", score, value);
        }

        private static HireSettlementScoreItem FitItem(PaperShiftRunState state, PaperShiftDatabase database, JobDefinition job)
        {
            var recognition = CurrentRecognition(state);
            var requirementScore = RequirementScore(state, job);
            var profileScore = ProfileScore(state, database);
            var stress = state == null || state.Worker == null ? 0 : state.Worker.Stress;
            var risk = state == null || state.Resume == null ? 0 : state.Resume.DeceptionRisk;

            var score = recognition * 45 / 100;
            score += requirementScore * 25 / 100;
            score += profileScore * 20 / 100;
            score += (100 - stress) * 10 / 100;
            score -= risk / 4;
            score = Clamp(score, 0, 100);

            return Item(Fit, "适配", score, "认可度 " + recognition + "% " + score + "分");
        }

        private static HireSettlementScoreItem Item(string id, string label, int score, string valueText)
        {
            score = Clamp(score, 0, 100);
            return new HireSettlementScoreItem
            {
                Id = id,
                Label = label,
                ValueText = valueText,
                Score = score,
                Points = score * PointsPerScore,
                Tier = TierFor(score)
            };
        }

        private static HireSettlementScoreTier TierFor(int score)
        {
            if (score >= 85)
            {
                return HireSettlementScoreTier.SuperRare;
            }

            return score >= 60 ? HireSettlementScoreTier.Rare : HireSettlementScoreTier.Normal;
        }

        private static int SalaryScore(PaperShiftDatabase database, int salary)
        {
            if (salary <= 0)
            {
                return 0;
            }

            var min = int.MaxValue;
            var max = 0;
            if (database != null && database.Companies != null)
            {
                for (var i = 0; i < database.Companies.Length; i++)
                {
                    var company = database.Companies[i];
                    if (company == null || company.Jobs == null)
                    {
                        continue;
                    }

                    for (var j = 0; j < company.Jobs.Length; j++)
                    {
                        var job = company.Jobs[j];
                        if (job == null)
                        {
                            continue;
                        }

                        min = Math.Min(min, job.SalaryMin);
                        max = Math.Max(max, job.SalaryMax);
                    }
                }
            }

            if (min == int.MaxValue || max <= min)
            {
                return Clamp(salary / 200, 0, 100);
            }

            return Clamp(20 + (salary - min) * 80 / (max - min), 0, 100);
        }

        private static int RequirementScore(PaperShiftRunState state, JobDefinition job)
        {
            if (state == null || state.Worker == null || job == null || job.Requirements == null || job.Requirements.Length == 0)
            {
                return 65;
            }

            var total = 0;
            var weight = 0;
            for (var i = 0; i < job.Requirements.Length; i++)
            {
                var requirement = job.Requirements[i];
                if (requirement == null || string.IsNullOrEmpty(requirement.StatId))
                {
                    continue;
                }

                var value = state.Worker.GetStat(requirement.StatId);
                var metScore = Clamp(55 + (value - requirement.MinValue) * 2, 0, 100);
                var requirementWeight = Math.Max(1, requirement.Weight);
                total += metScore * requirementWeight;
                weight += requirementWeight;
            }

            return weight <= 0 ? 65 : total / weight;
        }

        private static int ProfileScore(PaperShiftRunState state, PaperShiftDatabase database)
        {
            if (state == null)
            {
                return 50;
            }

            var profile = new FitProfileResolver(database).Build(state);
            return Average(
                profile.Get(FitDimension.Professionalism),
                profile.Get(FitDimension.Execution),
                profile.Get(FitDimension.Communication),
                profile.Get(FitDimension.Resilience));
        }

        private static CompanyDefinition FindCompany(PaperShiftRunState state, PaperShiftDatabase database)
        {
            if (state == null || database == null)
            {
                return null;
            }

            var companyId = string.Empty;
            if (state.CurrentJob != null && !string.IsNullOrEmpty(state.CurrentJob.CompanyId))
            {
                companyId = state.CurrentJob.CompanyId;
            }
            else if (state.Interview != null)
            {
                companyId = state.Interview.CompanyId;
            }

            var company = database.FindCompany(companyId);
            if (company != null)
            {
                return company;
            }

            var jobId = state.CurrentJob == null ? string.Empty : state.CurrentJob.JobId;
            for (var i = 0; database.Companies != null && i < database.Companies.Length; i++)
            {
                company = database.Companies[i];
                if (company != null && company.FindJob(jobId) != null)
                {
                    return company;
                }
            }

            return null;
        }

        private static JobDefinition FindJob(PaperShiftRunState state, PaperShiftDatabase database, CompanyDefinition company)
        {
            if (state == null)
            {
                return null;
            }

            var jobId = state.CurrentJob == null ? string.Empty : state.CurrentJob.JobId;
            if (company != null)
            {
                var job = company.FindJob(jobId);
                if (job != null)
                {
                    return job;
                }
            }

            if (database == null || database.Companies == null)
            {
                return null;
            }

            var title = state.CurrentJob == null ? string.Empty : state.CurrentJob.JobTitle;
            for (var i = 0; i < database.Companies.Length; i++)
            {
                company = database.Companies[i];
                if (company == null || company.Jobs == null)
                {
                    continue;
                }

                for (var j = 0; j < company.Jobs.Length; j++)
                {
                    var job = company.Jobs[j];
                    if (job != null && (job.Id == jobId || job.DisplayName == title))
                    {
                        return job;
                    }
                }
            }

            return null;
        }

        private static PaperShiftDatabase EnsureDatabase(PaperShiftDatabase database)
        {
            if (database != null)
            {
                return database;
            }

            var fallback = PaperShiftSeedData.CreateDefaultDatabase();
            PaperShiftSeedData.ApplyRuntimeDefaults(fallback);
            return fallback;
        }

        private static string CurrentJobTitle(PaperShiftRunState state, JobDefinition job)
        {
            if (state != null && state.CurrentJob != null && !string.IsNullOrEmpty(state.CurrentJob.JobTitle))
            {
                return state.CurrentJob.JobTitle;
            }

            if (job != null && !string.IsNullOrEmpty(job.DisplayName))
            {
                return job.DisplayName;
            }

            return "新岗位";
        }

        private static int CurrentSalary(PaperShiftRunState state)
        {
            if (state == null)
            {
                return 0;
            }

            if (state.CurrentJob != null && state.CurrentJob.Salary > 0)
            {
                return state.CurrentJob.Salary;
            }

            return state.Interview == null ? 0 : Math.Max(0, state.Interview.Salary);
        }

        private static int CurrentRecognition(PaperShiftRunState state)
        {
            if (state == null)
            {
                return 0;
            }

            if (state.CurrentJob != null && state.CurrentJob.Recognition > 0)
            {
                return Clamp(state.CurrentJob.Recognition, 0, 100);
            }

            return state.Interview == null ? 0 : Clamp(state.Interview.Recognition, 0, 100);
        }

        private static int CompanyAgeMonths(PaperShiftRunState state, CompanyDefinition company)
        {
            if (state == null || company == null || company.FoundedYear <= 0)
            {
                return 0;
            }

            return Math.Max(0, MonthIndex(state.CurrentYear, NormalizeMonth(state.CurrentMonth)) - MonthIndex(company.FoundedYear, NormalizeMonth(company.FoundedMonth)));
        }

        private static int GenerationElapsedMonths(PaperShiftRunState state)
        {
            if (state == null || state.CurrentYear <= 0)
            {
                return 0;
            }

            var startYear = state.GenerationStartYear > 0 ? state.GenerationStartYear : PaperShiftGameService.FirstGenerationStartYear;
            var startMonth = state.GenerationStartMonth > 0 ? state.GenerationStartMonth : PaperShiftGameService.FirstGenerationStartMonth;
            return Math.Max(0, MonthIndex(state.CurrentYear, NormalizeMonth(state.CurrentMonth)) - MonthIndex(startYear, NormalizeMonth(startMonth)));
        }

        private static int MonthIndex(int year, int month)
        {
            return year * 12 + NormalizeMonth(month) - 1;
        }

        private static bool HasTag(string[] tagIds, string tagId)
        {
            if (tagIds == null || string.IsNullOrEmpty(tagId))
            {
                return false;
            }

            for (var i = 0; i < tagIds.Length; i++)
            {
                if (tagIds[i] == tagId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ProspectLabel(int score)
        {
            if (score >= 85)
            {
                return "成长空间极高";
            }

            return score >= 60 ? "成长空间不错" : "成长空间一般";
        }

        private static int Count(string[] values)
        {
            return values == null ? 0 : values.Length;
        }

        private static int Average(params int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < values.Length; i++)
            {
                total += values[i];
            }

            return total / values.Length;
        }

        private static int NormalizeMonth(int month)
        {
            if (month < 1)
            {
                return 1;
            }

            return month > 12 ? 12 : month;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
