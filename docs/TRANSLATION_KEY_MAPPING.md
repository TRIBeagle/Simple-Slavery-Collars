# Translation Key Mapping (1.5 → 1.6)

All translation keys have been renamed with the `SSC_` prefix in v1.6.
Keys not listed here are **NEW** in 1.6 (no old equivalent).

## Collar — Direct Actions

| Old Key | New Key |
|---------|---------|
| LabelWordShackle | SSC_Shackle_Label |
| CommandDescriptionShackle | SSC_Shackle_Desc |
| Label_CollarExplosive_Arm | SSC_Explosive_Arm |
| Desc_CollarExplosive_Arm | SSC_Explosive_Arm_Desc |
| Label_CollarExplosive_Detonate | SSC_Explosive_Detonate |
| Desc_CollarExplosive_Detonate | SSC_Explosive_Detonate_Desc |
| ReasonArmedExplosiveCollar | SSC_Explosive_Armed_Reason |
| Label_CollarElectric_Arm | SSC_Electric_Arm |
| Desc_CollarElectric_Arm | SSC_Electric_Arm_Desc |
| Label_CollarCrypto_Arm | SSC_Crypto_Arm |
| Desc_CollarCrypto_Arm | SSC_Crypto_Arm_Desc |

## Collar — State / Reason

| Old Key | New Key |
|---------|---------|
| CollarState_Armed | SSC_Collar_StateArmed |
| CollarState_Unarmed | SSC_Collar_StateUnarmed |
| Desc_CollarRemoteOnly | SSC_Collar_RemoteOnly_Desc |
| Reason_CollarRemoteOnly | SSC_Collar_RemoteOnly_Reason |

## Messages / Letters

| Old Key | New Key |
|---------|---------|
| LetterIncidentECHeartAttack | SSC_Letter_HeartAttack |
| LetterLabelECHeartAttack | SSC_Letter_HeartAttackLabel |
| MessageAssimilationSlave | SSC_Message_Assimilation |
| TargetSetSlaveCollar | SSC_Job_SetCollar |
| ReasonFailedSetSlaveCollar | SSC_Job_SetCollarFailed |
| SimpleSlaveryCollars_MigrationDone | SSC_Migration_Done |

## Slave Time / Stage

| Old Key | New Key |
|---------|---------|
| SimpleSlaveryCollars_SlaveTime_HoursOnly | SSC_SlaveTime_Hours |
| SimpleSlaveryCollars_SlaveTime_DaysOnly | SSC_SlaveTime_Days |
| SimpleSlaveryCollars_SlaveTime_QuadrumDays | SSC_SlaveTime_QuadrumDays |
| SimpleSlaveryCollars_SlaveTime_YearQuadrumDays | SSC_SlaveTime_YearQuadrumDays |
| SimpleSlaveryCollars_SlaveStageSuffix | SSC_Stage_Suffix |
| SuppressionSlavestageFactor | SSC_Stage_SuppressionFactor |

## Role Requirements (unchanged)

| Old Key | New Key |
|---------|---------|
| RoleRequirementLabelNotSlave | RoleRequirementLabelNotSlave |
| RoleRequirementLabelSlaveStage | RoleRequirementLabelSlaveStage |

## Remote — Legacy Mode

| Old Key | New Key |
|---------|---------|
| Label_CollarExplosive_Arm_Remote | SSC_Remote_ExplosiveArm |
| Desc_CollarExplosive_Arm_Remote | SSC_Remote_ExplosiveArm_Desc |
| Label_CollarExplosive_Detonate_Remote | SSC_Remote_ExplosiveDetonate |
| Desc_CollarExplosive_Detonate_Remote | SSC_Remote_ExplosiveDetonate_Desc |
| Label_CollarElectric_Arm_Remote | SSC_Remote_ElectricArm |
| Desc_CollarElectric_Arm_Remote | SSC_Remote_ElectricArm_Desc |
| Label_CollarCrypto_Arm_Remote | SSC_Remote_CryptoArm |
| Desc_CollarCrypto_Arm_Remote | SSC_Remote_CryptoArm_Desc |

