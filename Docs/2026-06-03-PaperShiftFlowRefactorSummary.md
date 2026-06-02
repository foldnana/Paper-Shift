# PaperShift 流程与 GameService 重构总结

日期：2026-06-03

## 改动目标

本次修改主要解决两个问题：

1. 面试、试用期、事件选项后的结算逻辑分散，容易出现状态去向不一致。
2. `PaperShiftGameService` 过大，生成劳动者、找工作、面试、试用期、事件、退休、传承等逻辑全部混在一个类里，后续维护成本太高。

## 认可度系统整理

- 删除了旧的面试轮次、面试进度、试用期进度、离职风险等分裂字段。
- 将面试阶段和试用期阶段统一使用 `Recognition` 表示认可度。
- `InterviewState` 使用 `Recognition` 判断是否达到 Offer 门槛。
- `CurrentJobState` 使用 `Recognition` 表示试用期转正概率/认可度。
- 条件判断和效果应用统一改为 `RecognitionAtLeast` / `RecognitionAtMost`、`AddRecognition` / `SetRecognition`。

这样后续事件、按钮、状态栏、UI 展示都不需要再区分“面试满意度”“试用进度”“转正进度”等多个概念。

## 事件选项后的统一结算

- 给 `EventOptionDefinition` 增加 `RunCheckpointAfterChoice`。
- 增加 `CheckedOption` 辅助创建需要统一结算的事件选项。
- 事件选项可以选择是否进入 checkpoint。
- 对需要 checkpoint 的选项，事件效果会先转成 `FlowRuleResult`，再走统一结算逻辑。
- `DirectPass`、`DirectFail`、`ReturnToJobSearch`、`EndRun` 现在可以带上指令文案和结束原因。

这让事件选项不再只是“改几个数值就结束”，而是可以完整闭环到继续、失败、进入试用期、入职结算、重新找工作、结束本代等去向。

## 统一 Checkpoint Resolver

新增 `FlowCheckpointResolver`，集中处理这些动作：

- 准备面试
- 参加面试
- 努力工作
- 申请入职
- 事件选项后的结算

它负责：

- 找到当前阶段对应的公司和岗位。
- 计算规则流、事件效果、行为带来的认可度和压力变化。
- 统一处理直接成功、直接失败、回到求职、结束本代。
- 在普通推进后触发事件。
- 将结果转换成 `InterviewStepResult` / `ProbationStepResult`。

`PaperShiftGameService` 现在只保留对外入口，把具体 checkpoint 计算委托给 `FlowCheckpointResolver`。

## 劳动者生命周期拆分

新增 `WorkerLifecycleResolver`，专门处理：

- 随机生成劳动者。
- 入职成功后结算本代。
- 退休或异常结束本代。
- 生成下一代候选孩子。
- 从孩子中选择下一代并开启新一轮。
- 统一退休/结束原因文案。

这部分原本也堆在 `PaperShiftGameService` 里，现在独立出来后，后续要扩展“人生总结”“孩子初始标签”“传承规则”会更清楚。

## 结果类型拆分

新增 `PaperShiftStepResults.cs`，把这些结果类型从 `PaperShiftGameService` 底部移出：

- `InterviewStepOutcome`
- `InterviewStepResult`
- `ProbationStepOutcome`
- `ProbationStepResult`
- `TriggeredEvent`
- `EventOptionChoiceResult`

这些是流程返回值，不属于 Service 行为本体。拆出去后，`PaperShiftGameService` 的结构更干净。

## Presenter 和 UI 同步

- `PaperShiftGamePresenter` 增加事件选项结算后的统一导航。
- 事件选项后会根据 checkpoint 结果进入新闻、工作、找工作、简历或退休界面。
- 面试阶段 UI 改为显示面试认可度。
- 试用期 UI 改为显示试用认可度/转正概率。
- 底部状态栏在试用期隐藏面试状态，显示试用期认可度。
- 修复创建劳动者界面和选择标签界面缺少 `PaperShift.Model` 引用导致的编译问题。

## GameService 瘦身结果

`PaperShiftGameService` 从约 1778 行降到约 904 行。

现在它主要负责：

- 对外提供游戏流程 API。
- 协调数据库、条件、效果、规则、checkpoint、生命周期模块。
- 保留公共入口，避免 Presenter 层大规模改动。

核心复杂逻辑已经拆分到：

- `FlowCheckpointResolver`
- `WorkerLifecycleResolver`
- `FlowCheckpointResult`
- `PaperShiftStepResults`

## 验证

已执行：

```powershell
dotnet build "Paper Shift.sln" --no-restore
```

结果：

- 0 个警告
- 0 个错误

## 后续建议

下一步可以继续拆：

- `EventResolver`：把事件触发、事件查找、可选项筛选从 `PaperShiftGameService` 移出。
- `JobSearchResolver`：把岗位抽取、初始认可度、薪资抽取移出。
- `ResumeResolver`：把简历包装、隐藏标签、包装风险计算移出。

这样 `PaperShiftGameService` 最终可以压到一个很舒服的门面类，只负责串联模块，不再承载规则细节。
