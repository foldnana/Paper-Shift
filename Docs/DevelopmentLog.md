# Paper Shift 开发记录

最后更新：2026-05-29

## 当前项目结构

Paper Shift 是一个 Unity UI 原型项目，核心方向是人生/工作模拟玩法。当前主要开发场景是：

- `Assets/Scenes/PaperShiftUI.unity`

代码结构大致如下：

- `Domain`：运行时状态、枚举、人物档案等纯数据结构。
- `Data`：标签、公司、岗位、事件等种子数据定义。
- `Runtime`：核心玩法规则、条件判断、效果结算、权重随机、运行时兜底启动。
- `Presenter`：Unity UI 绑定、界面刷新、自定义图形组件、视图引用组件。
- `Controller`：页面切换控制。
- `Editor`：场景生成器和数据库生成工具。

## 当前关键场景组件

现在场景中已经显式挂载运行时组件，不再只依赖 RuntimeBootstrap 动态添加：

- `UI Controller`
  - `PaperShiftSceneController`
  - `PaperShiftGamePresenter`
  - `PaperShiftPrototypeBinder`

- `主要玩法界面`
  - `PaperShiftScreenView`
  - `PaperShiftGameplayViewReferences`

`PaperShiftPrototypeBinder.GameplayView` 已经在场景中直接引用到 `主要玩法界面` 上的 `PaperShiftGameplayViewReferences`。

## 主玩法 UI 绑定规则

主玩法界面后续不要再写按 GameObject 名字查找物体的代码。

主玩法界面的 UI 连接统一通过这个组件维护：

- `Assets/PaperShift/Scripts/Presenter/PaperShiftGameplayViewReferences.cs`

当前重要引用包括：

- `SelfCard`
- `JobCard`
- `SelfTagsRoot`
- `JobTransition`
- `StartInterviewButton`
- `ReapplyButton`
- `StartWorkButton`
- `JobProgressButton`

如果之后调整主玩法 UI 层级、改名、换按钮，只需要在 Inspector 里更新这些引用，不应该在代码里新增字符串查找。

## 已完成内容

### Job Card 状态过渡

`State Transition Overlay` 已经存在于场景中的 Job Card 下，不是在运行时创建。

`PaperShiftJobCardTransition` 已改为使用序列化引用：

- `Overlay`
- `Ribbon`
- `IconText`
- `TitleText`
- `DetailText`

它支持两种播放方式：

- `Show(...)`：用代码传入本次状态文案。
- `ShowPreauthored()`：恢复并播放场景中预先写好的文案。

`再投一家` 流程使用的是场景预写内容，播放完过渡后再刷新新的 Job Card。

### 再投一家流程

当前设计流程是：

`ReapplyButton.onClick -> BeginReapplyJobWithTransition -> ReapplyJobAfterTransition -> ShowPreauthored Overlay -> 等待过渡时间 -> Presenter.FindInterviewAndShow -> RefreshAll`

主要实现位置：

- `Assets/PaperShift/Scripts/Presenter/PaperShiftPrototypeBinder.cs`

### 人物卡标签显示

Self Card 的标签区由以下引用驱动：

- `PaperShiftGameplayViewReferences.SelfTagsRoot`
- `PaperShiftCandidateTagGridView`

显示规则：

- 显示当前人物身上的 `State.Worker.Tags`。
- 最多 12 个格子。
- 已有标签使用 `Assets/PaperShift/Prefab/标签.prefab`。
- 空位使用 `Assets/PaperShift/Prefab/Empty Slot.prefab`。

`PaperShiftGameplayViewReferences.IsComplete(...)` 会检查主玩法引用是否完整。缺引用时会在 Console 中明确提示缺哪个字段，避免静默失败。

### 共享主玩法界面

当前场景是一个“主要玩法界面”承载多个逻辑状态，而不是每个状态都有独立界面。

相关逻辑：

- `PaperShiftSceneController.CurrentScreen` 记录当前请求显示的逻辑页面。
- 如果场景里没有单独的 Work 或 InterviewFailure 页面，会回落显示 JobSearch 对应的主玩法界面。

## 当前已知风险

### 其它页面仍有原型期名字查找

目前重点清理的是主玩法链路。

其它页面里仍然有一些按名字查找 UI 的代码，例如：

- `PaperShiftPrototypeBinder`
- `PaperShiftTagSelectionView`
- `PaperShiftResumeTagListView`

这些对原型/生成式界面还可以暂时接受，但如果对应 UI 后续也要手动维护，就应该继续迁移成显式引用组件。

建议做法：

- 每个稳定下来的手工 UI 页面建立自己的 `ViewReferences` 组件。
- 一次迁移一个页面。
- 不要继续往手工维护的界面里增加名字查找。

### SceneBuilder 与手工场景存在差异

`PaperShiftSceneBuilder` 仍然会生成旧结构和旧命名，比如：

- `Primary Bottom Action`
- `Secondary Bottom Action`

如果从 SceneBuilder 重新生成场景，可能覆盖当前手工调整过的 UI 和显式引用。

后续需要二选一：

- 更新 SceneBuilder，让它也生成新的显式引用组件。
- 或者把 `PaperShiftUI.unity` 当作手工维护的主场景，暂时不要用 SceneBuilder 重建它。

### 仍需要 Play Mode 实测

本地编译检查已经通过，但当前问题主要集中在 Unity 场景引用和 Play Mode 行为上。

继续开发前建议在 Unity 中按顺序验证：

- 选择开局标签。
- 进入主玩法界面。
- 检查 Self Card 标签是否显示当前人物标签。
- 点击 `button 再投一家`。
- 检查已有 `State Transition Overlay` 是否播放。
- 检查过渡后是否刷新出新的 Job Card。

## 后续开发约定

- 手工维护的 UI 必须优先使用序列化引用。
- 代码负责状态、数据绑定、显隐控制，不负责猜场景层级。
- 场景中已经做好的状态视觉，代码只做显示/隐藏和必要的数据刷新。
- 改 UI 名字或层级时，同步更新 Inspector 引用。
- 新增 `ViewReferences` 组件时，建议提供 `IsComplete(...)` 检查，缺引用时明确报错。
- 不要让 `PaperShiftPrototypeBinder` 无限膨胀；后续应该逐步拆成更小的页面 Binder。

## 建议下一步

1. 在 Unity Play Mode 中完整验证主玩法链路。
2. 如果 SceneBuilder 还会继续使用，更新它，让它生成 `PaperShiftGameplayViewReferences`。
3. 将 `PaperShiftPrototypeBinder` 逐步拆成页面级 Binder。
4. 当标签选择页、简历页、预算页 UI 稳定后，也迁移到显式引用方式。
5. 增加一个轻量的 Editor 校验工具，用来检查场景关键引用是否缺失。
