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
            database.FlowMoments = CreateFlowMoments();
            database.LaterLifeRules = CreateLaterLifeRules();
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
                database.Tags = MergeTagRuntimeDefaults(database.Tags, CreateTags());
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

            if (database.FlowMoments == null || database.FlowMoments.Length == 0)
            {
                database.FlowMoments = CreateFlowMoments();
            }

            if (database.LaterLifeRules == null || database.LaterLifeRules.Length == 0)
            {
                database.LaterLifeRules = CreateLaterLifeRules();
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
                    Passive(EffectKind.AddFitScore, "professionalism", 4)),
                Tag("ai_influence", "AI耳濡目染", "从小接触工具化工作，对新工具更敏感。", "rare", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "professionalism", 5),
                    Passive(EffectKind.AddFitScore, "execution", 3)),
                Tag("city_life", "城市生活", "成长资源更稳定，但生活成本和比较压力也更明显。", "normal", TagPolarity.Mixed,
                    Passive(EffectKind.AddFitScore, "communication", 2),
                    Passive(EffectKind.AddFitScore, "resilience", -1)),
                Tag("stable_family", "稳定家庭", "家庭节奏较稳，抗压和沟通更自然。", "normal", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "resilience", 4),
                    Passive(EffectKind.AddFitScore, "communication", 2)),
                Tag("parent_expectation", "父母期望", "从小被要求不能掉队，执行力高，但压力风险也高。", "rare", TagPolarity.Mixed,
                    Passive(EffectKind.AddFitScore, "execution", 5),
                    Passive(EffectKind.AddFitScore, "resilience", -5),
                    Passive(EffectKind.PassiveEventWeight, "stress_breakdown", 10)),
                Tag("upward_seed", "逆袭苗子", "资源不多，但有罕见的上冲势头。", "super_rare", TagPolarity.Positive,
                    Passive(EffectKind.AddFitScore, "execution", 8),
                    Passive(EffectKind.AddFitScore, "professionalism", 6),
                    Passive(EffectKind.AddFitScore, "resilience", 4)),
                Tag("early_mature", "早熟", "很早就理解生活的压力，做事稳但不太松弛。", "normal", TagPolarity.Mixed,
                    Passive(EffectKind.AddFitScore, "maturity", 6),
                    Passive(EffectKind.AddFitScore, "resilience", 2),
                    Passive(EffectKind.AddFitScore, "communication", -2))
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
            FindTag(tags, "ai_influence").Conditions = InheritanceOnlyConditions();
            FindTag(tags, "ai_influence").ConditionalEffects = new[]
            {
                Conditional(new[] { "ai", "tooling", "fast_change" }, GameEventPhase.Any,
                    Effect(EffectKind.AddJobWeight, "", 12),
                    Effect(EffectKind.AddRecognition, "", 5),
                    Effect(EffectKind.AddEventWeight, "ai_shortcut", 8))
            };
            FindTag(tags, "city_life").Conditions = InheritanceOnlyConditions();
            FindTag(tags, "stable_family").Conditions = InheritanceOnlyConditions();
            FindTag(tags, "parent_expectation").Conditions = InheritanceOnlyConditions();
            FindTag(tags, "upward_seed").Conditions = InheritanceOnlyConditions();
            FindTag(tags, "early_mature").Conditions = InheritanceOnlyConditions();

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
                    FoundedYear = 2019,
                    FoundedMonth = 4,
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
                    FoundedYear = 2023,
                    FoundedMonth = 9,
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
                        CheckedOption("confess", "坦白解释", Effect(EffectKind.AddRecognition, "", -8), Effect(EffectKind.AddStress, "", 4), Log("你解释了包装痕迹，面试官有点失望。")),
                        CheckedOption("double_down", "继续圆谎", Effect(EffectKind.AddRecognition, "", 10), Effect(EffectKind.AddResumeRisk, "", 15), Log("你暂时圆过去了，但被识破的风险更高。")),
                        CheckedOption("caught", "被当场识破", Effect(EffectKind.AddRecognition, "", -25), AddTag("dishonest_resume"), Banner("你获得了负面标签：不诚实"))
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
                    CooldownYears = 1,
                    Conditions = new[] { new ConditionDefinition { Kind = ConditionKind.HasTag, Key = "ai_pioneer" } },
                    Options = new[]
                    {
                        Option("use", "谨慎使用", Effect(EffectKind.AddRecognition, "", 12), Effect(EffectKind.AddStress, "", -3), Log("AI先行者标签发挥了作用，主管注意到了效率提升。")),
                        Option("teach", "教给同事", Effect(EffectKind.AddStat, PaperShiftWorkerAttributes.Ability, 4), Effect(EffectKind.AddRecognition, "", 6), Log("你教会了同事，团队关系更好了。"))
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
                        CheckedOption("work", "接下救火", Effect(EffectKind.AddRecognition, "", 10), Effect(EffectKind.AddStress, "", 12), Log("你扛住了周末救火，认可度上涨。")),
                        CheckedOption("refuse", "拒绝加班", Effect(EffectKind.AddRecognition, "", -9), Effect(EffectKind.AddStress, "", -5), Log("你保住了休息，但主管对你的认可下降。"))
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
                        CheckedOption("rest", "强制休息", Effect(EffectKind.AddStress, "", -35), Effect(EffectKind.DirectFail, "", 0, "你不得不暂停这份机会，先把自己拉回来。")),
                        CheckedOption("lie_flat", "直接躺平", Effect(EffectKind.AddStress, "", -55), AddTag("lying_flat"), Effect(EffectKind.ReturnToJobSearch, "", 0), Log("你选择暂时躺平，重新寻找更能承受的工作。")),
                        CheckedOption("collapse", "硬扛到底", Effect(EffectKind.EndRun, "", 0, "压力彻底压垮了这一代打工人生。"))
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

        private static FlowMomentDefinition[] CreateFlowMoments()
        {
            return new[]
            {
                Moment("prepare_company_notes", "PrepareInterview", "他把公司和岗位信息重新梳理了一遍，回答时心里有了底。", 12, null,
                    Effect(EffectKind.AddRecognition, "", 4),
                    Effect(EffectKind.AddStress, "", 1)),
                Moment("prepare_overthinking", "PrepareInterview", "资料越查越多，他反而开始担心自己准备错了方向。", 8,
                    Conditions(Condition(ConditionKind.StressAtLeast, 45)),
                    Effect(EffectKind.AddRecognition, "", -2),
                    Effect(EffectKind.AddStress, "", 5)),
                Moment("prepare_mock_answer", "PrepareInterview", "他提前把一段经历讲顺了，至少不再像背稿。", 10, null,
                    Effect(EffectKind.AddRecognition, "", 3),
                    Effect(EffectKind.AddStress, "", -1)),
                Moment("interview_clear_case", "AttendInterview", "他把一个具体案例讲清楚了，面试官开始追问细节。", 12, null,
                    Effect(EffectKind.AddRecognition, "", 6),
                    Effect(EffectKind.AddStress, "", 2)),
                Moment("interview_stumble", "AttendInterview", "问到关键细节时他卡了一下，气氛短暂地冷了下来。", 10, null,
                    Effect(EffectKind.AddRecognition, "", -5),
                    Effect(EffectKind.AddStress, "", 5)),
                Moment("interview_honest_limit", "AttendInterview", "他承认自己有一块短板，但顺手讲了补救办法。", 8,
                    Conditions(Condition(ConditionKind.RecognitionAtMost, 65)),
                    Effect(EffectKind.AddRecognition, "", 3),
                    Effect(EffectKind.AddStress, "", 3)),
                Moment("probation_clean_delivery", "WorkProbation", "今天交出去的活很干净，主管没有多说，但明显少皱了几次眉。", 12, null,
                    Effect(EffectKind.AddRecognition, "", 5),
                    Effect(EffectKind.AddStress, "", 4)),
                Moment("probation_small_miss", "WorkProbation", "一个小错误被指出来了，问题不大，但他得把坑补上。", 12, null,
                    Effect(EffectKind.AddRecognition, "", -4),
                    Effect(EffectKind.AddStress, "", 5)),
                Moment("probation_helped_by_peer", "WorkProbation", "旁边的老员工顺手提醒了一句，他少走了一段弯路。", 7,
                    Conditions(Condition(ConditionKind.RecognitionAtMost, 70)),
                    Effect(EffectKind.AddRecognition, "", 4),
                    Effect(EffectKind.AddStress, "", -2)),
                Moment("probation_expectation_rises", "WorkProbation", "前几天表现不错后，主管开始把更麻烦的活也交给他。", 8,
                    Conditions(Condition(ConditionKind.RecognitionAtLeast, 75)),
                    Effect(EffectKind.AddRecognition, "", 2),
                    Effect(EffectKind.AddStress, "", 7)),
                Moment("regularization_materials_checked", "ApplyRegularization", "申请递上去后，人事把材料转给主管确认。", 12, null,
                    Effect(EffectKind.AddStress, "", 2)),
                Moment("regularization_recent_work_speaks", "ApplyRegularization", "最近几次稳定交付帮他说了话，主管的态度缓和了一些。", 8,
                    Conditions(Condition(ConditionKind.RecognitionAtLeast, 65)),
                    Effect(EffectKind.AddRecognition, "", 3),
                    Effect(EffectKind.AddStress, "", 1)),
                Moment("regularization_extra_question", "ApplyRegularization", "临到转正前，主管又补问了一个之前没谈清楚的问题。", 8, null,
                    Effect(EffectKind.AddRecognition, "", -3),
                    Effect(EffectKind.AddStress, "", 4))
            };
        }

        private static LaterLifeRuleDefinition[] CreateLaterLifeRules()
        {
            return new[]
            {
                LifeRule("high_income_home", "收入转化为稳定生活", 10,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.ScoreAtLeast, "income", 70)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddFamilyStability, "", 10),
                        LifeEffect(LaterLifeEffectKind.AddEducationResource, "", 6),
                        LifeEffect(LaterLifeEffectKind.AddLifePressure, "", 4),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "买房", "家境稳定"))),
                LifeRule("excellent_fit_family", "适配高带来更稳的家庭节奏", 20,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.ScoreAtLeast, "fit", 75)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddParentCare, "", 8),
                        LifeEffect(LaterLifeEffectKind.AddLifePressure, "", -8),
                        LifeEffect(LaterLifeEffectKind.AddChildChance, "", 10),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "stable_family", 0))),
                LifeRule("high_score_parent_expectation", "成功家庭的竞争压力", 30,
                    LifeConditions(
                        LifeCondition(LaterLifeConditionKind.ScoreAtLeast, "total", 3800),
                        LifeChance(0.55f)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddLifePressure, "", 12),
                        LifeEffect(LaterLifeEffectKind.AddParentCare, "", -8),
                        LifeEffect(LaterLifeEffectKind.AddHeirStress, "", 10),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "parent_expectation", 0),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "补课", "竞争加剧"),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "家里条件变好了，但比较和期待也一起变多。"))),
                LifeRule("city_success_cost", "高收入城市生活的代价", 40,
                    LifeConditions(
                        LifeCondition(LaterLifeConditionKind.ScoreAtLeast, "income", 80),
                        LifeChance(0.35f)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddFamilyStability, "", -6),
                        LifeEffect(LaterLifeEffectKind.AddLifePressure, "", 8),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "city_life", 0),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "房贷", "城市变贵"))),
                LifeRule("good_prospect_industry_insight", "高前景岗位带来行业见识", 50,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.ScoreAtLeast, "prospect", 72)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddIndustryInsight, "", 12),
                        LifeEffect(LaterLifeEffectKind.AddFamilyReputation, "", 6),
                        LifeEffect(LaterLifeEffectKind.AddChildChance, "", 5))),
                LifeRule("good_prospect_turbulence", "风口行业的波动", 60,
                    LifeConditions(
                        LifeCondition(LaterLifeConditionKind.ScoreAtLeast, "prospect", 78),
                        LifeChance(0.35f)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddLifeRisk, "", 12),
                        LifeEffect(LaterLifeEffectKind.AddProspectScore, "", -8),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "early_mature", 0),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "转向", "风口变脸"),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "行业不是一直往上，后来也经历过几次风口变脸。"))),
                LifeRule("ai_work_lineage", "AI岗位影响下一代", 70,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.JobHasTag, "ai", 0)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddIndustryInsight, "", 12),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "ai_influence", 0),
                        LifeEffect(LaterLifeEffectKind.AddHeirStat, PaperShiftWorkerAttributes.Major, 5),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "孩子很早就听过工具、模型和自动化这些词。"))),
                LifeRule("late_settle_down", "太晚稳定下来", 80,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.WorkerAgeAtLeast, "", 35)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddChildChance, "", -32),
                        LifeEffect(LaterLifeEffectKind.AddLifePressure, "", 8),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "晚定", "稳定来迟"),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "稳定来得有点晚，很多人生选择都被压缩了。"))),
                LifeRule("high_pressure_home", "高压工作影响家庭氛围", 90,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.StressAtLeast, "", 65)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddLifePressure, "", 10),
                        LifeEffect(LaterLifeEffectKind.AddParentCare, "", -10),
                        LifeEffect(LaterLifeEffectKind.AddHeirStress, "", 8),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "parent_expectation", 0),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "忙碌", "很少闲下"))),
                LifeRule("low_income_hard_start", "收入低导致家庭起步艰难", 100,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.ScoreAtMost, "income", 45)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddFamilyStability, "", -14),
                        LifeEffect(LaterLifeEffectKind.AddEducationResource, "", -6),
                        LifeEffect(LaterLifeEffectKind.AddChildChance, "", -18),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "钱一直不宽裕，很多计划只能往后拖。"))),
                LifeRule("poor_miracle_child", "低资源家庭的小概率翻盘后代", 110,
                    LifeConditions(
                        LifeCondition(LaterLifeConditionKind.ScoreAtMost, "income", 45),
                        LifeChance(0.28f)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddSpecialOpportunity, "", 35),
                        LifeEffect(LaterLifeEffectKind.AddChildCount, "", 1),
                        LifeEffect(LaterLifeEffectKind.AddHeirStat, PaperShiftWorkerAttributes.Ability, 16),
                        LifeEffect(LaterLifeEffectKind.AddHeirStat, PaperShiftWorkerAttributes.Education, 10),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "upward_seed", 0),
                        LifeEffect(LaterLifeEffectKind.AddMilestone, "", 0, "贵人", "改写路径"),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "但低处也会冒出意外的火星，一个孩子抓住了罕见的机会。"))),
                LifeRule("resume_risk_shadow", "简历风险留下处事阴影", 120,
                    LifeConditions(LifeCondition(LaterLifeConditionKind.ResumeRiskAtLeast, "", 45)),
                    LifeEffects(
                        LifeEffect(LaterLifeEffectKind.AddFamilyReputation, "", -8),
                        LifeEffect(LaterLifeEffectKind.AddLifeRisk, "", 10),
                        LifeEffect(LaterLifeEffectKind.AddHeirTag, "early_mature", 0),
                        LifeEffect(LaterLifeEffectKind.AddStoryFragment, "", 0, "那次包装简历的经验后来成了家里偶尔会提起的提醒。")))
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

        private static ConditionDefinition[] InheritanceOnlyConditions()
        {
            return new[]
            {
                new ConditionDefinition { Kind = ConditionKind.EventSeen, Key = "__inheritance_only__" }
            };
        }

        private static bool HasEvent(GameEventDefinition[] events, string id)
        {
            return FindEvent(events, id) != null;
        }

        private static TagDefinition[] MergeTagRuntimeDefaults(TagDefinition[] tags, TagDefinition[] defaults)
        {
            var result = new System.Collections.Generic.List<TagDefinition>();
            if (tags != null)
            {
                result.AddRange(tags);
            }

            for (var i = 0; i < defaults.Length; i++)
            {
                var target = FindTag(result.ToArray(), defaults[i].Id);
                if (target == null)
                {
                    result.Add(defaults[i]);
                    continue;
                }

                target.Effects = defaults[i].Effects;
                target.ConditionalEffects = defaults[i].ConditionalEffects;
                target.Polarity = defaults[i].Polarity;
                target.RarityId = defaults[i].RarityId;
                target.Conditions = defaults[i].Conditions;
            }

            return result.ToArray();
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

        private static EventOptionDefinition CheckedOption(string id, string label, params EffectDefinition[] effects)
        {
            return new EventOptionDefinition { Id = id, Label = label, Effects = effects, RunCheckpointAfterChoice = true };
        }

        private static FlowMomentDefinition Moment(string id, string action, string text, int weight, ConditionDefinition[] conditions, params EffectDefinition[] effects)
        {
            return new FlowMomentDefinition
            {
                Id = id,
                DisplayName = id,
                Text = text,
                Action = action,
                BaseWeight = weight,
                Conditions = conditions ?? new ConditionDefinition[0],
                Effects = effects ?? new EffectDefinition[0]
            };
        }

        private static ConditionDefinition[] Conditions(params ConditionDefinition[] conditions)
        {
            return conditions;
        }

        private static ConditionDefinition Condition(ConditionKind kind, int value, string key = null, string text = null)
        {
            return new ConditionDefinition { Kind = kind, Key = key, IntValue = value, TextValue = text };
        }

        private static LaterLifeRuleDefinition LifeRule(
            string id,
            string name,
            int priority,
            LaterLifeConditionDefinition[] conditions,
            LaterLifeEffectDefinition[] effects)
        {
            return new LaterLifeRuleDefinition
            {
                Id = id,
                DisplayName = name,
                Priority = priority,
                Conditions = conditions,
                Effects = effects
            };
        }

        private static LaterLifeConditionDefinition[] LifeConditions(params LaterLifeConditionDefinition[] conditions)
        {
            return conditions;
        }

        private static LaterLifeConditionDefinition LifeCondition(LaterLifeConditionKind kind, string key, int value)
        {
            return new LaterLifeConditionDefinition { Kind = kind, Key = key, IntValue = value };
        }

        private static LaterLifeConditionDefinition LifeChance(float chance)
        {
            return new LaterLifeConditionDefinition { Kind = LaterLifeConditionKind.RandomChance, FloatValue = chance };
        }

        private static LaterLifeEffectDefinition[] LifeEffects(params LaterLifeEffectDefinition[] effects)
        {
            return effects;
        }

        private static LaterLifeEffectDefinition LifeEffect(LaterLifeEffectKind kind, string key, int value, string text = null, string secondary = null)
        {
            return new LaterLifeEffectDefinition { Kind = kind, Key = key, IntValue = value, TextValue = text, SecondaryText = secondary };
        }
    }
}
