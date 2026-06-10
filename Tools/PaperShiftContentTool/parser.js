(function (root, factory) {
  if (typeof module === "object" && module.exports) {
    module.exports = factory();
  } else {
    root.PaperShiftContentParser = factory();
  }
})(typeof self !== "undefined" ? self : (typeof globalThis !== "undefined" ? globalThis : this), function () {
  "use strict";

  const packTemplate = () => ({
    Format: "PaperShiftContentPack",
    Version: 1,
    MergeMode: "merge",
    Rarities: [],
    Stats: [],
    Tags: [],
    WorkTags: [],
    Companies: [],
    Events: [],
    FlowMoments: [],
    LaterLifeRules: [],
    LastNames: [],
    MaleFirstNames: [],
    FemaleFirstNames: []
  });

  const effectTemplate = () => ({
    Timing: "Immediate",
    Kind: "None",
    Key: "",
    IntValue: 0,
    FloatValue: 0,
    TextValue: "",
    SecondaryText: "",
    TagScope: "Worker",
    EndReason: "None",
    Temporary: false,
    DurationYears: 0
  });

  const conditionTemplate = () => ({
    Kind: "Always",
    Key: "",
    Operator: "GreaterOrEqual",
    IntValue: 0,
    FloatValue: 0,
    TextValue: "",
    Invert: false
  });

  const laterLifeConditionTemplate = () => ({
    Kind: "Always",
    Key: "",
    IntValue: 0,
    FloatValue: 0,
    Invert: false
  });

  const laterLifeEffectTemplate = () => ({
    Kind: "None",
    Key: "",
    IntValue: 0,
    TextValue: "",
    SecondaryText: ""
  });

  const flowMomentTemplate = id => ({
    Id: id,
    DisplayName: id,
    Text: "",
    Action: "",
    BaseWeight: 10,
    Conditions: [],
    Effects: []
  });

  const enumMaps = {
    TagScope: ["Worker", "Company", "Job", "Heir", "Intent", "Event"],
    TagPolarity: ["Neutral", "Positive", "Negative", "Mixed"],
    GameEventPhase: ["Any", "Interview", "Probation", "WorkYear", "Budget", "Retirement"],
    EventNoticeType: ["Log", "Banner", "Modal"],
    FitDimension: ["Maturity", "Physique", "Presence", "Credentials", "Professionalism", "Execution", "Communication", "Resilience"],
    RequirementTarget: ["FitDimension", "RawAttribute", "WorkerTag", "WorkTag", "ResumeField"],
    FlowDirective: ["None", "DirectPass", "DirectFail", "EndRun", "ReturnToJobSearch"],
    CompareOperator: ["Equal", "NotEqual", "GreaterOrEqual", "Greater", "LessOrEqual", "Less"],
    ConditionKind: [
      "Always",
      "Stat",
      "HasTag",
      "MissingTag",
      "BudgetAtLeast",
      "BudgetAtMost",
      "Phase",
      "AgeAtLeast",
      "AgeAtMost",
      "MoneyAtLeast",
      "CompanyHasTag",
      "JobHasTag",
      "RandomChance",
      "EventSeen",
      "WorkYearsAtLeast",
      "RecognitionAtLeast",
      "ResumeRiskAtLeast",
      "StressAtLeast",
      "StressAtMost",
      "RecognitionAtMost",
      "ResumeFieldMode",
      "ResumeFieldHidden",
      "ResumeFieldExaggerated",
      "AnyResumeFieldHidden",
      "AnyResumeFieldExaggerated",
      "ResumeTagHidden",
      "AnyResumeTagHidden",
      "ActionIs",
      "CurrentJobMonthsAtLeast",
      "CurrentJobMonthsAtMost"
    ],
    EffectTiming: ["Immediate", "Passive"],
    EffectKind: [
      "None",
      "AddStat",
      "SetStat",
      "AddMoney",
      "AddStress",
      "AddHealth",
      "AddTag",
      "RemoveTag",
      "AddHeir",
      "AddLog",
      "AddBanner",
      "AddRecognition",
      "SetRecognition",
      "AddResumeRisk",
      "EndRun",
      "PassiveStatBonus",
      "AddFitScore",
      "AddJobWeight",
      "AddEventWeight",
      "TriggerEvent",
      "DirectPass",
      "DirectFail",
      "ReturnToJobSearch",
      "PassiveSalaryPercent",
      "PassiveEventWeight",
      "PassiveStressPerYear"
    ],
    RunEndReason: ["None", "Retired", "Fired", "Quit", "HealthCollapse", "StressCollapse", "Accident", "Custom"],
    LaterLifeConditionKind: [
      "Always",
      "ScoreAtLeast",
      "ScoreAtMost",
      "WorkerAgeAtLeast",
      "WorkerAgeAtMost",
      "StressAtLeast",
      "StressAtMost",
      "ResumeRiskAtLeast",
      "WorkerStatAtLeast",
      "WorkerStatAtMost",
      "HasWorkerTag",
      "CompanyHasTag",
      "JobHasTag",
      "EventSeen",
      "RandomChance",
      "LaterLifeValueAtLeast",
      "LaterLifeValueAtMost"
    ],
    LaterLifeEffectKind: [
      "None",
      "AddFamilyStability",
      "AddEducationResource",
      "AddIndustryInsight",
      "AddLifePressure",
      "AddParentCare",
      "AddFamilyReputation",
      "AddLifeRisk",
      "AddSpecialOpportunity",
      "AddChildChance",
      "AddChildCount",
      "AddHeirStat",
      "AddHeirStress",
      "AddHeirTag",
      "AddMilestone",
      "AddStoryFragment",
      "AddWorkYears",
      "AddProspectScore",
      "AddPressureScore"
    ]
  };

  function parse(text) {
    const pack = packTemplate();
    const errors = [];
    const warnings = [];
    let current = null;

    const lines = String(text || "").replace(/\r\n/g, "\n").split("\n");
    lines.forEach((rawLine, index) => {
      const lineNo = index + 1;
      const trimmed = rawLine.trim();
      if (!trimmed || trimmed.startsWith("#")) {
        return;
      }

      if (trimmed.startsWith("@")) {
        if (trimmed.toLowerCase() === "@end") {
          finishBlock();
          return;
        }

        if (current) {
          errors.push(lineNo + ": 上一个 @" + current.kind + " 没有用 @end 结束。");
          finishBlock();
        }

        beginBlock(trimmed, lineNo);
        return;
      }

      if (!current) {
        applyRootLine(trimmed, lineNo);
        return;
      }

      const separator = trimmed.indexOf(":");
      if (separator < 0) {
        errors.push(lineNo + ": 内容行需要使用 key: value 格式。");
        return;
      }

      const key = trimmed.slice(0, separator).trim();
      const value = trimmed.slice(separator + 1).trim();
      applyBlockLine(current, key, value, lineNo);
    });

    if (current) {
      errors.push("文件结尾: @" + current.kind + " 没有用 @end 结束。");
      finishBlock();
    }

    validate(pack, errors, warnings);
    convertEnumsForUnity(pack);
    return { pack, errors, warnings };

    function beginBlock(command, lineNo) {
      const parts = command.slice(1).trim().split(/\s+/);
      const kind = (parts[0] || "").toLowerCase();
      const id = parts[1] || "";
      if (!kind) {
        errors.push(lineNo + ": 空的块声明。");
        return;
      }

      if (kind !== "names" && !id) {
        errors.push(lineNo + ": @" + kind + " 需要 ID。");
      }

      current = { kind, id, lineNo, value: createBlockValue(kind, id), lastOption: null, lastConditional: null, lastEventWeight: null, lastJob: null };
      if (!current.value) {
        errors.push(lineNo + ": 不支持的块类型 @" + kind + "。");
        current = null;
      }
    }

    function finishBlock() {
      if (!current || !current.value) {
        current = null;
        return;
      }

      switch (current.kind) {
        case "rarity":
          pack.Rarities.push(current.value);
          break;
        case "stat":
          pack.Stats.push(current.value);
          break;
        case "tag":
          pack.Tags.push(current.value);
          break;
        case "worktag":
        case "work-tag":
        case "work_tag":
          pack.WorkTags.push(current.value);
          break;
        case "company":
          pack.Companies.push(current.value);
          break;
        case "event":
          pack.Events.push(current.value);
          break;
        case "flowmoment":
        case "flow-moment":
        case "flow_moment":
          pack.FlowMoments.push(current.value);
          break;
        case "liferule":
        case "life-rule":
        case "life_rule":
          pack.LaterLifeRules.push(current.value);
          break;
        case "names":
          break;
      }

      current = null;
    }

    function applyRootLine(line, lineNo) {
      const separator = line.indexOf(":");
      if (separator < 0) {
        errors.push(lineNo + ": 顶层内容需要使用 key: value 格式。");
        return;
      }

      const key = normalizeKey(line.slice(0, separator));
      const value = line.slice(separator + 1).trim();
      if (key === "mergemode") {
        pack.MergeMode = value || "merge";
      } else if (key === "version") {
        pack.Version = toInt(value, 1);
      } else if (key === "lastnames") {
        pack.LastNames = splitList(value);
      } else if (key === "malefirstnames") {
        pack.MaleFirstNames = splitList(value);
      } else if (key === "femalefirstnames") {
        pack.FemaleFirstNames = splitList(value);
      } else {
        warnings.push(lineNo + ": 未识别的顶层字段 " + key + "。");
      }
    }

    function applyBlockLine(block, key, value, lineNo) {
      const normalized = normalizeKey(key);
      switch (block.kind) {
        case "rarity":
          applyRarityLine(block.value, normalized, value, lineNo);
          break;
        case "stat":
          applyStatLine(block.value, normalized, value, lineNo);
          break;
        case "tag":
          applyTagLine(block, normalized, value, lineNo);
          break;
        case "worktag":
        case "work-tag":
        case "work_tag":
          applyWorkTagLine(block, normalized, value, lineNo);
          break;
        case "company":
          applyCompanyLine(block, normalized, value, lineNo);
          break;
        case "event":
          applyEventLine(block, normalized, value, lineNo);
          break;
        case "flowmoment":
        case "flow-moment":
        case "flow_moment":
          applyFlowMomentLine(block, normalized, value);
          break;
        case "liferule":
        case "life-rule":
        case "life_rule":
          applyLifeRuleLine(block, normalized, value, lineNo);
          break;
        case "names":
          applyNamesLine(pack, normalized, value, lineNo);
          break;
        default:
          errors.push(lineNo + ": 无法处理 @" + block.kind + " 的字段。");
          break;
      }
    }
  }

  function createBlockValue(kind, id) {
    switch (kind) {
      case "rarity":
        return { Id: id, DisplayName: id, Weight: 10, Color: colorFromHex("#ffffff") };
      case "stat":
        return { Id: id, DisplayName: id, MinValue: 0, MaxValue: 100, StartMin: 20, StartMax: 80, HigherIsBetter: true, Description: "" };
      case "tag":
        return {
          Id: id,
          DisplayName: id,
          Description: "",
          Scope: "Worker",
          Polarity: "Neutral",
          RarityId: "normal",
          Unique: true,
          Conditions: [],
          Effects: [],
          ConditionalEffects: []
        };
      case "worktag":
      case "work-tag":
      case "work_tag":
        return { Id: id, DisplayName: id, Description: "", Requirements: [], Effects: [], EventWeights: [] };
      case "company":
        return { Id: id, DisplayName: id, Industry: "", FoundedYear: 2026, FoundedMonth: 1, TagIds: [], Jobs: [] };
      case "event":
        return { Id: id, DisplayName: id, Body: "", Phase: "Any", NoticeType: "Log", BaseWeight: 10, CooldownYears: 0, Conditions: [], Options: [] };
      case "flowmoment":
      case "flow-moment":
      case "flow_moment":
        return flowMomentTemplate(id);
      case "liferule":
      case "life-rule":
      case "life_rule":
        return { Id: id, DisplayName: id, Priority: 100, Conditions: [], Effects: [] };
      case "names":
        return {};
      default:
        return null;
    }
  }

  function applyRarityLine(rarity, key, value) {
    if (setCommonText(rarity, key, value)) {
      return;
    }

    if (key === "weight") {
      rarity.Weight = toInt(value, rarity.Weight);
    } else if (key === "color") {
      rarity.Color = colorFromHex(value);
    }
  }

  function applyStatLine(stat, key, value) {
    if (setCommonText(stat, key, value)) {
      return;
    }

    if (key === "range") {
      const parts = splitLoose(value);
      stat.MinValue = toInt(parts[0], stat.MinValue);
      stat.MaxValue = toInt(parts[1], stat.MaxValue);
    } else if (key === "start") {
      const parts = splitLoose(value);
      stat.StartMin = toInt(parts[0], stat.StartMin);
      stat.StartMax = toInt(parts[1], stat.StartMax);
    } else if (key === "min") {
      stat.MinValue = toInt(value, stat.MinValue);
    } else if (key === "max") {
      stat.MaxValue = toInt(value, stat.MaxValue);
    } else if (key === "startmin") {
      stat.StartMin = toInt(value, stat.StartMin);
    } else if (key === "startmax") {
      stat.StartMax = toInt(value, stat.StartMax);
    } else if (key === "higherisbetter") {
      stat.HigherIsBetter = toBool(value, stat.HigherIsBetter);
    }
  }

  function applyTagLine(block, key, value, lineNo) {
    const tag = block.value;
    if (setCommonText(tag, key, value)) {
      return;
    }

    if (key === "scope") {
      tag.Scope = value || tag.Scope;
    } else if (key === "polarity") {
      tag.Polarity = value || tag.Polarity;
    } else if (key === "rarity") {
      tag.RarityId = value || tag.RarityId;
    } else if (key === "unique") {
      tag.Unique = toBool(value, tag.Unique);
    } else if (key === "condition") {
      tag.Conditions.push(parseCondition(value));
    } else if (key === "effect") {
      tag.Effects.push(parseEffect(value));
    } else if (key === "conditional") {
      const conditional = parseConditional(value);
      tag.ConditionalEffects.push(conditional);
      block.lastConditional = conditional;
    } else if (key === "conditionaleffect") {
      ensureLastConditional(block, lineNo).Effects.push(parseEffect(value));
    } else if (key === "conditionalcondition") {
      ensureLastConditional(block, lineNo).Conditions.push(parseCondition(value));
    }
  }

  function applyWorkTagLine(block, key, value, lineNo) {
    const workTag = block.value;
    if (setCommonText(workTag, key, value)) {
      return;
    }

    if (key === "requirement") {
      workTag.Requirements.push(parseWorkRequirement(value));
    } else if (key === "effect") {
      workTag.Effects.push(parseEffect(value));
    } else if (key === "eventweight") {
      const eventWeight = parseEventWeight(value);
      workTag.EventWeights.push(eventWeight);
      block.lastEventWeight = eventWeight;
    } else if (key === "eventweightcondition") {
      ensureLastEventWeight(block, lineNo).Conditions.push(parseCondition(value));
    }
  }

  function applyCompanyLine(block, key, value, lineNo) {
    const company = block.value;
    if (setCommonText(company, key, value)) {
      return;
    }

    if (key === "industry") {
      company.Industry = value;
    } else if (key === "founded") {
      const parts = value.split(/[-/.,\s]+/).filter(Boolean);
      company.FoundedYear = toInt(parts[0], company.FoundedYear);
      company.FoundedMonth = toInt(parts[1], company.FoundedMonth);
    } else if (key === "foundedyear") {
      company.FoundedYear = toInt(value, company.FoundedYear);
    } else if (key === "foundedmonth") {
      company.FoundedMonth = toInt(value, company.FoundedMonth);
    } else if (key === "tags") {
      company.TagIds = splitList(value);
    } else if (key === "job") {
      const job = parseJob(value);
      company.Jobs.push(job);
      block.lastJob = job;
    } else if (key === "jobrequirement") {
      ensureLastJob(block, lineNo).Requirements.push(parseStatRequirement(value));
    }
  }

  function applyEventLine(block, key, value, lineNo) {
    const event = block.value;
    if (setCommonText(event, key, value)) {
      return;
    }

    if (key === "body") {
      event.Body = value;
    } else if (key === "phase") {
      event.Phase = value || event.Phase;
    } else if (key === "notice" || key === "noticetype") {
      event.NoticeType = value || event.NoticeType;
    } else if (key === "baseweight") {
      event.BaseWeight = toInt(value, event.BaseWeight);
    } else if (key === "cooldown" || key === "cooldownyears") {
      event.CooldownYears = toInt(value, event.CooldownYears);
    } else if (key === "condition") {
      event.Conditions.push(parseCondition(value));
    } else if (key === "option") {
      const option = parseEventOption(value);
      event.Options.push(option);
      block.lastOption = option;
    } else if (key === "optioncondition") {
      ensureLastOption(block, lineNo).Conditions.push(parseCondition(value));
    } else if (key === "optioneffect") {
      ensureLastOption(block, lineNo).Effects.push(parseEffect(value));
    }
  }

  function applyFlowMomentLine(block, key, value) {
    const moment = block.value;
    if (setCommonText(moment, key, value)) {
      return;
    }

    if (key === "text" || key === "body") {
      moment.Text = value;
    } else if (key === "action") {
      moment.Action = value;
    } else if (key === "baseweight" || key === "weight") {
      moment.BaseWeight = toInt(value, moment.BaseWeight);
    } else if (key === "condition") {
      moment.Conditions.push(parseCondition(value));
    } else if (key === "effect") {
      moment.Effects.push(parseEffect(value));
    }
  }

  function applyLifeRuleLine(block, key, value) {
    const rule = block.value;
    if (setCommonText(rule, key, value)) {
      return;
    }

    if (key === "priority") {
      rule.Priority = toInt(value, rule.Priority);
    } else if (key === "condition") {
      rule.Conditions.push(parseLaterLifeCondition(value));
    } else if (key === "effect") {
      rule.Effects.push(parseLaterLifeEffect(value));
    }
  }

  function applyNamesLine(pack, key, value) {
    if (key === "lastnames") {
      pack.LastNames = splitList(value);
    } else if (key === "malefirstnames") {
      pack.MaleFirstNames = splitList(value);
    } else if (key === "femalefirstnames") {
      pack.FemaleFirstNames = splitList(value);
    }
  }

  function setCommonText(target, key, value) {
    if (key === "name" || key === "displayname") {
      target.DisplayName = value;
      return true;
    }

    if (key === "description") {
      target.Description = value;
      return true;
    }

    return false;
  }

  function parseCondition(value) {
    const pairs = parsePairs(value);
    const condition = conditionTemplate();
    fillCondition(condition, pairs);
    return condition;
  }

  function parseLaterLifeCondition(value) {
    const pairs = parsePairs(value);
    const condition = laterLifeConditionTemplate();
    const bare = pairs._;
    condition.Kind = pairs.kind || bare[0] || condition.Kind;
    condition.Key = pairs.key || bare[1] || condition.Key;
    condition.IntValue = toInt(firstDefined(pairs.int, pairs.value, bare[2]), condition.IntValue);
    condition.FloatValue = toFloat(firstDefined(pairs.float, pairs.chance), condition.FloatValue);
    condition.Invert = toBool(pairs.invert, condition.Invert);
    return condition;
  }

  function parseEffect(value) {
    const pairs = parsePairs(value);
    const effect = effectTemplate();
    const bare = pairs._;
    if (bare[0] && isTiming(bare[0])) {
      effect.Timing = normalizeTiming(bare[0]);
      effect.Kind = bare[1] || effect.Kind;
      effect.Key = bare[2] || effect.Key;
      effect.IntValue = toInt(bare[3], effect.IntValue);
    } else {
      effect.Kind = bare[0] || effect.Kind;
      effect.Key = bare[1] || effect.Key;
      effect.IntValue = toInt(bare[2], effect.IntValue);
    }

    effect.Timing = normalizeTiming(pairs.timing || effect.Timing);
    effect.Kind = pairs.kind || effect.Kind;
    effect.Key = firstDefined(pairs.key, pairs.id, effect.Key);
    effect.IntValue = toInt(firstDefined(pairs.int, pairs.value, pairs.delta), effect.IntValue);
    effect.FloatValue = toFloat(firstDefined(pairs.float, pairs.chance), effect.FloatValue);
    effect.TextValue = firstDefined(pairs.text, pairs.textvalue, effect.TextValue);
    effect.SecondaryText = firstDefined(pairs.secondary, pairs.secondarytext, effect.SecondaryText);
    effect.TagScope = firstDefined(pairs.scope, pairs.tagscope, effect.TagScope);
    effect.EndReason = firstDefined(pairs.end, pairs.endreason, effect.EndReason);
    effect.Temporary = toBool(pairs.temporary, effect.Temporary);
    effect.DurationYears = toInt(firstDefined(pairs.years, pairs.duration, pairs.durationyears), effect.DurationYears);
    return effect;
  }

  function parseLaterLifeEffect(value) {
    const pairs = parsePairs(value);
    const effect = laterLifeEffectTemplate();
    const bare = pairs._;
    effect.Kind = pairs.kind || bare[0] || effect.Kind;
    effect.Key = firstDefined(pairs.key, pairs.id, bare[1], effect.Key);
    effect.IntValue = toInt(firstDefined(pairs.int, pairs.value, pairs.delta, bare[2]), effect.IntValue);
    effect.TextValue = firstDefined(pairs.text, pairs.textvalue, effect.TextValue);
    effect.SecondaryText = firstDefined(pairs.secondary, pairs.secondarytext, effect.SecondaryText);
    return effect;
  }

  function fillCondition(condition, pairs) {
    const bare = pairs._;
    condition.Kind = pairs.kind || bare[0] || condition.Kind;
    condition.Key = firstDefined(pairs.key, pairs.id, bare[1], condition.Key);
    condition.Operator = firstDefined(pairs.op, pairs.operator, condition.Operator);
    condition.IntValue = toInt(firstDefined(pairs.int, pairs.value, bare[2]), condition.IntValue);
    condition.FloatValue = toFloat(firstDefined(pairs.float, pairs.chance), condition.FloatValue);
    condition.TextValue = firstDefined(pairs.text, pairs.textvalue, condition.TextValue);
    condition.Invert = toBool(pairs.invert, condition.Invert);
  }

  function parseConditional(value) {
    const pairs = parsePairs(value);
    return {
      Phase: firstDefined(pairs.phase, "Any"),
      WorkTagIds: splitList(firstDefined(pairs.worktags, pairs.tags, pairs.worktagids, "")),
      Conditions: [],
      Effects: []
    };
  }

  function parseWorkRequirement(value) {
    const pairs = parsePairs(value);
    const bare = pairs._;
    return {
      Target: firstDefined(pairs.target, bare[0], "FitDimension"),
      Dimension: firstDefined(pairs.dimension, pairs.dim, bare[1], "Execution"),
      Key: firstDefined(pairs.key, pairs.stat, ""),
      Operator: firstDefined(pairs.op, pairs.operator, "GreaterOrEqual"),
      IntValue: toInt(firstDefined(pairs.int, pairs.value, pairs.min, bare[2]), 50),
      HardFail: toBool(firstDefined(pairs.hard, pairs.hardfail), false),
      RecognitionOnPass: toInt(firstDefined(pairs.passrecognition, pairs.recognitiononpass), 0),
      RecognitionOnFail: toInt(firstDefined(pairs.failrecognition, pairs.recognitiononfail), 0),
      StressOnPass: toInt(firstDefined(pairs.passstress, pairs.stressonpass), 0),
      StressOnFail: toInt(firstDefined(pairs.failstress, pairs.stressonfail), 0),
      FailEventId: firstDefined(pairs.failevent, pairs.faileventid, "")
    };
  }

  function parseEventWeight(value) {
    const pairs = parsePairs(value);
    const bare = pairs._;
    return {
      EventId: firstDefined(pairs.event, pairs.eventid, pairs.id, bare[0], ""),
      WeightDelta: toInt(firstDefined(pairs.delta, pairs.weight, bare[1]), 0),
      Conditions: []
    };
  }

  function parseJob(value) {
    const pairs = parsePairs(value);
    const bare = pairs._;
    return {
      Id: firstDefined(pairs.id, bare[0], ""),
      DisplayName: firstDefined(pairs.name, pairs.displayname, bare[1], ""),
      IntentTagIds: splitList(firstDefined(pairs.intenttags, "")),
      TagIds: splitList(firstDefined(pairs.tags, "")),
      SalaryMin: salaryMin(firstDefined(pairs.salary, ""), toInt(pairs.salarymin, 6000)),
      SalaryMax: salaryMax(firstDefined(pairs.salary, ""), toInt(pairs.salarymax, 12000)),
      Difficulty: toInt(pairs.difficulty, 40),
      OfferThreshold: toInt(firstDefined(pairs.threshold, pairs.offerthreshold), 70),
      WorkIntensity: toInt(firstDefined(pairs.intensity, pairs.workintensity), 40),
      PromotionBase: toInt(firstDefined(pairs.promotion, pairs.promotionbase), 8),
      Requirements: []
    };
  }

  function parseStatRequirement(value) {
    const pairs = parsePairs(value);
    const bare = pairs._;
    return {
      StatId: firstDefined(pairs.stat, pairs.statid, pairs.id, bare[0], ""),
      MinValue: toInt(firstDefined(pairs.min, pairs.value, bare[1]), 0),
      Weight: toInt(firstDefined(pairs.weight, bare[2]), 1),
      MissingTagId: firstDefined(pairs.missingtag, pairs.missingtagid, "")
    };
  }

  function parseEventOption(value) {
    const pairs = parsePairs(value);
    const bare = pairs._;
    return {
      Id: firstDefined(pairs.id, bare[0], ""),
      Label: firstDefined(pairs.label, pairs.name, bare[1], ""),
      RunCheckpointAfterChoice: toBool(firstDefined(pairs.checkpoint, pairs.runcheckpoint), false),
      Conditions: [],
      Effects: []
    };
  }

  function ensureLastConditional(block, lineNo) {
    if (!block.lastConditional) {
      const conditional = parseConditional("");
      block.value.ConditionalEffects.push(conditional);
      block.lastConditional = conditional;
      console.warn(lineNo + ": 自动创建空 conditional。");
    }

    return block.lastConditional;
  }

  function ensureLastEventWeight(block, lineNo) {
    if (!block.lastEventWeight) {
      const eventWeight = parseEventWeight("");
      block.value.EventWeights.push(eventWeight);
      block.lastEventWeight = eventWeight;
      console.warn(lineNo + ": 自动创建空 eventWeight。");
    }

    return block.lastEventWeight;
  }

  function ensureLastOption(block, lineNo) {
    if (!block.lastOption) {
      const option = parseEventOption("");
      block.value.Options.push(option);
      block.lastOption = option;
      console.warn(lineNo + ": 自动创建空 option。");
    }

    return block.lastOption;
  }

  function ensureLastJob(block, lineNo) {
    if (!block.lastJob) {
      const job = parseJob("");
      block.value.Jobs.push(job);
      block.lastJob = job;
      console.warn(lineNo + ": 自动创建空 job。");
    }

    return block.lastJob;
  }

  function validate(pack, errors, warnings) {
    checkDuplicates(pack.Tags, "人物标签", errors);
    checkDuplicates(pack.WorkTags, "工作标签", errors);
    checkDuplicates(pack.Events, "事件", errors);
    checkDuplicates(pack.LaterLifeRules, "后半生规则", errors);
    checkDuplicates(pack.Companies, "公司", errors);
    checkDuplicates(pack.Stats, "属性", errors);

    const tagIds = new Set(pack.Tags.map(item => item.Id));
    const workTagIds = new Set(pack.WorkTags.map(item => item.Id));
    const eventIds = new Set(pack.Events.map(item => item.Id));
    const statIds = new Set(pack.Stats.map(item => item.Id));

    pack.Tags.forEach(tag => {
      if (!tag.DisplayName || tag.DisplayName === tag.Id) {
        warnings.push("人物标签 " + tag.Id + " 没有设置中文 name。");
      }

      tag.Effects.forEach(effect => warnEffectReference(effect, tagIds, eventIds, warnings, "人物标签 " + tag.Id));
      tag.ConditionalEffects.forEach(conditional => {
        conditional.WorkTagIds.forEach(id => {
          if (id && !workTagIds.has(id)) {
            warnings.push("人物标签 " + tag.Id + " 引用了未在本包定义的工作标签 " + id + "；如果它来自已有数据库，可以忽略。");
          }
        });
        conditional.Effects.forEach(effect => warnEffectReference(effect, tagIds, eventIds, warnings, "人物标签 " + tag.Id));
      });
    });

    pack.WorkTags.forEach(tag => {
      tag.EventWeights.forEach(weight => {
        if (weight.EventId && !eventIds.has(weight.EventId)) {
          warnings.push("工作标签 " + tag.Id + " 提到了事件 " + weight.EventId + "；如果事件来自已有数据库，可以忽略。");
        }
      });
    });

    pack.Events.forEach(event => {
      if (!event.Options.length) {
        warnings.push("事件 " + event.Id + " 没有选项，弹窗事件通常至少需要一个 option。");
      }
      warnEventConditionMismatch(event, warnings);
      event.Options.forEach(option => option.Effects.forEach(effect => warnEffectReference(effect, tagIds, eventIds, warnings, "事件 " + event.Id)));
    });

    pack.Companies.forEach(company => {
      company.TagIds.forEach(id => {
        if (id && !workTagIds.has(id)) {
          warnings.push("公司 " + company.Id + " 引用了未在本包定义的工作标签 " + id + "；如果它来自已有数据库，可以忽略。");
        }
      });
      company.Jobs.forEach(job => {
        job.TagIds.forEach(id => {
          if (id && !workTagIds.has(id)) {
            warnings.push("岗位 " + job.Id + " 引用了未在本包定义的工作标签 " + id + "；如果它来自已有数据库，可以忽略。");
          }
        });
        job.Requirements.forEach(requirement => {
          if (requirement.StatId && !statIds.has(requirement.StatId)) {
            warnings.push("岗位 " + job.Id + " 要求属性 " + requirement.StatId + "；如果属性来自已有数据库，可以忽略。");
          }
        });
      });
    });
  }

  function warnEffectReference(effect, tagIds, eventIds, warnings, owner) {
    if (!effect || !effect.Kind) {
      return;
    }

    if ((effect.Kind === "AddTag" || effect.Kind === "RemoveTag") && effect.Key && !tagIds.has(effect.Key)) {
      warnings.push(owner + " 的效果 " + effect.Kind + " 引用了未在本包定义的人物标签 " + effect.Key + "；如果它来自已有数据库，可以忽略。");
    }

    if ((effect.Kind === "TriggerEvent" || effect.Kind === "AddEventWeight" || effect.Kind === "PassiveEventWeight") && effect.Key && !eventIds.has(effect.Key)) {
      warnings.push(owner + " 的效果 " + effect.Kind + " 提到了事件 " + effect.Key + "；如果事件来自已有数据库，可以忽略。");
    }
  }

  function warnEventConditionMismatch(event, warnings) {
    const text = [event.DisplayName, event.Body].filter(Boolean).join(" ");
    if (!text) {
      return;
    }

    const kinds = eventConditionKinds(event);
    if (/(隐藏|隐瞒)/.test(text) && !hasAny(kinds, ["ResumeFieldHidden", "AnyResumeFieldHidden", "ResumeTagHidden", "AnyResumeTagHidden"])) {
      warnings.push("事件 " + event.Id + " 文案提到了隐藏/隐瞒，但缺少 ResumeFieldHidden、AnyResumeFieldHidden、ResumeTagHidden 或 AnyResumeTagHidden 条件。");
    }

    if (/(夸大|虚报|伪造)/.test(text) && !hasAny(kinds, ["ResumeFieldExaggerated", "AnyResumeFieldExaggerated", "ResumeFieldMode", "ResumeRiskAtLeast"])) {
      warnings.push("事件 " + event.Id + " 文案提到了夸大/虚报/伪造，但缺少 ResumeFieldExaggerated、AnyResumeFieldExaggerated、ResumeFieldMode 或 ResumeRiskAtLeast 条件。");
    }

    if (arrayOf(event.Conditions).length === 0 && event.BaseWeight > 0) {
      warnings.push("事件 " + event.Id + " 没有触发条件，会作为普通随机事件进入事件池。");
    }
  }

  function eventConditionKinds(event) {
    const kinds = [];
    arrayOf(event.Conditions).forEach(condition => kinds.push(enumName("ConditionKind", condition.Kind, String(condition.Kind || ""))));
    arrayOf(event.Options).forEach(option => {
      arrayOf(option.Conditions).forEach(condition => kinds.push(enumName("ConditionKind", condition.Kind, String(condition.Kind || ""))));
    });
    return kinds;
  }

  function hasAny(values, expected) {
    for (let i = 0; i < expected.length; i += 1) {
      if (values.includes(expected[i])) {
        return true;
      }
    }

    return false;
  }

  function checkDuplicates(items, label, errors) {
    const seen = new Set();
    items.forEach(item => {
      if (!item || !item.Id) {
        errors.push(label + " 存在空 ID。");
        return;
      }

      if (seen.has(item.Id)) {
        errors.push(label + " ID 重复: " + item.Id);
      }

      seen.add(item.Id);
    });
  }

  function convertEnumsForUnity(pack) {
    pack.Tags.forEach(tag => {
      tag.Scope = enumNumber("TagScope", tag.Scope);
      tag.Polarity = enumNumber("TagPolarity", tag.Polarity);
      tag.Conditions.forEach(convertConditionEnums);
      tag.Effects.forEach(convertEffectEnums);
      tag.ConditionalEffects.forEach(conditional => {
        conditional.Phase = enumNumber("GameEventPhase", conditional.Phase);
        conditional.Conditions.forEach(convertConditionEnums);
        conditional.Effects.forEach(convertEffectEnums);
      });
    });

    pack.WorkTags.forEach(tag => {
      tag.Requirements.forEach(requirement => {
        requirement.Target = enumNumber("RequirementTarget", requirement.Target);
        requirement.Dimension = enumNumber("FitDimension", requirement.Dimension);
        requirement.Operator = enumNumber("CompareOperator", requirement.Operator);
      });
      tag.Effects.forEach(convertEffectEnums);
      tag.EventWeights.forEach(weight => weight.Conditions.forEach(convertConditionEnums));
    });

    pack.Events.forEach(event => {
      event.Phase = enumNumber("GameEventPhase", event.Phase);
      event.NoticeType = enumNumber("EventNoticeType", event.NoticeType);
      event.Conditions.forEach(convertConditionEnums);
      event.Options.forEach(option => {
        option.Conditions.forEach(convertConditionEnums);
        option.Effects.forEach(convertEffectEnums);
      });
    });

    pack.FlowMoments.forEach(moment => {
      moment.Conditions.forEach(convertConditionEnums);
      moment.Effects.forEach(convertEffectEnums);
    });

    pack.LaterLifeRules.forEach(rule => {
      rule.Conditions.forEach(condition => {
        condition.Kind = enumNumber("LaterLifeConditionKind", condition.Kind);
      });
      rule.Effects.forEach(effect => {
        effect.Kind = enumNumber("LaterLifeEffectKind", effect.Kind);
      });
    });
  }

  function convertConditionEnums(condition) {
    condition.Kind = enumNumber("ConditionKind", condition.Kind);
    condition.Operator = enumNumber("CompareOperator", condition.Operator);
  }

  function convertEffectEnums(effect) {
    effect.Timing = enumNumber("EffectTiming", effect.Timing);
    effect.Kind = enumNumber("EffectKind", effect.Kind);
    effect.TagScope = enumNumber("TagScope", effect.TagScope);
    effect.EndReason = enumNumber("RunEndReason", effect.EndReason);
  }

  function enumNumber(mapName, value) {
    if (typeof value === "number") {
      return value;
    }

    const entries = enumMaps[mapName] || [];
    const normalized = normalizeEnumName(value);
    for (let i = 0; i < entries.length; i += 1) {
      if (normalizeEnumName(entries[i]) === normalized) {
        return i;
      }
    }

    return value;
  }

  function normalizeEnumName(value) {
    return String(value || "").trim().replace(/[-_\s]/g, "").toLowerCase();
  }

  function parsePairs(value) {
    const tokens = tokenize(value);
    const result = { _: [] };
    tokens.forEach(token => {
      const eq = token.indexOf("=");
      if (eq < 0) {
        result._.push(unquote(token));
        return;
      }

      const key = normalizeKey(token.slice(0, eq));
      result[key] = unquote(token.slice(eq + 1));
    });

    return result;
  }

  function tokenize(value) {
    const tokens = [];
    let token = "";
    let quote = "";
    const text = String(value || "");
    for (let i = 0; i < text.length; i += 1) {
      const char = text[i];
      if (quote) {
        if (char === quote) {
          quote = "";
        } else if (char === "\\" && i + 1 < text.length) {
          i += 1;
          token += text[i];
        } else {
          token += char;
        }
        continue;
      }

      if (char === "\"" || char === "'") {
        quote = char;
        continue;
      }

      if (/\s/.test(char)) {
        if (token) {
          tokens.push(token);
          token = "";
        }
        continue;
      }

      token += char;
    }

    if (token) {
      tokens.push(token);
    }

    return tokens;
  }

  function splitList(value) {
    if (!value) {
      return [];
    }

    return String(value)
      .split(/[，,|]/)
      .map(item => item.trim())
      .filter(Boolean);
  }

  function splitLoose(value) {
    return String(value || "").split(/\.{2}|[-,，\s]+/).filter(Boolean);
  }

  function normalizeKey(value) {
    return String(value || "").trim().replace(/[-_\s]/g, "").toLowerCase();
  }

  function toInt(value, fallback) {
    if (value === undefined || value === null || value === "") {
      return fallback;
    }

    const parsed = parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function toFloat(value, fallback) {
    if (value === undefined || value === null || value === "") {
      return fallback;
    }

    const parsed = parseFloat(value);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function toBool(value, fallback) {
    if (value === undefined || value === null || value === "") {
      return fallback;
    }

    const normalized = String(value).trim().toLowerCase();
    if (["true", "yes", "1", "是", "启用"].includes(normalized)) {
      return true;
    }

    if (["false", "no", "0", "否", "关闭"].includes(normalized)) {
      return false;
    }

    return fallback;
  }

  function unquote(value) {
    return String(value || "").trim();
  }

  function firstDefined() {
    for (let i = 0; i < arguments.length; i += 1) {
      if (arguments[i] !== undefined && arguments[i] !== null && arguments[i] !== "") {
        return arguments[i];
      }
    }

    return "";
  }

  function isTiming(value) {
    const normalized = String(value || "").toLowerCase();
    return normalized === "passive" || normalized === "immediate";
  }

  function normalizeTiming(value) {
    return String(value || "").toLowerCase() === "passive" ? "Passive" : "Immediate";
  }

  function salaryMin(value, fallback) {
    const parts = splitLoose(value);
    return toInt(parts[0], fallback);
  }

  function salaryMax(value, fallback) {
    const parts = splitLoose(value);
    return toInt(parts[1], fallback);
  }

  function colorFromHex(value) {
    const normalized = String(value || "").trim().replace("#", "");
    if (![3, 6, 8].includes(normalized.length)) {
      return { r: 1, g: 1, b: 1, a: 1 };
    }

    const full = normalized.length === 3
      ? normalized.split("").map(char => char + char).join("")
      : normalized;
    const r = parseInt(full.slice(0, 2), 16) / 255;
    const g = parseInt(full.slice(2, 4), 16) / 255;
    const b = parseInt(full.slice(4, 6), 16) / 255;
    const a = full.length >= 8 ? parseInt(full.slice(6, 8), 16) / 255 : 1;
    return { r, g, b, a };
  }

  function toPspack(input) {
    const pack = typeof input === "string" ? JSON.parse(input) : (input || {});
    const lines = [
      "# Paper Shift Content Pack v1",
      "MergeMode: " + (pack.MergeMode || "merge"),
      ""
    ];

    writeNames(lines, pack);
    arrayOf(pack.Rarities).forEach(item => writeRarity(lines, item));
    arrayOf(pack.Stats).forEach(item => writeStat(lines, item));
    arrayOf(pack.Tags).forEach(item => writeTag(lines, item));
    arrayOf(pack.WorkTags).forEach(item => writeWorkTag(lines, item));
    arrayOf(pack.Events).forEach(item => writeEvent(lines, item));
    arrayOf(pack.FlowMoments).forEach(item => writeFlowMoment(lines, item));
    arrayOf(pack.LaterLifeRules).forEach(item => writeLifeRule(lines, item));
    arrayOf(pack.Companies).forEach(item => writeCompany(lines, item));

    return lines.join("\n").replace(/\n{3,}/g, "\n\n").trim() + "\n";
  }

  function writeNames(lines, pack) {
    const hasNames = arrayOf(pack.LastNames).length > 0
      || arrayOf(pack.MaleFirstNames).length > 0
      || arrayOf(pack.FemaleFirstNames).length > 0;
    if (!hasNames) {
      return;
    }

    lines.push("@names");
    writeListLine(lines, "lastNames", pack.LastNames);
    writeListLine(lines, "maleFirstNames", pack.MaleFirstNames);
    writeListLine(lines, "femaleFirstNames", pack.FemaleFirstNames);
    lines.push("@end", "");
  }

  function writeRarity(lines, rarity) {
    if (!rarity || !rarity.Id) {
      return;
    }

    lines.push("@rarity " + rarity.Id);
    writeTextLine(lines, "name", rarity.DisplayName);
    writeTextLine(lines, "weight", rarity.Weight);
    if (rarity.Color) {
      writeTextLine(lines, "color", colorToHex(rarity.Color));
    }
    lines.push("@end", "");
  }

  function writeStat(lines, stat) {
    if (!stat || !stat.Id) {
      return;
    }

    lines.push("@stat " + stat.Id);
    writeTextLine(lines, "name", stat.DisplayName);
    writeTextLine(lines, "description", stat.Description);
    lines.push("range: " + valueOr(stat.MinValue, 0) + ".." + valueOr(stat.MaxValue, 100));
    lines.push("start: " + valueOr(stat.StartMin, 20) + ".." + valueOr(stat.StartMax, 80));
    if (stat.HigherIsBetter === false) {
      lines.push("higherIsBetter: false");
    }
    lines.push("@end", "");
  }

  function writeTag(lines, tag) {
    if (!tag || !tag.Id) {
      return;
    }

    lines.push("@tag " + tag.Id);
    writeTextLine(lines, "name", tag.DisplayName);
    writeTextLine(lines, "description", tag.Description);
    writeTextLine(lines, "rarity", tag.RarityId || "normal");
    writeTextLine(lines, "polarity", enumName("TagPolarity", tag.Polarity, "Neutral"));
    if (enumName("TagScope", tag.Scope, "Worker") !== "Worker") {
      writeTextLine(lines, "scope", enumName("TagScope", tag.Scope, "Worker"));
    }
    if (tag.Unique === false) {
      lines.push("unique: false");
    }
    arrayOf(tag.Conditions).forEach(condition => lines.push(conditionLine("condition", condition)));
    arrayOf(tag.Effects).forEach(effect => lines.push(effectLine("effect", effect)));
    arrayOf(tag.ConditionalEffects).forEach(conditional => {
      lines.push(conditionalLine(conditional));
      arrayOf(conditional.Conditions).forEach(condition => lines.push(conditionLine("conditionalCondition", condition)));
      arrayOf(conditional.Effects).forEach(effect => lines.push(effectLine("conditionalEffect", effect)));
    });
    lines.push("@end", "");
  }

  function writeWorkTag(lines, tag) {
    if (!tag || !tag.Id) {
      return;
    }

    lines.push("@workTag " + tag.Id);
    writeTextLine(lines, "name", tag.DisplayName);
    writeTextLine(lines, "description", tag.Description);
    arrayOf(tag.Requirements).forEach(requirement => lines.push(requirementLine(requirement)));
    arrayOf(tag.Effects).forEach(effect => lines.push(effectLine("effect", effect)));
    arrayOf(tag.EventWeights).forEach(weight => {
      lines.push(eventWeightLine(weight));
      arrayOf(weight.Conditions).forEach(condition => lines.push(conditionLine("eventWeightCondition", condition)));
    });
    lines.push("@end", "");
  }

  function writeEvent(lines, event) {
    if (!event || !event.Id) {
      return;
    }

    lines.push("@event " + event.Id);
    writeTextLine(lines, "name", event.DisplayName);
    writeTextLine(lines, "body", event.Body);
    writeTextLine(lines, "phase", enumName("GameEventPhase", event.Phase, "Any"));
    writeTextLine(lines, "notice", enumName("EventNoticeType", event.NoticeType, "Log"));
    writeTextLine(lines, "baseWeight", valueOr(event.BaseWeight, 10));
    if (valueOr(event.CooldownYears, 0) !== 0) {
      writeTextLine(lines, "cooldown", event.CooldownYears);
    }
    arrayOf(event.Conditions).forEach(condition => lines.push(conditionLine("condition", condition)));
    arrayOf(event.Options).forEach(option => {
      lines.push(optionLine(option));
      arrayOf(option.Conditions).forEach(condition => lines.push(conditionLine("optionCondition", condition)));
      arrayOf(option.Effects).forEach(effect => lines.push(effectLine("optionEffect", effect)));
    });
    lines.push("@end", "");
  }

  function writeFlowMoment(lines, moment) {
    if (!moment || !moment.Id) {
      return;
    }

    lines.push("@flowMoment " + moment.Id);
    writeTextLine(lines, "name", moment.DisplayName);
    writeTextLine(lines, "text", moment.Text);
    writeTextLine(lines, "action", moment.Action);
    writeTextLine(lines, "baseWeight", valueOr(moment.BaseWeight, 10));
    arrayOf(moment.Conditions).forEach(condition => lines.push(conditionLine("condition", condition)));
    arrayOf(moment.Effects).forEach(effect => lines.push(effectLine("effect", effect)));
    lines.push("@end", "");
  }

  function writeLifeRule(lines, rule) {
    if (!rule || !rule.Id) {
      return;
    }

    lines.push("@lifeRule " + rule.Id);
    writeTextLine(lines, "name", rule.DisplayName);
    writeTextLine(lines, "priority", valueOr(rule.Priority, 100));
    arrayOf(rule.Conditions).forEach(condition => lines.push(lifeConditionLine(condition)));
    arrayOf(rule.Effects).forEach(effect => lines.push(lifeEffectLine(effect)));
    lines.push("@end", "");
  }

  function writeCompany(lines, company) {
    if (!company || !company.Id) {
      return;
    }

    lines.push("@company " + company.Id);
    writeTextLine(lines, "name", company.DisplayName);
    writeTextLine(lines, "industry", company.Industry);
    lines.push("founded: " + valueOr(company.FoundedYear, 2026) + "-" + pad2(valueOr(company.FoundedMonth, 1)));
    writeListLine(lines, "tags", company.TagIds);
    arrayOf(company.Jobs).forEach(job => {
      lines.push(jobLine(job));
      arrayOf(job.Requirements).forEach(requirement => lines.push(statRequirementLine(requirement)));
    });
    lines.push("@end", "");
  }

  function conditionalLine(conditional) {
    const pairs = [
      pair("phase", enumName("GameEventPhase", conditional.Phase, "Any")),
      listPair("workTags", conditional.WorkTagIds)
    ];
    return "conditional: " + pairs.filter(Boolean).join(" ");
  }

  function conditionLine(prefix, condition) {
    const pairs = [
      pair("kind", enumName("ConditionKind", condition.Kind, "Always")),
      pair("key", condition.Key),
      enumName("CompareOperator", condition.Operator, "GreaterOrEqual") === "GreaterOrEqual"
        ? ""
        : pair("op", enumName("CompareOperator", condition.Operator, "GreaterOrEqual")),
      numberPair("int", condition.IntValue),
      floatPair("float", condition.FloatValue),
      pair("text", condition.TextValue),
      condition.Invert ? pair("invert", true) : ""
    ];
    return prefix + ": " + pairs.filter(Boolean).join(" ");
  }

  function lifeConditionLine(condition) {
    const pairs = [
      pair("kind", enumName("LaterLifeConditionKind", condition.Kind, "Always")),
      pair("key", condition.Key),
      numberPair("int", condition.IntValue),
      floatPair("float", condition.FloatValue),
      condition.Invert ? pair("invert", true) : ""
    ];
    return "condition: " + pairs.filter(Boolean).join(" ");
  }

  function effectLine(prefix, effect) {
    const timing = enumName("EffectTiming", effect.Timing, "Immediate");
    const pairs = [
      timing === "Immediate" ? "" : pair("timing", timing),
      pair("kind", enumName("EffectKind", effect.Kind, "None")),
      pair("key", effect.Key),
      numberPair("int", effect.IntValue),
      floatPair("float", effect.FloatValue),
      pair("text", effect.TextValue),
      pair("secondary", effect.SecondaryText),
      enumName("TagScope", effect.TagScope, "Worker") === "Worker" ? "" : pair("scope", enumName("TagScope", effect.TagScope, "Worker")),
      enumName("RunEndReason", effect.EndReason, "None") === "None" ? "" : pair("end", enumName("RunEndReason", effect.EndReason, "None")),
      effect.Temporary ? pair("temporary", true) : "",
      numberPair("years", effect.DurationYears)
    ];
    return prefix + ": " + pairs.filter(Boolean).join(" ");
  }

  function lifeEffectLine(effect) {
    const pairs = [
      pair("kind", enumName("LaterLifeEffectKind", effect.Kind, "None")),
      pair("key", effect.Key),
      numberPair("int", effect.IntValue),
      pair("text", effect.TextValue),
      pair("secondary", effect.SecondaryText)
    ];
    return "effect: " + pairs.filter(Boolean).join(" ");
  }

  function requirementLine(requirement) {
    const pairs = [
      pair("target", enumName("RequirementTarget", requirement.Target, "FitDimension")),
      pair("dimension", enumName("FitDimension", requirement.Dimension, "Execution")),
      pair("key", requirement.Key),
      enumName("CompareOperator", requirement.Operator, "GreaterOrEqual") === "GreaterOrEqual"
        ? ""
        : pair("op", enumName("CompareOperator", requirement.Operator, "GreaterOrEqual")),
      pair("int", valueOr(requirement.IntValue, 50)),
      requirement.HardFail ? pair("hard", true) : "",
      numberPair("passRecognition", requirement.RecognitionOnPass),
      numberPair("failRecognition", requirement.RecognitionOnFail),
      numberPair("passStress", requirement.StressOnPass),
      numberPair("failStress", requirement.StressOnFail),
      pair("failEvent", requirement.FailEventId)
    ];
    return "requirement: " + pairs.filter(Boolean).join(" ");
  }

  function eventWeightLine(weight) {
    const pairs = [
      pair("event", weight.EventId),
      numberPair("delta", weight.WeightDelta)
    ];
    return "eventWeight: " + pairs.filter(Boolean).join(" ");
  }

  function optionLine(option) {
    const pairs = [
      pair("id", option.Id),
      pair("label", option.Label),
      option.RunCheckpointAfterChoice ? pair("checkpoint", true) : ""
    ];
    return "option: " + pairs.filter(Boolean).join(" ");
  }

  function jobLine(job) {
    const pairs = [
      pair("id", job.Id),
      pair("name", job.DisplayName),
      listPair("tags", job.TagIds),
      listPair("intentTags", job.IntentTagIds),
      pair("salary", valueOr(job.SalaryMin, 6000) + ".." + valueOr(job.SalaryMax, 12000)),
      pair("difficulty", valueOr(job.Difficulty, 40)),
      pair("threshold", valueOr(job.OfferThreshold, 70)),
      pair("intensity", valueOr(job.WorkIntensity, 40)),
      pair("promotion", valueOr(job.PromotionBase, 8))
    ];
    return "job: " + pairs.filter(Boolean).join(" ");
  }

  function statRequirementLine(requirement) {
    const pairs = [
      pair("stat", requirement.StatId),
      pair("min", valueOr(requirement.MinValue, 0)),
      pair("weight", valueOr(requirement.Weight, 1)),
      pair("missingTag", requirement.MissingTagId)
    ];
    return "jobRequirement: " + pairs.filter(Boolean).join(" ");
  }

  function writeTextLine(lines, key, value) {
    if (value === undefined || value === null || value === "") {
      return;
    }

    lines.push(key + ": " + value);
  }

  function writeListLine(lines, key, values) {
    if (!values || values.length === 0) {
      return;
    }

    lines.push(key + ": " + arrayOf(values).join(","));
  }

  function pair(key, value) {
    if (value === undefined || value === null || value === "") {
      return "";
    }

    return key + "=" + quoteValue(value);
  }

  function numberPair(key, value) {
    return value === undefined || value === null || value === 0 ? "" : pair(key, value);
  }

  function floatPair(key, value) {
    return value === undefined || value === null || Number(value) === 0 ? "" : pair(key, value);
  }

  function listPair(key, values) {
    const list = arrayOf(values);
    return list.length === 0 ? "" : pair(key, list.join(","));
  }

  function quoteValue(value) {
    const text = String(value);
    if (!/[\s"'=]/.test(text)) {
      return text;
    }

    return "\"" + text.replace(/\\/g, "\\\\").replace(/"/g, "\\\"") + "\"";
  }

  function enumName(mapName, value, fallback) {
    if (typeof value === "number") {
      return enumMaps[mapName] && enumMaps[mapName][value] ? enumMaps[mapName][value] : fallback;
    }

    const normalized = normalizeEnumName(value);
    const entries = enumMaps[mapName] || [];
    for (let i = 0; i < entries.length; i += 1) {
      if (normalizeEnumName(entries[i]) === normalized) {
        return entries[i];
      }
    }

    return value || fallback;
  }

  function colorToHex(color) {
    const r = colorByte(color.r);
    const g = colorByte(color.g);
    const b = colorByte(color.b);
    return "#" + r + g + b;
  }

  function colorByte(value) {
    const byte = Math.max(0, Math.min(255, Math.round(Number(value || 0) * 255)));
    return byte.toString(16).padStart(2, "0");
  }

  function pad2(value) {
    return String(value).padStart(2, "0");
  }

  function arrayOf(value) {
    return Array.isArray(value) ? value : [];
  }

  function valueOr(value, fallback) {
    return value === undefined || value === null ? fallback : value;
  }

  function summarize(pack) {
    return [
      ["属性", arrayOf(pack.Stats).length],
      ["人物标签", arrayOf(pack.Tags).length],
      ["工作标签", arrayOf(pack.WorkTags).length],
      ["公司", arrayOf(pack.Companies).length],
      ["事件", arrayOf(pack.Events).length],
      ["流程小插曲", arrayOf(pack.FlowMoments).length],
      ["后半生规则", arrayOf(pack.LaterLifeRules).length],
      ["稀有度", arrayOf(pack.Rarities).length],
    ];
  }

  return {
    parse,
    toPspack,
    summarize,
    stringify: pack => JSON.stringify(pack, null, 2)
  };
});
