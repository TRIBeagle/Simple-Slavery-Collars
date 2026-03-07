// SimpleSlaveryCollars | Debugs | SimpleSlaveryCollars_DebugActions.cs
// 목적   : DevMode DebugAction으로 Pawn의 TimeAsSlaveTicks를 손쉽게 조정
// 용도   : Debug 메뉴에서 Reset/Stage 경계점/직접입력 실행 → CompSlave 및 레코드 동기화
// 변경   : [FIX] 리플렉션 코드를 SSC_ReflectionCache로 교체하여 CompSlave과의 중복 제거
// 주의   : RecordType.Time은 AddTo 금지 → 반드시 DefMap.set_Item(절대값) 경로 사용

using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Debugs
{
    /// <summary>
    /// Debug 탭 "Simple Slavery Collars" 항목.
    /// Pawn TimeAsSlaveTicks 값을 CompSlave(SSOT)에 직접 조정, 필요 시 Record도 동기화.
    /// </summary>
    public static class SimpleSlaveryDebugActions
    {
        private const int TicksPerDay = 60000;

        // [옵션] true: Comp 수정 시 Record도 즉시 맞춤 / false: 세이브-타임만 반영
        private static readonly bool AlsoSyncLegacyRecord = true;

        /// <summary>
        /// Pawn의 TimeAsSlaveTicks를 Debug 메뉴에서 조정하는 진입점.
        /// - Reset / Stage 경계점 점프 / 직접 입력 / Record 동기화 토글
        /// </summary>
        [DebugAction(
            category = "Simple Slavery Collars",
            name = "Adjust TimeAsSlave...",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        private static void AdjustTimeAsSlave_Comp(Pawn pawn)
        {
            if (pawn == null || pawn.DestroyedOrNull()) return;
            if (!Prefs.DevMode) return;

            var rec = DefDatabase<RecordDef>.GetNamed("TimeAsSlave", errorOnFail: false);
            int curTicks = GetCompTicks(pawn);
            bool isSteadfast = SimpleSlaveryUtility.IsSteadfast(pawn);

            // [Step] 모드 옵션으로 Stage 기간 읽기
            float s1 = SimpleSlaveryCollarsSetting.Slavestage1Period;
            float s2 = SimpleSlaveryCollarsSetting.Slavestage2Period;
            float s3 = SimpleSlaveryCollarsSetting.Slavestage3Period;
            float s4 = SimpleSlaveryCollarsSetting.Slavestage4Period;

            // [Step] Stage 경계 계산
            float b2 = s1;
            float b3 = s1 + s2;
            float b4 = s1 + s2 + s3;
            float b5 = s1 + s2 + s3 + s4;

            var opts = new List<FloatMenuOption>
            {
                new FloatMenuOption($"Current (Comp): {curTicks} ticks (~{curTicks/(float)TicksPerDay:0.##} d)", null)
            };

            // [UI] Reset
            opts.Add(new FloatMenuOption("Reset (set to 0)", () =>
            {
                ApplyCompAndMaybeRecord(pawn, rec, 0);
                Toast("Comp.TimeAsSlave = 0");
            }));

            // [UI] Stage 경계점 점프
            AddSetToBoundaryOption(opts, pawn, rec, "Set to Stage 2", b2);
            AddSetToBoundaryOption(opts, pawn, rec, "Set to Stage 3", b3);
            AddSetToBoundaryOption(opts, pawn, rec, "Set to Stage 4", b4);
            if (!isSteadfast)
                AddSetToBoundaryOption(opts, pawn, rec, "Set to Stage 5", b5);

            // [UI] 직접 입력
            opts.Add(new FloatMenuOption("Set exact (days)...", () =>
            {
                int curDays = Mathf.Max(0, Mathf.RoundToInt(GetCompTicks(pawn) / (float)TicksPerDay));
                Find.WindowStack.Add(new Dialog_SSCIntInput(
                    title: "Set TimeAsSlave (days)",
                    initialValue: curDays,
                    min: 0, max: 3650,
                    onConfirm: d =>
                    {
                        int ticks = Mathf.Max(0, d * TicksPerDay);
                        ApplyCompAndMaybeRecord(pawn, rec, ticks);
                        Toast($"Comp ≈ {d}d");
                    }));
            }));

            // [UI] 동기화 토글 상태 표시
            opts.Add(new FloatMenuOption($"Also sync Record now: {(AlsoSyncLegacyRecord ? "ON" : "OFF")}", null));

            Find.WindowStack.Add(new FloatMenu(opts));
        }

        // ---------- helpers ----------

        /// <summary>Stage 경계선 옵션 추가.</summary>
        private static void AddSetToBoundaryOption(List<FloatMenuOption> opts, Pawn pawn, RecordDef rec, string label, float startDays)
        {
            opts.Add(new FloatMenuOption($"{label} (≈ {startDays:0.##} d)", () =>
            {
                int targetTicks = Mathf.Max(0, Mathf.RoundToInt(startDays * TicksPerDay));
                ApplyCompAndMaybeRecord(pawn, rec, targetTicks);
                Toast(label);
            }));
        }

        // ===== Comp 접근 =====
        private static int GetCompTicks(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<CompSlave>();
            float f = comp?.TimeAsSlaveTicks ?? 0f;
            return Mathf.RoundToInt(f);
        }

        private static void SetCompTicks_Absolute(Pawn pawn, int targetTicks)
        {
            var comp = pawn?.TryGetComp<CompSlave>();
            comp?.SetTimeAsSlaveTicks(targetTicks);
        }

        // ===== Comp + (옵션) Record 동기화 =====
        private static void ApplyCompAndMaybeRecord(Pawn pawn, RecordDef rec, int ticks)
        {
            SetCompTicks_Absolute(pawn, ticks);
            if (AlsoSyncLegacyRecord && rec != null)
            {
                SetRecordTicks_Absolute(pawn, rec, ticks);
            }
        }

        // ===== Record 직접 세팅 — SSC_ReflectionCache 사용 =====

        /// <summary>Record(Time) 절대값 세팅. SSC_ReflectionCache.TrySetRecord() 사용.</summary>
        private static void SetRecordTicks_Absolute(Pawn pawn, RecordDef rec, int targetTicks)
        {
            if (pawn?.records == null) return;
            if (!SimpleSlaveryReflectionUtility.TrySetRecord(pawn.records, rec, (float)Mathf.Max(0, targetTicks)))
            {
                Log.Error("[SSC] SetRecordTicks_Absolute failed via SSC_ReflectionCache.");
            }
        }

        private static void Toast(string msg)
        {
            Messages.Message("[SSC] " + msg, MessageTypeDefOf.TaskCompletion, false);
        }
    }

    /// <summary>
    /// 간단한 정수 입력 다이얼로그. OK/Cancel 지원.
    /// </summary>
    internal class Dialog_SSCIntInput : Window
    {
        private readonly string _title;
        private readonly Action<int> _onConfirm;
        private readonly int _min;
        private readonly int _max;

        private string _buffer;
        private int _value;

        public override Vector2 InitialSize => new Vector2(360f, 150f);
        protected override float Margin => 12f;

        public Dialog_SSCIntInput(string title, int initialValue, int min, int max, Action<int> onConfirm)
        {
            _title = title;
            _min = min;
            _max = Math.Max(min, max);
            _onConfirm = onConfirm;

            _value = Mathf.Clamp(initialValue, _min, _max);
            _buffer = _value.ToString();

            forcePause = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 32f), _title);
            y += 36f;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, y, 80f, 28f), "Days:");
            string newBuf = Widgets.TextField(new Rect(inRect.x + 85f, y, inRect.width - 85f, 28f), _buffer);
            if (newBuf != _buffer)
            {
                _buffer = newBuf;
                if (int.TryParse(_buffer, out var parsed))
                    _value = Mathf.Clamp(parsed, _min, _max);
            }
            y += 40f;

            float half = (inRect.width - 10f) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 30f, half, 30f), "CancelButton".Translate()))
                Close();

            if (Widgets.ButtonText(new Rect(inRect.x + half + 10f, inRect.yMax - 30f, half, 30f), "OK".Translate()))
            {
                Close();
                _onConfirm?.Invoke(_value);
            }
        }
    }
}