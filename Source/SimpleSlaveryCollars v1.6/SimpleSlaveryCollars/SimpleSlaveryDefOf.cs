// SimpleSlaveryCollars | Core | SSC_DefOf.cs
// 목적   : 모드에서 사용하는 Hediff, Job, MentalState, Thought, Record, BodyPart, Trait 정의를 DefOf로 캐싱
// 용도   : RimWorld DefOf 시스템을 통해 XML 정의를 C#에서 직접 참조 가능하게 함
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : EnsureInitializedInCtor 호출 필수 (DefOfHelper) — DefOf 사용 시 Null 참조 방지

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// Simple Slavery Collars 모드에서 사용하는 모든 DefOf 데이터
    /// </summary>
    [DefOf]
    public static class SimpleSlaveryDefOf
    {
        static SimpleSlaveryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SimpleSlaveryDefOf));
        }

        // --- HediffDefs ---
        public static HediffDef Crypto_Stasis;
        public static HediffDef Electrocuted;
        public static HediffDef Enslaved;

        // --- JobDefs ---
        public static JobDef SetSlaveCollar;
        public static JobDef ShackleSlave;
        public static JobDef ActivateRemoteCollar;
        public static JobDef ActivateRemoteCollarGroup;

        // --- MentalStateDefs ---
        public static MentalStateDef CryptoStasis;

        // --- ThoughtDefs ---
        public static ThoughtDef SlaveCollar;
        public static ThoughtDef ExplosiveCollar;
        public static ThoughtDef WasEnslaved_Assimilation;

        // --- RecordDefs ---
        public static RecordDef TimeAsSlave;

        // --- BodyPartDefs ---
        public static BodyPartDef Neck;

        // --- TraitDefOfs ---
        public static TraitDef Nerves;
    }
}
