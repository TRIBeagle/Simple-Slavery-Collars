// SimpleSlaveryCollars | Patches | Patch_WorkGiver_Warden_SelfTarget_JobOnThing.cs
// 목적   : Warden이 자기 자신을 대상(Emancipate/Execute/Imprison/Suppress)으로 잘못 선택하는 경우를 일괄 차단
// 용도   : 4개의 WorkGiver_Warden_*.JobOnThing에 동일한 Prefix를 적용
// 주의   : 기존 Patch_WorkGiver_Warden_Emancipate/Execute/Imprison/Suppress_JobOnThing.cs 4개를 이 파일로 통합

using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Warden이 자기 자신을 Emancipate 대상으로 선택하는 경우를 차단.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_Warden_EmancipateSlave), "JobOnThing")]
    public static class Patch_WorkGiver_Warden_EmancipateSlave_JobOnThing
    {
        static bool Prefix(Pawn pawn, Thing t, ref Job __result)
        {
            try
            {
                if (pawn.IsSlaveOfColony && t is Pawn targetPawn && pawn == targetPawn)
                {
                    __result = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_WorkGiver_Warden_EmancipateSlave_JobOnThing.Prefix error: {ex}");
            }
            return true;
        }
    }

    /// <summary>
    /// Warden이 자기 자신을 Execute 대상으로 선택하는 경우를 차단.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_Warden_ExecuteSlave), "JobOnThing")]
    public static class Patch_WorkGiver_Warden_ExecuteSlave_JobOnThing
    {
        static bool Prefix(Pawn pawn, Thing t, ref Job __result)
        {
            try
            {
                if (pawn.IsSlaveOfColony && t is Pawn targetPawn && pawn == targetPawn)
                {
                    __result = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_WorkGiver_Warden_ExecuteSlave_JobOnThing.Prefix error: {ex}");
            }
            return true;
        }
    }

    /// <summary>
    /// Warden이 자기 자신을 Imprison 대상으로 선택하는 경우를 차단.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_Warden_ImprisonSlave), "JobOnThing")]
    public static class Patch_WorkGiver_Warden_ImprisonSlave_JobOnThing
    {
        static bool Prefix(Pawn pawn, Thing t, ref Job __result)
        {
            try
            {
                if (pawn.IsSlaveOfColony && t is Pawn targetPawn && pawn == targetPawn)
                {
                    __result = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_WorkGiver_Warden_ImprisonSlave_JobOnThing.Prefix error: {ex}");
            }
            return true;
        }
    }

    /// <summary>
    /// Warden이 자기 자신을 Suppress 대상으로 선택하는 경우를 차단.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_Warden_SuppressSlave), "JobOnThing")]
    public static class Patch_WorkGiver_Warden_SuppressSlave_JobOnThing
    {
        static bool Prefix(Pawn pawn, Thing t, ref Job __result)
        {
            try
            {
                if (pawn.IsSlaveOfColony && t is Pawn targetPawn && pawn == targetPawn)
                {
                    __result = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_WorkGiver_Warden_SuppressSlave_JobOnThing.Prefix error: {ex}");
            }
            return true;
        }
    }
}
