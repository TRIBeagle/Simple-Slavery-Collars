// SimpleSlaveryCollars | Gizmos | Dialog_RemoteCollarManager.cs
// 목적 : 원격 노예 칼라 관리 창 — 콘솔 기즈모에서 열리는 통합 관리 UI
// 용도 : 그룹 필터 + 폰 리스트 + 개별/그룹 예약을 한 창에서 처리
// 주의 : 콘솔 전원 꺼지면 자동 닫힘. 예약은 기존 WorkGiver 시스템으로 처리

using RimWorld;
using System;
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

        // 정렬 상태
        private SortColumn sortColumn = SortColumn.None;
        private bool sortAscending = true;
        private SortColumn lastSortColumn = SortColumn.None;
        private bool lastSortAscending = true;

        // 레이아웃 상수
        private const float RowHeight = 36f;
        private const float HeaderHeight = 24f;
        private const float BtnH = 24f;
        private const float PortraitSize = 32f;

        // 컬럼 오프셋 (초상화 뒤)
        private const float ColPortrait = 2f;
        private const float ColName = 36f;
        private const float NameWidth = 148f;
        private const float ColCollar = 188f;
        private const float CollarWidth = 70f;
        private const float ColStatus = 262f;
        private const float StatusWidth = 56f;
        private const float ColAction = 322f;

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

        #region 그룹 필터

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
                {
                    // 선택된 탭: 밝은 배경 + 흰색 텍스트
                    Widgets.DrawBoxSolidWithOutline(btnRect, new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0.3f));
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(btnRect, groups[i].key.Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                    if (Widgets.ButtonInvisible(btnRect))
                    {
                        currentGroup = groups[i].grp;
                        InvalidateCache();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(btnRect, groups[i].key.Translate()))
                    {
                        currentGroup = groups[i].grp;
                        InvalidateCache();
                    }
                }

                cx += btnW + gap;
            }

            return y + BtnH;
        }

        #endregion

        #region 폰 리스트

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

        /// <summary>클릭 가능한 정렬 헤더.</summary>
        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerLeft;
            float x = rect.x;

            DrawSortableHeaderLabel(new Rect(x + ColName, rect.y, NameWidth, rect.height),
                "SSC_Console_Header_Name".Translate(), SortColumn.Name);
            DrawSortableHeaderLabel(new Rect(x + ColCollar, rect.y, CollarWidth, rect.height),
                "SSC_Console_Header_Collar".Translate(), SortColumn.Collar);
            DrawSortableHeaderLabel(new Rect(x + ColStatus, rect.y, StatusWidth, rect.height),
                "SSC_Console_Header_Status".Translate(), SortColumn.Status);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width);
            GUI.color = Color.white;
        }

        /// <summary>정렬 표시자가 있는 클릭 가능 헤더 라벨.</summary>
        private void DrawSortableHeaderLabel(Rect rect, string label, SortColumn col)
        {
            // 정렬 방향 표시
            string displayLabel = label;
            if (sortColumn == col)
                displayLabel += sortAscending ? " \u25B2" : " \u25BC";

            // 마우스 하이라이트
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.Label(rect, displayLabel);

            if (Widgets.ButtonInvisible(rect))
            {
                if (sortColumn == col)
                    sortAscending = !sortAscending;
                else
                {
                    sortColumn = col;
                    sortAscending = true;
                }
                InvalidateCache();
            }
        }

        /// <summary>개별 폰 행: 초상화 | 이름 | 칼라 종류 | 상태 | 액션 버튼.</summary>
        private void DrawPawnRow(Rect rowRect, PawnCollarInfo info)
        {
            float baseX = rowRect.x;
            float btnY = rowRect.y + (rowRect.height - BtnH) / 2f;

            // ── 초상화 ──
            Rect portraitRect = new Rect(baseX + ColPortrait,
                rowRect.y + (rowRect.height - PortraitSize) / 2f,
                PortraitSize, PortraitSize);
            DrawPawnPortrait(portraitRect, info.pawn);

            Text.Anchor = TextAnchor.MiddleLeft;

            // 이름 (컬러)
            Widgets.Label(new Rect(baseX + ColName, rowRect.y, NameWidth, rowRect.height),
                GetColoredLabel(info.pawn));

            // 칼라 종류
            Widgets.Label(new Rect(baseX + ColCollar, rowRect.y, CollarWidth, rowRect.height),
                GetCollarTypeLabel(info.collar));

            // 상태
            string status = info.collar.IsArmed
                ? "SSC_Collar_Arm".Translate().ToString()
                : "SSC_Collar_Disarm".Translate().ToString();
            Widgets.Label(new Rect(baseX + ColStatus, rowRect.y, StatusWidth, rowRect.height), status);

            Text.Anchor = TextAnchor.UpperLeft;

            // 액션 버튼
            float actionX = baseX + ColAction;
            if (comp.IsPawnReserved(info.pawn))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(actionX, rowRect.y, 100f, rowRect.height),
                    "SSC_Remote_AlreadyReservedShort".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                DrawActionButtons(actionX, btnY, info);
            }
        }

        /// <summary>폰 초상화(상반신) 렌더링.</summary>
        private static void DrawPawnPortrait(Rect rect, Pawn pawn)
        {
            var portrait = PortraitsCache.Get(
                pawn, new Vector2(50f, 50f), Rot4.South,
                cameraOffset: new Vector3(0f, 0f, 0.12f),
                cameraZoom: 1.8f);
            GUI.DrawTexture(rect, portrait);
        }

        /// <summary>칼라 종류/상태에 따른 개별 액션 버튼.</summary>
        private void DrawActionButtons(float x, float y, PawnCollarInfo info)
        {
            float btnW = 56f;
            float gap = 4f;

            if (!info.collar.IsArmed)
            {
                // 무장 버튼
                var armAction = GetArmAction(info.collar);
                if (armAction.HasValue)
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Arm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, armAction.Value);
                        InvalidateCache();
                    }
                }
            }
            else
            {
                // 해제 버튼
                var disarmAction = GetDisarmAction(info.collar);
                if (disarmAction.HasValue)
                {
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Disarm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, disarmAction.Value);
                        InvalidateCache();
                    }
                    x += btnW + gap;
                }

                // 폭발 전용: 폭발 버튼
                if (info.collar is SlaveCollar_Explosive)
                {
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Explosive_Detonate".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DetonateExplosive);
                        InvalidateCache();
                    }
                    GUI.color = Color.white;
                }
            }
        }

        #endregion

        #region 하단 버튼

        /// <summary>하단: 일괄 무장/해제 + 전체 취소 + Warden 경고.</summary>
        private void DrawBottomButtons(Rect rect)
        {
            float btnW = 90f;
            float gap = 4f;
            float x = rect.x;

            // [일괄 무장]
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_ArmAll".Translate()))
            {
                BulkReserve(armed: false);
            }
            x += btnW + gap;

            // [일괄 해제]
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_DisarmAll".Translate()))
            {
                BulkReserve(armed: true);
            }
            x += btnW + gap;

            // [전체 취소]
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_CancelAllReservations".Translate()))
            {
                CancelAllReservations();
            }
            x += btnW + gap;

            // Warden 미할당 경고
            if (!HasWardenOnMap())
            {
                x += 4f;
                GUI.color = new Color(1f, 0.6f, 0.3f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, rect.y, rect.xMax - x, rect.height),
                    "SSC_Console_NoWardenWarning".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        /// <summary>현재 필터 내 미예약 폰에 대해 일괄 무장 또는 해제 예약.</summary>
        private void BulkReserve(bool armed)
        {
            var pawns = GetFilteredPawns();
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                var info = pawns[i];
                if (comp.IsPawnReserved(info.pawn)) continue;
                if (info.collar.IsArmed != armed) continue;

                // armed=true인 폰 → 해제 액션, armed=false인 폰 → 무장 액션
                RemoteCollarAction? action = armed ? GetDisarmAction(info.collar) : GetArmAction(info.collar);
                if (!action.HasValue) continue;

                comp.ReserveJobForPawn(info.pawn, action.Value);
                count++;
            }
            if (count > 0)
            {
                string actionLabel = armed
                    ? "SSC_Collar_Disarm".Translate().ToString()
                    : "SSC_Collar_Arm".Translate().ToString();
                Messages.Message(
                    "SSC_Remote_GroupReserved".Translate(count, actionLabel),
                    MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                Messages.Message("SSC_Remote_NoEligiblePawn".Translate(), MessageTypeDefOf.RejectInput);
            }
            InvalidateCache();
        }

        /// <summary>모든 예약을 해제 (필터 무관).</summary>
        private void CancelAllReservations()
        {
            var allReserved = new List<Pawn>(comp.GetAllReservedPawns());
            int count = allReserved.Count;

            for (int i = 0; i < allReserved.Count; i++)
                comp.ReleaseReservation(allReserved[i]);

            // 그룹 Job 대기 중이면 함께 해제
            if (comp.groupJobPending)
                comp.groupJobPending = false;

            if (count > 0)
            {
                Messages.Message(
                    "SSC_Console_AllCancelledCount".Translate(count),
                    MessageTypeDefOf.TaskCompletion);
            }
            InvalidateCache();
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

        #endregion

        #region 데이터/정렬

        /// <summary>정렬 컬럼.</summary>
        private enum SortColumn { None, Name, Collar, Status }

        /// <summary>폰+칼라 쌍.</summary>
        private struct PawnCollarInfo
        {
            public Pawn pawn;
            public SlaveApparel collar;
        }

        /// <summary>캐시된 폰 리스트 반환. CacheInterval 틱마다 또는 정렬 변경 시 갱신.</summary>
        private List<PawnCollarInfo> GetFilteredPawns()
        {
            int tick = Find.TickManager.TicksGame;
            bool sortChanged = sortColumn != lastSortColumn || sortAscending != lastSortAscending;

            if (cachedPawns != null && lastCacheGroup == currentGroup
                && !sortChanged && tick - lastCacheTick < CacheInterval)
                return cachedPawns;

            lastCacheTick = tick;
            lastCacheGroup = currentGroup;
            lastSortColumn = sortColumn;
            lastSortAscending = sortAscending;

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

            ApplySort(cachedPawns);
            return cachedPawns;
        }

        /// <summary>리스트 정렬 적용.</summary>
        private void ApplySort(List<PawnCollarInfo> list)
        {
            if (sortColumn == SortColumn.None || list.Count < 2) return;

            Comparison<PawnCollarInfo> comparison;
            switch (sortColumn)
            {
                case SortColumn.Name:
                    comparison = (a, b) => string.Compare(a.pawn.LabelShort, b.pawn.LabelShort, StringComparison.Ordinal);
                    break;
                case SortColumn.Collar:
                    comparison = (a, b) => GetCollarSortKey(a.collar) - GetCollarSortKey(b.collar);
                    break;
                case SortColumn.Status:
                    comparison = (a, b) => a.collar.IsArmed.CompareTo(b.collar.IsArmed);
                    break;
                default:
                    return;
            }

            if (sortAscending)
                list.Sort(comparison);
            else
                list.Sort((a, b) => comparison(b, a));
        }

        /// <summary>캐시 강제 무효화.</summary>
        private void InvalidateCache()
        {
            lastCacheTick = -1;
        }

        #endregion

        #region 헬퍼 — 칼라↔액션 매핑

        /// <summary>칼라 종류에 맞는 무장 액션 반환. 새 칼라 추가 시 여기만 확장.</summary>
        private static RemoteCollarAction? GetArmAction(SlaveApparel collar)
        {
            if (collar is SlaveCollar_Explosive) return RemoteCollarAction.ArmExplosive;
            if (collar is SlaveCollar_Electric) return RemoteCollarAction.ArmElectric;
            if (collar is SlaveCollar_Crypto) return RemoteCollarAction.ArmCrypto;
            return null;
        }

        /// <summary>칼라 종류에 맞는 해제 액션 반환. 새 칼라 추가 시 여기만 확장.</summary>
        private static RemoteCollarAction? GetDisarmAction(SlaveApparel collar)
        {
            if (collar is SlaveCollar_Explosive) return RemoteCollarAction.DisarmExplosive;
            if (collar is SlaveCollar_Electric) return RemoteCollarAction.DisarmElectric;
            if (collar is SlaveCollar_Crypto) return RemoteCollarAction.DisarmCrypto;
            return null;
        }

        /// <summary>정렬용 칼라 종류 키.</summary>
        private static int GetCollarSortKey(SlaveApparel collar)
        {
            if (collar is SlaveCollar_Explosive) return 0;
            if (collar is SlaveCollar_Electric) return 1;
            if (collar is SlaveCollar_Crypto) return 2;
            return 3;
        }

        /// <summary>폰 이름 + 신분 컬러링 (식민=하늘/노예=금/죄수=빨강).</summary>
        private static string GetColoredLabel(Pawn pawn)
        {
            string name = pawn.LabelShort;
            string title = pawn.story?.Title;
            string label = title != null ? $"{name}, {title}" : name;

            // 신분 태그 (짧은 회색 텍스트)
            string typeTag;
            string color;
            if (pawn.IsSlaveOfColony)
            {
                color = "#ffd700";
                typeTag = "SSC_Console_PawnType_Slave".Translate();
            }
            else if (pawn.IsPrisonerOfColony)
            {
                color = "#ff9090";
                typeTag = "SSC_Console_PawnType_Prisoner".Translate();
            }
            else if (pawn.IsColonist)
            {
                color = "#a2c8ff";
                typeTag = "SSC_Console_PawnType_Colonist".Translate();
            }
            else
            {
                return label;
            }

            return $"<color={color}>{label}</color> <color=#888>[{typeTag}]</color>";
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