## Remote — Group UI

| Old Key | New Key |
|---------|---------|
| RemoteCollar_NoEligiblePawn | SSC_Remote_NoEligiblePawn |

## Remote — Reservation Messages

| Old Key | New Key |
|---------|---------|
| RemoteCollar_ReservedJob | SSC_Remote_Reserved |
| RemoteCollar_GroupReserved | SSC_Remote_GroupReserved |
| RemoteCollar_AlreadyReserved | SSC_Remote_AlreadyReserved |
| RemoteCollar_AlreadyReservedShort | SSC_Remote_AlreadyReservedShort |

## Remote — Action Labels

| Old Key | New Key |
|---------|---------|
| RemoteCollarAction_ArmExplosive | SSC_Action_ArmExplosive |
| RemoteCollarAction_DisarmExplosive | SSC_Action_DisarmExplosive |
| RemoteCollarAction_DetonateExplosive | SSC_Action_DetonateExplosive |
| RemoteCollarAction_ArmElectric | SSC_Action_ArmElectric |
| RemoteCollarAction_DisarmElectric | SSC_Action_DisarmElectric |
| RemoteCollarAction_ArmCrypto | SSC_Action_ArmCrypto |
| RemoteCollarAction_DisarmCrypto | SSC_Action_DisarmCrypto |

## Settings

| Old Key | New Key |
|---------|---------|
| shacklesDefaultSetting_title | SSC_Setting_ShacklesDefault_Title |
| shacklesDefaultSetting_desc | SSC_Setting_ShacklesDefault_Desc |
| slavestageEnableSetting_title | SSC_Setting_SlaveStage_Title |
| slavestageEnableSetting_desc | SSC_Setting_SlaveStage_Desc |
| rebelcyclechangeEnableSetting_title | SSC_Setting_RebelCycle_Title |
| rebelcyclechangeEnableSetting_desc | SSC_Setting_RebelCycle_Desc |
| removeworkspeeddebuffEnableSetting_title | SSC_Setting_RemoveWorkDebuff_Title |
| removeworkspeeddebuffEnableSetting_desc | SSC_Setting_RemoveWorkDebuff_Desc |
| assignslaveEnableSetting_title | SSC_Setting_AssignSlave_Title |
| assignslaveEnableSetting_desc | SSC_Setting_AssignSlave_Desc |
| stage5SlaveWorkUnlockEnableSetting_title | SSC_Setting_Stage5WorkUnlock_Title |
| stage5SlaveWorkUnlockEnableSetting_desc | SSC_Setting_Stage5WorkUnlock_Desc |
| assimilationslaveEnableSetting_title | SSC_Setting_Assimilation_Title |
| assimilationslaveEnableSetting_desc | SSC_Setting_Assimilation_Desc |
| remoteOnlyOnConsoleEnableSetting_title | SSC_Setting_RemoteOnlyConsole_Title |
| remoteOnlyOnConsoleEnableSetting_desc | SSC_Setting_RemoteOnlyConsole_Desc |
| slavestage1Period_title | SSC_Setting_Stage1Period_Title |
| slavestage1Period_desc | SSC_Setting_Stage1Period_Desc |
| slavestage2Period_title | SSC_Setting_Stage2Period_Title |
| slavestage2Period_desc | SSC_Setting_Stage2Period_Desc |
| slavestage3Period_title | SSC_Setting_Stage3Period_Title |
| slavestage3Period_desc | SSC_Setting_Stage3Period_Desc |
| slavestage4Period_title | SSC_Setting_Stage4Period_Title |
| slavestage4Period_desc | SSC_Setting_Stage4Period_Desc |
| resetAllSetting_title | SSC_Setting_ResetAll_Title |
