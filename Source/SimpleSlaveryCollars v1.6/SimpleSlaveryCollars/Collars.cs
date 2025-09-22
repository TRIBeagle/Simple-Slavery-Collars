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
                // 상태별 아이콘 경로 지정
                var iconPath = armed
                    ? "UI/Commands/DetonateCollar_Explosive"    // 활성화(장전됨)일 때 아이콘
                    : "UI/Commands/ArmCollar_Explosive";  // 비활성화(비장전)일 때 아이콘
                var disabled = new Command_Action
                {
                    defaultLabel = status, // 라벨엔 오직 상태만!
                    defaultDesc = "Desc_CollarRemoteOnly".Translate(), // 기존 설명 그대로
                    icon = ContentFinder<Texture2D>.Get(iconPath, true),
                    action = () =>
                    {
                        Messages.Message("Reason_CollarRemoteOnly".Translate(), MessageTypeDefOf.RejectInput);
                    }
                };
                disabled.Disable("Reason_CollarRemoteOnly".Translate()); // 비활성 사유도 그대로

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
                        // Doesn't matter if pawn is slave or not when armed
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

        public void GoBoom()
        {
            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 1.5f, DamageDefOf.Bomb, this, 50);
            var destroyNeck = new DamageInfo(DamageDefOf.Bomb, 100f, 100f, -1f, this, Wearer.RaceProps.body.AllParts.Find(part => part.def == SSC_BodyPartDefOf.Neck));
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
                    defaultLabel = status, // 라벨엔 오직 상태만!
                    defaultDesc = "Desc_CollarRemoteOnly".Translate(), // 기존 설명 그대로
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true),
                    action = () =>
                    {
                        Messages.Message("Reason_CollarRemoteOnly".Translate(), MessageTypeDefOf.RejectInput);
                    }
                };
                disabled.Disable("Reason_CollarRemoteOnly".Translate()); // 비활성 사유도 그대로

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
        public void Zap()
        {
            var zap = new DamageInfo(DamageDefOf.Burn, 1f, 100f, -1f, this, Wearer.RaceProps.body.AllParts.Find(part => part.def == SSC_BodyPartDefOf.Neck));
            var zap2 = new DamageInfo(DamageDefOf.Stun, 1f, 100f, -1f, this, Wearer.RaceProps.body.AllParts.Find(part => part.def == SSC_BodyPartDefOf.Neck));
            if (Wearer.Downed || !Wearer.Spawned)
            {
                armed = false;
                return;
            }
            SoundInfo info = SoundInfo.InMap(new TargetInfo(Wearer.PositionHeld, Wearer.MapHeld));
            SoundDefOf.Power_OffSmall.PlayOneShot(info);
            Wearer.TakeDamage(zap);
            Wearer.TakeDamage(zap2);
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
                    defaultLabel = status, // 라벨엔 오직 상태만!
                    defaultDesc = "Desc_CollarRemoteOnly".Translate(), // 기존 설명 그대로
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true),
                    action = () =>
                    {
                        Messages.Message("Reason_CollarRemoteOnly".Translate(), MessageTypeDefOf.RejectInput);
                    }
                };
                disabled.Disable("Reason_CollarRemoteOnly".Translate()); // 비활성 사유도 그대로

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
                // 만약 mindState가 null이면 hediff만 제거 (혹은 아무것도 안함)
                Wearer.health.RemoveHediff(cryptoStasis);
            }
        }


        public void CryptoStasis()
        {
            Hediff_CryptoStasis revertMentalState = null;
            if (!Wearer.health.hediffSet.HasHediff(SSC_HediffDefOf.Crypto_Stasis))
            {
                Wearer.health.AddHediff(SSC_HediffDefOf.Crypto_Stasis);
                revertMentalState = Wearer.health.hediffSet.GetFirstHediffOfDef(SSC_HediffDefOf.Crypto_Stasis) as Hediff_CryptoStasis;
                revertMentalState.SaveMemory();
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
