using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    public class SlaveCollar_Electric : SlaveApparel
    {
        public bool armed = false;
        public int zap_cooldown = 0;
        public const int zap_period = 50;

        public override IEnumerable<Gizmo> SlaveGizmos()
        {
            if (SimpleSlaveryCollarsSetting.RemoteOnlyOnConsoleEnable)
            {
                var status = armed ? "CollarState_Armed".Translate() : "CollarState_Unarmed".Translate();
                var disabled = new Command_Action
                {
                    defaultLabel = status,
                    defaultDesc = "Desc_CollarRemoteOnly".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true),
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
            armCollar.defaultLabel = "Label_CollarElectric_Arm".Translate();
            armCollar.defaultDesc = "Desc_CollarElectric_Arm".Translate();
            armCollar.toggleAction = delegate
            {
                armed = !armed;
            };
            armCollar.activateSound = SoundDefOf.Click;
            armCollar.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true);
            yield return armCollar;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref armed, "armed", false);
            Scribe_Values.Look<int>(ref zap_cooldown, "zap_cooldown", 0);
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!armed) return;

            zap_cooldown -= delta;
            if (zap_cooldown <= 0)
            {
                Zap();
                zap_cooldown = zap_period;
            }
        }
        /// <summary>
        /// - 감전 실행.
        /// - Downed/Spawned 체크를 Neck 탐색보다 먼저 수행
        /// - Neck이 없는 종족 방어 — corePart 폴백
        /// - 각 TakeDamage 후 Dead/Downed 체크로 연쇄 크래시 방지
        /// </summary>
        public void Zap()
        {
            // 상태 체크를 DamageInfo 생성보다 먼저 수행
            if (Wearer.Downed || !Wearer.Spawned)
            {
                armed = false;
                return;
            }

            // Neck이 없는 종족 방어 — corePart(몸통)로 폴백
            var neck = Wearer.RaceProps.body.AllParts.Find(part => part.def == SimpleSlaveryDefOf.Neck);
            if (neck == null)
            {
                neck = Wearer.RaceProps.body.corePart;
            }
            if (neck == null)
            {
                armed = false;
                return;
            }

            SoundInfo info = SoundInfo.InMap(new TargetInfo(Wearer.PositionHeld, Wearer.MapHeld));
            SoundDefOf.Power_OffSmall.PlayOneShot(info);

            var zap = new DamageInfo(DamageDefOf.Burn, 1f, 100f, -1f, this, neck);
            Wearer.TakeDamage(zap);

            // 첫 데미지로 Dead/Downed 시 중단
            if (Wearer.Dead || Wearer.Downed)
            {
                armed = false;
                return;
            }

            var zap2 = new DamageInfo(DamageDefOf.Stun, 1f, 100f, -1f, this, neck);
            Wearer.TakeDamage(zap2);

            // 두 번째 데미지로 Dead 시 중단
            if (Wearer.Dead)
            {
                armed = false;
                return;
            }

            Wearer.health.AddHediff(SimpleSlaveryDefOf.Electrocuted);
            SimpleSlaveryUtility.TryHeartAttack(Wearer);
        }
    }
}