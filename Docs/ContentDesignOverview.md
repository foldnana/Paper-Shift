# Paper Shift 内容包 v2 设计概览

最后更新：2026-06-10

## 适配新机制

v2 内容包针对六项机制重新设计：

1. **事件一代一次**：CooldownYears=0 的事件每代只触发一次，不再无限循环。只有 `overtime_fire`(cooldown=2) 和 `weekend_overtime`(cooldown=1) 可重复出现。
2. **简历包装条件系统**：新增 Kind 20-26 条件，可精确判断字段隐藏/夸大/伪造状态。
3. **夸大简历影响流程**：experience、salary 字段接入 FitProfile 兼容逻辑，夸大给有限初始认可加成但增加风险。
4. **动作限定条件**：事件和条件效果可用 `ActionIs` 限定在 `AttendInterview`、`WorkProbation`、`ApplyRegularization` 等具体操作上。
5. **试用时间条件**：事件可用 `CurrentJobMonthsAtMost` / `CurrentJobMonthsAtLeast` 限定当前工作开始后的月份，避免“第一天”事件在后期出现。
6. **流程小插曲**：普通流程按钮后的反馈由 `FlowMoments` 提供，文本描述发生了什么，认可度和压力变化只通过进度条体现。

申请转正会保留事件可能性，但普通事件会先经过频率控制。转正概率越高，越容易直接进入转正成功/失败结算；强制事件仍会优先触发。

## 平衡性核心改动

### 不存在免费午餐

| 旧版 | v2 |
|------|-----|
| mentor_help +10认可 -6压力 | 删除 |
| boss_appreciation +8认可 -4压力 | 删除 |
| forced_rest -20压力 -6认可 | -12压力 -10认可 |
| quit_urge -25压力 | -15压力（放弃机会） |
| lucky_break +10认可 -5压力 | +6认可 -3压力 |

### 简历包装专属事件（7个）

| 事件 | 条件 | 核心惩罚 |
|------|------|----------|
| hidden_age_caught | ResumeFieldHidden(age) | 隐瞒年龄被追问 |
| hidden_education_probed | ResumeFieldHidden(education) | 学历空白被反复问 |
| exaggerated_salary_check | ResumeFieldExaggerated(salary) | 夸大薪资要流水 |
| exaggerated_experience_grilled | ResumeFieldExaggerated(experience) | 夸大经历被深挖 |
| any_hidden_field_suspicion | AnyResumeFieldHidden | 简历有疑点被逐项核对 |
| any_exaggerated_field_audit | AnyResumeFieldExaggerated + strict_rules | 合规审查夸大痕迹 |
| hidden_tag_discovered | AnyResumeTagHidden | 隐藏标签被同事发现 |

### 低薪工作隐性税

| 旧版 | v2 |
|------|-----|
| 保安 4000-6000 | 3800-5500 |
| 服务员 4500-6500 | 4200-6000 |
| 后半生低薪只减生育率 | 新增 low_income_health_decay（健康加速衰退） |
| 无 | 新增 low_income_no_pension（存不下钱，跨代压力） |

### 标签隐性代价

| 标签 | 加成 | 代价 |
|------|------|------|
| quick_learner | professionalism+3, execution+2 | PassiveStressPerYear+1 |
| smooth_talker | communication+5, presence+2 | resume_audit权重+6 |
| good_look | presence+7, communication+1 | credentials-2 |
| perfectionist | execution+4, professionalism+3 | PassiveStressPerYear+4 |
| family_burden | execution+3 | resilience-4, PassiveStressPerYear+3 |

### 简历风险跨代传播

| 后半生规则 | 条件 | 跨代影响 |
|------------|------|----------|
| resume_risk_shadow | ResumeRisk>=40 + 55%概率 | 家庭声誉-12, 生活风险+14, 继承broken_home标签 |
| resume_risk_caught | ResumeRisk>=60 + 35%概率 | 家庭声誉-18, 生活风险+20, 家庭稳定-14, 继承second_gen_migrant标签 |

## 内容统计

| 类型 | 种子数据 | v2内容包 | 合计 |
|------|----------|----------|------|
| 人物标签 | 15 | 17 | 32 |
| 工作标签 | 12 | 8 | 20 |
| 公司 | 2 | 19 | 21 |
| 事件 | 5 | 38 | 43 |
| 流程小插曲 | 13 | 13 | 13（同 ID 覆盖） |
| 后半生规则 | 13 | 17 | 30 |

## 事件冷却设计

| 事件 | CooldownYears | 设计理由 |
|------|---------------|----------|
| overtime_fire | 2 | 加火救急是反复出现的场景，允许每2年再触发 |
| weekend_overtime | 1 | 加班压力中等，每年可能再来 |
| 其余36个 | 0（一代一次） | 每次遭遇不可逆，增加策略深度 |

## 数值设计参考

- AddRecognition: 常规 ±4~6，强事件 ±8~10，极端 ±15~16
- AddStress: 常规 +3~8，强事件 +10~14，降压力 -12~-15（但有代价）
- AddResumeRisk: +4~16，夸大风险高于隐藏
- FitScore: 被动加成 ±2~7，不再出现纯正面无代价标签
- 后半生生活风险: +8~20，简历造假被揭发最高
