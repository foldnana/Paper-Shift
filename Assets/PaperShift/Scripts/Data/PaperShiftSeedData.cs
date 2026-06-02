using PaperShift.Domain;
using UnityEngine;

namespace PaperShift.Data
{
    public static class PaperShiftSeedData
    {
        public static PaperShiftDatabase CreateDefaultDatabase()
        {
            var database = ScriptableObject.CreateInstance<PaperShiftDatabase>();
            database.Eras = CreateEras();
            database.Rarities = CreateRarities();
            database.Stats = CreateStats();
            database.Tags = CreateTags();
            database.WorkTags = CreateWorkTags();
            database.Companies = CreateCompanies();
            database.Events = CreateEvents();
            database.LastNames = new[] { "李", "王", "林", "赵", "陈", "周", "顾", "许" };
            database.MaleFirstNames = new[] { "知行", "安和", "星远", "景明", "修远" };
            database.FemaleFirstNames = new[] { "小满", "君语", "安禾", "清许", "南星" };
            return database;
        }

        public static void ApplyRuntimeDefaults(PaperShiftDatabase database)
        {
            if (database == null)
            {
                return;
            }

            if (database.WorkTags == null || database.WorkTags.Length == 0)
            {
                database.WorkTags = CreateWorkTags();
            }

            if (database.Tags == null || database.Tags.Length == 0)
            {
                database.Tags = CreateTags();
            }
            else
            {
                MergeTagRuntimeDefaults(database.Tags, CreateTags());
            }

            if (database.Events == null || database.Events.Length == 0)
            {
                database.Events = CreateEvents();
            }
            else
            {
                MergeEventRuntimeDefaults(database.Events, CreateEvents());
                if (!HasEvent(database.Events, "stress_breakdown"))
                {
                    database.Events = Append(database.Events, FindEvent(CreateEvents(), "stress_breakdown"));
                }
            }
        }

        private static EraDefinition[] CreateEras()
        {
            return new[]
            {
                new EraDefinition { Id = "timeline", DisplayName = "年份推进", StartYear = 2026, EndYear = 9999, Description = "新开游戏第一代从 2026 年开始，后代按流程自然向后推进。" }
            };
        }

        private static RarityDefinition[] CreateRarities()
        {
            return new[]
            {
                new RarityDefinition { Id = "normal", DisplayName = "普通", Weight = 72, Color = Color.white },
                new RarityDefinition { Id = "rare", DisplayName = "稀有", Weight = 23, Color = new Color(0.16f, 0.62f, 0.92f) },
                new RarityDefinition { Id = "super_rare", DisplayName = "超稀有", Weight = 5, Color = new Color(1f, 0.82f, 0.24f) }
            };
        }

        private static StatDefinition[] CreateStats()
        {
            return new[]
            {
                new StatDefinition { Id = PaperShiftWorkerAttributes.Height, DisplayName = "身高", StartMin = 155, StartMax = 188, Description = "劳动者的身高信息，可在简历中隐藏或夸大。" },
                new StatDefinition { Id = PaperShiftWorkerAttributes.Appearance, DisplayName = "形象", StartMin = 25, StartMax = 88, Description = "影响简历吸引力和面试第一印象。" },
                new StatDefinition { Id = PaperShiftWorkerAttributes.Education, DisplayName = "教育", StartMin = 20, StartMax = 88, Description = "影响学历呈现、学习相关岗位和面试基础分。" },
                new StatDefinition { Id = PaperShiftWorkerAttributes.Family, DisplayName = "家境", StartMin = 10, StartMax = 82, Description = "表达出身、底气、人脉和社会化程度。" },
                new StatDefinition { Id = PaperShiftWorkerAttributes.Major, DisplayName = "专业", StartMin = 20, StartMax = 88, Description = "影响专业方向、岗位匹配和简历含金量。" },
                new StatDefinition { Id = PaperShiftWorkerAttributes.Ability, DisplayName = "能力", StartMin = 20, StartMax = 88, Description = "影响工作表现、试用认可度和岗位适配。" }
            };
        }

