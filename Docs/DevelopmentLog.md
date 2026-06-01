# Paper Shift 开发记录

最后更新：2026-06-01

## 项目定位

Paper Shift 是一个 Unity UI 原型项目，核心方向是“打工人生 / 求职模拟 / 代际选择”。

当前主流程是短流程：

1. 创建劳动者。
2. 选择初始标签。
3. 编辑和包装简历。
4. 投递并推进面试。
5. 进入试用期。
6. 申请入职成功后立刻结算这一代。
7. 在人生总结页选择下一代。

目前明确不做“正式工作多年经营”的长流程。预算、年度工作、退休等旧逻辑可以作为历史原型保留，但主链路以“入职即结算”为准。

## 当前项目结构

- `Assets/Scenes/PaperShiftUI.unity`：当前手工维护的主场景。
- `Assets/PaperShift/Scripts/Domain`：运行时状态、人物档案、标签实例、预算、面试、工作、结算等纯数据结构。
- `Assets/PaperShift/Scripts/Data`：年代、属性、标签、公司、岗位、事件等定义，以及默认种子数据。
- `Assets/PaperShift/Scripts/Runtime`：核心规则服务、条件判断、效果结算、权重随机。
- `Assets/PaperShift/Scripts/Presenter`：Unity UI 绑定、页面刷新、视图引用、自定义图形组件。
- `Assets/PaperShift/Scripts/Controller`：页面切换控制。
- `Assets/PaperShift/Scripts/Editor`：场景引用安装、场景引用校验、数据库资产生成、历史场景生成器。

## 已确认玩法约束

- 新开一局回到第一代时固定从 2026 年开始。
- 第二代及后续世代从上一代结算后的时间继续向后推进，不能重置回 2026 年。
- 不再使用年代/时代作为运行时玩法分支；旧的 era 数据只作为历史原型和兼容字段保留。
- 不需要面试轮数配置，面试推进仍按当前短流程文案和概率表现。
- 不再继承资金或金币，下一代初始金币仍为 0。
- 人物一开始没有启动资金属性；金币只是独立资源，不承担出身表达，家境、教育、能力等属性承担出身表达。
- 入职结算页的金币获得量使用结算字段显示，不用月薪临时代替。
- 入职成功后立即结算本代，并进入人生总结 / 下一代选择。

## UI 维护约定

整页、卡片、按钮、状态区、弹层、固定版式都必须在 Unity 编辑器场景中提前创建好。运行时代码只负责：

- 显示和隐藏已有对象。
- 填充已有文本。
- 修改已有控件状态。
- 绑定已有按钮行为。
- 从已保存的 prefab 创建重复项。

允许运行时创建的 UI 仅限“数据驱动重复项”，例如：

- 标签选择行 prefab。
- 简历标签项 prefab。
- 人物卡标签 prefab。
- 空槽 prefab。
- 日志行 prefab 或场景中预置的日志行模板。

不允许运行时临时拼装具体 UI 结构，例如 `new GameObject` 后再补 `Text`、`Outline`、自定义 Graphic 来兜底生成界面。缺 prefab 或缺场景引用时，应明确报警并让编辑器场景补齐。

## 场景引用工具

手工调整 UI 层级或 Inspector 引用后，使用：

- `Paper Shift/Install Scene View References`

它会把现有场景层级重新绑定到运行时组件，但不负责重建场景。

检查当前场景引用是否完整，使用：

- `Paper Shift/Validate Scene View References`

它只做校验和 Console 提示，不修改场景。重点检查：

- `PaperShiftSceneController.ScreenViews`
- `PaperShiftPrototypeBinder`
- 页面 Binder
- 主玩法 `PaperShiftGameplayViewReferences`
- 允许运行时实例化的 prefab 引用

## 数据来源约定

当前运行时内容的权威来源是：

- `Assets/PaperShift/Scripts/Data/PaperShiftSeedData.cs`
- `Assets/PaperShift/Scripts/Data/PaperShiftDefinitions.cs`

`Assets/PaperShift/Scripts/Model/PaperShiftGameModel.cs` 是历史 UI 原型数据，只在 Unity Editor 下编译，服务于已退役的场景生成器。新增标签、岗位、事件、属性时，不再扩写 `PaperShiftGameModel.CreatePrototype()`。

## 当前重点风险

- 旧的预算和年度工作逻辑仍存在，但主链路不使用。后续可以删除、隐藏，或转为未来长流程实验分支。
- 部分 Editor 安装器仍依赖 GameObject 名称查找。稳定下来的手工 UI 页面应逐步迁移到显式 `ViewReferences`。
- 场景引用和 prefab 引用是 Play Mode 风险集中点，开发前建议先跑一次 `Validate Scene View References`。
- 当前还没有 EditMode 测试。Runtime 层大多是纯 C#，适合先补“第一代 2026、后续世代年份向后推进、初始金币为 0、无继承资金、入职即结算”等规则测试。

## 建议下一步

1. 在 Unity 中运行 `Paper Shift/Validate Scene View References`。
2. Play Mode 验证主链路：创建劳动者、选标签、投简历、推进面试、试用期、申请入职、人生总结、选择下一代。
3. 检查场景里仍显示“年代切换”的交互是否需要在 UI 上隐藏或改成纯展示，运行时代码不再依赖时代分支。
4. 为 Runtime 补一组轻量 EditMode 测试，锁住这次确认过的核心规则。
