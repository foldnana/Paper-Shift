# Paper Shift 内容包编写规则

最后更新：2026-06-10

本文档用于给设计者和其他 AI 编写 Paper Shift 的外部内容包。内容包先写成 `.pspack` 文本，再通过 `Tools/PaperShiftContentTool` 转成 Unity 可导入的 JSON。

工具也支持反向转换：把 `PaperShiftContentPack.json` 导入工具后，可以转回 `.pspack` 文本继续编辑。反向转换会规范化格式，不保证和原始文本逐字一致。

## 1. 基本原则

- 内容服务于“入职即结算”的短流程，不设计面试轮数和试用期长度。
- 岗位刷新默认接近随机，只有人物标签、事件或特殊规则影响岗位权重。
- 人物标签必须有通用效果，不能只在某一种工作标签命中时才生效。
- 工作标签描述工作环境、要求、风险和事件倾向，不要写成某一个具体岗位专属逻辑。
- 事件必须能改变流程或状态，不要只写无效果日志。
- 后半生规则要同时考虑奖励和代价，高分也应可能带来压力、期待、风险或家庭问题。
- 所有 ID 使用英文小写、数字和下划线，例如 `night_shift_warning`。

## 2. 文件结构

一个内容块以 `@类型 ID` 开始，以 `@end` 结束。

示例：

```text
@tag pressure_absorber
name: 情绪稳定
description: 遇到压力时不容易被带着走。
rarity: rare
polarity: Positive
effect: passive AddFitScore resilience 6
@end
```

顶层字段：

```text
MergeMode: merge
```

`merge` 会把同 ID 内容覆盖到现有数据库中；`replace` 会用内容包替换对应数据。

注意：`.pspack` 里使用 `AddStress`、`Interview`、`Positive` 这类可读枚举名；工具导出的 JSON 会自动转成 Unity 更稳妥的数字枚举。

## 3. 支持的内容块

### 3.1 人物标签 `@tag`

常用字段：

- `name`: 显示名。
- `description`: 描述。
- `rarity`: `normal`、`rare`、`super_rare`。
- `polarity`: `Neutral`、`Positive`、`Negative`、`Mixed`。
- `effect`: 通用效果。
- `conditional`: 条件效果组。
- `conditionalEffect`: 写在最近一个 `conditional` 下面。
- `conditionalCondition`: 写在最近一个 `conditional` 下面。

示例：

```text
@tag hard_worker_plus
name: 特别能熬
description: 能顶住重活，但长期会积累身体和情绪代价。
rarity: rare
polarity: Mixed
effect: passive AddFitScore resilience 8
effect: passive PassiveStressPerYear int=2
conditional: phase=Any workTags=physical,high_pressure
conditionalEffect: AddRecognition int=8
conditionalEffect: AddStress int=-4
@end
```

### 3.2 工作标签 `@workTag`

常用字段：

- `name`: 显示名。
- `description`: 描述。
- `requirement`: 工作要求，可读适配画像或原始属性。
- `effect`: 工作标签直接产生的流程效果。
- `eventWeight`: 修改事件权重。
- `eventWeightCondition`: 写在最近一个 `eventWeight` 下面。

示例：

```text
@workTag appearance_sensitive
name: 形象要求
description: 会明显看重外在、表达和第一印象。
requirement: target=FitDimension dimension=Presence int=58 passRecognition=7 failRecognition=-9 failStress=4
eventWeight: event=appearance_question delta=16
eventWeightCondition: kind=RecognitionAtMost int=55
@end
```

`requirement` 参数：

- `target`: `FitDimension`、`RawAttribute`、`WorkerTag`。
- `dimension`: `Maturity`、`Physique`、`Presence`、`Credentials`、`Professionalism`、`Execution`、`Communication`、`Resilience`。
- `key`: 当 `target=RawAttribute` 或 `WorkerTag` 时使用。
- `op`: `Equal`、`NotEqual`、`GreaterOrEqual`、`Greater`、`LessOrEqual`、`Less`。
- `int`: 阈值。
- `hard`: 是否硬失败。
- `passRecognition` / `failRecognition`: 通过或不通过时的认可度变化。
- `passStress` / `failStress`: 通过或不通过时的压力变化。
- `failEvent`: 不满足时提高触发的事件 ID。

