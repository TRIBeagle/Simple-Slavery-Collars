// SimpleSlaveryCollars | Utilities | SimpleSlaveryUtility.cs
// 목적   : 노예 관련 공용 유틸 함수 집합
// 용도   : Stage 판정, 칼라 제어, 정신붕괴/심장발작 유발, 시간 기록 관리, UI 표시 문자열 처리
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 기존 주석 제거 후 요약 주석 추가
// 주의   : Stage5 = (x ≥ SlaveStage4 && !Steadfast), Stage4 = (S3 < x < S4) 또는 (x ≥ S4 && Steadfast)
// 저장   : TimeAsSlaveTicks는 CompSlave가 진실원천, Record는 하위호환 폴백

using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars.Utilities
{
    /// <summary>
    /// 노예 제어/판정/표시 관련 유틸리티 모음.
    /// Stage 정의: Stage4 = (S3 < x < S4) 또는 (x ≥ S4 && Steadfast), Stage5 = (x ≥ S4 && !Steadfast).
    /// </summary>
    public static class SimpleSlaveryUtility
    {
        /// <summary>
        /// Pawn이 Colony 구성원(Colonist/PrisonerOfColony/SlaveOfColony)인지 판정합니다.
        /// </summary>
        public static bool IsColonyMember(Pawn pawn)
        {
            return pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }

        /// <summary>
        /// Pawn이 정착민이거나 Stage5 노예인지 여부를 판정합니다. Stage5는 (x ≥ SlaveStage4) && !Steadfast 조건입니다.
        /// </summary>
        public static bool IsStage5Slave(Pawn pawn)
        {
            if (pawn == null) return false;

            if (!pawn.IsSlaveOfColony) return false;

            float timeAsSlave = SimpleSlaveryUtility.TimeAsSlave(pawn);
            bool steadfast = SimpleSlaveryUtility.IsSteadfast(pawn);

            if (timeAsSlave >= SimpleSlaveryUtility.SlaveStage4 && !steadfast)
                return true;

            return false;
        }

        /// <summary>
        /// 지정 Apparel이 SlaveCollar 태그를 가진 칼라인지 판정합니다.
        /// </summary>
        public static bool IsSlaveCollar(Apparel apparel)
        {
            return apparel?.def?.apparel?.defaultOutfitTags?.Contains("SlaveCollar") ?? false;
        }

        /// <summary>
        /// Pawn이 착용한 SlaveCollar 인스턴스를 반환합니다. 없으면 null을 반환합니다.
        /// </summary>
        public static Apparel GetSlaveCollar(Pawn pawn)
        {
            // O(1) 캐시 조회 우선
            var cached = SlaveCollarRegistry.GetCached(pawn);
            if (cached != null) return cached;

            // 폴백: WornApparel 순회
            if (pawn?.apparel == null) return null;
            var worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (IsSlaveCollar(worn[i]))
                {
                    // 폴백에서 찾으면 레지스트리에 자가 복구 등록
                    if (worn[i] is SlaveApparel sa)
                        SlaveCollarRegistry.Register(pawn, sa);
                    return worn[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Pawn에게 지정한 SlaveCollar를 착용시키고 강제 착용 목록에 등록합니다.
        /// </summary>
        public static void GiveSlaveCollar(Pawn pawn, Apparel collar = null)
        {
            if (pawn == null)
            {
                Log.Error("Tried to give a collar to a null pawn.");
                return;
            }

            pawn.apparel.Wear(collar, true);

            if (pawn.outfits == null)
            {
                pawn.outfits = new Pawn_OutfitTracker();
            }

            pawn.outfits.forcedHandler.SetForced(collar, true);
        }

        /// <summary>
        /// 지정 확률로 즉시 정신붕괴를 유발합니다. 기본 메시지는 폭발 칼라 무장 사유를 사용합니다.
        /// </summary>
        public static void TryInstantBreak(Pawn pawn, float chance, MentalStateDef breakDef)
        {
            if (pawn.Downed) return;
            if (pawn.jobs?.curDriver?.asleep == true) return;
            if (pawn.InMentalState) return;
            if (pawn.mindState?.mentalStateHandler == null) return;

            if (Rand.Chance(chance))
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(breakDef, "SSC_Explosive_Armed_Reason".Translate(pawn.Name.ToStringShort));
            }
        }

        /// <summary>
        /// Berserk 기본 정신상태로 즉시 붕괴를 시도합니다.
        /// </summary>
        public static void TryInstantBreak(Pawn pawn, float chance)
        {
            TryInstantBreak(pawn, chance, MentalStateDefOf.Berserk);
        }

        /// <summary>
        /// Pawn에게 나이 기반 확률로 심장마비를 유발하려 시도합니다. 성공 시 편지를 발송합니다.
        /// </summary>
        public static void TryHeartAttack(Pawn pawn)
        {
            int age = pawn.ageTracker.AgeBiologicalYears;
            float oldAge = pawn.RaceProps.lifeExpectancy;

            float youngAge = 30f;
            float minChance = 0.0001f;
            float maxChance = 0.01f;

            float chance = Math.Max(((Math.Min(Math.Max(age, youngAge), oldAge) - youngAge) / (oldAge - youngAge)) * maxChance, minChance);
            BodyPartRecord heart = FindBodyPart(pawn, BodyPartDefOf.Heart);

            if (heart != null && Rand.Chance(chance))
            {
                pawn.health.AddHediff(SimpleSlaveryDefOf.HeartAttack, heart);

                string text = "SSC_Letter_HeartAttack".Translate(pawn.Name.ToString());
                Find.LetterStack.ReceiveLetter("SSC_Letter_HeartAttackLabel".Translate(), text, LetterDefOf.NegativeEvent);
            }
        }

        /// <summary>
        /// Pawn이 동화 불가(Steadfast)인지 판정합니다.
        /// 바닐라 확고한 충성심(Recruitable=false) 상태면 Stage5 진입 불가.
        /// </summary>
        public static bool IsSteadfast(Pawn pawn)
        {
            if (pawn?.guest == null) return false;
            if (SimpleSlaveryCollarsSetting.IgnoreUnwaveringLoyalty) return false;
            return !pawn.guest.Recruitable;
        }

        // 칼라 장착 시 정신붕괴 확률 체크용 (IsSteadfast와 무관)
        private static volatile TraitDef _wimpTraitDef;
        internal static TraitDef WimpTrait => _wimpTraitDef ?? (_wimpTraitDef = TraitDef.Named("Wimp"));

        /// <summary>
        /// CompSlave(TimeAsSlaveTicks)를 우선으로 노예 경과 시간을 반환합니다. Comp 없으면 Record 폴백.
        /// </summary>
        public static float TimeAsSlave(Pawn pawn)
        {
            CompSlave comp = pawn?.TryGetComp<CompSlave>();
            if (comp != null) return comp.TimeAsSlaveTicks;
            return pawn?.records?.GetValue(SimpleSlaveryDefOf.TimeAsSlave) ?? 0f;
        }

        /// <summary>
        /// 틱 단위 노예 시간을 UI 표기용 문자열로 변환합니다. 옵션이 켜져 있으면 Stage 접미사를 추가합니다.
        /// </summary>
        public static string FormatEnslaveDurationReadable(Pawn pawn, float ticks)
        {
            if (ticks < 0f) ticks = 0f;

            int TicksPerDay = GenDate.TicksPerDay;
            int TicksPerHour = GenDate.TicksPerHour;

            int totalDays = Mathf.FloorToInt(ticks / (float)TicksPerDay);

            if (totalDays < 1)
            {
                int hours = Mathf.FloorToInt(ticks / (float)TicksPerHour);
                return AddSlaveStageSuffix(pawn, "SSC_SlaveTime_Hours".Translate(hours), ticks);
            }

            if (totalDays < 15)
            {
                return AddSlaveStageSuffix(pawn, "SSC_SlaveTime_Days".Translate(totalDays), ticks);
            }

            if (totalDays < 60)
            {
                int quadrum = totalDays / 15;
                int dayInQuadrum = totalDays % 15;
                return AddSlaveStageSuffix(pawn, "SSC_SlaveTime_QuadrumDays".Translate(quadrum, dayInQuadrum), ticks);
            }

            int years = totalDays / 60;
            int remainder = totalDays % 60;
            int quadrumY = remainder / 15;
            int dayInQuadrumY = remainder % 15;

            return AddSlaveStageSuffix(pawn, "SSC_SlaveTime_YearQuadrumDays".Translate(years, quadrumY, dayInQuadrumY), ticks);
        }

        /// <summary>
        /// Stage 접미사(Suffix)를 추가합니다.
        /// Stage5는 (x ≥ S4 && !Steadfast), 그 외는 Stage4 이하로 취급됩니다.
        /// </summary>
        private static string AddSlaveStageSuffix(Pawn pawn, string baseText, float ticks)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable) return baseText;

            float daysTotal = Mathf.Max(0f, ticks / GenDate.TicksPerDay);

            float s1 = SimpleSlaveryCollarsSetting.Slavestage1Period;
            float s2 = SimpleSlaveryCollarsSetting.Slavestage2Period;
            float s3 = SimpleSlaveryCollarsSetting.Slavestage3Period;
            float s4 = SimpleSlaveryCollarsSetting.Slavestage4Period;

            int stage = 1;

            if (daysTotal >= s1) stage = 2;
            if (daysTotal >= s1 + s2) stage = 3;
            if (daysTotal >= s1 + s2 + s3) stage = 4;
            if (daysTotal >= s1 + s2 + s3 + s4) stage = 5;

            if (stage >= 5 && pawn != null && IsSteadfast(pawn)) stage = 4;

            string tail = "SSC_Stage_Suffix".Translate(stage);
            return $"{baseText} {tail}";
        }

        /// <summary>바닐라 BodyDef.GetPartsWithDef (Dictionary 캐시) 기반 부위 검색.</summary>
        internal static BodyPartRecord FindBodyPart(Pawn pawn, BodyPartDef def)
        {
            var parts = pawn.RaceProps.body.GetPartsWithDef(def);
            return (parts != null && parts.Count > 0) ? parts[0] : null;
        }

        /// <summary>Neck 부위 검색 후 없으면 corePart로 폴백. 칼라 효과 적용 시 공용.</summary>
        internal static BodyPartRecord GetNeckOrCorePart(Pawn pawn)
        {
            if (pawn == null) return null;
            return FindBodyPart(pawn, SimpleSlaveryDefOf.Neck) ?? pawn.RaceProps.body.corePart;
        }

        #region Stage 경계값 틱 캐시
        // 설정은 게임 중 변경 안됨. 같은 틱 내 반복 곱셈 제거
        private static int _stageCacheTick = -1;
        private static float _cachedS1, _cachedS2, _cachedS3, _cachedS4;

        /// <summary>틱 단위 캐시 갱신. 같은 틱이면 스킵.</summary>
        private static void RefreshStageCacheIfNeeded()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick == _stageCacheTick) return;
            _stageCacheTick = tick;

            float tpd = GenDate.TicksPerDay;
            _cachedS1 = tpd * SimpleSlaveryCollarsSetting.Slavestage1Period;
            _cachedS2 = _cachedS1 + tpd * SimpleSlaveryCollarsSetting.Slavestage2Period;
            _cachedS3 = _cachedS2 + tpd * SimpleSlaveryCollarsSetting.Slavestage3Period;
            _cachedS4 = _cachedS3 + tpd * SimpleSlaveryCollarsSetting.Slavestage4Period;
        }

        /// <summary>Stage1 경계 틱 값입니다.</summary>
        public static float SlaveStage1 { get { RefreshStageCacheIfNeeded(); return _cachedS1; } }

        /// <summary>Stage2 경계 틱 값입니다.</summary>
        public static float SlaveStage2 { get { RefreshStageCacheIfNeeded(); return _cachedS2; } }

        /// <summary>Stage3 경계 틱 값입니다.</summary>
        public static float SlaveStage3 { get { RefreshStageCacheIfNeeded(); return _cachedS3; } }

        /// <summary>Stage4 경계 틱 값입니다. Stage5는 x ≥ Stage4 && !Steadfast 입니다.</summary>
        public static float SlaveStage4 { get { RefreshStageCacheIfNeeded(); return _cachedS4; } }
        #endregion
    }
}
