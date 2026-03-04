using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SimpleSlaveryCollars
{
    public abstract class SlaveApparel : Apparel
    {
        public abstract IEnumerable<Gizmo> SlaveGizmos();
    }

    [StaticConstructorOnStartup]
    public class SlaveCollar_Explosive : SlaveApparel
    {
        public bool armed = false;
        public int arm_cooldown = 0;

        public override IEnumerable<Gizmo> SlaveGizmos()
        {
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
                        SlaveUtility.TryInstantBreak(Wearer, Rand.Range(0.25f, 0.33f));
                        arm_cooldown = 2500;
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
        /// [FIX] 폭발 실행.
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

            // [FIX] 폭발로 이미 사망했으면 추가 데미지 불필요
            if (Wearer.Dead) return;

            // [FIX] Neck이 없는 종족 방어 — corePart(몸통)로 폴백
            var neck = Wearer.RaceProps.body.AllParts.Find(part => part.def == SSC_BodyPartDefOf.Neck);
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
            if (!armed || arm_cooldown <= 0) return;

            arm_cooldown = Math.Max(arm_cooldown - delta, 0);
        }
    }

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
        /// [FIX] 감전 실행.
        /// - Downed/Spawned 체크를 Neck 탐색보다 먼저 수행
        /// - Neck이 없는 종족 방어 — corePart 폴백
        /// - 각 TakeDamage 후 Dead/Downed 체크로 연쇄 크래시 방지
        /// </summary>
        public void Zap()
        {
            // [FIX] 상태 체크를 DamageInfo 생성보다 먼저 수행
            if (Wearer.Downed || !Wearer.Spawned)
            {
                armed = false;
                return;
            }

            // [FIX] Neck이 없는 종족 방어 — corePart(몸통)로 폴백
            var neck = Wearer.RaceProps.body.AllParts.Find(part => part.def == SSC_BodyPartDefOf.Neck);
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

            // [FIX] 첫 데미지로 Dead/Downed 시 중단
            if (Wearer.Dead || Wearer.Downed)
            {
                armed = false;
                return;
            }

            var zap2 = new DamageInfo(DamageDefOf.Stun, 1f, 100f, -1f, this, neck);
            Wearer.TakeDamage(zap2);

            // [FIX] 두 번째 데미지로 Dead 시 중단
            if (Wearer.Dead)
            {
                armed = false;
                return;
            }

            Wearer.health.AddHediff(SSC_HediffDefOf.Electrocuted);
            SlaveUtility.TryHeartAttack(Wearer);
        }
    }


    public class SlaveCollar_Crypto : SlaveApparel
    {
        public bool armed = false;

        public override IEnumerable<Gizmo> SlaveGizmos()
        {
            if (SimpleSlaveryCollarsSetting.RemoteOnlyOnConsoleEnable)
            {
                var status = armed ? "CollarState_Armed".Translate() : "CollarState_Unarmed".Translate();
                var disabled = new Command_Action
                {
                    defaultLabel = status,
                    defaultDesc = "Desc_CollarRemoteOnly".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true),
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
            armCollar.defaultLabel = "Label_CollarCrypto_Arm".Translate();
            armCollar.defaultDesc = "Desc_CollarCrypto_Arm".Translate();
            armCollar.toggleAction = delegate
            {
                armed = !armed;
                if (!armed)
                    RevertMentalState();
            };
            armCollar.activateSound = SoundDefOf.Click;
            armCollar.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true);
            yield return armCollar;

        }

        public void RevertMentalState()
        {
            if (Wearer == null || Wearer.health == null)
                return;

            var hediffSet = Wearer.health.hediffSet;
            if (hediffSet == null)
                return;

            Hediff cryptoStasis = hediffSet.GetFirstHediffOfDef(SSC_HediffDefOf.Crypto_Stasis);
            if (cryptoStasis == null)
                return;

            var stasis = cryptoStasis as Hediff_CryptoStasis;
            var memory = stasis != null ? stasis.revertMentalStateDef : null;

            if (Wearer.mindState != null && Wearer.mindState.mentalStateHandler != null)
            {
                if (memory != null)
                {
                    Wearer.mindState.mentalStateHandler.TryStartMentalState(memory, reason: null, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
                    Wearer.health.RemoveHediff(cryptoStasis);
                    if (Rand.Value > 0.66f)
                    {
                        Wearer.health.AddHediff(HediffDefOf.CryptosleepSickness);
                    }
                }
                else
                {
                    Wearer.health.RemoveHediff(cryptoStasis);
                    Wearer.mindState.mentalStateHandler.Reset();
                    if (Rand.Value > 0.66f)
                    {
                        Wearer.health.AddHediff(HediffDefOf.CryptosleepSickness);
                    }
                }
            }
            else
            {
                Wearer.health.RemoveHediff(cryptoStasis);
            }
        }


        /// <summary>
        /// [FIX] 크립토 스테이시스 적용.
        /// - Hediff 캐스트 실패(XML hediffClass 미설정 등) 시 NRE 방지
        /// </summary>
        public void CryptoStasis()
        {
            Hediff_CryptoStasis revertMentalState = null;
            if (!Wearer.health.hediffSet.HasHediff(SSC_HediffDefOf.Crypto_Stasis))
            {
                Wearer.health.AddHediff(SSC_HediffDefOf.Crypto_Stasis);
                revertMentalState = Wearer.health.hediffSet.GetFirstHediffOfDef(SSC_HediffDefOf.Crypto_Stasis) as Hediff_CryptoStasis;
                // [FIX] 캐스트 실패 방어 — XML에서 hediffClass가 Hediff_CryptoStasis가 아니면 null
                revertMentalState?.SaveMemory();
            }
            if (Wearer.InBed())
            {
                armed = false;
                RevertMentalState();
                return;
            }
            if (Wearer.mindState.mentalStateHandler.CurStateDef != SSC_MentalStateDefOf.CryptoStasis)
                Wearer.mindState.mentalStateHandler.TryStartMentalState(SSC_MentalStateDefOf.CryptoStasis, reason: null, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
            if (Rand.Value < 0.33f)
            {
                FleckMaker.ThrowTornadoDustPuff(Wearer.TrueCenter() + Vector3Utility.RandomHorizontalOffset(0.5f), Wearer.Map, Rand.Range(0.25f, 1f), new Color(0.65f, 0.9f, 0.93f));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref armed, "armed", false);
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!armed) return;

            CryptoStasis();
        }
    }
}