using System.Collections.Generic;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class FlowRuleResult
    {
        private readonly Dictionary<string, int> eventWeights = new Dictionary<string, int>();

        public int RecognitionDelta;
        public int? RecognitionOverride;
        public int JobWeightDelta;
        public int StressDelta;
        public int ResumeRiskDelta;
        public FlowDirective Directive = FlowDirective.None;
        public string TriggerEventId;
        public readonly List<string> Logs = new List<string>();

        public IDictionary<string, int> EventWeights
        {
            get { return eventWeights; }
        }

        public void AddEventWeight(string eventId, int delta)
        {
            if (string.IsNullOrEmpty(eventId) || delta == 0)
            {
                return;
            }

            eventWeights.TryGetValue(eventId, out var current);
            eventWeights[eventId] = current + delta;
        }

        public int EventWeight(string eventId)
        {
            return !string.IsNullOrEmpty(eventId) && eventWeights.TryGetValue(eventId, out var value) ? value : 0;
        }

        public void SetDirective(FlowDirective directive)
        {
            if (directive == FlowDirective.None || Directive != FlowDirective.None)
            {
                return;
            }

            Directive = directive;
        }
    }
}
