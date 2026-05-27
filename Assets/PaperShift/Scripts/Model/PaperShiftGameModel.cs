using System.Collections.Generic;
using UnityEngine;

namespace PaperShift.Model
{
    public enum PaperShiftScreen
    {
        Create,
        Tags,
        Resume,
        JobSearch,
        InterviewFailure,
        Work,
        Budget,
        News,
        Retirement
    }

    public enum PortraitKind
    {
        Worker,
        Male,
        Baby
    }

    public enum TagRarity
    {
        Normal,
        Rare,
        SuperRare
    }

    public sealed class PaperShiftGameModel
    {
        public WorkerData Worker = new WorkerData();
        public List<string> Eras = new List<string>();
        public List<TagData> AvailableTags = new List<TagData>();
        public List<TagData> SelectedTags = new List<TagData>();
        public List<ResumeLineData> ResumeLines = new List<ResumeLineData>();
        public List<string> ResumeIntent = new List<string>();
        public CandidateCardData JobSearchSelf = new CandidateCardData();
        public CandidateCardData JobOffer = new CandidateCardData();
        public CandidateCardData FailureSelf = new CandidateCardData();
        public CandidateCardData FailureOffer = new CandidateCardData();
        public CandidateCardData WorkSelf = new CandidateCardData();
        public CandidateCardData WorkJob = new CandidateCardData();
        public CandidateCardData NewsSelf = new CandidateCardData();
        public CandidateCardData NewsJob = new CandidateCardData();
        public BudgetData Budget = new BudgetData();
        public RetirementData Retirement = new RetirementData();