        private static TagDefinition[] CreateTags()
        {
            var tags = new[]
            {
                Tag("good_accounting", "会算账", "做事细，懂成本，也更容易看出风险。", "normal", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "execution", 3),
                    Passive(EffectKind.AddFitScore, "professionalism", 3)),
                Tag("hard_worker", "能吃苦", "抗压和执行更稳，遇到累活也不容易崩。", "normal", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "resilience", 8),
                    Passive(EffectKind.AddFitScore, "execution", 3)),
                Tag("intro_by_neighbor", "同乡介绍", "有一点熟人关系，机会来得更快，但也会有人情压力。", "normal", TagPolarity.Mixed,
                    Passive(EffectKind.AddFitScore, "communication", 2)),
                Tag("family_craft", "祖传手艺", "有可传承的手艺底子，专业性和执行力更稳。", "super_rare", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "professionalism", 8),
                    Passive(EffectKind.AddFitScore, "execution", 4)),
                Tag("ai_pioneer", "AI先行者", "适应工具化和变化频繁的工作更快。", "rare", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "professionalism", 5),
                    Passive(EffectKind.AddFitScore, "execution", 4)),
                Tag("social_anxiety", "社恐", "沟通场景吃亏，但独立作业时压力更低。", "normal", TagPolarity.Mixed,
                    Passive(EffectKind.AddFitScore, "communication", -8),
                    Passive(EffectKind.AddFitScore, "resilience", -2)),
                Tag("dishonest_resume", "不诚实", "履历可信度受损，更容易被核查。", "rare", TagPolarity.Negative,
                    Passive(EffectKind.AddFitScore, "communication", -4),
                    Passive(EffectKind.PassiveEventWeight, "resume_audit", 20)),
                Tag("remote_first", "远程优先", "更适合远程和独立作业环境。", "normal", TagPolarity.Neutral,
                    Passive(EffectKind.AddFitScore, "execution", 2)),
                Tag("ai_intent", "AI/技术岗", "技术和工具相关工作上手更快。", "rare", TagPolarity.Neutral,
                    Passive(EffectKind.AddFitScore, "professionalism", 4))
            };

            FindTag(tags, "hard_worker").ConditionalEffects = new[]
            {
                Conditional(new[] { "physical", "high_pressure" }, GameEventPhase.Any,
                    Effect(EffectKind.AddJobWeight, "", 8),
                    Effect(EffectKind.AddRecognition, "", 6),
                    Effect(EffectKind.AddStress, "", -6))
            };
            FindTag(tags, "social_anxiety").ConditionalEffects = new[]
            {
                Conditional(new[] { "customer_contact", "high_communication" }, GameEventPhase.Any,
                    Effect(EffectKind.AddRecognition, "", -7),
                    Effect(EffectKind.AddStress, "", 8)),
                Conditional(new[] { "remote", "solo_work" }, GameEventPhase.Any,
                    Effect(EffectKind.AddStress, "", -6))
            };
            FindTag(tags, "ai_pioneer").ConditionalEffects = new[]
            {
                Conditional(new[] { "tooling", "fast_change", "ai" }, GameEventPhase.Any,
                    Effect(EffectKind.AddJobWeight, "", 12),
                    Effect(EffectKind.AddRecognition, "", 8),
                    Effect(EffectKind.AddEventWeight, "ai_shortcut", 18))
            };
            FindTag(tags, "intro_by_neighbor").ConditionalEffects = new[]
            {
                Conditional(new[] { "local" }, GameEventPhase.Any,
                    Effect(EffectKind.AddJobWeight, "", 18)),
                Conditional(new[] { "local" }, GameEventPhase.Interview,
                    Effect(EffectKind.AddRecognition, "", 10))
            };
            FindTag(tags, "dishonest_resume").ConditionalEffects = new[]
            {
                Conditional(new[] { "strict_rules" }, GameEventPhase.Any,
                    Effect(EffectKind.AddEventWeight, "resume_audit", 25))
            };
            FindTag(tags, "remote_first").ConditionalEffects = new[]
            {
                Conditional(new[] { "remote", "solo_work" }, GameEventPhase.Any,
                    Effect(EffectKind.AddJobWeight, "", 16),
                    Effect(EffectKind.AddRecognition, "", 4),
                    Effect(EffectKind.AddStress, "", -4))
            };
            FindTag(tags, "ai_intent").ConditionalEffects = new[]
            {
                Conditional(new[] { "ai", "tooling", "fast_change" }, GameEventPhase.Any,
                    Effect(EffectKind.AddJobWeight, "", 18),
                    Effect(EffectKind.AddRecognition, "", 4),
                    Effect(EffectKind.AddEventWeight, "ai_shortcut", 8))
            };

            return tags;
        }

        private static WorkTagDefinition[] CreateWorkTags()
        {
            return new[]
            {
                WorkTag("ai", "AI相关", "需要适应新工具和技术变化。",
                    Rule(FitDimension.Professionalism, 56, false, 7, -6, 0, 2),
                    Rule(FitDimension.Execution, 50, false, 4, -4, 0, 1)),
                WorkTag("physical", "体力劳动", "身体消耗更高。",
                    Rule(FitDimension.Physique, 52, false, 5, -8, -2, 6),
                    Rule(FitDimension.Resilience, 48, false, 4, -5, -2, 5)),
                WorkTag("local", "本地关系", "熟人、本地经验和社会化程度更有帮助。",
                    Rule(FitDimension.Maturity, 45, false, 4, -2, 0, 0)),
                WorkTag("remote", "远程", "更依赖自驱和独立执行。",
                    Rule(FitDimension.Execution, 52, false, 6, -5, -2, 2)),
                WorkTag("office", "办公室", "看重学历、专业性和表达。",
                    Rule(FitDimension.Credentials, 45, false, 4, -4, 0, 1),
                    Rule(FitDimension.Communication, 42, false, 3, -3, 0, 2)),
                WorkTag("customer_contact", "客户接触", "需要沟通、外在和情绪稳定。",
                    Rule(FitDimension.Communication, 55, false, 7, -8, 0, 7),
                    Rule(FitDimension.Presence, 45, false, 4, -4, 0, 3)),
                WorkTag("strict_rules", "规则严格", "简历包装和不诚实更容易被查。",
                    Rule(FitDimension.Execution, 55, false, 5, -6, 0, 3),
                    EventWeight("resume_audit", 20, ConditionKind.ResumeRiskAtLeast, 15)),
                WorkTag("high_pressure", "高压", "压力增长更快。",
                    Rule(FitDimension.Resilience, 58, false, 6, -8, -3, 10),
                    EventWeight("stress_breakdown", 20, ConditionKind.StressAtLeast, 70)),
                WorkTag("tooling", "工具化", "需要学习工具和流程自动化。",
                    Rule(FitDimension.Professionalism, 50, false, 5, -4, 0, 1)),
                WorkTag("fast_change", "变化频繁", "需要快速适应。",
                    Rule(FitDimension.Execution, 55, false, 5, -5, 0, 4)),
                WorkTag("solo_work", "独立作业", "沟通压力低，但自驱要求高。",
                    Rule(FitDimension.Execution, 50, false, 4, -5, -1, 1))
            };
        }

        private static CompanyDefinition[] CreateCompanies()
        {
            return new[]
            {
                new CompanyDefinition
                {
                    Id = "cloud_farm",
                    DisplayName = "云端农场",
                    Industry = "智慧农业",
                    EraIds = new string[0],
                    TagIds = new[] { "local", "ai" },
                    Jobs = new[]
                    {
                        new JobDefinition
                        {
                            Id = "smart_tractor_operator",
                            DisplayName = "农机操作员",
                            TagIds = new[] { "ai", "physical", "local", "tooling" },
                            SalaryMin = 9000,
                            SalaryMax = 13000,
                            Difficulty = 36,
                            WorkIntensity = 48,
                            Requirements = new[]
                            {
                                new StatRequirement { StatId = PaperShiftWorkerAttributes.Ability, MinValue = 40, Weight = 2 },
                                new StatRequirement { StatId = PaperShiftWorkerAttributes.Major, MinValue = 35, Weight = 2 }
                            }
                        }
                    }
                },
                new CompanyDefinition
                {
                    Id = "white_tower_tech",
                    DisplayName = "白塔科技",
                    Industry = "AI 服务",
                    EraIds = new string[0],
                    TagIds = new[] { "ai", "remote", "strict_rules" },
                    Jobs = new[]
                    {
                        new JobDefinition
                        {
                            Id = "ai_trainer",
                            DisplayName = "AI训练师",
                            TagIds = new[] { "ai", "remote", "office", "tooling", "fast_change", "solo_work" },
                            SalaryMin = 14000,
                            SalaryMax = 22000,
                            Difficulty = 58,
                            WorkIntensity = 45,
                            PromotionBase = 12,
                            QuitRiskBase = 8,
                            Requirements = new[]
                            {
                                new StatRequirement { StatId = PaperShiftWorkerAttributes.Major, MinValue = 55, Weight = 3 },
                                new StatRequirement { StatId = PaperShiftWorkerAttributes.Education, MinValue = 50, Weight = 2 },
                                new StatRequirement { StatId = PaperShiftWorkerAttributes.Ability, MinValue = 45, Weight = 1 }
                            }
                        }
                    }
                }
            };
        }

        private static GameEventDefinition[] CreateEvents()
        {
            return new[]
            {
                new GameEventDefinition
                {
                    Id = "resume_audit",
                    DisplayName = "简历追问",
                    Body = "面试官追问了简历里最夸张的一段经历。",
                    Phase = GameEventPhase.Interview,
                    NoticeType = EventNoticeType.Modal,
                    BaseWeight = 25,
                    Conditions = new[] { new ConditionDefinition { Kind = ConditionKind.ResumeRiskAtLeast, IntValue = 20 } },
                    Options = new[]
                    {
                        Option("confess", "坦白解释", Effect(EffectKind.AddInterviewProgress, "", -8), Effect(EffectKind.AddStress, "", 4), Log("你解释了包装痕迹，面试官有点失望。")),
                        Option("double_down", "继续圆谎", Effect(EffectKind.AddInterviewProgress, "", 10), Effect(EffectKind.AddResumeRisk, "", 15), Log("你暂时圆过去了，但被识破的风险更高。")),
                        Option("caught", "被当场识破", Effect(EffectKind.AddInterviewProgress, "", -25), AddTag("dishonest_resume"), Banner("你获得了负面标签：不诚实"))
                    }
                },
                new GameEventDefinition
                {
                    Id = "ai_shortcut",
                    DisplayName = "工具捷径",
                    Body = "你发现一个自动化工具，能让今天的活快很多。",
                    Phase = GameEventPhase.Probation,
                    NoticeType = EventNoticeType.Log,
                    BaseWeight = 18,
                    Conditions = new[] { new ConditionDefinition { Kind = ConditionKind.HasTag, Key = "ai_pioneer" } },
                    Options = new[]
                    {
                        Option("use", "谨慎使用", Effect(EffectKind.AddPromotionProgress, "", 12), Effect(EffectKind.AddStress, "", -3), Log("AI先行者标签发挥了作用，主管注意到了效率提升。")),
                        Option("teach", "教给同事", Effect(EffectKind.AddStat, PaperShiftWorkerAttributes.Ability, 4), Effect(EffectKind.AddPromotionProgress, "", 6), Log("你教会了同事，团队关系更好了。"))
                    }
                },
                new GameEventDefinition
                {
                    Id = "overtime_fire",
                    DisplayName = "周末救火",
                    Body = "项目周末出了问题，主管希望你立刻上线处理。",
                    Phase = GameEventPhase.Probation,
                    NoticeType = EventNoticeType.Modal,
                    BaseWeight = 22,
                    CooldownYears = 2,
                    Options = new[]
                    {
                        Option("work", "接下救火", Effect(EffectKind.AddPromotionProgress, "", 10), Effect(EffectKind.AddStress, "", 12), Log("你扛住了周末救火，升职进度上涨。")),
                        Option("refuse", "拒绝加班", Effect(EffectKind.AddQuitRisk, "", 9), Effect(EffectKind.AddStress, "", -5), Log("你保住了休息，但离职风险上升。"))
                    }
                },
                new GameEventDefinition
                {
                    Id = "stress_breakdown",
                    DisplayName = "压力过载",
                    Body = "你已经撑到了极限，身体和情绪都在发出警告。",
                    Phase = GameEventPhase.Any,
                    NoticeType = EventNoticeType.Modal,
                    BaseWeight = 8,
                    Conditions = new[] { new ConditionDefinition { Kind = ConditionKind.StressAtLeast, IntValue = 90 } },
                    Options = new[]
                    {
                        Option("rest", "强制休息", Effect(EffectKind.AddStress, "", -35), Effect(EffectKind.DirectFail, "", 0, "你不得不暂停这份机会，先把自己拉回来。")),
                        Option("lie_flat", "直接躺平", Effect(EffectKind.AddStress, "", -55), AddTag("lying_flat"), Effect(EffectKind.ReturnToJobSearch, "", 0), Log("你选择暂时躺平，重新寻找更能承受的工作。")),
                        Option("collapse", "硬扛到底", Effect(EffectKind.EndRun, "", 0, "压力彻底压垮了这一代打工人生。"))
                    }
                },
                new GameEventDefinition
                {
                    Id = "romance_intro",
                    DisplayName = "朋友介绍",
                    Body = "朋友想给你介绍一个很聊得来的人。",
                    Phase = GameEventPhase.Budget,
                    NoticeType = EventNoticeType.Modal,
                    BaseWeight = 12,
                    Conditions = new[] { new ConditionDefinition { Kind = ConditionKind.BudgetAtLeast, Key = "romance", IntValue = 20 } },
                    Options = new[]
                    {
                        Option("date", "去见一面", Effect(EffectKind.AddStress, "", -6), Effect(EffectKind.AddHeir, "", 0, "未来后代", "未知"), Log("你认真经营关系，未来多了一种可能。")),
                        Option("save", "先专注攒钱", Effect(EffectKind.AddMoney, "", 1000), Log("你把时间换成了存款。"))
                    }
                }
            };
        }

        private static TagDefinition Tag(string id, string name, string description, string rarity, TagPolarity polarity, params EffectDefinition[] effects)
        {
            return new TagDefinition
            {
                Id = id,
                DisplayName = name,
                Description = description,
                Scope = TagScope.Worker,
                RarityId = rarity,
                Polarity = polarity,
                Effects = effects
            };
        }

        private static TagDefinition FindTag(TagDefinition[] tags, string id)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                if (tags[i].Id == id)
                {
                    return tags[i];
                }
            }

            return null;
        }

        private static bool HasEvent(GameEventDefinition[] events, string id)
        {
            return FindEvent(events, id) != null;
        }

        private static void MergeTagRuntimeDefaults(TagDefinition[] tags, TagDefinition[] defaults)
        {
            for (var i = 0; i < defaults.Length; i++)
            {
                var target = FindTag(tags, defaults[i].Id);
                if (target == null)
                {
                    continue;
                }

                target.Effects = defaults[i].Effects;
                target.ConditionalEffects = defaults[i].ConditionalEffects;
                target.Polarity = defaults[i].Polarity;
                target.RarityId = defaults[i].RarityId;
            }
        }

        private static void MergeEventRuntimeDefaults(GameEventDefinition[] events, GameEventDefinition[] defaults)
        {
            for (var i = 0; i < defaults.Length; i++)
            {
                var target = FindEvent(events, defaults[i].Id);
                if (target == null)
                {
                    continue;
                }

                target.Phase = defaults[i].Phase;
                target.BaseWeight = defaults[i].BaseWeight;
                target.Conditions = defaults[i].Conditions;
                target.Options = defaults[i].Options;
            }
        }

        private static GameEventDefinition FindEvent(GameEventDefinition[] events, string id)
        {
            if (events == null)
            {
                return null;
            }

            for (var i = 0; i < events.Length; i++)
            {
                if (events[i] != null && events[i].Id == id)
                {
                    return events[i];
                }
            }

            return null;
        }

        private static GameEventDefinition[] Append(GameEventDefinition[] events, GameEventDefinition item)
        {
            if (item == null)
            {
                return events ?? new GameEventDefinition[0];
            }

            if (events == null)
            {
                return new[] { item };
            }

            var result = new GameEventDefinition[events.Length + 1];
            for (var i = 0; i < events.Length; i++)
            {
                result[i] = events[i];
            }

            result[result.Length - 1] = item;
            return result;
        }

        private static ConditionalEffectDefinition Conditional(string[] workTagIds, GameEventPhase phase, params EffectDefinition[] effects)
        {
            return new ConditionalEffectDefinition
            {
                WorkTagIds = workTagIds,
                Phase = phase,
                Effects = effects
            };
        }

        private static WorkTagDefinition WorkTag(string id, string name, string description, params object[] rules)
        {
            var requirements = new System.Collections.Generic.List<WorkRequirementDefinition>();
            var eventWeights = new System.Collections.Generic.List<EventWeightDefinition>();
            for (var i = 0; i < rules.Length; i++)
            {
                if (rules[i] is WorkRequirementDefinition requirement)
                {
                    requirements.Add(requirement);
                }
                else if (rules[i] is EventWeightDefinition eventWeight)
                {
                    eventWeights.Add(eventWeight);
                }
            }

            return new WorkTagDefinition
            {
                Id = id,
                DisplayName = name,
                Description = description,
                Requirements = requirements.ToArray(),
                EventWeights = eventWeights.ToArray()
            };
        }

        private static WorkRequirementDefinition Rule(FitDimension dimension, int minValue, bool hardFail, int recognitionOnPass, int recognitionOnFail, int stressOnPass, int stressOnFail)
        {
            return new WorkRequirementDefinition
            {
                Target = RequirementTarget.FitDimension,
                Dimension = dimension,
                IntValue = minValue,
                HardFail = hardFail,
                RecognitionOnPass = recognitionOnPass,
                RecognitionOnFail = recognitionOnFail,
                StressOnPass = stressOnPass,
                StressOnFail = stressOnFail
            };
        }

        private static EventWeightDefinition EventWeight(string eventId, int delta, ConditionKind condition, int value)
        {
            return new EventWeightDefinition
            {
                EventId = eventId,
                WeightDelta = delta,
                Conditions = new[] { new ConditionDefinition { Kind = condition, IntValue = value } }
            };
        }

        private static EffectDefinition Passive(EffectKind kind, string key, int value)
        {
            return new EffectDefinition { Timing = EffectTiming.Passive, Kind = kind, Key = key, IntValue = value };
        }

        private static EffectDefinition Effect(EffectKind kind, string key, int value, string text = null, string secondary = null)
        {
            return new EffectDefinition { Kind = kind, Key = key, IntValue = value, TextValue = text, SecondaryText = secondary };
        }

        private static EffectDefinition AddTag(string tagId)
        {
            return new EffectDefinition { Kind = EffectKind.AddTag, Key = tagId };
        }

        private static EffectDefinition Log(string text)
        {
            return new EffectDefinition { Kind = EffectKind.AddLog, TextValue = text };
        }

        private static EffectDefinition Banner(string text)
        {
            return new EffectDefinition { Kind = EffectKind.AddBanner, TextValue = text };
        }

        private static EventOptionDefinition Option(string id, string label, params EffectDefinition[] effects)
        {
            return new EventOptionDefinition { Id = id, Label = label, Effects = effects };
        }
    }
}
