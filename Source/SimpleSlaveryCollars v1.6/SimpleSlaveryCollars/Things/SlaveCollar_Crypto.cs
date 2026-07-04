// SimpleSlaveryCollars | Things | SlaveCollar_Crypto.cs
// 목적 : 동결 노예 칼라. 무장(armed) 시 착용자를 정신동결(CryptoStasis) 상태로 고정
// 용도 : 직접 토글 또는 원격 콘솔에서 제어. 비폭력 제압 수단
// 주의 : RevertMentalState()에서 이전 정신상태 복원. mindState null 방어 필수

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Gizmos;

namespace SimpleSlaveryCollars
{
    public class SlaveCollar_Crypto : SlaveApparel
    {
        // 이펙트 쿨다운 — 저장 불필요 (시각 효과 전용)
        private int _fleckCooldown;
        // Hediff 존재 보장/침대 자동해제 검사 간격 카운터 — 저장 불필요 (성능)
        private int _stasisCheckCooldown;
        // 로드 후 1회 일관성 검증 플래그 — 저장 불필요
        private bool _postLoadValidated;

        public override RemoteCollarAction? ArmAction => RemoteCollarAction.ArmCrypto;
        public override RemoteCollarAction? DisarmAction => RemoteCollarAction.DisarmCrypto;
        public override int CollarSortKey => 2;
        public override string TypeLabelKey => "SSC_Console_CollarCrypto";

        /// <summary>무장은 armed만 올림(동결은 Tick에서 적용). 해제 시 정신상태 복원.</summary>
        public override void SetArmed(bool active, Pawn pawn)
        {
            armed = active;
            if (!active) RevertMentalStateFor(pawn);
        }

        /// <summary>스트립 시 무장 해제 + (생존 시) 정신상태 복원.</summary>
        public override void NotifyStripped(Pawn pawn)
        {
            armed = false;
            if (!pawn.Dead) RevertMentalStateFor(pawn);
        }


        public override IEnumerable<Gizmo> SlaveGizmos()
        {
            // 충전 기즈모 (충전 ON일 때만)
            if (SimpleSlaveryCollarsSetting.CollarChargeEnable)
                yield return new Gizmo_SlaveCollarStatus { collar = this };

            if (SimpleSlaveryCollarsSetting.RemoteOnlyOnConsoleEnable)
            {
                yield return MakeRemoteOnlyDisabledGizmo("UI/Commands/DetonateCollar_Crypto");
                yield break;
            }
            // 1. Arm the collar
            yield return MakeArmedToggle("SSC_Crypto_Arm", "SSC_Crypto_Arm_Desc",
                "UI/Commands/DetonateCollar_Crypto", () => SetArmed(!IsArmed, Wearer));

        }

        /// <summary>Wearer 기준 정신상태 복원(내부 호출용 래퍼).</summary>
        public void RevertMentalState() => RevertMentalStateFor(Wearer);

