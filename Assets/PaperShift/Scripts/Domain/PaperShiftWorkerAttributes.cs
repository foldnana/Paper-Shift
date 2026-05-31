using System;

namespace PaperShift.Domain
{
    public static class PaperShiftWorkerAttributes
    {
        public const string Gender = "gender";
        public const string Personality = "personality";
        public const string Age = "age";
        public const string Height = "height";
        public const string Appearance = "appearance";
        public const string Education = "education";
        public const string Family = "family";
        public const string Major = "major";
        public const string Ability = "ability";

        public static readonly string[] CoreStatIds =
        {
            Height,
            Appearance,
            Education,
            Family,
            Major,
            Ability
        };

        public static string Canonicalize(string fieldId)
        {
            if (string.IsNullOrEmpty(fieldId))
            {
                return string.Empty;
            }

            switch (fieldId.Trim().ToLowerInvariant())
            {
                case "性别":
                case "gender":
                    return Gender;
                case "性格":
                case "personality":
                    return Personality;
                case "年龄":
                case "age":
                    return Age;
                case "身高":
                case "height":
                    return Height;
                case "形象":
                case "appearance":
                    return Appearance;
                case "教育":
                case "学历":
                case "education":
                    return Education;
                case "家境":
                case "family":
                    return Family;
                case "专业":
                case "career":
                case "major":
                    return Major;
                case "能力":
                case "income":
                case "ability":
                    return Ability;
                default:
                    return fieldId.Trim();
            }
        }

        public static string DisplayName(string fieldId)
        {
            switch (Canonicalize(fieldId))
            {
                case Gender: return "性别";
                case Personality: return "性格";
                case Age: return "年龄";
                case Height: return "身高";
                case Appearance: return "形象";
                case Education: return "教育";
                case Family: return "家境";
                case Major: return "专业";
                case Ability: return "能力";
                default: return fieldId;
            }
        }

        public static string DisplayValue(WorkerProfile worker, string fieldId, bool exaggerated = false)
        {
            if (worker == null)
            {
                return string.Empty;
            }

            switch (Canonicalize(fieldId))
            {
                case Gender:
                    return worker.Gender;
                case Personality:
                    return string.IsNullOrEmpty(worker.Personality) ? "沉稳" : worker.Personality;
                case Age:
                    return worker.Age + "岁";
                case Height:
                    return Clamp(worker.GetStat(Height) + (exaggerated ? 5 : 0), 145, 205) + "cm";
                case Appearance:
                    return ScoreLabel(Clamp(worker.GetStat(Appearance) + (exaggerated ? 8 : 0), 0, 100));
                case Education:
                    return EducationLabel(Clamp(worker.GetStat(Education) + (exaggerated ? 16 : 0), 0, 100));
                case Family:
                    return FamilyLabel(Clamp(worker.GetStat(Family) + (exaggerated ? 14 : 0), 0, 100));
                case Major:
                    return MajorLabel(Clamp(worker.GetStat(Major) + (exaggerated ? 14 : 0), 0, 100));
                case Ability:
                    return AbilityLabel(Clamp(worker.GetStat(Ability) + (exaggerated ? 12 : 0), 0, 100));
                default:
                    return string.Empty;
            }
        }

        public static string RarityId(WorkerProfile worker, string fieldId)
        {
            if (worker == null)
            {
                return "normal";
            }

            switch (Canonicalize(fieldId))
            {
                case Height:
                    return ThresholdRarity(worker.GetStat(Height), 170, 185);
                case Appearance:
                    return ThresholdRarity(worker.GetStat(Appearance), 70, 82);
                case Education:
                    return ThresholdRarity(worker.GetStat(Education), 55, 82);
                case Family:
                    return ThresholdRarity(worker.GetStat(Family), 45, 72);
                case Major:
                    return ThresholdRarity(worker.GetStat(Major), 58, 78);
                case Ability:
                    return ThresholdRarity(worker.GetStat(Ability), 65, 82);
                default:
                    return "normal";
            }
        }

        public static string ScoreLabel(int value)
        {
            return (Clamp(value, 0, 100) / 10f).ToString("0.0") + "分";
        }

        public static string EducationLabel(int value)
        {
            if (value >= 82)
            {
                return "重点本科";
            }

            if (value >= 55)
            {
                return "普通本科";
            }

            if (value >= 35)
            {
                return "高中/职校";
            }

            return "基础教育";
        }

        public static string FamilyLabel(int value)
        {
            if (value >= 72)
            {
                return "富裕家庭";
            }

            if (value >= 45)
            {
                return "中产家庭";
            }

            if (value >= 22)
            {
                return "普通家庭";
            }

            return "拮据家庭";
        }

        public static string MajorLabel(int value)
        {
            if (value >= 78)
            {
                return "计算机";
            }

            if (value >= 58)
            {
                return "金融";
            }

            if (value >= 38)
            {
                return "文科";
            }

            return "无明确专业";
        }

        public static string AbilityLabel(int value)
        {
            if (value >= 82)
            {
                return "独当一面";
            }

            if (value >= 65)
            {
                return "能力较强";
            }

            if (value >= 45)
            {
                return "基础扎实";
            }

            return "平平无奇";
        }

        public static string BestAttributeLabel(WorkerProfile worker)
        {
            var bestId = Appearance;
            var bestValue = int.MinValue;
            for (var i = 0; i < CoreStatIds.Length; i++)
            {
                var id = CoreStatIds[i];
                if (id == Height)
                {
                    continue;
                }

                var value = worker.GetStat(id);
                if (value > bestValue)
                {
                    bestValue = value;
                    bestId = id;
                }
            }

            return DisplayName(bestId) + "强";
        }

        public static string ExpectedSalaryLabel(WorkerProfile worker)
        {
            var baseSalary = 5000 + worker.GetStat(Education) * 35 + worker.GetStat(Major) * 60 + worker.GetStat(Ability) * 70;
            baseSalary = (int)Math.Round(baseSalary / 500f) * 500;
            return "期望 " + Math.Max(4000, baseSalary - 2500) + "-" + Math.Max(6000, baseSalary + 3500);
        }

        public static string ResumeExperienceLabel(WorkerProfile worker)
        {
            if (worker.GetStat(Major) >= 78)
            {
                return "做过专业相关项目";
            }

            if (worker.GetStat(Ability) >= 72)
            {
                return "做过执行和协作项目";
            }

            if (worker.GetStat(Education) >= 72)
            {
                return "学习能力和文档基础较好";
            }

            return "做过基础兼职";
        }

        private static string ThresholdRarity(int value, int rareThreshold, int superRareThreshold)
        {
            if (value >= superRareThreshold)
            {
                return "super_rare";
            }

            return value >= rareThreshold ? "rare" : "normal";
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
