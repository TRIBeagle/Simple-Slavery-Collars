// SimpleSlaveryCollars | Things | SlaveCollar_Explosive.cs
// 목적 : 폭발 노예 칼라. 무장(armed) 시 폭발로 착용자 즉사
// 용도 : 직접 토글 또는 원격 콘솔(CompRemoteSlaveCollar)에서 제어
// 주의 : GoBoom() 실행 시 Wearer 사망 가능 → Position/Map 선캐싱 필수

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    [StaticConstructorOnStartup]
    public class SlaveCollar_Explosive : SlaveApparel
    {
        public bool armed = false;
        public int arm_cooldown = 0; // 무장 직후 정신붕괴 방지 쿨다운 (틱)

        public override bool IsArmed => armed;

        // Explosive는 대기 전력만 소모 (신호 수신 대기)
        protected override float ChargePerTick => 0.000005f; // ~200000틱(≈56분)에 완전 방전

        public override IEnumerable<Gizmo> SlaveGizmos()
        {
            // 충전 ON → 통합 기즈모
            if (SimpleSlaveryCollarsSetting.CollarChargeEnable)
            {
                yield return new SimpleSlaveryCollars.Gizmos.Gizmo_SlaveCollarStatus { collar = this };
                yield break;
            }

            if (SimpleSlaveryCollarsSetting.RemoteOnlyOnConsoleEnable)
            {
                var status = armed ? "CollarState_Armed".Translate() : "CollarState_Unarmed".Translate();
                var iconPath = armed
                    ? "UI/Commands/DetonateCollar_Explosive"
                    : "UI/Commands/ArmCollar_Explosive";
                var disabled = new Command_Action
                {
                    defaultLabel = status,
                    defaultDesc = "Desc_CollarRemoteOnly".Translate(),
                    icon = ContentFinder<Texture2D>.Get(iconPath, true),
                    action = () =>
                    {
                        Messages.Message("Reason_CollarRemoteOnly".Translate(), MessageTypeDefOf.RejectInput);
                    }
                };
                disabled.Disable("Reason_CollarRemoteOnly".Translate());

                yield return disabled;
                yield break;
            }
            // 1. Arm the collar
            var armCollar = new Command_Toggle();
            Func<bool> isArmed = () => armed;
            armCollar.isActive = isArmed;
            armCollar.defaultLabel = "Label_CollarExplosive_Arm".Translate();
            armCollar.defaultDesc = "Desc_CollarExplosive_Arm".Translate();
            armCollar.toggleAction = delegate
            {
                armed = !armed;
                if (armed)
                {
                    if (arm_cooldown == 0)
                    {
                        SimpleSlaveryUtility.TryInstantBreak(Wearer, Rand.Range(0.25f, 0.33f)); // 25~33% 확률 정신붕괴
                        arm_cooldown = 2500; // 약 42초 쿨다운 (2500틱)
                    }
                }
            };
            armCollar.activateSound = SoundDefOf.Click;
            armCollar.icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true);
            yield return armCollar;

            // 2. Detonate the collar
            if (armed)
            {
                var detonate = new Command_Action();
                detonate.defaultLabel = "Label_CollarExplosive_Detonate".Translate();
                detonate.defaultDesc = "Desc_CollarExplosive_Detonate".Translate();
                detonate.action = delegate
                {
                    GoBoom();
                };
                detonate.activateSound = SoundDefOf.Click;
                detonate.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Explosive", true);
                yield return detonate;
            }
        }

        /// <summary>
        /// - 폭발 실행.
        /// - 폭발 전에 Position/Map을 캐시 (폭발로 Wearer가 사망하면 Map이 null이 됨)
        /// - 폭발 후 Wearer.Dead 체크
        /// - Neck이 없는 종족(기계족, 커스텀 외계인 등)에서 NRE 방지
        /// </summary>
        public void GoBoom()
        {
            // 폭발 전 위치/맵 캐시 — DoExplosion에서 Wearer가 사망할 수 있음
            var pos = Wearer.Position;
            var map = Wearer.Map;
            GenExplosion.DoExplosion(pos, map, 1.5f, DamageDefOf.Bomb, this, 50);

            // 폭발로 이미 사망했으면 추가 데미지 불필요
            if (Wearer.Dead) return;

            // Neck이 없는 종족 방어 — corePart(몸통)로 폴백
            var neck = Wearer.RaceProps.body.AllParts.Find(part => part.def == SimpleSlaveryDefOf.Neck);
            if (neck == null)
            {
                neck = Wearer.RaceProps.body.corePart;
            }
            if (neck == null) return;

            var destroyNeck = new DamageInfo(DamageDefOf.Bomb, 100f, 100f, -1f, this, neck);
            Wearer.TakeDamage(destroyNeck);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref armed, "armed", false);
            Scribe_Values.Look<int>(ref arm_cooldown, "arm_cooldown", 0);
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // 충전 부족 or EMP → 강제 해제
            if (armed && !IsOperational)
                armed = false;

            if (!armed || arm_cooldown <= 0) return;
            arm_cooldown = Math.Max(arm_cooldown - delta, 0);
        }
    }
}