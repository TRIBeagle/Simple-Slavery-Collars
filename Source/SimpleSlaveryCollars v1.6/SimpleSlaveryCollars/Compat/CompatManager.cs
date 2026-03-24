// SimpleSlaveryCollars | Compat | CompatManager.cs
// 목적 : 호환 패치 초기화·에러 집계·1회 보고 매니저
// 용도 : ModLister로 모드 활성 판별, 패치 성공/실패를 캐싱 후 ReportAllOnce로 요약
// 주의 : Report 이후 런타임 에러는 동일 id당 1회만 경고

using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars.Compat
{
    /// <summary>모드별 패치 성공/실패 집계 및 1회 보고 담당.</summary>
    internal sealed class CompatMod
    {
        internal readonly string PackageId;
        internal readonly string LogPrefix;
        private readonly HashSet<string> ok = new HashSet<string>();
        private readonly Dictionary<string, string> fail = new Dictionary<string, string>();
        private bool reported;

        internal CompatMod(string packageId, string logPrefix)
        {
            PackageId = packageId;
            LogPrefix = logPrefix;
        }

        internal bool IsActive => CompatManager.IsActive(PackageId);
        internal int OkCount => ok.Count;
        internal int FailCount => fail.Count;

        internal void Patched(string id) => ok.Add(id);

        /// <summary>실패 기록. Report 이후면 즉시 경고.</summary>
        internal void Failed(string id, string reason)
        {
            fail[id] = reason;
            if (reported) Log.Warning($"{LogPrefix} {id} failed: {reason}");
        }

        /// <summary>모드별 요약을 1회만 출력.</summary>
        internal void ReportOnce()
        {
            if (reported || !IsActive) return;
            reported = true;

            if (FailCount == 0)
                Log.Message($"{LogPrefix} {OkCount} compatibility patches active.");
            else
                Log.Message($"{LogPrefix} compatibility patches partial: ok={OkCount}, failed={FailCount}.");

            if (FailCount > 0)
            {
                foreach (var kv in fail)
                    Log.Warning($"{LogPrefix} {kv.Key} failed: {kv.Value}");
            }
        }
    }

    /// <summary>호환 패치 전역 엔트리.</summary>
    internal static class CompatManager
    {
        internal const string Pkg_HAR = "erdelf.HumanoidAlienRaces";
        internal const string LOG_HAR = "[SSC/HAR]";

        internal static readonly CompatMod HAR = new CompatMod(Pkg_HAR, LOG_HAR);

        /// <summary>모드 활성 확인.</summary>
        internal static bool IsActive(string packageId, bool ignorePostfix = false)
            => ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix) != null;

        /// <summary>모든 호환 패치 실행 + 보고 (각 1회).</summary>
        internal static void ReportAllOnce()
        {
            if (HAR.IsActive)
            {
                try { Compat_HAR.RunPatching(); }
                catch (System.Exception e)
                {
                    Log.Warning($"{HAR.LogPrefix} Compatibility failed to load: {e.Message}");
                }
                HAR.ReportOnce();
            }
        }
    }
}