### 3.3 事件 `@event`

常用字段：

- `name`: 事件名。
- `body`: 弹窗正文。
- `phase`: `Any`、`Interview`、`Probation`、`WorkYear`、`Budget`、`Retirement`。
- `notice`: `Log`、`Banner`、`Modal`。
- `baseWeight`: 基础权重。
- `condition`: 触发条件。
- `effect`: 事件本体效果。消息事件和观看型弹窗常用它。
- `option`: 选项。
- `optionCondition`: 写在最近一个 `option` 下面。
- `optionEffect`: 写在最近一个 `option` 下面。

示例：

```text
@event age_question
name: 年龄追问
body: 面试官开始追问为什么现在才来应聘这个岗位。
phase: Interview
notice: Modal
baseWeight: 18
condition: kind=AgeAtLeast int=35
option: id=explain label=解释转行原因 checkpoint=true
optionEffect: AddRecognition int=6 text="解释让对方稍微理解了你的处境。"
optionEffect: AddStress int=4
option: id=avoid label=含糊带过 checkpoint=true
optionEffect: AddResumeRisk int=12 text="回避让简历风险上升。"
optionEffect: AddRecognition int=-7
@end
```

事件分三种展示方式：

- 消息事件：`notice: Log` 或 `Banner`，不写 `option`，直接结算 `effect`，只在主界面消息里显示。
- 观看事件：`notice: Modal`，不写 `option`，弹窗展示正文，玩家点继续后结算 `effect`。
- 选择事件：`notice: Modal` 并写 `option`，玩家选择后结算对应 `optionEffect`。

任何主要动作都会从符合条件的事件池里抽取一个事件。普通消息事件应占较高权重，弹窗/选择事件用较低权重控制频率。

事件默认一代只触发一次。如果事件需要隔几年重复出现，设置 `cooldown` 为大于 0 的值。如果事件是高频消息事件，可以设置 `cooldown: -1`，表示不限次数重复。

准备面试、参加面试、试用期工作、申请转正都会触发事件抽取。事件用 `ActionIs` 限定具体动作：准备面试用 `ActionIs PrepareInterview`，正式面试用 `ActionIs AttendInterview`，努力工作用 `ActionIs WorkProbation`，申请转正用 `ActionIs ApplyRegularization`。事件选项结算不会自动再抽普通事件，只有 `TriggerEvent` 等显式效果才会引发后续事件。

提到隐藏、隐瞒信息的事件，必须加 `ResumeFieldHidden`、`AnyResumeFieldHidden`、`ResumeTagHidden` 或 `AnyResumeTagHidden` 条件。提到夸大、虚报、伪造的事件，必须加 `ResumeFieldExaggerated`、`AnyResumeFieldExaggerated`、`ResumeFieldMode` 或 `ResumeRiskAtLeast` 条件。

消息事件示例：

```text
@event probation_small_mistake
name: 小错误
body: 一个小错误被指出来了，问题不大，但他得把坑补上。
phase: Probation
notice: Log
baseWeight: 120
cooldown: -1
condition: kind=ActionIs text=WorkProbation
condition: kind=RecognitionAtMost int=75
effect: AddRecognition int=-4
effect: AddStress int=5
@end
```

事件内容建议：

- 高频消息事件：权重建议 70-140，文本只写发生了什么，不直接写 `+5%` 或概率变化。
- 观看型弹窗：权重建议 8-25，适合重要但不需要选择的插曲。
- 选择事件：权重建议 5-25，适合简历被查、过劳危机、客户投诉升级等有明显分支的内容。

### 3.4 后半生规则 `@lifeRule`

后半生规则用于入职结算后，推演这一代后来的生活，并影响下一代。

常用字段：

- `name`: 规则名。
- `priority`: 越小越早执行。
- `condition`: 触发条件。
- `effect`: 后半生效果。

示例：