        public static PaperShiftGameModel CreatePrototype()
        {
            var model = new PaperShiftGameModel();

            model.Worker = new WorkerData
            {
                LastName = "李",
                FirstName = "小满",
                Gender = "女",
                Era = "现代城市",
                Age = 24,
                Body = "常人 4.5分",
                Literacy = "熟练",
                Logic = "57",
                Social = "42",
                Education = "普通本科",
                Family = "小康",
                Advantage = "学习能力强",
                Asset = "启动资金 8000",
                Coin = "2,135"
            };

            model.Eras.AddRange(new[]
            {
                "古代\n农耕", "近代\n工业", "现代\n城市", "近未来\n智能", "远未来\n星际", "后工作\n时代"
            });

            model.AvailableTags.AddRange(new[]
            {
                new TagData("会算账", "交易和理财更容易成功\n更容易发现账本里的坑"),
                new TagData("能吃苦", "体力劳动压力降低\n基础职业更稳定"),
                new TagData("同乡介绍", "更容易获得入行机会\n欠人情时压力提高"),
                new TagData("祖传手艺", "工匠路线成长更快\n可传给下一代", TagRarity.SuperRare),
                new TagData("AI先行者", "AI 时代适应大幅提高\n更容易解锁新职业", TagRarity.Rare),
                new TagData("社恐", "客户关系更难提升\n独立工作压力降低")
            });

            model.SelectedTags.AddRange(new[]
            {
                new TagData("AI先行者", string.Empty, TagRarity.Rare),
                new TagData("会算账", string.Empty),
                new TagData("祖传手艺", string.Empty, TagRarity.SuperRare)
            });

            model.ResumeIntent.AddRange(new[] { "AI/技术岗", "远程优先", "月薪过万" });
            model.ResumeLines.AddRange(new[]
            {
                new ResumeLineData("学历", "普通本科", new[] { "显示", "隐藏", "夸大", "无中生有" }, 0, new[] { ChipTone.Normal, ChipTone.Normal, ChipTone.Warn, ChipTone.Fake }),
                new ResumeLineData("经历", "做过数据标注兼职", new[] { "显示", "隐藏", "夸大成AI项目", "写大厂实习" }, 0, new[] { ChipTone.Normal, ChipTone.Normal, ChipTone.Warn, ChipTone.Fake }),
                new ResumeLineData("能力", "逻辑57 / 社交42", new[] { "显示", "隐藏", "夸大逻辑", "写会管理" }, 0, new[] { ChipTone.Normal, ChipTone.Normal, ChipTone.Warn, ChipTone.Fake }),
                new ResumeLineData("标签", "AI先行者、会算账、祖传手艺", new[] { "显示", "隐藏社恐", "夸大AI经验", "写项目负责人" }, 0, new[] { ChipTone.Normal, ChipTone.Normal, ChipTone.Warn, ChipTone.Fake }),
                new ResumeLineData("薪资", "期望 10000-18000", new[] { "保守", "正常", "虚高", "写已拿Offer" }, 1, new[] { ChipTone.Normal, ChipTone.Normal, ChipTone.Warn, ChipTone.Fake })
            });

            model.JobSearchSelf = new CandidateCardData
            {
                Badge = "24岁",
                Corner = "第1代",
                Name = "李小满",
                Subtitle = "女 求职者",
                RingText = "压力 20",
                Portrait = PortraitKind.Worker,
                RingColor = new Color(0.145f, 0.8f, 0.553f),
                Rows = new[]
                {
                    InfoPair.Normal("体魄", "4.5分"),
                    InfoPair.Normal("逻辑", "57"),
                    InfoPair.Normal("学历", "普通本科"),
                    InfoPair.Normal("家境", "小康"),
                    InfoPair.Rare("优势", "★学习强"),
                    InfoPair.Rare("意向", "★AI技术")
                },
                Tags = new[]
                {
                    new TagData("AI先行者", string.Empty, TagRarity.Rare),
                    new TagData("会算账", string.Empty),
                    new TagData("祖传手艺", string.Empty, TagRarity.SuperRare),
                    new TagData("AI/技术岗", string.Empty, TagRarity.Rare)
                },
                EventLines = new[] { "当前意向：AI/技术岗，更容易刷到新职业、远程岗、技术岗" }
            };

            model.JobOffer = new CandidateCardData
            {
                Badge = "1轮",
                Corner = "面试",
                Name = "云端农场",
                Subtitle = "智慧农业 · 农机操作员",
                RingText = "难度 36",
                Portrait = PortraitKind.Male,
                RingColor = new Color(0.996f, 0.835f, 0.29f),
                Rows = new[]
                {
                    InfoPair.Normal("学历", "不限"),
                    InfoPair.Normal("地点", "城郊"),
                    InfoPair.Normal("要求", "懂AI"),
                    InfoPair.Normal("风险", "算法考核"),
                    InfoPair.Rare("岗位", "★新职业"),
                    InfoPair.Rare("薪资", "★11000")
                },
                Tags = new[]
                {
                    new TagData("包住宿", string.Empty),
                    new TagData("要懂AI", string.Empty),
                    new TagData("郊区通勤", string.Empty)
                },
                Progress = new ProgressData("28.5%", "面试官不满意", 0.45f, "就这份了", PaperShiftScreen.Work, PaperShiftScreen.Work)
            };

            model.FailureSelf = CloneCard(model.JobSearchSelf);
            model.FailureSelf.RingText = "压力 25";
            model.FailureSelf.Rows = new[]
            {
                InfoPair.Normal("体魄", "4.5分"),
                InfoPair.Normal("逻辑", "57"),
                InfoPair.Normal("学历", "普通本科"),
                InfoPair.Normal("家境", "小康"),
                InfoPair.Rare("优势", "★学习强"),
                InfoPair.Rare("资产", "★8000")
            };
            model.FailureSelf.Tags = new[]
            {
                new TagData("AI先行者", string.Empty, TagRarity.Rare),
                new TagData("会算账", string.Empty),
                new TagData("祖传手艺", string.Empty, TagRarity.SuperRare)
            };
            model.FailureSelf.EventLines = new[]
            {
                "面试官认为岗位经验不足，你的压力提升了",
                "你试图解释转行原因，但没有说服对方"
            };

            model.FailureOffer = new CandidateCardData
            {
                Badge = "2轮",
                Corner = "失败",
                Name = "云端农场",
                Subtitle = "农机操作员",
                RingText = "难度 36",
                Portrait = PortraitKind.Male,
                Disabled = true,
                Rows = new[]
                {
                    InfoPair.Normal("要求", "懂AI"),
                    InfoPair.Normal("薪资", "11000"),
                    InfoPair.Normal("结果", "未录用"),
                    InfoPair.Normal("原因", "经验少")
                }
            };

            model.WorkSelf = new CandidateCardData
            {
                Badge = "25岁",
                Corner = "第1代",
                Name = "李小满",
                Subtitle = "女 AI训练师",
                RingText = "压力 38",
                Portrait = PortraitKind.Worker,
                RingColor = new Color(0.145f, 0.8f, 0.553f),
                Rows = new[]
                {
                    InfoPair.Rare("职业", "★AI训练师"),
                    InfoPair.Rare("月薪", "★18000"),
                    InfoPair.Normal("存款", "12600"),
                    InfoPair.Normal("关系", "单身"),
                    InfoPair.Normal("健康", "82"),
                    InfoPair.Normal("前途", "上升")
                },
                EventLines = new[]
                {
                    "面试通过，正式入职 AI 训练师",
                    "入职培训消耗 1200 元，压力提升了",
                    "你的 AI 先行者标签被主管发现了"
                }
            };

            model.WorkJob = new CandidateCardData
            {
                Badge = "入职",
                Corner = "岗位",
                Name = "白塔科技",
                Subtitle = "AI训练师",
                RingText = "强度 45",
                Portrait = PortraitKind.Male,
                RingColor = new Color(0.549f, 0.133f, 0.933f),
                Rows = new[]
                {
                    InfoPair.Rare("岗位", "★新职业"),
                    InfoPair.Rare("月薪", "★18000"),
                    InfoPair.Normal("合同", "1年"),
                    InfoPair.Normal("地点", "远程"),
                    InfoPair.Normal("风险", "更新快"),
                    InfoPair.Normal("前途", "上升")
                },
                Tags = new[]
                {
                    new TagData("AI行业", string.Empty, TagRarity.Rare),
                    new TagData("远程办公", string.Empty),
                    new TagData("审核压力", string.Empty)
                },
                Progress = new ProgressData("2.7%", "被裁风险", 0.09f, "预算", PaperShiftScreen.Budget, PaperShiftScreen.Budget, "#8c22ee", "#ffe64d")
            };

            model.NewsSelf = CloneCard(model.WorkSelf);
            model.NewsSelf.Badge = "27岁";
            model.NewsSelf.RingText = "压力 42";
            model.NewsSelf.Rows = new[]
            {
                InfoPair.Rare("职业", "★AI训练师"),
                InfoPair.Rare("月薪", "★18000"),
                InfoPair.Normal("存款", "28600"),
                InfoPair.Normal("前途", "上升")
            };
            model.NewsSelf.Tags = new[]
            {
                new TagData("AI先行者", string.Empty, TagRarity.Rare),
                new TagData("远程办公", string.Empty),
                new TagData("审核压力", string.Empty)
            };
            model.NewsSelf.EventLines = new string[0];

            model.NewsJob = new CandidateCardData
            {
                Badge = "2033",
                Corner = "岗位",
                Name = "人机团队协调员",
                Subtitle = "新职业机会",
                RingText = "风口 55",
                Portrait = PortraitKind.Male,
                RingColor = new Color(0.996f, 0.835f, 0.29f),
                Rows = new[]
                {
                    InfoPair.Normal("门槛", "管理"),
                    InfoPair.Normal("风险", "高压"),
                    InfoPair.Rare("月薪", "★26000"),
                    InfoPair.Rare("机会", "★稀有")
                },
                Tags = new[]
                {
                    new TagData("要管理", string.Empty),
                    new TagData("懂AI", string.Empty, TagRarity.Rare),
                    new TagData("压力高", string.Empty)
                }
            };

            model.Budget = new BudgetData
            {
                Salary = "18000",
                Rent = "4500",
                Distributable = "13500",
                Items = new[]
                {
                    new BudgetItem("饮食", 28, "#249ee8"),
                    new BudgetItem("住房", 33, "#8c22ee"),
                    new BudgetItem("恋爱", 13, "#ff6aa0"),
                    new BudgetItem("教育", 12, "#52c8be"),
                    new BudgetItem("存款", 14, "#f0b33b")
                },
                Impacts = new[]
                {
                    new ImpactData("恋爱事件", "+13%", "#ff4f8f"),
                    new ImpactData("结婚事件", "+8%", "#ff4f8f"),
                    new ImpactData("生子事件", "+5%", "#ff8a00"),
                    new ImpactData("后代成长", "+12%", "#249ee8")
                },
                Notes = new[]
                {
                    new TagData("饮食高", "健康恢复更快，但存款变慢"),
                    new TagData("恋爱预算", "越高越容易触发约会、结婚、生子事件", TagRarity.Rare),
                    new TagData("教育投入", "越高，下一代学历、技能和初始标签越好")
                }
            };

            model.Retirement = RetirementData.CreatePrototype();

            return model;
        }

