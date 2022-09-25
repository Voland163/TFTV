using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.DamageKeywords;

namespace PRMBetterClasses.Tactical.Entities.DamageKeywords
{
    internal class SharedSoloEffectorDamageKeywordsDataDef
    {
        //public PiercingDamageKeywordDataDef PiercingKeyword;
        //public DamageKeywordDef DamageKeyword;
        //public DamageKeywordDef BlastKeyword;
        //public SyphoningDamageKeywordDataDef SyphonKeyword;
        //public ShreddingDamageKeywordDataDef ShreddingKeyword;
        public AddStatusDamageKeywordDataDef SoloAcidKeyword;
        //public AddStatusDamageKeywordDataDef BleedingKeyword;
        public AddStatusDamageKeywordDataDef SoloPoisonousKeyword;
        //public AddStatModificationDamageKeywordDataDef PsychicKeyword;
        public AddStatusDamageKeywordDataDef SoloViralKeyword;
        public StunDamageKeywordDataDef SoloSonicKeyword;
        public StunDamageKeywordDataDef SoloShockKeyword;
        public AddStatusDamageKeywordDataDef SoloParalysingKeyword;
        //public DamageKeywordDef BurningKeyword;
        //public DamageKeywordDef GooKeyword;
        //public DamageKeywordDef MistKeyword;

        public static readonly string Prefix = "BC_SoloEffector";

        public SharedSoloEffectorDamageKeywordsDataDef()
        {
            SharedDamageKeywordsDataDef sharedDamageKeywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;

            SoloAcidKeyword = Helper.CreateDefFromClone(
                sharedDamageKeywords.AcidKeyword,
                "b7b634c4-851a-4aec-93b1-6ebc03961ca4",
                Prefix + sharedDamageKeywords.AcidKeyword.name);
            SoloAcidKeyword.KeywordApplicationPriority = sharedDamageKeywords.AcidKeyword.KeywordApplicationPriority + 1;
            SoloAcidKeyword.SoloEffector = true;
            SoloAcidKeyword.SingleApplicationPerActor = false;
            SoloAcidKeyword.SingleApplicationPerBodypart = false;

            SoloPoisonousKeyword = Helper.CreateDefFromClone(
                sharedDamageKeywords.PoisonousKeyword,
                "e6c86f7c-5cd4-4bc6-afd1-53f3616eddce",
                Prefix + sharedDamageKeywords.PoisonousKeyword.name);
            SoloPoisonousKeyword.KeywordApplicationPriority = sharedDamageKeywords.PoisonousKeyword.KeywordApplicationPriority + 1;
            SoloPoisonousKeyword.SoloEffector = true;
            SoloPoisonousKeyword.SingleApplicationPerActor = false;
            SoloPoisonousKeyword.SingleApplicationPerBodypart = false;

            SoloViralKeyword = Helper.CreateDefFromClone(
                sharedDamageKeywords.ViralKeyword,
                "058512c3-9cba-4d8e-9f30-cb6273520221",
                Prefix + sharedDamageKeywords.ViralKeyword.name);
            SoloViralKeyword.KeywordApplicationPriority = sharedDamageKeywords.ViralKeyword.KeywordApplicationPriority + 1;
            SoloViralKeyword.SoloEffector = true;
            SoloViralKeyword.SingleApplicationPerActor = false;
            SoloViralKeyword.SingleApplicationPerBodypart = false;

            SoloSonicKeyword = Helper.CreateDefFromClone(
                sharedDamageKeywords.SonicKeyword,
                "63192ff6-aa47-4282-8a6b-4a359136866e",
                Prefix + sharedDamageKeywords.SonicKeyword.name);
            SoloSonicKeyword.KeywordApplicationPriority = sharedDamageKeywords.SonicKeyword.KeywordApplicationPriority + 1;
            SoloSonicKeyword.SoloEffector = true;
            SoloSonicKeyword.SingleApplicationPerActor = false;
            SoloSonicKeyword.SingleApplicationPerBodypart = false;

            SoloShockKeyword = Helper.CreateDefFromClone(
                sharedDamageKeywords.ShockKeyword,
                "9d4be529-3e16-4ebf-8a48-46d5800eebf2",
                Prefix + sharedDamageKeywords.ShockKeyword.name);
            SoloShockKeyword.KeywordApplicationPriority = sharedDamageKeywords.ShockKeyword.KeywordApplicationPriority + 1;
            SoloShockKeyword.SoloEffector = true;
            SoloShockKeyword.SingleApplicationPerActor = false;
            SoloShockKeyword.SingleApplicationPerBodypart = false;

            SoloParalysingKeyword = Helper.CreateDefFromClone(
                sharedDamageKeywords.ParalysingKeyword,
                "e56f11e2-5fbb-4b42-ab43-734c929418d1",
                Prefix + sharedDamageKeywords.ParalysingKeyword.name);
            SoloParalysingKeyword.KeywordApplicationPriority = sharedDamageKeywords.ParalysingKeyword.KeywordApplicationPriority + 1;
            SoloParalysingKeyword.SoloEffector = true;
            SoloParalysingKeyword.SingleApplicationPerActor = false;
            SoloParalysingKeyword.SingleApplicationPerBodypart = false;
        }
    }
}
