namespace PaperShift.Domain
{
    public enum PaperShiftPhase
    {
        CreateWorker,
        SelectTags,
        EditResume,
        Interview,
        Probation,
        Working,
        Budget,
        EventModal,
        Retirement
    }

    public enum TagScope
    {
        Worker,
        Company,
        Job,
        Heir,
        Intent,
        Event
    }

    public enum TagPolarity
    {
        Neutral,
        Positive,
        Negative,
        Mixed
    }

    public enum ResumePackagingMode
    {
        Hide,
        Normal,
        Exaggerate,
        Fake
    }

    public enum GameEventPhase
    {
        Any,
        Interview,
        Probation,
        WorkYear,
        Budget,
        Retirement
    }

    public enum FitDimension
    {
        Maturity,
        Physique,
        Presence,
        Credentials,
        Professionalism,
        Execution,
        Communication,
        Resilience
    }

    public enum RequirementTarget
    {
        FitDimension,
        RawAttribute,
        WorkerTag,
        WorkTag,
        ResumeField
    }

    public enum FlowDirective
    {
        None,
        DirectPass,
        DirectFail,
        EndRun,
        ReturnToJobSearch
    }

    public enum EventNoticeType
    {
        Log,
        Banner,
        Modal
    }

    public enum CompareOperator
    {
        Equal,
        NotEqual,
        GreaterOrEqual,
        Greater,
        LessOrEqual,
        Less
    }

    public enum ConditionKind
    {
        Always,
        Stat,
        HasTag,
        MissingTag,
        BudgetAtLeast,
        BudgetAtMost,
        Phase,
        AgeAtLeast,
        AgeAtMost,
        MoneyAtLeast,
        CompanyHasTag,
        JobHasTag,
        RandomChance,
        EventSeen,
        WorkYearsAtLeast,
        InterviewProgressAtLeast,
        ResumeRiskAtLeast,
        StressAtLeast,
        StressAtMost,
        RecognitionAtLeast,
        RecognitionAtMost
    }

    public enum EffectTiming
    {
        Immediate,
        Passive
    }

    public enum EffectKind
    {
        None,
        AddStat,
        SetStat,
        AddMoney,
        AddStress,
        AddHealth,
        AddTag,
        RemoveTag,
        AddHeir,
        AddLog,
        AddBanner,
        AddInterviewProgress,
        SetInterviewProgress,
        AddPromotionProgress,
        AddQuitRisk,
        AddResumeRisk,
        EndRun,
        PassiveStatBonus,
        AddFitScore,
        AddRecognition,
        SetRecognition,
        AddJobWeight,
        AddEventWeight,
        TriggerEvent,
        DirectPass,
        DirectFail,
        ReturnToJobSearch,
        PassiveInterviewScore,
        PassiveSalaryPercent,
        PassiveEventWeight,
        PassiveStressPerYear
    }

    public enum RunEndReason
    {
        None,
        Retired,
        Fired,
        Quit,
        HealthCollapse,
        StressCollapse,
        Accident,
        Custom
    }
}