        private static CandidateCardData CloneCard(CandidateCardData source)
        {
            return new CandidateCardData
            {
                Badge = source.Badge,
                Corner = source.Corner,
                Name = source.Name,
                Subtitle = source.Subtitle,
                RingText = source.RingText,
                Portrait = source.Portrait,
                RingColor = source.RingColor,
                Rows = source.Rows,
                Tags = source.Tags,
                EventLines = source.EventLines,
                Progress = source.Progress,
                Disabled = source.Disabled
            };
        }
    }

    public sealed class WorkerData
    {
        public string LastName;
        public string FirstName;
        public string Gender;
        public string Era;
        public int Age;
        public string Body;
        public string Literacy;
        public string Logic;
        public string Social;
        public string Education;
        public string Family;
        public string Advantage;
        public string Asset;
        public string Coin;

        public string FullName => LastName + FirstName;
    }

    public sealed class TagData
    {
        public string Name;
        public string Description;
        public TagRarity Rarity;

        public TagData(string name, string description, TagRarity rarity = TagRarity.Normal)
        {
            Name = name;
            Description = description;
            Rarity = rarity;
        }
    }

    public enum ChipTone
    {
        Normal,
        Warn,
        Fake
    }

    public sealed class ResumeLineData
    {
        public string Label;
        public string Value;
        public string[] Actions;
        public int ActiveIndex;
        public ChipTone[] Tones;

