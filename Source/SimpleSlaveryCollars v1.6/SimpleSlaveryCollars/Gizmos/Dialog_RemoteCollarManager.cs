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
        private Vector2 scrollPos;

        // ── 조합 필터 (모두 false = 전체) ──
        private bool filterColonist;
        private bool filterSlave;
        private bool filterPrisoner;
        private readonly HashSet<System.Type> filterCollarTypes = new HashSet<System.Type>();

        /// <summary>알려진 칼라 종류. 새 칼라 추가 시 여기만 확장.</summary>
        private static readonly (System.Type type, string labelKey)[] KnownCollarTypes =
        {
            (typeof(SlaveCollar_Explosive), "SSC_Console_CollarExplosive"),
            (typeof(SlaveCollar_Electric),  "SSC_Console_CollarElectric"),
            (typeof(SlaveCollar_Crypto),    "SSC_Console_CollarCrypto"),
        };

        // 캐시 — 매 프레임 재스캔 방지, 30틱마다 갱신
        private List<PawnCollarInfo> cachedPawns;
        private int lastCacheTick = -1;
        private int lastFilterHash = -1;
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
        private const float NameWidth = 120f;
        private const float ColType = 158f;
        private const float TypeWidth = 44f;
        private const float ColCollar = 204f;
        private const float CollarWidth = 46f;
        private const float ColStatus = 252f;
        private const float StatusWidth = 40f;
        private const float ColAction = 294f;

        // 동적 창 크기
        private const float WindowWidth = 500f;
        private const float MinWindowHeight = 250f;
        private const float MaxWindowHeight = 600f;
        // 고정 영역 높이: 타이틀(40) + 필터2행(52) + 간격(6) + 헤더(26) + 하단(40) + 마진(36*2)
        private const float FixedAreaHeight = 236f;

        public override Vector2 InitialSize => new Vector2(WindowWidth, MinWindowHeight);

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

            // ── 동적 높이 계산: 폰 수에 맞춤 ──
            var pawns = GetFilteredPawns();
            float desiredHeight = FixedAreaHeight + pawns.Count * RowHeight;
            desiredHeight = Mathf.Clamp(desiredHeight, MinWindowHeight, MaxWindowHeight);
            if (Mathf.Abs(windowRect.height - desiredHeight) > 2f)
            {
                float centerX = windowRect.center.x;
                float centerY = windowRect.center.y;
                windowRect.height = desiredHeight;
                windowRect.x = centerX - windowRect.width / 2f;
                windowRect.y = centerY - windowRect.height / 2f;
                // 화면 밖으로 나가지 않도록 클램프
                windowRect.x = Mathf.Clamp(windowRect.x, 0f, UI.screenWidth - windowRect.width);
                windowRect.y = Mathf.Clamp(windowRect.y, 0f, UI.screenHeight - windowRect.height);
            }

            float y = inRect.y;

            // ── 타이틀 ──
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 36f), "SSC_Console_WindowTitle".Translate());
            Text.Font = GameFont.Small;
            y += 40f;

            // ── 조합 필터 ──
            y = DrawFilters(inRect.x, y, inRect.width);
            y += 6f;

            // ── 폰 리스트 ──
            float bottomHeight = 34f;
            Rect listArea = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - bottomHeight - 6f);
            DrawPawnList(listArea);

            // ── 하단 버튼 ──
            DrawBottomButtons(new Rect(inRect.x, inRect.yMax - bottomHeight, inRect.width, bottomHeight));
        }

        #region 조합 필터

        /// <summary>2줄 조합 필터: 신분 행 + 칼라 행. 각 행에 [전체] 토글 포함.</summary>
        private float DrawFilters(float x, float y, float width)
        {
            float gap = 3f;
            float rowGap = 4f;
            float labelW = 40f;
            float allBtnW = 40f;  // [전체] 버튼
            float btnW = 62f;     // 개별 필터 버튼

            // ── 1행: 신분 필터 ──
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, labelW, BtnH), "SSC_Console_Header_Type".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            float btnX = x + labelW + 2f;

            // [전체] 토글 — 하나라도 켜져 있으면 모두 끔, 모두 꺼져 있으면 전체 = 이미 전체
            bool anyTypeOn = filterColonist || filterSlave || filterPrisoner;
            bool allTypeOff = !anyTypeOn; // 전부 OFF = 전체 표시 중
            if (DrawFilterToggle(new Rect(btnX, y, allBtnW, BtnH),
                "SSC_Console_FilterAll".Translate(), allTypeOff))
            {
                if (!allTypeOff)
                {
                    filterColonist = false; filterSlave = false; filterPrisoner = false;
                    InvalidateCache();
                }
            }
            btnX += allBtnW + gap;

            filterColonist = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_PawnType_Colonist".Translate(), filterColonist);
            btnX += btnW + gap;
            filterSlave = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_PawnType_Slave".Translate(), filterSlave);
            btnX += btnW + gap;
            filterPrisoner = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_PawnType_Prisoner".Translate(), filterPrisoner);

            y += BtnH + rowGap;

            // ── 2행: 칼라 종류 필터 (동적) ──
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, labelW, BtnH), "SSC_Console_Header_Collar".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            btnX = x + labelW + 2f;

            // [전체] 토글
            bool allCollarOff = filterCollarTypes.Count == 0;
            if (DrawFilterToggle(new Rect(btnX, y, allBtnW, BtnH),
                "SSC_Console_FilterAll".Translate(), allCollarOff))
            {
                if (!allCollarOff)
                {
                    filterCollarTypes.Clear();
                    InvalidateCache();
                }
            }
            btnX += allBtnW + gap;

            for (int i = 0; i < KnownCollarTypes.Length; i++)
            {
                var ct = KnownCollarTypes[i];
                bool active = filterCollarTypes.Contains(ct.type);
                bool newActive = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                    ct.labelKey.Translate(), active);

                if (newActive != active)
                {
                    if (newActive) filterCollarTypes.Add(ct.type);
                    else filterCollarTypes.Remove(ct.type);
                    InvalidateCache();
                }
                btnX += btnW + gap;
            }

            y += BtnH;
            return y;
        }

        /// <summary>토글 필터 버튼. 활성=밝은 배경, 비활성=일반. 반환: 새 상태.</summary>
        private bool DrawFilterToggle(Rect rect, string label, bool active)
        {
            if (active)
            {
                Widgets.DrawBoxSolidWithOutline(rect, new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0.3f));
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, label);
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(rect))
                {
                    InvalidateCache();
                    return false;
                }
                return true;
            }
            else
            {
                if (Widgets.ButtonText(rect, label))
                {
                    InvalidateCache();
                    return true;
                }
                return false;
            }
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
            DrawSortableHeaderLabel(new Rect(x + ColType, rect.y, TypeWidth, rect.height),
                "SSC_Console_Header_Type".Translate(), SortColumn.Type);
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

            // 이름 (컬러) — 텍스트 넘치면 폰트 축소
            string nameLabel = GetColoredLabel(info.pawn);
            Rect nameRect = new Rect(baseX + ColName, rowRect.y, NameWidth, rowRect.height);
            float nameTextWidth = Text.CalcSize(info.pawn.LabelShort).x;
            if (info.pawn.story?.Title != null)
                nameTextWidth = Text.CalcSize(info.pawn.LabelShort + ", " + info.pawn.story.Title).x;
            if (nameTextWidth > NameWidth - 4f)
                Text.Font = GameFont.Tiny;
            Widgets.Label(nameRect, nameLabel);
            Text.Font = GameFont.Small;

            // 신분
            Widgets.Label(new Rect(baseX + ColType, rowRect.y, TypeWidth, rowRect.height),
                GetPawnTypeLabel(info.pawn));

            // 칼라 종류
            Widgets.Label(new Rect(baseX + ColCollar, rowRect.y, CollarWidth, rowRect.height),
                GetCollarTypeLabel(info.collar));

            // 상태
            string status = info.collar.IsArmed
                ? "SSC_Collar_Arm".Translate().ToString()
                : "SSC_Collar_Disarm".Translate().ToString();
            Widgets.Label(new Rect(baseX + ColStatus, rowRect.y, StatusWidth, rowRect.height), status);

            Text.Anchor = TextAnchor.UpperLeft;

            // 액션 버튼 — 남은 폭을 채움
            float actionX = baseX + ColAction;
            float actionW = rowRect.xMax - actionX;
            if (comp.IsPawnReserved(info.pawn))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(actionX, rowRect.y, actionW, rowRect.height),
                    "SSC_Remote_AlreadyReservedShort".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                DrawActionButtons(actionX, btnY, actionW, info);
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

        /// <summary>칼라 종류/상태에 따른 개별 액션 버튼. 남은 폭을 균등 분배.</summary>
        private void DrawActionButtons(float x, float y, float totalW, PawnCollarInfo info)
        {
            float gap = 3f;

            if (!info.collar.IsArmed)
            {
                // 비무장: [무장] 1개 — 전체 폭 사용
                var armAction = GetArmAction(info.collar);
                if (armAction.HasValue)
                {
                    if (Widgets.ButtonText(new Rect(x, y, totalW, BtnH), "SSC_Collar_Arm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, armAction.Value);
                        InvalidateCache();
                    }
                }
            }
            else if (info.collar is SlaveCollar_Explosive)
            {
                // 폭발 무장: [해제] [폭발] 2개 — 반씩
                float btnW = (totalW - gap) / 2f;
                if (Widgets.ButtonText(new Rect(x, y, btnW, BtnH), "SSC_Collar_Disarm".Translate()))
                {
                    comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DisarmExplosive);
                    InvalidateCache();
                }

                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (Widgets.ButtonText(new Rect(x + btnW + gap, y, btnW, BtnH), "SSC_Explosive_Detonate".Translate()))
                {
                    comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DetonateExplosive);
                    InvalidateCache();
                }
                GUI.color = Color.white;
            }
            else
            {
                // 기타 무장 (전기/암호화): [해제] 1개 — 전체 폭 사용
                var disarmAction = GetDisarmAction(info.collar);
                if (disarmAction.HasValue)
                {
                    if (Widgets.ButtonText(new Rect(x, y, totalW, BtnH), "SSC_Collar_Disarm".Translate()))
                    {
                        comp.ReserveJobForPawn(info.pawn, disarmAction.Value);
                        InvalidateCache();
                    }
                }
            }
        }

        #endregion

        #region 하단 버튼

        /// <summary>하단: 일괄 무장/해제(드롭다운) + 일괄 폭발 + 예약 취소 + Warden 경고.</summary>
        private void DrawBottomButtons(Rect rect)
        {
            float btnW = 86f;
            float gap = 3f;
            float x = rect.x;

            // [일괄 무장 ▾]
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_ArmAll".Translate() + " \u25BE"))
            {
                OpenBulkMenu(arm: true);
            }
            x += btnW + gap;

            // [일괄 해제 ▾]
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_DisarmAll".Translate() + " \u25BE"))
            {
                OpenBulkMenu(arm: false);
            }
            x += btnW + gap;

            // [일괄 폭발] — 무장된 폭발 칼라 전체 폭발
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_DetonateAll".Translate()))
            {
                BulkDetonate();
            }
            GUI.color = Color.white;
            x += btnW + gap;

            // [예약 취소]
            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height),
                "SSC_Console_CancelAllReservations".Translate()))
            {
                CancelAllReservations();
            }
            x += btnW + gap;

            // Warden 미할당 경고
            if (!HasWardenOnMap())
            {
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

        /// <summary>무장된 폭발 칼라 전체 폭발 예약.</summary>
        private void BulkDetonate()
        {
            var pawns = GetFilteredPawns();
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                var info = pawns[i];
                if (comp.IsPawnReserved(info.pawn)) continue;
                if (!(info.collar is SlaveCollar_Explosive)) continue;
                if (!info.collar.IsArmed) continue;

                comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DetonateExplosive);
                count++;
            }
            if (count > 0)
            {
                Messages.Message(
                    "SSC_Remote_GroupReserved".Translate(count, "SSC_Explosive_Detonate".Translate()),
                    MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                Messages.Message("SSC_Remote_NoEligiblePawn".Translate(), MessageTypeDefOf.RejectInput);
            }
            InvalidateCache();
        }

        /// <summary>일괄 무장/해제 드롭다운 메뉴. [전체] + 칼라 종류별 항목.</summary>
        private void OpenBulkMenu(bool arm)
        {
            var pawns = GetFilteredPawns();
            var options = new List<FloatMenuOption>();
            System.Func<SlaveApparel, RemoteCollarAction?> getAction = arm ? (System.Func<SlaveApparel, RemoteCollarAction?>)GetArmAction : GetDisarmAction;

            // ── [전체] 항목 — 모든 칼라 종류 일괄 ──
            var allEligible = new List<PawnCollarInfo>();
            for (int i = 0; i < pawns.Count; i++)
            {
                var info = pawns[i];
                if (comp.IsPawnReserved(info.pawn)) continue;
                if (arm && info.collar.IsArmed) continue;
                if (!arm && !info.collar.IsArmed) continue;
                if (!getAction(info.collar).HasValue) continue;
                allEligible.Add(info);
            }

            string allLabel = "SSC_Console_FilterAll".Translate() + $" ({allEligible.Count})";
            if (allEligible.Count == 0)
            {
                options.Add(new FloatMenuOption(allLabel, null));
            }
            else
            {
                var capturedAll = allEligible;
                options.Add(new FloatMenuOption(allLabel, () =>
                {
                    int count = 0;
                    for (int i = 0; i < capturedAll.Count; i++)
                    {
                        var action = getAction(capturedAll[i].collar);
                        if (!action.HasValue) continue;
                        comp.ReserveJobForPawn(capturedAll[i].pawn, action.Value);
                        count++;
                    }
                    if (count > 0)
                    {
                        string actionLabel = arm
                            ? "SSC_Collar_Arm".Translate().ToString()
                            : "SSC_Collar_Disarm".Translate().ToString();
                        Messages.Message(
                            "SSC_Remote_GroupReserved".Translate(count, actionLabel),
                            MessageTypeDefOf.TaskCompletion);
                    }
                    InvalidateCache();
                }));
            }

            // ── 칼라 종류별 항목 ──
            for (int t = 0; t < KnownCollarTypes.Length; t++)
            {
                var ct = KnownCollarTypes[t];
                var eligible = new List<PawnCollarInfo>();
                for (int i = 0; i < pawns.Count; i++)
                {
                    var info = pawns[i];
                    if (comp.IsPawnReserved(info.pawn)) continue;
                    if (!ct.type.IsInstanceOfType(info.collar)) continue;
                    if (arm && info.collar.IsArmed) continue;
                    if (!arm && !info.collar.IsArmed) continue;
                    eligible.Add(info);
                }

                string label = "    " + ct.labelKey.Translate() + $" ({eligible.Count})";
                if (eligible.Count == 0)
                {
                    options.Add(new FloatMenuOption(label, null));
                }
                else
                {
                    var captured = eligible;
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        int count = 0;
                        for (int i = 0; i < captured.Count; i++)
                        {
                            var action = getAction(captured[i].collar);
                            if (!action.HasValue) continue;
                            comp.ReserveJobForPawn(captured[i].pawn, action.Value);
                            count++;
                        }
                        if (count > 0)
                        {
                            string actionLabel = arm
                                ? "SSC_Collar_Arm".Translate().ToString()
                                : "SSC_Collar_Disarm".Translate().ToString();
                            Messages.Message(
                                "SSC_Remote_GroupReserved".Translate(count, actionLabel),
                                MessageTypeDefOf.TaskCompletion);
                        }
                        InvalidateCache();
                    }));
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
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
        private enum SortColumn { None, Name, Type, Collar, Status }

        /// <summary>폰+칼라 쌍.</summary>
        private struct PawnCollarInfo
        {
            public Pawn pawn;
            public SlaveApparel collar;
        }

        /// <summary>캐시된 폰 리스트 반환. CacheInterval 틱마다 또는 필터/정렬 변경 시 갱신.</summary>
        private List<PawnCollarInfo> GetFilteredPawns()
        {
            int tick = Find.TickManager.TicksGame;
            int curFilterHash = ComputeFilterHash();
            bool sortChanged = sortColumn != lastSortColumn || sortAscending != lastSortAscending;

            if (cachedPawns != null && curFilterHash == lastFilterHash
                && !sortChanged && tick - lastCacheTick < CacheInterval)
                return cachedPawns;

            lastCacheTick = tick;
            lastFilterHash = curFilterHash;
            lastSortColumn = sortColumn;
            lastSortAscending = sortAscending;

            if (cachedPawns == null)
                cachedPawns = new List<PawnCollarInfo>();
            else
                cachedPawns.Clear();

            bool anyTypeFilter = filterColonist || filterSlave || filterPrisoner;
            bool anyCollarFilter = filterCollarTypes.Count > 0;

            var allPawns = comp.parent.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                var p = allPawns[i];
                if (p.Dead || !p.Spawned) continue;

                // 신분 필터 (모두 꺼짐 = 전체 통과)
                if (anyTypeFilter)
                {
                    bool pass = false;
                    // 노예 판정을 먼저 (IsColonist은 노예도 true이므로)
                    if (filterSlave && p.IsSlaveOfColony) pass = true;
                    if (!pass && filterPrisoner && p.IsPrisonerOfColony) pass = true;
                    if (!pass && filterColonist && p.IsColonist && !p.IsSlaveOfColony) pass = true;
                    if (!pass) continue;
                }

                var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveApparel;
                if (collar == null) continue;

                // 칼라 종류 필터 (모두 꺼짐 = 전체 통과)
                if (anyCollarFilter && !filterCollarTypes.Contains(collar.GetType()))
                    continue;

                cachedPawns.Add(new PawnCollarInfo { pawn = p, collar = collar });
            }

            ApplySort(cachedPawns);
            return cachedPawns;
        }

        /// <summary>필터 상태 해시 (캐시 무효화 판정용).</summary>
        private int ComputeFilterHash()
        {
            int h = (filterColonist ? 1 : 0) | (filterSlave ? 2 : 0) | (filterPrisoner ? 4 : 0);
            foreach (var t in filterCollarTypes)
                h ^= t.GetHashCode();
            return h;
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
                case SortColumn.Type:
                    comparison = (a, b) => GetPawnTypeSortKey(a.pawn) - GetPawnTypeSortKey(b.pawn);
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

        /// <summary>폰 이름 컬러링 (노예=금/죄수=빨강/식민=하늘). 노예 판정 먼저.</summary>
        private static string GetColoredLabel(Pawn pawn)
        {
            string name = pawn.LabelShort;
            string title = pawn.story?.Title;
            string label = title != null ? $"{name}, {title}" : name;

            if (pawn.IsSlaveOfColony) return $"<color=#ffd700>{label}</color>";
            if (pawn.IsPrisonerOfColony) return $"<color=#ff9090>{label}</color>";
            if (pawn.IsColonist) return $"<color=#a2c8ff>{label}</color>";
            return label;
        }

        /// <summary>폰 신분 라벨 (별도 컬럼용). 노예 판정을 먼저(IsColonist이 노예도 포함하므로).</summary>
        private static string GetPawnTypeLabel(Pawn pawn)
        {
            if (pawn.IsSlaveOfColony) return "SSC_Console_PawnType_Slave".Translate();
            if (pawn.IsPrisonerOfColony) return "SSC_Console_PawnType_Prisoner".Translate();
            if (pawn.IsColonist) return "SSC_Console_PawnType_Colonist".Translate();
            return "";
        }

        /// <summary>정렬용 신분 키 (노예 판정을 먼저).</summary>
        private static int GetPawnTypeSortKey(Pawn pawn)
        {
            if (pawn.IsSlaveOfColony) return 1;
            if (pawn.IsPrisonerOfColony) return 2;
            if (pawn.IsColonist) return 0;
            return 3;
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
