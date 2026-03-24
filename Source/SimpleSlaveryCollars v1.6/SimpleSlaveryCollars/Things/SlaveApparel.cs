// SimpleSlaveryCollars | Things | SlaveApparel.cs
// 목적 : 모든 노예 칼라의 추상 기반 클래스
// 용도 : SlaveGizmos() 인터페이스 + 충전/EMP 공용 로직
// 주의 : 충전은 Wd 단위. charge(0~1)은 내부 비율, 실제 용량은 Extension+품질로 결정
//        기존 세이브에서 ssc_charge 키 없으면 만충(1f)으로 로드

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>모든 노예 칼라(Explosive/Electric/Crypto)의 추상 기반 클래스.</summary>
    public abstract class SlaveApparel : Apparel
    {
        // ── 충전 ──
        /// <summary>현재 충전 비율 (0~1). 1 = 만충, 0 = 방전.</summary>
        public float charge = 1f;

        /// <summary>충전 임계값. 이 이하면 armed 불가.</summary>
        public const float ChargeThreshold = 0.05f;

        /// <summary>자가충전 허용 여부. Stage5 노예 또는 식민자가 직접 충전소에서 충전.</summary>
        public bool selfRechargeAllowed = false;

        /// <summary>자동 충전 임계값. 이 이하면 자가충전/Warden 충전 트리거. Gizmo에서 드래그 조절 가능.</summary>
        public float rechargeThreshold = 0.5f;

        // 충전 시스템 마이그레이션 플래그 — 기존 세이브에서 false로 로드, 1회 메시지 후 true
        private bool _chargeMigrated;

        // ── Extension 캐시 ──
        private CollarBatteryExtension _extCache;
        private bool _extSearched;

        /// <summary>이 칼라의 배터리 Extension. 없으면 기본값 사용.</summary>
        public CollarBatteryExtension BatteryExt
        {
            get
            {
                if (!_extSearched)
                {
                    _extCache = def.GetModExtension<CollarBatteryExtension>();
                    _extSearched = true;
                }
                return _extCache;
            }
        }

        // ── 품질 배율 캐시 ──
        private float _qualityMultiplier = -1f;

        /// <summary>품질에 따른 배터리 용량 배율 (0.5~2.5).</summary>
        public float QualityMultiplier
        {
            get
            {
                if (_qualityMultiplier < 0f)
                {
                    if (this.TryGetQuality(out QualityCategory qc))
                    {
                        switch (qc)
                        {
                            case QualityCategory.Awful:       _qualityMultiplier = 0.5f; break;
                            case QualityCategory.Poor:        _qualityMultiplier = 0.7f; break;
                            case QualityCategory.Normal:      _qualityMultiplier = 1.0f; break;
                            case QualityCategory.Good:        _qualityMultiplier = 1.3f; break;
                            case QualityCategory.Excellent:   _qualityMultiplier = 1.6f; break;
                            case QualityCategory.Masterwork:  _qualityMultiplier = 2.0f; break;
                            case QualityCategory.Legendary:   _qualityMultiplier = 2.5f; break;
                            default:                          _qualityMultiplier = 1.0f; break;
                        }
                    }
                    else
                    {
                        _qualityMultiplier = 1.0f;
                    }
                }
                return _qualityMultiplier;
            }
        }

        // ── Wd 기반 계산 ──

        /// <summary>이 칼라의 실제 배터리 용량 (Wd). Extension 기본값 × 품질 배율.</summary>
        public float BatteryCapacityWd
        {
            get
            {
                float baseCapacity = BatteryExt?.batteryCapacity ?? 50f;
                return baseCapacity * QualityMultiplier;
            }
        }

        /// <summary>대기 소모량 (Wd/틱).</summary>
        private float IdleDrainPerTick
        {
            get
            {
                float wdPerDay = BatteryExt?.idleDrain ?? 10f;
                return wdPerDay / 60000f;
            }
        }

        /// <summary>작동(armed) 소모량 (Wd/틱).</summary>
        private float ActiveDrainPerTick
        {
            get
            {
                float wdPerDay = BatteryExt?.activeDrain ?? 30f;
                return wdPerDay / 60000f;
            }
        }

        /// <summary>현재 충전량 (Wd).</summary>
        public float ChargeWd => charge * BatteryCapacityWd;

        /// <summary>충전이 충분한지 여부.</summary>
        public bool HasCharge => !SimpleSlaveryCollarsSetting.CollarChargeEnable || charge > ChargeThreshold;

        // ── EMP ──
        /// <summary>EMP 비활성화 남은 틱. 0이면 정상.</summary>
        public int empDisabledTicks;

        /// <summary>EMP로 비활성화 중인지 여부.</summary>
        public bool IsEmpDisabled => SimpleSlaveryCollarsSetting.CollarEmpEnable && empDisabledTicks > 0;

        /// <summary>칼라가 작동 가능한 상태인지 (충전 충분 + EMP 아님).</summary>
        public bool IsOperational => HasCharge && !IsEmpDisabled;

        // ── 추상 ──
        /// <summary>칼라 전용 기즈모 반환. Pawn 선택 시 UI에 노출.</summary>
        public abstract IEnumerable<Gizmo> SlaveGizmos();

        /// <summary>이 칼라가 armed 상태인지. 서브클래스에서 구현.</summary>
        public abstract bool IsArmed { get; }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref charge, "ssc_charge", 1f);
            Scribe_Values.Look(ref empDisabledTicks, "ssc_empDisabledTicks", 0);
            Scribe_Values.Look(ref selfRechargeAllowed, "ssc_selfRechargeAllowed", false);
            Scribe_Values.Look(ref rechargeThreshold, "ssc_rechargeThreshold", 0.5f);
            Scribe_Values.Look(ref _chargeMigrated, "ssc_chargeMigrated", false);
        }

        /// <summary>
        /// 틱 처리: 충전 소모 + EMP 쿨다운.
        /// 서브클래스는 base.TickInterval(delta)를 반드시 호출해야 함.
        /// </summary>
        protected override void TickInterval(int delta)
        {
            // [FIX] 충전 시스템 마이그레이션 — 기존 세이브에서 첫 로드 시 1회 알림
            if (!_chargeMigrated && SimpleSlaveryCollarsSetting.CollarChargeEnable)
            {
                _chargeMigrated = true;
                if (Wearer != null)
                    Log.Message($"[SSC] {Wearer.LabelShort}: 충전 시스템 첫 적용. 만충 상태로 시작.");
            }

            // EMP 쿨다운 감소
            if (empDisabledTicks > 0)
                empDisabledTicks = Mathf.Max(0, empDisabledTicks - delta);

            // 충전 소모 (충전 옵션 ON일 때)
            if (SimpleSlaveryCollarsSetting.CollarChargeEnable && charge > 0f)
            {
                // Wd/틱 → charge 비율로 변환 (drainWd / capacityWd)
                float capacity = BatteryCapacityWd;
                if (capacity <= 0f) return;

                float drainPerTick = IsArmed ? ActiveDrainPerTick : IdleDrainPerTick;
                drainPerTick *= SimpleSlaveryCollarsSetting.CollarDrainMultiplier;
                float drainRatio = (drainPerTick * delta) / capacity;
                charge = Mathf.Max(0f, charge - drainRatio);
            }
        }

        /// <summary>EMP 피격 처리. 외부에서 호출.</summary>
        public void ApplyEmp(int durationTicks)
        {
            if (!SimpleSlaveryCollarsSetting.CollarEmpEnable) return;
            empDisabledTicks = Mathf.Max(empDisabledTicks, durationTicks);
        }

        /// <summary>Wd 단위로 충전. 부분 충전 가능. 실제 충전된 Wd를 반환.</summary>
        public float RechargeWd(float availableWd)
        {
            float capacity = BatteryCapacityWd;
            if (capacity <= 0f) return 0f;

            float neededWd = (1f - charge) * capacity;
            float actualWd = Mathf.Min(neededWd, availableWd);
            charge = Mathf.Clamp01(charge + actualWd / capacity);
            return actualWd;
        }

        /// <summary>충전량 퍼센트 (0~100).</summary>
        public int ChargePercent => Mathf.RoundToInt(charge * 100f);

        /// <summary>의상 정보창에 배터리 용량/소모 속도를 보호막 벨트 스타일로 표기.</summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (var s in base.SpecialDisplayStats())
                yield return s;

            if (!SimpleSlaveryCollarsSetting.CollarChargeEnable)
                yield break;

            // 최대 용량
            yield return new StatDrawEntry(
                StatCategoryDefOf.Apparel,
                "SSC_Stat_BatteryCapacity".Translate(),
                BatteryCapacityWd.ToString("F1") + " Wd",
                "SSC_Stat_BatteryCapacity_Desc".Translate(),
                displayPriorityWithinCategory: 1000);

            // 대기 소모 (Wd/일)
            float idleDrain = (BatteryExt?.idleDrain ?? 10f) * SimpleSlaveryCollarsSetting.CollarDrainMultiplier;
            yield return new StatDrawEntry(
                StatCategoryDefOf.Apparel,
                "SSC_Stat_IdleDrain".Translate(),
                idleDrain.ToString("F1") + " Wd/" + "SSC_Stat_Day".Translate(),
                "SSC_Stat_IdleDrain_Desc".Translate(),
                displayPriorityWithinCategory: 999);

            // 활성 소모 (Wd/일)
            float activeDrain = (BatteryExt?.activeDrain ?? 30f) * SimpleSlaveryCollarsSetting.CollarDrainMultiplier;
            yield return new StatDrawEntry(
                StatCategoryDefOf.Apparel,
                "SSC_Stat_ActiveDrain".Translate(),
                activeDrain.ToString("F1") + " Wd/" + "SSC_Stat_Day".Translate(),
                "SSC_Stat_ActiveDrain_Desc".Translate(),
                displayPriorityWithinCategory: 998);
        }

        #region 레지스트리 통합
        /// <summary>착용 시 SlaveCollarRegistry에 등록.</summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            SlaveCollarRegistry.Register(pawn, this);
        }

        /// <summary>해제 시 SlaveCollarRegistry에서 등록 해제.</summary>
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            SlaveCollarRegistry.Unregister(pawn);
        }
        #endregion
    }
}
