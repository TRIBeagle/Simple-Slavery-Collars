// SimpleSlaveryCollars | Things | SlaveApparel.cs
// 목적 : 모든 노예 칼라의 추상 기반 클래스
// 용도 : SlaveGizmos() 인터페이스 + 충전/EMP 공용 로직
// 주의 : 충전 필드는 ExposeData로 저장. 기존 세이브에서 키 없으면 만충(1f)으로 로드

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>모든 노예 칼라(Explosive/Electric/Crypto)의 추상 기반 클래스.</summary>
    public abstract class SlaveApparel : Apparel
    {
        // ── 충전 ──
        /// <summary>현재 충전량 (0~1). 1 = 만충, 0 = 방전.</summary>
        public float charge = 1f;

        /// <summary>대기 소모율 (틱당). 모드옵션 기반으로 계산.</summary>
        private float IdleChargePerTick =>
            1f / (SimpleSlaveryCollarsSetting.CollarBatteryDays * 60000f);

        /// <summary>작동(armed) 소모 배율. 서브클래스에서 오버라이드.</summary>
        protected virtual float ActiveChargeMultiplier => 1f;

        /// <summary>충전 임계값. 이 이하면 armed 불가.</summary>
        public const float ChargeThreshold = 0.05f;

        /// <summary>자가충전 허용 여부. Stage5 노예 또는 식민자가 직접 콘솔에서 충전.</summary>
        public bool selfRechargeAllowed = false;

        /// <summary>자동 충전 임계값. 이 이하면 자가충전/Warden 충전 트리거.</summary>
        public const float RechargeThreshold = 0.5f;

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
        }

        /// <summary>
        /// 틱 처리: 충전 소모 + EMP 쿨다운.
        /// 서브클래스는 base.TickInterval(delta)를 반드시 호출해야 함.
        /// </summary>
        protected virtual void TickInterval(int delta)
        {
            // EMP 쿨다운 감소
            if (empDisabledTicks > 0)
            {
                empDisabledTicks = Mathf.Max(0, empDisabledTicks - delta);
            }

            // 충전 소모 (충전 옵션 ON일 때)
            if (SimpleSlaveryCollarsSetting.CollarChargeEnable && charge > 0f)
            {
                float drain = IdleChargePerTick * delta;
                if (IsArmed)
                    drain *= ActiveChargeMultiplier;
                charge = Mathf.Max(0f, charge - drain);
            }
        }

        /// <summary>EMP 피격 처리. 외부에서 호출.</summary>
        public void ApplyEmp(int durationTicks)
        {
            if (!SimpleSlaveryCollarsSetting.CollarEmpEnable) return;
            empDisabledTicks = Mathf.Max(empDisabledTicks, durationTicks);
        }

        /// <summary>콘솔에서 충전. amount만큼 충전량 회복.</summary>
        public void Recharge(float amount)
        {
            charge = Mathf.Clamp01(charge + amount);
        }

        /// <summary>충전량 퍼센트 (0~100).</summary>
        public int ChargePercent => Mathf.RoundToInt(charge * 100f);
    }
}
