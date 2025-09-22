// SimpleSlaveryCollars | Core | DefOf.cs
// 목적   : 모드에서 사용하는 Hediff, Job, MentalState, Thought, Record, BodyPart, Trait 정의를 DefOf로 캐싱
// 용도   : RimWorld DefOf 시스템을 통해 XML 정의를 C#에서 직접 참조 가능하게 함
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : EnsureInitializedInCtor 호출 필수 (DefOfHelper) — DefOf 사용 시 Null 참조 방지

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// HediffDef 캐시 (노예 전용 헤디프).
    /// </summary>
    [DefOf]
    public static class SSC_HediffDefOf
    {
        static SSC_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }
        public static HediffDef Crypto_Stasis;
        public static HediffDef Electrocuted;
        public static HediffDef Enslaved;
    }

    /// <summary>
    /// JobDef 캐시 (노예 전용 작업).
    /// </summary>
    [DefOf]
    public static class SSC_JobDefOf
    {
        static SSC_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
        public static JobDef SetSlaveCollar;
        public static JobDef ShackleSlave;
        public static JobDef ActivateRemoteCollar;
        public static JobDef ActivateRemoteCollarGroup;
    }

    /// <summary>
    /// MentalStateDef 캐시 (CryptoStasis 정신 상태).
    /// </summary>
    [DefOf]
    public static class SSC_MentalStateDefOf
    {
        static SSC_MentalStateDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MentalStateDefOf));
        }
        public static MentalStateDef CryptoStasis;
    }

    /// <summary>
    /// ThoughtDef 캐시 (노예 전용 사상).
    /// </summary>
    [DefOf]
    public static class SSC_ThoughtDefOf
    {
        static SSC_ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThoughtDefOf));
        }
        public static ThoughtDef SlaveCollar;
        public static ThoughtDef ExplosiveCollar;
        public static ThoughtDef WasEnslaved_Assimilation;
    }

    /// <summary>
    /// RecordDef 캐시 (노예화 경과 시간 기록).
    /// </summary>
    [DefOf]
    public static class SSC_RecordDefOf
    {
        static SSC_RecordDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RecordDefOf));
        }
        public static RecordDef TimeAsSlave;
    }

    /// <summary>
    /// BodyPartDef 캐시 (칼라 데미지 대상 파츠).
    /// </summary>
    [DefOf]
    public static class SSC_BodyPartDefOf
    {
        static SSC_BodyPartDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BodyPartDefOf));
        }
        public static BodyPartDef Neck;
    }

    /// <summary>
    /// TraitDef 캐시 (노예 관련 특성).
    /// </summary>
    [DefOf]
    public static class SSC_TraitDefOf
    {
        static SSC_TraitDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TraitDefOf));
        }
        public static TraitDef Nerves;
    }
}
