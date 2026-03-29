// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.Gizmos.cs
// 목적 : 원격 콘솔 UI/Gizmo 생성 — 관리 창 또는 레거시 토글 버튼
// 용도 : CompRemoteSlaveCollar의 UI 로직 partial 분리
// 주의 : 옵션 ON → 단일 "관리" 기즈모 → Dialog_RemoteCollarManager 창
//        옵션 OFF → 기존 토글/일괄 버튼 유지

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;
using SimpleSlaveryCollars.Gizmos;

namespace SimpleSlaveryCollars
{
    public partial class CompRemoteSlaveCollar
    {
        #region Gizmo
        /// <summary>
        /// 콘솔에 추가 Gizmo 제공.
        /// - 옵션 ON(RemoteOnlyOnConsoleEnable): 단일 "관리" 기즈모 → 창
        /// - 옵션 OFF: 기존 토글/일괄 버튼 전부 노출
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!this.PowerOn)
                yield break;

            // ── 옵션 ON: 단일 관리 기즈모 → Dialog 창 ──
            if (SimpleSlaveryCollarsSetting.RemoteOnlyOnConsoleEnable)
            {
                yield return new Command_Action
                {
                    defaultLabel = "SSC_Console_ManageCollars".Translate(),
                    defaultDesc = "SSC_Console_ManageCollars_Desc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true),
                    action = () =>
                    {
                        if (!Find.WindowStack.IsOpen<Dialog_RemoteCollarManager>())
                            Find.WindowStack.Add(new Dialog_RemoteCollarManager(this));
                    }
                };
                yield break;
            }

            // ── 옵션 OFF: 기존 토글/일괄 버튼 전부 노출(원래 코드 유지) ──

            // [UI] 1) 폭발 목걸이 장전(토글)
            var armCollarExplosive = new Command_Toggle();
            Func<bool> isArmedExplosive = () => remoteArmedExplosive;
            armCollarExplosive.isActive = isArmedExplosive;
            armCollarExplosive.defaultLabel = "SSC_Remote_ExplosiveArm".Translate();
            armCollarExplosive.defaultDesc = "SSC_Remote_ExplosiveArm_Desc".Translate();
            armCollarExplosive.toggleAction = delegate
            {
                remoteArmedExplosive = !remoteArmedExplosive;
                DoRemoteCollarExplosive();
            };
            armCollarExplosive.activateSound = SoundDefOf.Click;
            armCollarExplosive.icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true);
            yield return armCollarExplosive;

            // [UI] 2) 폭발 목걸이 폭발(Armed일 때만 노출)
            if (remoteArmedExplosive)
            {
                var detonate = new Command_Action();
                detonate.defaultLabel = "SSC_Remote_ExplosiveDetonate".Translate();
                detonate.defaultDesc = "SSC_Remote_ExplosiveDetonate_Desc".Translate();
                detonate.action = delegate
                {
                    DoRemoteCollarGoBoom();
                };
                detonate.activateSound = SoundDefOf.Click;
                detonate.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Explosive", true);
                yield return detonate;
            }

            // [UI] 3) 감전 목걸이 장전(토글)
            var armCollarElectric = new Command_Toggle();
            Func<bool> isArmedElectric = () => remoteArmedElectric;
            armCollarElectric.isActive = isArmedElectric;
            armCollarElectric.defaultLabel = "SSC_Remote_ElectricArm".Translate();
            armCollarElectric.defaultDesc = "SSC_Remote_ElectricArm_Desc".Translate();
            armCollarElectric.toggleAction = delegate
            {
                remoteArmedElectric = !remoteArmedElectric;
                DoRemoteCollarElectric();
            };
            armCollarElectric.activateSound = SoundDefOf.Click;
            armCollarElectric.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true);
            yield return armCollarElectric;

            // [UI] 4) 크립토(동결) 목걸이 장전(토글)
            var armCollarCrypto = new Command_Toggle();
            Func<bool> isArmedCrypto = () => remoteArmedCrypto;
            armCollarCrypto.isActive = isArmedCrypto;
            armCollarCrypto.defaultLabel = "SSC_Remote_CryptoArm".Translate();
            armCollarCrypto.defaultDesc = "SSC_Remote_CryptoArm_Desc".Translate();
            armCollarCrypto.toggleAction = delegate
            {
                remoteArmedCrypto = !remoteArmedCrypto;
                DoRemoteCollarCrypto();
            };
            armCollarCrypto.activateSound = SoundDefOf.Click;
            armCollarCrypto.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true);
            yield return armCollarCrypto;
        }
        #endregion
    }
}