        public ResumeLineData(string label, string value, string[] actions, int activeIndex, ChipTone[] tones)
        {
            Label = label;
            Value = value;
            Actions = actions;
            ActiveIndex = activeIndex;
            Tones = tones;
        }
    }

    public struct InfoPair
    {
        public string Label;
        public string Value;
        public bool IsRare;

        public static InfoPair Normal(string label, string value)
        {
            return new InfoPair { Label = label, Value = value, IsRare = false };
        }

        public static InfoPair Rare(string label, string value)
        {
            return new InfoPair { Label = label, Value = value, IsRare = true };
        }
    }

    public sealed class CandidateCardData
    {
        public string Badge;
        public string Corner;
        public string Name;
        public string Subtitle;
        public string RingText;
        public PortraitKind Portrait;
        public Color RingColor = Color.green;
        public InfoPair[] Rows = new InfoPair[0];
        public TagData[] Tags = new TagData[0];
        public string[] EventLines = new string[0];
        public ProgressData Progress;
        public bool Disabled;
    }

    public sealed class ProgressData
    {
        public string Percent;
        public string Label;
        public float Fill;
        public string Button;
        public PaperShiftScreen ButtonTarget;
        public PaperShiftScreen FallbackTarget;
        public string BarColorHtml;
        public string FillColorHtml;