```text
@lifeRule high_income_expectation_cost
name: 高收入家庭的期待代价
priority: 140
condition: ScoreAtLeast income 80
condition: RandomChance chance=0.45
effect: AddFamilyStability int=8
effect: AddLifePressure int=10
effect: AddHeirStress int=8
effect: AddHeirTag key=parent_expectation
effect: AddMilestone text="补课" secondary="竞争加剧"
effect: AddStoryFragment text="条件变好了，但比较和期待也一起变多。"
@end
```

### 3.7 公司和岗位 `@company`

示例：

```text
@company late_light_logistics
name: 长明物流
industry: 城市配送
founded: 2024-06
tags: local,high_pressure
job: id=night_dispatcher name=夜班调度员 tags=night_shift,high_pressure,customer_contact salary=8000..12000 difficulty=48 threshold=70 intensity=66
jobRequirement: stat=ability min=45 weight=2
@end
```

岗位参数：

- `id`: 岗位 ID。
- `name`: 岗位显示名。
- `tags`: 工作标签列表。
- `salary`: `最低..最高`。
- `difficulty`: 难度。
- `threshold`: offer 或入职认可度阈值。
- `intensity`: 工作强度。
- `promotion`: 晋升基础值。

## 4. 条件写法

普通事件和流程条件使用：

```text
condition: kind=StressAtLeast int=80
condition: kind=JobHasTag key=high_pressure
condition: kind=RandomChance chance=0.35
```

可用条件：

- `Always`
- `Stat`
- `HasTag`
- `MissingTag`
- `BudgetAtLeast`
- `BudgetAtMost`
- `Phase`
- `AgeAtLeast`
- `AgeAtMost`
- `MoneyAtLeast`
- `CompanyHasTag`
- `JobHasTag`
- `RandomChance`
- `EventSeen`
- `WorkYearsAtLeast`
- `RecognitionAtLeast`
- `RecognitionAtMost`
- `ResumeRiskAtLeast`
- `StressAtLeast`
- `StressAtMost`
- `ResumeFieldMode`
- `ResumeFieldHidden`
- `ResumeFieldExaggerated`
- `AnyResumeFieldHidden`
- `AnyResumeFieldExaggerated`
- `ResumeTagHidden`
- `AnyResumeTagHidden`
- `ActionIs`
- `CurrentJobMonthsAtLeast`
- `CurrentJobMonthsAtMost`

简历包装条件：

- `ResumeFieldHidden`：指定字段被隐藏，`key` 填字段 ID，例如 `age`、`education`、`ability`。
- `ResumeFieldExaggerated`：指定字段被夸大或伪造。
- `AnyResumeFieldHidden`：任意基础信息被隐藏。
- `AnyResumeFieldExaggerated`：任意基础信息被夸大或伪造。
- `ResumeTagHidden`：指定人物标签被隐藏，`key` 填标签 ID。
- `AnyResumeTagHidden`：任意人物标签被隐藏。
- `ResumeFieldMode`：指定字段处于某种包装模式，`key` 填字段 ID，`text` 可填 `Hide`、`Normal`、`Exaggerate`、`Fake`。

流程动作和时间条件：

- `ActionIs`：限定事件或条件效果只在某个操作上生效。`text` 或 `key` 可填 `PrepareInterview`、`AttendInterview`、`WorkProbation`、`ApplyRegularization`、`EventChoice`。
- `CurrentJobMonthsAtLeast`：当前试用工作开始后至少经过多少个月。第一份试用工作开始后，第一次点击“努力工作”结算时通常为 1。
- `CurrentJobMonthsAtMost`：当前试用工作开始后最多经过多少个月。写“第一天”“刚入职”这类事件时，应配合 `ActionIs WorkProbation` 和 `CurrentJobMonthsAtMost 1` 使用。

示例：

```text
condition: kind=ResumeFieldHidden key=age
condition: kind=AnyResumeFieldExaggerated
condition: kind=ResumeFieldMode key=education text=Exaggerate
condition: kind=AnyResumeTagHidden
condition: kind=ActionIs text=ApplyRegularization
condition: kind=CurrentJobMonthsAtMost int=1
```

