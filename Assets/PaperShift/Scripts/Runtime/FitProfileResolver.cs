using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class FitProfileResolver
    {
        private readonly PaperShiftDatabase database;

        public FitProfileResolver(PaperShiftDatabase database)
        {
            this.database = database;
        }

        public FitProfile Build(PaperShiftRunState state)
        {
            var profile = new FitProfile();
            if (state == null || state.Worker == null)
            {
                return profile;
            }

            var age = PresentAge(state);
            var height = PresentStat(state, PaperShiftWorkerAttributes.Height, 5);
            var appearance = PresentStat(state, PaperShiftWorkerAttributes.Appearance, 8);
            var education = PresentStat(state, PaperShiftWorkerAttributes.Education, 16);
            var family = PresentStat(state, PaperShiftWorkerAttributes.Family, 14);
            var major = PresentStat(state, PaperShiftWorkerAttributes.Major, 14, "career", "experience");
            var ability = PresentStat(state, PaperShiftWorkerAttributes.Ability, 12, "income", "experience", "salary");

            profile.Set(FitDimension.Maturity, Average(age, 2, ability, 2, education, 1));
            profile.Set(FitDimension.Physique, Average(height, 3, age, 1));
            profile.Set(FitDimension.Presence, Average(appearance, 3, height, 1));
            profile.Set(FitDimension.Credentials, Average(education, 1));
            profile.Set(FitDimension.Professionalism, Average(major, 3, education, 1, ability, 2));
            profile.Set(FitDimension.Execution, Average(ability, 3, major, 1));
            profile.Set(FitDimension.Communication, Average(appearance, 2, family, 1));
            profile.Set(FitDimension.Resilience, Average(ability, 2, family, 1, age, 1));

            ApplyPersonality(profile, state.Worker.Personality);
            ApplyTagBaseEffects(profile, state);
            return profile;
        }

        private PresentedValue PresentAge(PaperShiftRunState state)
        {
            var mode = PackagingMode(state, PaperShiftWorkerAttributes.Age);
            if (mode == ResumePackagingMode.Hide)
            {
                return PresentedValue.Hidden();
            }

            var age = state.Worker.Age;
            if (mode == ResumePackagingMode.Exaggerate || mode == ResumePackagingMode.Fake)
            {
                age = age <= 28 ? age + 2 : age - 4;
            }

            return PresentedValue.Shown(AgeScore(age));
        }

        private PresentedValue PresentStat(PaperShiftRunState state, string statId, int exaggerateDelta, params string[] aliases)
        {
            var mode = PackagingMode(state, statId, aliases);
            if (mode == ResumePackagingMode.Hide)
            {
                return PresentedValue.Hidden();
            }

            var value = state.Worker.GetStat(statId);
            if (mode == ResumePackagingMode.Exaggerate || mode == ResumePackagingMode.Fake)
            {
                value += mode == ResumePackagingMode.Fake ? exaggerateDelta * 2 : exaggerateDelta;
            }

            if (statId == PaperShiftWorkerAttributes.Height)
            {
                value = HeightScore(value);
            }

            return PresentedValue.Shown(Clamp(value, 0, 100));
        }

        private ResumePackagingMode PackagingMode(PaperShiftRunState state, string fieldId, params string[] aliases)
        {
            if (state.Resume == null || state.Resume.Packaging == null)
            {
                return ResumePackagingMode.Normal;
            }

            fieldId = PaperShiftWorkerAttributes.Canonicalize(fieldId);
            for (var i = 0; i < state.Resume.Packaging.Count; i++)
            {
                var choice = state.Resume.Packaging[i];
                if (choice != null && IsPackagingFieldMatch(choice.FieldId, fieldId, aliases))
                {
                    return choice.Mode;
                }
            }

            return ResumePackagingMode.Normal;
        }

        private static bool IsPackagingFieldMatch(string choiceFieldId, string fieldId, string[] aliases)
        {
            var choice = PaperShiftWorkerAttributes.Canonicalize(choiceFieldId);
            if (choice == PaperShiftWorkerAttributes.Canonicalize(fieldId))
            {
                return true;
            }

            if (aliases == null)
            {
                return false;
            }

            for (var i = 0; i < aliases.Length; i++)
            {
                if (choice == PaperShiftWorkerAttributes.Canonicalize(aliases[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyTagBaseEffects(FitProfile profile, PaperShiftRunState state)
        {
            for (var i = 0; i < state.Worker.Tags.Count; i++)
            {
                var tag = database.FindTag(state.Worker.Tags[i].TagId);
                if (tag == null || tag.Effects == null)
                {
                    continue;
                }

                for (var j = 0; j < tag.Effects.Length; j++)
                {
                    var effect = tag.Effects[j];
                    if (effect == null || effect.Timing != EffectTiming.Passive || effect.Kind != EffectKind.AddFitScore)
                    {
                        continue;
                    }

                    if (TryParseDimension(effect.Key, out var dimension))
                    {
                        profile.Add(dimension, effect.IntValue);
                    }
                }
            }
        }

        private static void ApplyPersonality(FitProfile profile, string personality)
        {
            switch (personality)
            {
                case "沉稳":
                    profile.Add(FitDimension.Resilience, 6);
                    profile.Add(FitDimension.Execution, 2);
                    break;
                case "谨慎":
                    profile.Add(FitDimension.Execution, 5);
                    profile.Add(FitDimension.Resilience, 2);
                    break;
                case "开朗":
                    profile.Add(FitDimension.Communication, 6);
                    break;
                case "灵活":
                    profile.Add(FitDimension.Execution, 3);
                    profile.Add(FitDimension.Communication, 3);
                    break;
                case "较真":
                    profile.Add(FitDimension.Execution, 6);
                    profile.Add(FitDimension.Communication, -2);
                    break;
                case "随和":
                    profile.Add(FitDimension.Communication, 4);
                    profile.Add(FitDimension.Resilience, 2);
                    break;
            }
        }

        private static int Average(PresentedValue first, int firstWeight)
        {
            return Average(first, firstWeight, PresentedValue.Hidden(), 0, PresentedValue.Hidden(), 0);
        }

        private static int Average(PresentedValue first, int firstWeight, PresentedValue second, int secondWeight)
        {
            return Average(first, firstWeight, second, secondWeight, PresentedValue.Hidden(), 0);
        }

        private static int Average(PresentedValue first, int firstWeight, PresentedValue second, int secondWeight, PresentedValue third, int thirdWeight)
        {
            var total = 0;
            var weight = 0;
            Add(first, firstWeight, ref total, ref weight);
            Add(second, secondWeight, ref total, ref weight);
            Add(third, thirdWeight, ref total, ref weight);
            return weight <= 0 ? 50 : total / weight;
        }

        private static void Add(PresentedValue value, int valueWeight, ref int total, ref int weight)
        {
            if (!value.Visible || valueWeight <= 0)
            {
                return;
            }

            total += value.Value * valueWeight;
            weight += valueWeight;
        }

        public static bool TryParseDimension(string id, out FitDimension dimension)
        {
            switch ((id ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "maturity":
                    dimension = FitDimension.Maturity;
                    return true;
                case "physique":
                    dimension = FitDimension.Physique;
                    return true;
                case "presence":
                    dimension = FitDimension.Presence;
                    return true;
                case "credentials":
                    dimension = FitDimension.Credentials;
                    return true;
                case "professionalism":
                    dimension = FitDimension.Professionalism;
                    return true;
                case "execution":
                    dimension = FitDimension.Execution;
                    return true;
                case "communication":
                    dimension = FitDimension.Communication;
                    return true;
                case "resilience":
                    dimension = FitDimension.Resilience;
                    return true;
                default:
                    dimension = FitDimension.Execution;
                    return false;
            }
        }

        private static int AgeScore(int age)
        {
            if (age <= 18)
            {
                return 42;
            }

            if (age <= 24)
            {
                return 50 + (age - 18) * 6;
            }

            if (age <= 34)
            {
                return 86;
            }

            if (age <= 45)
            {
                return 86 - (age - 34) * 3;
            }

            return Clamp(53 - (age - 45) * 2, 20, 100);
        }

        private static int HeightScore(int height)
        {
            return Clamp((height - 145) * 100 / 60, 0, 100);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private struct PresentedValue
        {
            public bool Visible;
            public int Value;

            public static PresentedValue Hidden()
            {
                return new PresentedValue { Visible = false, Value = 0 };
            }

            public static PresentedValue Shown(int value)
            {
                return new PresentedValue { Visible = true, Value = value };
            }
        }
    }
}
