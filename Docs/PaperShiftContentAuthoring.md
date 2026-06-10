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

事件选项建议至少带一个状态变化或流程变化。常用效果见第 5 节。

事件默认一代只触发一次。如果事件需要重复出现，设置 `cooldown` / `CooldownYears` 为大于 0 的值。

普通流程事件不是每次操作必定检查成功。参加面试、试用期工作、申请转正、事件选项结算都会先按当前动作计算一次普通事件触发率。申请转正时，当前转正概率越高，普通事件越不容易打断；高压力、高简历风险仍会提高事件触发率。强制事件，例如 `TriggerEvent`、压力满值、极高简历风险审查，会优先于这个频率限制。

提到隐藏、隐瞒信息的事件，必须加 `ResumeFieldHidden`、`AnyResumeFieldHidden`、`ResumeTagHidden` 或 `AnyResumeTagHidden` 条件。提到夸大、虚报、伪造的事件，必须加 `ResumeFieldExaggerated`、`AnyResumeFieldExaggerated`、`ResumeFieldMode` 或 `ResumeRiskAtLeast` 条件。

### 3.4 流程小插曲 `@flowMoment`

流程小插曲用于普通流程按钮后的日常反馈，不弹出选项。它负责把“认可度/压力变化”包装成“发生了什么事”。进度条继续显示数值变化，文本不要直接写 `+5%`、`概率提高` 这类机械说明。

常用字段：

- `name`: 小插曲名。
- `text`: 玩家看到的反馈文本。
- `action`: `PrepareInterview`、`AttendInterview`、`WorkProbation`、`ApplyRegularization`、`EventChoice`。
- `baseWeight`: 基础权重。
- `condition`: 触发条件。
- `effect`: 这条小插曲带来的效果。

示例：

```text
@flowMoment probation_small_mistake
name: 小错误
text: 一个小错误被指出来了，问题不大，但他得把坑补上。
action: WorkProbation
baseWeight: 12
condition: kind=RecognitionAtMost int=75
effect: AddRecognition int=-4
effect: AddStress int=5
@end
```

流程小插曲和事件的分工：

- 小插曲：高频、无选项、只展示一段反馈文本，适合“今天表现不错”“回答卡壳”“主管临时追问”。
- 事件：低频、有选项、有明显分支，适合“简历被查”“过劳危机”“客户投诉升级”。

### 3.5 后半生规则 `@lifeRule`

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

### 3.6 公司和岗位 `@company`

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
事件必须至少有 1 个 option，每个 option 至少有 1 个 optionEffect。
流程小插曲使用 @flowMoment，必须写 action、text 和至少 1 个 effect；text 只写发生了什么，不直接写数值变化。
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
