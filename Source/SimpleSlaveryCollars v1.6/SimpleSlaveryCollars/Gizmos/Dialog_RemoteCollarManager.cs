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
        private readonly CompRemoteSlaveCollar _comp;
        private Vector2 _scrollPos;

        // ── 조합 필터 (모두 false = 전체) ──
        private bool _filterColonist;
        private bool _filterSlave;
        private bool _filterPrisoner;
        private readonly HashSet<System.Type> _filterCollarTypes = new HashSet<System.Type>();

        /// <summary>알려진 칼라 종류. 새 칼라 추가 시 여기만 확장.</summary>
        private static readonly (System.Type type, string labelKey)[] KnownCollarTypes =
        {
            (typeof(SlaveCollar_Explosive), "SSC_Console_CollarExplosive"),
            (typeof(SlaveCollar_Electric),  "SSC_Console_CollarElectric"),
            (typeof(SlaveCollar_Crypto),    "SSC_Console_CollarCrypto"),
        };

        // 캐시 — 매 프레임 재스캔 방지, 30틱마다 갱신
        private List<PawnCollarInfo> _cachedPawns;
        private int _lastCacheTick = -1;
        private int _lastFilterHash = -1;
        private const int CacheInterval = 30;

        // 정렬 상태
        private SortColumn _sortColumn = SortColumn.None;
        private bool _sortAscending = true;
        private SortColumn _lastSortColumn = SortColumn.None;
        private bool _lastSortAscending = true;

        // 레이아웃 상수
        private const float RowHeight = 36f;
        private const float HeaderHeight = 24f;
        private const float BtnH = 24f;
        private const float PortraitSize = 32f;

        // 컬럼 오프셋 (초상화 뒤) — 정렬 화살표(▲/▼) 포함 2줄 방지를 위해 여유 확보
        private const float ColPortrait = 2f;
        private const float ColName = 36f;
        private const float NameWidth = 140f;
        private const float ColType = 178f;
        private const float TypeWidth = 54f;
        private const float ColCollar = 234f;
        private const float CollarWidth = 58f;
        private const float ColStatus = 294f;
        private const float StatusWidth = 50f;
        private const float ColAction = 346f;

        // 고정 창 크기 — 10명 표시 기준 (10 × 36 + 고정 영역)
        private const float WindowWidth = 620f;
        private const float WindowHeight = 564f;

        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);

        public Dialog_RemoteCollarManager(CompRemoteSlaveCollar comp)
        {
            this._comp = comp;
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
            if (_comp == null || !_comp.PowerOn)
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

        /// <summary>2줄 조합 필터: 신분 행 + 칼라 행. 버튼이 남은 폭을 균등 분배.</summary>
        private float DrawFilters(float x, float y, float width)
        {
            float gap = 3f;
            float rowGap = 4f;
            float labelW = 40f;

            // ── 1행: 신분 필터 (전체 + 3개 = 4버튼) ──
            DrawFilterLabel(x, y, labelW, "SSC_Console_Header_Type".Translate());
            float availW = width - labelW - 2f;
            int typeCount = 4; // 전체, 정착민, 노예, 죄수
            float btnW = (availW - gap * (typeCount - 1)) / typeCount;
            float btnX = x + labelW + 2f;

            bool anyTypeOn = _filterColonist || _filterSlave || _filterPrisoner;
            if (DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_FilterAll".Translate(), !anyTypeOn))
            {
                if (anyTypeOn)
                {
                    _filterColonist = false; _filterSlave = false; _filterPrisoner = false;
                    InvalidateCache();
                }
            }
            btnX += btnW + gap;

            _filterColonist = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_PawnType_Colonist".Translate(), _filterColonist);
            btnX += btnW + gap;
            _filterSlave = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_PawnType_Slave".Translate(), _filterSlave);
            btnX += btnW + gap;
            _filterPrisoner = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_PawnType_Prisoner".Translate(), _filterPrisoner);

            y += BtnH + rowGap;

            // ── 2행: 칼라 종류 필터 (전체 + 동적) ──
            DrawFilterLabel(x, y, labelW, "SSC_Console_Header_Collar".Translate());
            int collarCount = 1 + KnownCollarTypes.Length; // 전체 + 종류별
            btnW = (availW - gap * (collarCount - 1)) / collarCount;
            btnX = x + labelW + 2f;

            bool allCollarOff = _filterCollarTypes.Count == 0;
            if (DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                "SSC_Console_FilterAll".Translate(), allCollarOff))
            {
                if (!allCollarOff)
                {
                    _filterCollarTypes.Clear();
                    InvalidateCache();
                }
            }
            btnX += btnW + gap;

            for (int i = 0; i < KnownCollarTypes.Length; i++)
            {
                var ct = KnownCollarTypes[i];
                bool active = _filterCollarTypes.Contains(ct.type);
                bool newActive = DrawFilterToggle(new Rect(btnX, y, btnW, BtnH),
                    ct.labelKey.Translate(), active);

                if (newActive != active)
                {
                    if (newActive) _filterCollarTypes.Add(ct.type);
                    else _filterCollarTypes.Remove(ct.type);
                    InvalidateCache();
                }
                btnX += btnW + gap;
            }

            y += BtnH;
            return y;
        }

        /// <summary>필터 행 라벨 (Tiny 폰트).</summary>
        private static void DrawFilterLabel(float x, float y, float w, string label)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, w, BtnH), label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
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

            Widgets.BeginScrollView(scrollRect, ref _scrollPos, viewRect);

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

            // 액션 컬럼 — 정렬 불필요, 라벨만 표시
            Widgets.Label(new Rect(x + ColAction, rect.y, rect.xMax - (x + ColAction), rect.height),
                "SSC_Console_Header_Action".Translate());

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
            if (_sortColumn == col)
                displayLabel += _sortAscending ? " \u25B2" : " \u25BC";

            // 마우스 하이라이트
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.Label(rect, displayLabel);

            if (Widgets.ButtonInvisible(rect))
            {
                if (_sortColumn == col)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumn = col;
                    _sortAscending = true;
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
            if (_comp.IsPawnReserved(info.pawn))
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

            // 칼라 작동 불가(충전 0 / EMP) 시 비활성 표시
            if (!info.collar.IsOperational)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                string inoperableText = "SSC_Collar_Inoperable".Translate();
                Text.Font = GameFont.Small;
                if (Text.CalcSize(inoperableText).x > totalW)
                    Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(x, y, totalW, BtnH), inoperableText);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                return;
            }

            if (!info.collar.IsArmed)
            {
                // 비무장: [무장] 1개 — 전체 폭 사용
                var armAction = GetArmAction(info.collar);
                if (armAction.HasValue)
                {
                    if (Widgets.ButtonText(new Rect(x, y, totalW, BtnH), "SSC_Collar_Arm".Translate()))
                    {
                        _comp.ReserveJobForPawn(info.pawn, armAction.Value);
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
                    _comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DisarmExplosive);
                    InvalidateCache();
                }

                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (Widgets.ButtonText(new Rect(x + btnW + gap, y, btnW, BtnH), "SSC_Explosive_Detonate".Translate()))
                {
                    _comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DetonateExplosive);
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
                        _comp.ReserveJobForPawn(info.pawn, disarmAction.Value);
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
                if (_comp.IsPawnReserved(info.pawn)) continue;
                if (!(info.collar is SlaveCollar_Explosive)) continue;
                if (!info.collar.IsArmed) continue;

                _comp.ReserveJobForPawn(info.pawn, RemoteCollarAction.DetonateExplosive);
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
                if (_comp.IsPawnReserved(info.pawn)) continue;
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
                        _comp.ReserveJobForPawn(capturedAll[i].pawn, action.Value);
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
                    if (_comp.IsPawnReserved(info.pawn)) continue;
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
                            _comp.ReserveJobForPawn(captured[i].pawn, action.Value);
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
            var allReserved = new List<Pawn>(_comp.GetAllReservedPawns());
            int count = allReserved.Count;

            for (int i = 0; i < allReserved.Count; i++)
                _comp.ReleaseReservation(allReserved[i]);

            // 그룹 Job 대기 중이면 함께 해제
            if (_comp.groupJobPending)
                _comp.groupJobPending = false;

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
            var colonists = _comp.parent.Map.mapPawns.FreeColonistsSpawned;
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
            bool sortChanged = _sortColumn != _lastSortColumn || _sortAscending != _lastSortAscending;

            if (_cachedPawns != null && curFilterHash == _lastFilterHash
                && !sortChanged && tick - _lastCacheTick < CacheInterval)
                return _cachedPawns;

            _lastCacheTick = tick;
            _lastFilterHash = curFilterHash;
            _lastSortColumn = _sortColumn;
            _lastSortAscending = _sortAscending;

            if (_cachedPawns == null)
                _cachedPawns = new List<PawnCollarInfo>();
            else
                _cachedPawns.Clear();

            bool anyTypeFilter = _filterColonist || _filterSlave || _filterPrisoner;
            bool anyCollarFilter = _filterCollarTypes.Count > 0;

            var allPawns = _comp.parent.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                var p = allPawns[i];
                if (p.Dead || !p.Spawned) continue;

                // 소속 필터 — 내 정착민/노예/죄수만 (반란 중 소속 변경 대응)
                if (!p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
                    && p.Faction != Faction.OfPlayer && p.HostFaction != Faction.OfPlayer)
                    continue;

                var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveApparel;
                if (collar == null) continue;

                // 신분 필터 (모두 꺼짐 = 전체 통과)
                if (anyTypeFilter)
                {
                    bool pass = false;
                    if (_filterSlave && p.IsSlaveOfColony) pass = true;
                    if (!pass && _filterPrisoner && p.IsPrisonerOfColony) pass = true;
                    if (!pass && _filterColonist && p.IsColonist && !p.IsSlaveOfColony) pass = true;
                    // 반란 등으로 소속 불명인 칼라 착용자 — 노예 필터 ON이면 통과
                    if (!pass && _filterSlave && !p.IsColonist && !p.IsPrisonerOfColony) pass = true;
                    if (!pass) continue;
                }

                // 칼라 종류 필터 (모두 꺼짐 = 전체 통과)
                if (anyCollarFilter && !_filterCollarTypes.Contains(collar.GetType()))
                    continue;

                _cachedPawns.Add(new PawnCollarInfo { pawn = p, collar = collar });
            }

            ApplySort(_cachedPawns);
            return _cachedPawns;
        }

        /// <summary>필터 상태 해시 (캐시 무효화 판정용).</summary>
        private int ComputeFilterHash()
        {
            int h = (_filterColonist ? 1 : 0) | (_filterSlave ? 2 : 0) | (_filterPrisoner ? 4 : 0);
            foreach (var t in _filterCollarTypes)
                h ^= t.GetHashCode();
            return h;
        }

        /// <summary>리스트 정렬 적용.</summary>
        private void ApplySort(List<PawnCollarInfo> list)
        {
            if (_sortColumn == SortColumn.None || list.Count < 2) return;

            Comparison<PawnCollarInfo> comparison;
            switch (_sortColumn)
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

            if (_sortAscending)
                list.Sort(comparison);
            else
                list.Sort((a, b) => comparison(b, a));
        }

        /// <summary>캐시 강제 무효화.</summary>
        private void InvalidateCache()
        {
            _lastCacheTick = -1;
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