        public ProgressData(string percent, string label, float fill, string button, PaperShiftScreen buttonTarget, PaperShiftScreen fallbackTarget, string barColorHtml = "#249ee8", string fillColorHtml = "#b7ffff")
        {
            Percent = percent;
            Label = label;
            Fill = fill;
            Button = button;
            ButtonTarget = buttonTarget;
            FallbackTarget = fallbackTarget;
            BarColorHtml = barColorHtml;
            FillColorHtml = fillColorHtml;
        }
    }

    public sealed class BudgetData
    {
        public string Salary;
        public string Rent;
        public string Distributable;
        public BudgetItem[] Items = new BudgetItem[0];
        public ImpactData[] Impacts = new ImpactData[0];
        public TagData[] Notes = new TagData[0];
    }

    public sealed class BudgetItem
    {
        public string Label;
        public int Percent;
        public string ColorHtml;

        public BudgetItem(string label, int percent, string colorHtml)
        {
            Label = label;
            Percent = percent;
            ColorHtml = colorHtml;
        }
    }

    public sealed class ImpactData
    {
        public string Label;
        public string Value;
        public string ColorHtml;

        public ImpactData(string label, string value, string colorHtml)
        {
            Label = label;
            Value = value;
            ColorHtml = colorHtml;
        }
    }

    public sealed class RetirementData
    {
        public string Coin;
        public string Reason;
        public string WorkYears;
        public string FinalJob;
        public string Savings;
        public string MentalState;
        public string[] Reasons;
        public HeirData[] Heirs;
        public TagData[] ChildTags;
        public InfoPair[] ChildRows;

        public static RetirementData CreatePrototype()
        {
            return new RetirementData
            {
                Coin = "2,335",
                Reason = "到龄退休",
                WorkYears = "38 年",
                FinalJob = "AI训练师",
                Savings = "86,000",
                MentalState = "还能聊天",
                Reasons = new[]
                {
                    "到龄退休\n正常进入下一代",
                    "意外死亡\n财产损耗更高",
                    "压力过大\n进精神病院",
                    "工伤退场\n留下赔偿和旧疾"
                },
                Heirs = new[]
                {
                    new HeirData("李君语", "学习好，适合接班", "50%"),
                    new HeirData("李知行", "体魄强，适合高危职业", "25%"),
                    new HeirData("李安禾", "社交高，适合经商", "25%")
                },
                ChildTags = new[]
                {
                    new TagData("本地户籍", string.Empty),
                    new TagData("AI先行者", string.Empty, TagRarity.Rare),
                    new TagData("祖传手艺", string.Empty, TagRarity.SuperRare),
                    new TagData("理智", string.Empty),
                    new TagData("会算账", string.Empty)
                },
                ChildRows = new[]
                {
                    InfoPair.Normal("性别", "女"),
                    InfoPair.Normal("年龄", "-岁"),
                    InfoPair.Normal("年代", "近未来"),
                    InfoPair.Normal("体魄", "3.5分"),
                    InfoPair.Normal("学历", "未知"),
                    InfoPair.Normal("家境", "小康"),
                    InfoPair.Normal("求职", "未开始"),
                    InfoPair.Normal("资产", "待分配"),
                    InfoPair.Normal("压力", "0")
                }
            };
        }
    }

    public sealed class HeirData
    {
        public string Name;
        public string Trait;
        public string Allocation;

        public HeirData(string name, string trait, string allocation)
        {
            Name = name;
            Trait = trait;
            Allocation = allocation;
        }
    }
}
