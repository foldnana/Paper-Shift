using PaperShift.Data;

namespace PaperShift.Runtime
{
    public enum InterviewStepOutcome
    {
        Continue,
        Passed,
        Failed,
        Event
    }

    public sealed class InterviewStepResult
    {
        public readonly InterviewStepOutcome Outcome;
        public readonly string Message;
        public readonly TriggeredEvent TriggeredEvent;
        public readonly int RecognitionBefore;
        public readonly int RecognitionAfter;

        public int RecognitionDelta
        {
            get { return RecognitionAfter - RecognitionBefore; }
        }

        private InterviewStepResult(InterviewStepOutcome outcome, string message, TriggeredEvent triggeredEvent, int recognitionBefore, int recognitionAfter)
        {
            Outcome = outcome;
            Message = message;
            TriggeredEvent = triggeredEvent;
            RecognitionBefore = recognitionBefore;
            RecognitionAfter = recognitionAfter;
        }

        public static InterviewStepResult Continue(string message, int recognitionBefore, int recognitionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Continue, message, null, recognitionBefore, recognitionAfter);
        }

        public static InterviewStepResult Passed(string message, int recognitionBefore, int recognitionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Passed, message, null, recognitionBefore, recognitionAfter);
        }

        public static InterviewStepResult Failed(string message)
        {
            return new InterviewStepResult(InterviewStepOutcome.Failed, message, null, 0, 0);
        }

        public static InterviewStepResult Failed(string message, int recognitionBefore, int recognitionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Failed, message, null, recognitionBefore, recognitionAfter);
        }

        public static InterviewStepResult Event(TriggeredEvent triggeredEvent, int recognitionBefore, int recognitionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Event, string.Empty, triggeredEvent, recognitionBefore, recognitionAfter);
        }
    }

    public enum ProbationStepOutcome
    {
        Continue,
        Passed,
        Failed,
        Event
    }

    public sealed class ProbationStepResult
    {
        public readonly ProbationStepOutcome Outcome;
        public readonly string Message;
        public readonly TriggeredEvent TriggeredEvent;
        public readonly int RecognitionBefore;
        public readonly int RecognitionAfter;
        public readonly int StressBefore;
        public readonly int StressAfter;

        public int RecognitionDelta
        {
            get { return RecognitionAfter - RecognitionBefore; }
        }

        public int StressDelta
        {
            get { return StressAfter - StressBefore; }
        }

        private ProbationStepResult(ProbationStepOutcome outcome, string message, TriggeredEvent triggeredEvent, int recognitionBefore, int recognitionAfter, int stressBefore, int stressAfter)
        {
            Outcome = outcome;
            Message = message;
            TriggeredEvent = triggeredEvent;
            RecognitionBefore = recognitionBefore;
            RecognitionAfter = recognitionAfter;
            StressBefore = stressBefore;
            StressAfter = stressAfter;
        }

        public static ProbationStepResult Continue(string message, int recognitionBefore, int recognitionAfter, int stressBefore, int stressAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Continue, message, null, recognitionBefore, recognitionAfter, stressBefore, stressAfter);
        }

        public static ProbationStepResult Passed(string message, int recognitionBefore, int recognitionAfter, int stressBefore, int stressAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Passed, message, null, recognitionBefore, recognitionAfter, stressBefore, stressAfter);
        }

        public static ProbationStepResult Failed(string message, int recognitionBefore, int recognitionAfter, int stressBefore, int stressAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Failed, message, null, recognitionBefore, recognitionAfter, stressBefore, stressAfter);
        }

        public static ProbationStepResult Event(TriggeredEvent triggeredEvent, int recognitionBefore, int recognitionAfter, int stressBefore, int stressAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Event, string.Empty, triggeredEvent, recognitionBefore, recognitionAfter, stressBefore, stressAfter);
        }
    }

    public sealed class TriggeredEvent
    {
        public readonly GameEventDefinition Event;
        public readonly EventOptionDefinition[] Options;

        public TriggeredEvent(GameEventDefinition gameEvent, EventOptionDefinition[] options)
        {
            Event = gameEvent;
            Options = options;
        }
    }

    public sealed class EventOptionChoiceResult
    {
        public readonly bool WasApplied;
        public readonly EventOptionDefinition Option;
        public readonly FlowCheckpointResult CheckpointResult;

        public TriggeredEvent TriggeredEvent
        {
            get { return CheckpointResult == null ? null : CheckpointResult.TriggeredEvent; }
        }

        private EventOptionChoiceResult(bool applied, EventOptionDefinition option, FlowCheckpointResult checkpointResult)
        {
            WasApplied = applied;
            Option = option;
            CheckpointResult = checkpointResult;
        }

        public static EventOptionChoiceResult Applied(EventOptionDefinition option, FlowCheckpointResult checkpointResult)
        {
            return new EventOptionChoiceResult(true, option, checkpointResult);
        }

        public static EventOptionChoiceResult Ignored()
        {
            return new EventOptionChoiceResult(false, null, null);
        }
    }
}