        /// <summary>
        /// 지정 Pawn의 CryptoStasis Hediff 제거 + 이전 정신상태 복원 + 후유증(33%).
        /// Wearer(RevertMentalState)와 Unequip/Strip 시점 pawn 인자 버전을 통합.
        /// </summary>
        private void RevertMentalStateFor(Pawn pawn)
        {
            var hediffSet = pawn?.health?.hediffSet;
            if (hediffSet == null)
                return;

            Hediff cryptoStasis = hediffSet.GetFirstHediffOfDef(SimpleSlaveryDefOf.Crypto_Stasis);
            if (cryptoStasis == null)
                return;

            // 이전 정신상태 복원 시도
            var stasisHediff = cryptoStasis as Hediff_CryptoStasis;
            var savedMentalState = stasisHediff?.revertMentalStateDef;

            if (pawn.mindState?.mentalStateHandler != null)
            {
                if (savedMentalState != null)
                    pawn.mindState.mentalStateHandler.TryStartMentalState(savedMentalState, reason: null, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
                else
                    pawn.mindState.mentalStateHandler.Reset();
            }

            // Hediff 제거 + 크립토슬립 후유증 (33%)
            pawn.health.RemoveHediff(cryptoStasis);
            if (Rand.Value > 0.66f)
                pawn.health.AddHediff(HediffDefOf.CryptosleepSickness);
        }


        /// <summary>
        /// - 크립토 스테이시스 적용.
        /// - Hediff 캐스트 실패(XML hediffClass 미설정 등) 시 NRE 방지
        /// </summary>
        public void CryptoStasis()
        {
            // [성능] Hediff 존재 보장 + 침대 자동해제 검사는 매 틱 불필요 — 30틱 간격으로만 수행.
            // 정신상태 강제(아래)는 값싸므로 매 틱 유지해 동결 상태를 즉시 재확립.
            if (_stasisCheckCooldown <= 0)
            {
                _stasisCheckCooldown = 30;

                if (!Wearer.health.hediffSet.HasHediff(SimpleSlaveryDefOf.Crypto_Stasis))
                {
                    Wearer.health.AddHediff(SimpleSlaveryDefOf.Crypto_Stasis);
                    // 캐스트 실패 방어 — XML에서 hediffClass가 Hediff_CryptoStasis가 아니면 null
                    var stasisHediff = Wearer.health.hediffSet.GetFirstHediffOfDef(SimpleSlaveryDefOf.Crypto_Stasis) as Hediff_CryptoStasis;
                    stasisHediff?.SaveMemory();
                }
                if (Wearer.InBed())
                {
                    armed = false;
                    RevertMentalState();
                    return;
                }
            }
            else
            {
                _stasisCheckCooldown--;
            }

            // mindState/mentalStateHandler null 방어 — 로딩 중, 디스폰 직후 등
            if (Wearer.mindState?.mentalStateHandler == null) return;
            if (Wearer.mindState.mentalStateHandler.CurStateDef != SimpleSlaveryDefOf.CryptoStasis)
                Wearer.mindState.mentalStateHandler.TryStartMentalState(SimpleSlaveryDefOf.CryptoStasis, reason: null, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
            // 이펙트 빈도 조절: 매 틱 33% → 10틱 간격 (성능 개선, 시각 차이 미미)
            if (_fleckCooldown <= 0 && Wearer.Map != null)
            {
                FleckMaker.ThrowTornadoDustPuff(Wearer.TrueCenter() + Vector3Utility.RandomHorizontalOffset(0.5f), Wearer.Map, Rand.Range(0.25f, 1f), new Color(0.65f, 0.9f, 0.93f));
                _fleckCooldown = 10;
            }
            else
            {
                _fleckCooldown--;
            }
        }

        // armed(ssc_armed)는 베이스 SlaveApparel에서 저장/로드하므로 ExposeData 오버라이드 불필요.

        public override void Notify_Unequipped(Pawn pawn)
        {
            bool wasArmed = armed;
            base.Notify_Unequipped(pawn); // armed=false + 레지스트리 해제
            // Notify_Unequipped 시점에는 Wearer가 null일 수 있으므로 pawn 인자 사용
            if (wasArmed)
                RevertMentalStateFor(pawn);
        }

        protected override void TickInterval(int delta)
        {
            // [FIX] 로드 후 armed=true + CryptoStasis Hediff 누락 좀비 상태 검증
            if (!_postLoadValidated)
            {
                _postLoadValidated = true;
                if (armed && Wearer != null
                    && Wearer.health?.hediffSet != null
                    && !Wearer.health.hediffSet.HasHediff(SimpleSlaveryDefOf.Crypto_Stasis))
                {
                    Log.Warning($"[SSC] {Wearer.LabelShort}: armed=true but CryptoStasis Hediff missing. Will be reapplied next tick.");
                }
            }

            base.TickInterval(delta);

            // 충전 부족 or EMP → 강제 해제 + 정신상태 복원
            if (armed && !IsOperational)
            {
                armed = false;
                RevertMentalState();
            }

            if (!armed) return;

            CryptoStasis();
        }
    }
}