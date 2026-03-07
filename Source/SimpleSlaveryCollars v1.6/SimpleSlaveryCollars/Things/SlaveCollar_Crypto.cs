using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars
{
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

            Hediff cryptoStasis = hediffSet.GetFirstHediffOfDef(SimpleSlaveryDefOf.Crypto_Stasis);
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
        /// - 크립토 스테이시스 적용.
        /// - Hediff 캐스트 실패(XML hediffClass 미설정 등) 시 NRE 방지
        /// </summary>
        public void CryptoStasis()
        {
            Hediff_CryptoStasis revertMentalState = null;
            if (!Wearer.health.hediffSet.HasHediff(SimpleSlaveryDefOf.Crypto_Stasis))
            {
                Wearer.health.AddHediff(SimpleSlaveryDefOf.Crypto_Stasis);
                revertMentalState = Wearer.health.hediffSet.GetFirstHediffOfDef(SimpleSlaveryDefOf.Crypto_Stasis) as Hediff_CryptoStasis;
                // 캐스트 실패 방어 — XML에서 hediffClass가 Hediff_CryptoStasis가 아니면 null
                revertMentalState?.SaveMemory();
            }
            if (Wearer.InBed())
            {
                armed = false;
                RevertMentalState();
                return;
            }
            if (Wearer.mindState.mentalStateHandler.CurStateDef != SimpleSlaveryDefOf.CryptoStasis)
                Wearer.mindState.mentalStateHandler.TryStartMentalState(SimpleSlaveryDefOf.CryptoStasis, reason: null, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
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