// SimpleSlaveryCollars | Gizmos | Dialog_RemoteCollarManager.cs
// 목적 : 원격 노예 칼라 관리 창 — 콘솔 기즈모에서 열리는 통합 관리 UI
// 용도 : 그룹 필터 + 폰 리스트 + 개별/그룹 예약을 한 창에서 처리
// 주의 : 콘솔 전원 꺼지면 자동 닫힘. 예약은 기존 WorkGiver 시스템으로 처리

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Gizmos
{
    /// <summary>원격 칼라 관리 창. 콘솔 기즈모 클릭으로 열림.</summary>
    public class Dialog_RemoteCollarManager : Window
    {
        private readonly CompRemoteSlaveCollar comp;
        private RemoteCollarPawnGroup currentGroup = RemoteCollarPawnGroup.All;
        private Vector2 scrollPos;

        // 캐시 — 매 프레임 재스캔 방지, 30틱마다 갱신
        private List<PawnCollarInfo> cachedPawns;
        private int lastCacheTick = -1;
        private RemoteCollarPawnGroup lastCacheGroup;
        private const int CacheInterval = 30;

        // 레이아웃 상수
        private const float RowHeight = 30f;
        private const float HeaderHeight = 24f;
        private const float BtnH = 24f;

        public override Vector2 InitialSize => new Vector2(620f, 460f);

        public Dialog_RemoteCollarManager(CompRemoteSlaveCollar comp)
        {
            this.comp = comp;
            doCloseButton = false;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            draggable = true;
            preventCameraMotion = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 전원 꺼지면 닫기
            if (comp == null || !comp.PowerOn)
            {
                Close();
                return;
            }

            float y = inRect.y;

            // ── 타이틀 ──
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 36f), "SSC_Console_WindowTitle".Translate());
            Text.Font = GameFont.Small;
            y += 40f;

            // ── 그룹 필터 ──
            y = DrawGroupFilter(inRect.x, y, inRect.width);
            y += 6f;

            // ── 폰 리스트 ──
            float bottomHeight = 34f;
            Rect listArea = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - bottomHeight - 6f);
            DrawPawnList(listArea);

            // ── 하단 버튼 ──
            DrawBottomButtons(new Rect(inRect.x, inRect.yMax - bottomHeight, inRect.width, bottomHeight));
        }

        /// <summary>그룹 필터 버튼 행.</summary>
        private float DrawGroupFilter(float x, float y, float width)
        {
            float gap = 4f;
            float btnW = (width - gap * 4f) / 5f;

            var groups = new (RemoteCollarPawnGroup grp, string key)[]
            {
                (RemoteCollarPawnGroup.All, "SSC_Remote_GroupAll"),
                (RemoteCollarPawnGroup.Slaves, "SSC_Remote_GroupSlaves"),
                (RemoteCollarPawnGroup.Prisoners, "SSC_Remote_GroupPrisoners"),
                (RemoteCollarPawnGroup.Colonists, "SSC_Remote_GroupColonists"),
                (RemoteCollarPawnGroup.SlavesAndPrisoners, "SSC_Remote_GroupSlavesAndPrisoners"),
            };

            float cx = x;
            for (int i = 0; i < groups.Length; i++)
            {
                Rect btnRect = new Rect(cx, y, btnW, BtnH);
                bool selected = currentGroup == groups[i].grp;

                if (selected)
                    Widgets.DrawHighlight(btnRect);

                if (Widgets.ButtonText(btnRect, groups[i].key.Translate(), drawBackground: !selected))
                {
                    currentGroup = groups[i].grp;
                    InvalidateCache();
                }

                cx += btnW + gap;
            }

            return y + BtnH;
        }

        /// <summary>헤더 + 스크롤 가능한 폰 리스트.</summary>
        private void DrawPawnList(Rect listArea)
        {
            var pawns = GetFilteredPawns();

            // 헤더
            Rect headerRect = new Rect(listArea.x, listArea.y, listArea.width, HeaderHeight);
            DrawHeader(headerRect);

            // 스크롤 영역
            Rect scrollRect = new Rect(listArea.x, listArea.y + HeaderHeight + 2f,
                                        listArea.width, listArea.height - HeaderHeight - 2f);
            float totalHeight = pawns.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, Mathf.Max(totalHeight, scrollRect.height));

            Widgets.BeginScrollView(scrollRect, ref scrollPos, viewRect);

            float rowY = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Rect rowRect = new Rect(0f, rowY, viewRect.width, RowHeight);
                if (i % 2 == 1)
                    Widgets.DrawLightHighlight(rowRect);

                DrawPawnRow(rowRect, pawns[i]);
                rowY += RowHeight;
            }

            Widgets.EndScrollView();
        }

        /// <summary>테이블 헤더.</summary>
        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerLeft;

            float x = rect.x + 4f;
            Widgets.Label(new Rect(x, rect.y, 170f, rect.height), "SSC_Console_Header_Name".Translate());
            x += 174f;
            Widgets.Label(new Rect(x, rect.y, 80f, rect.height), "SSC_Console_Header_Collar".Translate());
            x += 84f;
            Widgets.Label(new Rect(x, rect.y, 60f, rect.height), "SSC_Console_Header_Status".Translate());

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width);
            GUI.color = Color.white;
        }

        /// <summary>개별 폰 행: 이름 | 칼라 종류 | 상태 | 액션 버튼.</summary>
        private void DrawPawnRow(Rect rowRect, PawnCollarInfo info)
        {
            float x = rowRect.x + 4f;
            float btnY = rowRect.y + (rowRect.height - BtnH) / 2f;

            Text.Anchor = TextAnchor.MiddleLeft;

            // 이름 (컬러)
            Widgets.Label(new Rect(x, rowRect.y, 170f, rowRect.height), GetColoredLabel(info.pawn));
            x += 174f;

            // 칼라 종류
            Widgets.Label(new Rect(x, rowRect.y, 80f, rowRect.height), GetCollarTypeLabel(info.collar));
            x += 84f;

            // 상태
            string status = info.collar.IsArmed
                ? "SSC_Collar_Arm".Translate().ToString()
                : "SSC_Collar_Disarm".Translate().ToString();
            Widgets.Label(new Rect(x, rowRect.y, 60f, rowRect.height), status);
            x += 64f;

            Text.Anchor = TextAnchor.UpperLeft;

            // 액션 버튼
            if (comp.IsPawnReserved(info.pawn))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, rowRect.y, 100f, rowRect.height),
                    "SSC_Remote_AlreadyReservedShort".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                DrawActionButtons(x, btnY, info);
            }
        }

        /// <summary>칼라 종류/상태에 따른 액션 버튼.</summary>
        private void DrawActionButtons(float x, float y, PawnCollarInfo info)
        {
            float btnW = 56f;
            float gap = 4f;

            if (info.collar is SlaveCollar_Explosive explosive)
            {
                if (!explosive.armed)
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Arm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.ArmExplosive);
                        InvalidateCache();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Disarm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DisarmExplosive);
                        InvalidateCache();
                    }
                    x += btnW + gap;

                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Explosive_Detonate".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DetonateExplosive);
                        InvalidateCache();
                    }
                    GUI.color = Color.white;
                }
            }
            else if (info.collar is SlaveCollar_Electric electric)
            {
                if (!electric.armed)
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Arm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.ArmElectric);
                        InvalidateCache();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Disarm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DisarmElectric);
                        InvalidateCache();
                    }
                }
            }
            else if (info.collar is SlaveCollar_Crypto crypto)
            {
                if (!crypto.armed)
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Arm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.ArmCrypto);
                        InvalidateCache();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Disarm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DisarmCrypto);
                        InvalidateCache();
                    }
                }
            }
        }

        /// <summary>하단 버튼 + Warden 경고.</summary>
        private void DrawBottomButtons(Rect rect)
        {
            float btnW = 120f;
            float x = rect.x;

            // 전체 취소 버튼
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_CancelAllReservations".Translate()))
            {
                var pawns = GetFilteredPawns();
                int cancelled = 0;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (comp.IsPawnReserved(pawns[i].pawn))
                    {
                        comp.ReleaseReservation(pawns[i].pawn);
                        cancelled++;
                    }
                }
                if (cancelled > 0)
                {
                    Messages.Message(
                        "SSC_Remote_AllCancelled".Translate(cancelled, ""),
                        MessageTypeDefOf.RejectInput);
                }
                InvalidateCache();
            }

            // Warden 미할당 경고
            if (!HasWardenOnMap())
            {
                x += btnW + 8f;
                GUI.color = new Color(1f, 0.6f, 0.3f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, rect.y, rect.width - x + rect.x, rect.height),
                    "SSC_Console_NoWardenWarning".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        /// <summary>맵에 Warden 작업이 활성화된 식민자가 있는지.</summary>
        private bool HasWardenOnMap()
        {
            var colonists = comp.parent.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                var p = colonists[i];
                if (p.Dead || p.Downed) continue;
                if (p.workSettings != null && p.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
                    return true;
            }
            return false;
        }

        #region 헬퍼

        /// <summary>폰+칼라 쌍.</summary>
        private struct PawnCollarInfo
        {
            public Pawn pawn;
            public SlaveApparel collar;
        }

        /// <summary>캐시된 폰 리스트 반환. CacheInterval 틱마다 갱신.</summary>
        private List<PawnCollarInfo> GetFilteredPawns()
        {
            int tick = Find.TickManager.TicksGame;
            if (cachedPawns != null && lastCacheGroup == currentGroup
                && tick - lastCacheTick < CacheInterval)
                return cachedPawns;

            lastCacheTick = tick;
            lastCacheGroup = currentGroup;

            if (cachedPawns == null)
                cachedPawns = new List<PawnCollarInfo>();
            else
                cachedPawns.Clear();

            var allPawns = comp.parent.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                var p = allPawns[i];
                if (p.Dead || !p.Spawned) continue;

                switch (currentGroup)
                {
                    case RemoteCollarPawnGroup.Slaves:
                        if (!p.IsSlaveOfColony) continue; break;
                    case RemoteCollarPawnGroup.Prisoners:
                        if (!p.IsPrisonerOfColony) continue; break;
                    case RemoteCollarPawnGroup.Colonists:
                        if (!p.IsColonist) continue; break;
                    case RemoteCollarPawnGroup.SlavesAndPrisoners:
                        if (!p.IsSlaveOfColony && !p.IsPrisonerOfColony) continue; break;
                }

                var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveApparel;
                if (collar == null) continue;

                cachedPawns.Add(new PawnCollarInfo { pawn = p, collar = collar });
            }

            return cachedPawns;
        }

        /// <summary>캐시 강제 무효화.</summary>
        private void InvalidateCache()
        {
            lastCacheTick = -1;
        }

        /// <summary>폰 이름 컬러링 (식민=하늘/노예=금/죄수=빨강).</summary>
        private static string GetColoredLabel(Pawn pawn)
        {
            string name = pawn.LabelShort;
            string title = pawn.story?.Title;
            string label = title != null ? $"{name}, {title}" : name;

            if (pawn.IsColonist) return $"<color=#a2c8ff>{label}</color>";
            if (pawn.IsSlaveOfColony) return $"<color=#ffd700>{label}</color>";
            if (pawn.IsPrisonerOfColony) return $"<color=#ff9090>{label}</color>";
            return label;
        }

        /// <summary>칼라 종류 라벨.</summary>
        private static string GetCollarTypeLabel(SlaveApparel collar)
        {
            if (collar is SlaveCollar_Explosive) return "SSC_Console_CollarExplosive".Translate();
            if (collar is SlaveCollar_Electric) return "SSC_Console_CollarElectric".Translate();
            if (collar is SlaveCollar_Crypto) return "SSC_Console_CollarCrypto".Translate();
            return "?";
        }

        #endregion
    }
}