后半生条件使用：

```text
condition: ScoreAtLeast income 70
condition: WorkerAgeAtLeast int=35
condition: JobHasTag ai
condition: RandomChance chance=0.28
```

可用后半生条件：

- `Always`
- `ScoreAtLeast`
- `ScoreAtMost`
- `WorkerAgeAtLeast`
- `WorkerAgeAtMost`
- `StressAtLeast`
- `StressAtMost`
- `ResumeRiskAtLeast`
- `WorkerStatAtLeast`
- `WorkerStatAtMost`
- `HasWorkerTag`
- `CompanyHasTag`
- `JobHasTag`
- `EventSeen`
- `RandomChance`
- `LaterLifeValueAtLeast`
- `LaterLifeValueAtMost`

## 5. 效果写法

普通效果：

```text
effect: AddRecognition int=6
effect: AddStress int=-8
effect: AddResumeRisk int=12
effect: AddTag key=lying_flat
effect: TriggerEvent key=stress_breakdown
effect: DirectFail text="这次机会失败了。"
effect: ReturnToJobSearch text="他决定换一份工作。"
effect: EndRun text="这一代打工人生结束了。" end=StressCollapse
```

人物标签的通用被动效果：

```text
effect: passive AddFitScore execution 4
effect: passive PassiveEventWeight stress_breakdown -10
effect: passive AddJobWeight key=ai int=8
```

可用普通效果：

- `AddStat`
- `SetStat`
- `AddStress`
- `AddHealth`
- `AddTag`
- `RemoveTag`
- `AddHeir`
- `AddLog`
- `AddBanner`
- `AddRecognition`
- `SetRecognition`
- `AddResumeRisk`
- `EndRun`
- `AddFitScore`
- `AddJobWeight`
- `AddEventWeight`
- `TriggerEvent`
- `DirectPass`
- `DirectFail`
- `ReturnToJobSearch`
- `PassiveSalaryPercent`
- `PassiveEventWeight`
- `PassiveStressPerYear`

后半生效果：

- `AddFamilyStability`
- `AddEducationResource`
- `AddIndustryInsight`
- `AddLifePressure`
- `AddParentCare`
- `AddFamilyReputation`
- `AddLifeRisk`
- `AddSpecialOpportunity`
- `AddChildChance`
- `AddChildCount`
- `AddHeirStat`
- `AddHeirStress`
- `AddHeirTag`
- `AddMilestone`
- `AddStoryFragment`
- `AddWorkYears`
- `AddProspectScore`
- `AddPressureScore`

## 6. AI 生成内容的硬规则

让其他 AI 写内容时，可以直接给它这段要求：

```text
请为 Paper Shift 生成 .pspack 内容包。
必须使用英文小写下划线 ID。
每个 @tag 必须至少有 1 个通用 effect，不能只有 conditionalEffect。
工作标签要抽象成环境或要求，不要只服务某一个岗位。
事件统一使用 @event。消息事件用 notice=Log，不写 option，必须至少有 1 个 effect。
弹窗选择事件用 notice=Modal，必须至少有 1 个 option，每个 option 至少有 1 个 optionEffect。
每个主要动作都要有若干高权重消息事件，并用 ActionIs 限定动作；文本只写发生了什么，不直接写数值变化。
后半生规则必须包含随机性、代价或分支，不要只给纯奖励。
不要设计面试轮次、试用期长度、启动资金、求职意向。
输出纯文本，不要输出 Markdown 解释。
```

## 7. 推荐数值范围

- `AddRecognition`: 常规 `-10` 到 `+10`，强事件可到 `±20`。
- `AddStress`: 常规 `-10` 到 `+12`，危机事件可到 `+25`。
- `AddResumeRisk`: 常规 `+5` 到 `+20`。
- `AddFitScore`: 常规 `-8` 到 `+8`。
- `AddJobWeight`: 常规 `-20` 到 `+25`。
- `AddEventWeight`: 常规 `-20` 到 `+30`。
- `RandomChance`: 建议 `0.15` 到 `0.55`。

数值越大越容易破坏平衡。新内容最好先从小数值开始。
