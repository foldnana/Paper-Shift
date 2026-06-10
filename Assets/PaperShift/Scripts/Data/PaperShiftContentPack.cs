using System;

namespace PaperShift.Data
{
    [Serializable]
    public sealed class PaperShiftContentPack
    {
        public string Format = "PaperShiftContentPack";
        public int Version = 1;
        public string MergeMode = "merge";
        public RarityDefinition[] Rarities = new RarityDefinition[0];
        public StatDefinition[] Stats = new StatDefinition[0];
        public TagDefinition[] Tags = new TagDefinition[0];
        public WorkTagDefinition[] WorkTags = new WorkTagDefinition[0];
        public CompanyDefinition[] Companies = new CompanyDefinition[0];
        public GameEventDefinition[] Events = new GameEventDefinition[0];
        public FlowMomentDefinition[] FlowMoments = new FlowMomentDefinition[0];
        public LaterLifeRuleDefinition[] LaterLifeRules = new LaterLifeRuleDefinition[0];
        public string[] LastNames = new string[0];
        public string[] MaleFirstNames = new string[0];
        public string[] FemaleFirstNames = new string[0];
    }
}
