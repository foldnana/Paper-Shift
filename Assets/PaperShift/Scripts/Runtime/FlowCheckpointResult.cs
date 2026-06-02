using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public enum FlowCheckpointAction
    {
        PrepareInterview,
        AttendInterview,
        WorkProbation,
        ApplyRegularization,
        EventChoice
    }

    public enum FlowCheckpointOutcome
    {
        Continue,
        Passed,
        Failed,
        Event
    }

    public sealed class FlowCheckpointResult
    {
        public readonly FlowCheckpointAction Action;
        public readonly GameEventPhase Phase;
        public readonly FlowCheckpointOutcome Outcome;
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

        private FlowCheckpointResult(
            FlowCheckpointAction action,
            GameEventPhase phase,
            FlowCheckpointOutcome outcome,
            string message,
            TriggeredEvent triggeredEvent,
            int recognitionBefore,
            int recognitionAfter,
            int stressBefore,
            int stressAfter)
        {
            Action = action;
            Phase = phase;
            Outcome = outcome;
            Message = message;
            TriggeredEvent = triggeredEvent;
            RecognitionBefore = recognitionBefore;
            RecognitionAfter = recognitionAfter;
            StressBefore = stressBefore;
            StressAfter = stressAfter;
        }

        public static FlowCheckpointResult Continue(FlowCheckpointAction action, GameEventPhase phase, string message, FlowCheckpointSnapshot snapshot)
        {
            return Create(action, phase, FlowCheckpointOutcome.Continue, message, null, snapshot);
        }

        public static FlowCheckpointResult Passed(FlowCheckpointAction action, GameEventPhase phase, string message, FlowCheckpointSnapshot snapshot)
        {
            return Create(action, phase, FlowCheckpointOutcome.Passed, message, null, snapshot);
        }

        public static FlowCheckpointResult Failed(FlowCheckpointAction action, GameEventPhase phase, string message, FlowCheckpointSnapshot snapshot)
        {
            return Create(action, phase, FlowCheckpointOutcome.Failed, message, null, snapshot);
        }

        public static FlowCheckpointResult Event(FlowCheckpointAction action, GameEventPhase phase, TriggeredEvent triggeredEvent, FlowCheckpointSnapshot snapshot)
        {
            return Create(action, phase, FlowCheckpointOutcome.Event, string.Empty, triggeredEvent, snapshot);
        }

        private static FlowCheckpointResult Create(
            FlowCheckpointAction action,
            GameEventPhase phase,
            FlowCheckpointOutcome outcome,
            string message,
            TriggeredEvent triggeredEvent,
            FlowCheckpointSnapshot snapshot)
        {
            return new FlowCheckpointResult(
                action,
                phase,
                outcome,
                message,
                triggeredEvent,
                snapshot.RecognitionBefore,
                snapshot.RecognitionAfter,
                snapshot.StressBefore,
                snapshot.StressAfter);
        }
    }

    public struct FlowCheckpointSnapshot
    {
        public int RecognitionBefore;
        public int RecognitionAfter;
        public int StressBefore;
        public int StressAfter;

        public int RecognitionDelta
        {
            get { return RecognitionAfter - RecognitionBefore; }
        }

        public int StressDelta
        {
            get { return StressAfter - StressBefore; }
        }

        public static FlowCheckpointSnapshot Capture(PaperShiftRunState state, GameEventPhase phase)
        {
            var recognition = ReadRecognition(state, phase);
            var stress = state == null || state.Worker == null ? 0 : state.Worker.Stress;
            return new FlowCheckpointSnapshot
            {
                RecognitionBefore = recognition,
                RecognitionAfter = recognition,
                StressBefore = stress,
                StressAfter = stress
            };
        }

        public void CaptureAfter(PaperShiftRunState state, GameEventPhase phase)
        {
            RecognitionAfter = ReadRecognition(state, phase);
            StressAfter = state == null || state.Worker == null ? 0 : state.Worker.Stress;
        }

        private static int ReadRecognition(PaperShiftRunState state, GameEventPhase phase)
        {
            if (state == null)
            {
                return 0;
            }

            if (phase == GameEventPhase.Probation)
            {
                return state.CurrentJob == null ? 0 : state.CurrentJob.Recognition;
            }

            return state.Interview == null ? 0 : state.Interview.Recognition;
        }
    }
}
