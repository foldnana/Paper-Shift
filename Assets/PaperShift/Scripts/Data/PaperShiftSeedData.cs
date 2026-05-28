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
            database.Companies = CreateCompanies();
            database.Events = CreateEvents();
            database.LastNames = new[] { "李", "王", "林", "赵", "陈", "周", "顾", "许" };
            database.MaleFirstNames = new[] { "知行", "安和", "星远", "景明", "修远" };
            database.FemaleFirstNames = new[] { "小满", "君语", "安禾", "清许", "南星" };
            return database;
        }

        private static EraDefinition[] CreateEras()
        {
            return new[]
            {
                new EraDefinition { Id = "agrarian", DisplayName = "古代农耕", StartYear = 1800, EndYear = 1899, Description = "土地、手艺和宗族关系更重要。" },
                new EraDefinition { Id = "industrial", DisplayName = "近代工业", StartYear = 1900, EndYear = 1999, Description = "工厂、纪律和识字能力改变命运。" },
                new EraDefinition { Id = "modern", DisplayName = "现代城市", StartYear = 2000, EndYear = 2099, Description = "学历、技术和城市机会快速流动。" },
                new EraDefinition { Id = "near_future", DisplayName = "近未来智能", StartYear = 2100, EndYear = 2199, Description = "AI 协作和情绪稳定成为核心竞争力。" },
                new EraDefinition { Id = "far_future", DisplayName = "远未来星际", StartYear = 2200, EndYear = 2299, Description = "跨星球岗位带来高薪和高风险。" },
                new EraDefinition { Id = "post_work", DisplayName = "后工作时代", StartYear = 2300, EndYear = 2399, Description = "工作本身变稀缺，身份标签变得更关键。" }
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
                new StatDefinition { Id = "body", DisplayName = "体魄", StartMin = 25, StartMax = 85, Description = "影响健康恢复、体力劳动和高危职业。" },
                new StatDefinition { Id = "literacy", DisplayName = "识字", StartMin = 20, StartMax = 85, Description = "影响学习、考试和文书岗位。" },
                new StatDefinition { Id = "logic", DisplayName = "逻辑", StartMin = 15, StartMax = 90, Description = "影响技术岗位、复杂事件判断。" },
                new StatDefinition { Id = "social", DisplayName = "社交", StartMin = 10, StartMax = 90, Description = "影响面试、关系、销售和恋爱事件。" },
                new StatDefinition { Id = "mental", DisplayName = "精神状态", StartMin = 35, StartMax = 90, Description = "影响抗压和长期工作稳定性。" },
                new StatDefinition { Id = "family", DisplayName = "家境", StartMin = 0, StartMax = 80, Description = "影响初始资产、教育和后代资源。" },
                new StatDefinition { Id = "charm", DisplayName = "魅力", StartMin = 10, StartMax = 90, Description = "影响正面社交事件和特殊机会。" }
            };
        }

        private static TagDefinition[] CreateTags()
        {
            return new[]
            {
                Tag("good_accounting", "会算账", "交易和理财更容易成功，更容易发现账本里的坑。", "normal", TagPolarity.Positive,
                    Passive(EffectKind.PassiveEventWeight, "finance", 15), Passive(EffectKind.PassiveSalaryPercent, "office", 4)),
                Tag("hard_worker", "能吃苦", "体力劳动压力降低，基础职业更稳定。", "normal", TagPolarity.Positive,
                    Passive(EffectKind.PassiveStressPerYear, "physical", -6), Passive(EffectKind.PassiveInterviewScore, "physical", 8)),
                Tag("intro_by_neighbor", "同乡介绍", "更容易获得入行机会，欠人情时压力提高。", "normal", TagPolarity.Mixed,
                    Passive(EffectKind.PassiveInterviewScore, "local", 10)),
                Tag("family_craft", "祖传手艺", "工匠路线成长更快，可传给下一代。", "super_rare", TagPolarity.Positive,
                    Passive(EffectKind.PassiveInterviewScore, "craft", 18), Passive(EffectKind.PassiveEventWeight, "inheritance", 20)),
                Tag("ai_pioneer", "AI先行者", "AI 时代适应大幅提高，更容易解锁新职业。", "rare", TagPolarity.Positive,
                    Passive(EffectKind.PassiveInterviewScore, "ai", 18), Passive(EffectKind.PassiveEventWeight, "ai", 25)),
                Tag("social_anxiety", "社恐", "客户关系更难提升，但独立工作压力降低。", "normal", TagPolarity.Mixed,
                    Passive(EffectKind.PassiveInterviewScore, "sales", -12), Passive(EffectKind.PassiveStressPerYear, "remote", -5)),
                Tag("dishonest_resume", "不诚实", "被发现包装简历后留下的负面标签。", "rare", TagPolarity.Negative,
                    Passive(EffectKind.PassiveInterviewScore, "trust", -18)),
                Tag("remote_first", "远程优先", "更容易刷到远程岗位。", "normal", TagPolarity.Neutral,
                    Passive(EffectKind.PassiveEventWeight, "remote", 15)),
                Tag("ai_intent", "AI/技术岗", "求职时更偏向 AI、技术和新职业。", "rare", TagPolarity.Neutral,
                    Passive(EffectKind.PassiveEventWeight, "ai", 20))
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
                    EraIds = new[] { "modern", "near_future" },
                    TagIds = new[] { "local", "ai" },
                    Jobs = new[]
                    {
                        new JobDefinition
                        {
                            Id = "smart_tractor_operator",
                            DisplayName = "农机操作员",
                            IntentTagIds = new[] { "ai_intent" },
                            TagIds = new[] { "ai", "physical", "local" },
                            SalaryMin = 9000,
                            SalaryMax = 13000,
                            InterviewRounds = 2,
                            Difficulty = 36,
                            WorkIntensity = 48,
                            Requirements = new[]
                            {
                                new StatRequirement { StatId = "body", MinValue = 40, Weight = 2 },
                                new StatRequirement { StatId = "logic", MinValue = 35, Weight = 2 }
                            }
                        }
                    }
                },
                new CompanyDefinition
                {
                    Id = "white_tower_tech",
                    DisplayName = "白塔科技",
                    Industry = "AI 服务",
                    EraIds = new[] { "modern", "near_future" },
                    TagIds = new[] { "ai", "remote" },
                    Jobs = new[]
                    {
                        new JobDefinition
                        {
                            Id = "ai_trainer",
                            DisplayName = "AI训练师",
                            IntentTagIds = new[] { "ai_intent", "remote_first" },
                            TagIds = new[] { "ai", "remote", "office" },
                            SalaryMin = 14000,
                            SalaryMax = 22000,
                            InterviewRounds = 3,
                            Difficulty = 58,
                            WorkIntensity = 45,
                            PromotionBase = 12,
                            QuitRiskBase = 8,
                            Requirements = new[]
                            {
                                new StatRequirement { StatId = "logic", MinValue = 55, Weight = 3 },
                                new StatRequirement { StatId = "literacy", MinValue = 50, Weight = 2 },
                                new StatRequirement { StatId = "mental", MinValue = 45, Weight = 1 }
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
                    Phase = GameEventPhase.WorkYear,
                    NoticeType = EventNoticeType.Log,
                    BaseWeight = 18,
                    Conditions = new[] { new ConditionDefinition { Kind = ConditionKind.HasTag, Key = "ai_pioneer" } },
                    Options = new[]
                    {
                        Option("use", "谨慎使用", Effect(EffectKind.AddPromotionProgress, "", 12), Effect(EffectKind.AddStress, "", -3), Log("AI先行者标签发挥了作用，主管注意到了效率提升。")),
                        Option("teach", "教给同事", Effect(EffectKind.AddStat, "social", 4), Effect(EffectKind.AddPromotionProgress, "", 6), Log("你教会了同事，团队关系更好了。"))
                    }
                },
                new GameEventDefinition
                {
                    Id = "overtime_fire",
                    DisplayName = "周末救火",
                    Body = "项目周末出了问题，主管希望你立刻上线处理。",
                    Phase = GameEventPhase.WorkYear,
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
