using System.Collections.Generic;
using Entitas;
using PhantomBrigade.Data;
using UnityEngine;

namespace PhantomBrigade.Combat.Systems;

public class ProjectileFragmentationDelaySystem : ReactiveSystem<CombatEntity>
{
    private readonly List<CombatEntity> buffer = new List<CombatEntity>();

    private readonly IGroup<CombatEntity> timeToLiveGroup;

    private readonly CombatContext combat;

    public ProjectileFragmentationDelaySystem(Contexts contexts)
        : base((IContext<CombatEntity>)contexts.combat)
    {
        combat = contexts.combat;
        timeToLiveGroup = combat.GetGroup(CombatMatcher.AllOf(CombatMatcher.FragmentationTime, CombatMatcher.DataLinkSubsystemProjectile, CombatMatcher.ParentPart, CombatMatcher.ParentSubsystem).NoneOf(CombatMatcher.Destroyed, CombatMatcher.ProjectileDestructionPosition));
    }

    protected override ICollector<CombatEntity> GetTrigger(IContext<CombatEntity> context)
    {
        return context.CreateCollector(CombatMatcher.SimulationTime);
    }

    protected override bool Filter(CombatEntity entity)
    {
        return entity.hasSimulationTime;
    }

    protected override void Execute(List<CombatEntity> entities)
    {
        buffer.Clear();
        foreach (CombatEntity item in timeToLiveGroup)
        {
            buffer.Add(item);
        }

        float f = combat.simulationTime.f;
        _ = combat.simulationDeltaTime.f;
        for (int i = 0; i < buffer.Count; i++)
        {
            CombatEntity combatEntity = buffer[i];
            float num = combatEntity.fragmentationTime.f - f;
            if (num <= 0f)
            {
                combatEntity.RemoveFragmentationTime();
                TriggerFragmentation(combatEntity, 0f - num, f);
            }
        }
    }

    private void TriggerFragmentation(CombatEntity projectileSource, float overshootTime, float currentTime)
    {
        DataBlockSubsystemProjectile_V2 dataBlockSubsystemProjectile_V = projectileSource.dataLinkSubsystemProjectile.data;
        if (dataBlockSubsystemProjectile_V.fragmentationDelayed == null || dataBlockSubsystemProjectile_V.fragmentationDelayed.count < 1)
        {
            Debug.Log("Failed to fragment projectile " + projectileSource.ToLog() + " due to missing delayed fragmentation data");
            return;
        }

        if (!projectileSource.hasPosition || !projectileSource.hasFacing)
        {
            Debug.Log("Failed to fragment projectile " + projectileSource.ToLog() + " due to missing position or facing components");
            return;
        }

        DataBlockSubsystemProjectileFragmentationDelayed fragmentationDelayed = dataBlockSubsystemProjectile_V.fragmentationDelayed;
        DataContainerSubsystem dataContainerSubsystem = dataBlockSubsystemProjectile_V.parentSubsystem;
        int num = (projectileSource.hasProjectileIndex ? projectileSource.projectileIndex.generation : 0) + 1;
        if (num > fragmentationDelayed.generationLimit)
        {
            return;
        }

        float newDistance = (projectileSource.hasFlightInfo ? projectileSource.flightInfo.distance : 0f);
        projectileSource.TriggerProjectile(fromDeactivation: true);
        if (projectileSource.isProjectilePrimed)
        {
            projectileSource.isProjectilePrimed = false;
        }

        if (!string.IsNullOrEmpty(fragmentationDelayed.subsystemOverride))
        {
            dataContainerSubsystem = DataMultiLinker<DataContainerSubsystem>.GetEntry(fragmentationDelayed.subsystemOverride, printWarning: false);
            dataBlockSubsystemProjectile_V = dataContainerSubsystem?.projectileProcessed;
            if (dataBlockSubsystemProjectile_V == null)
            {
                Debug.Log("Failed to fragment projectile " + projectileSource.ToLog() + " due to missing projectile data or override subsystem " + fragmentationDelayed.subsystemOverride);
                return;
            }
        }

        CombatEntity combatEntity = (projectileSource.hasSourceEntity ? IDUtility.GetCombatEntity(projectileSource.sourceEntity.combatID) : null);
        ActionEntity parentAction = (projectileSource.hasParentAction ? IDUtility.GetActionEntity(projectileSource.parentAction.actionID) : null);
        Vector3 v = projectileSource.position.v;
        Vector3 vector = projectileSource.facing.v;
        Vector3 vector2 = v + vector * 100f;
        if (projectileSource.hasProjectileTargetPosition)
        {
            vector2 = projectileSource.projectileTargetPosition.v;
        }

        float num2 = 1f;
        if (projectileSource.hasMovementSpeedCurrent)
        {
            num2 = projectileSource.movementSpeedCurrent.f;
        }
        else if (projectileSource.hasAuthoritativeRigidbody)
        {
            Rigidbody rb = projectileSource.authoritativeRigidbody.rb.rb;
            if (rb != null)
            {
                num2 = rb.velocity.magnitude;
            }
        }

        Vector3 direction = Utilities.GetDirection(v, vector2);
        CombatEntity combatEntity2 = null;
        if (projectileSource.hasProjectileTargetEntity)
        {
            combatEntity2 = IDUtility.GetCombatEntity(projectileSource.projectileTargetEntity.combatID);
        }

        if (fragmentationDelayed.rotationToTarget != null)
        {
            if (combatEntity2 != null)
            {
                vector2 = combatEntity2.GetCenterPoint();
            }

            if (num2 > 50f && combatEntity2 != null && combatEntity2.hasVelocity && combatEntity2.velocity.v.sqrMagnitude > 0f && dataBlockSubsystemProjectile_V.guidanceData == null)
            {
                Vector3 vector3 = vector2;
                Vector3 v2 = combatEntity2.velocity.v;
                float targetVelocityModifier = DataLinker<DataContainerSettingsSimulation>.data.targetVelocityModifier;
                int targetTrackingIterations = DataLinker<DataContainerSettingsSimulation>.data.targetTrackingIterations;
                for (int i = 0; i < targetTrackingIterations; i++)
                {
                    float num3 = (vector2 - v).magnitude / num2;
                    vector2 = v2 * (num3 * targetVelocityModifier) + vector3;
                }
            }

            Vector3 direction2 = Utilities.GetDirection(v, vector2);
            Quaternion from = Quaternion.LookRotation(direction);
            Quaternion to = Quaternion.LookRotation(direction2);
            Quaternion quaternion = Quaternion.RotateTowards(from, to, fragmentationDelayed.rotationToTarget.rotationLimit);
            vector2 = v + quaternion * Vector3.forward * 100f;
            vector = quaternion * Vector3.forward;
        }

        string fxKey = fragmentationDelayed.fxKey;
        if (!string.IsNullOrEmpty(fxKey))
        {
            AssetPoolUtility.ActivateInstance(fxKey, projectileSource.position.v, -vector);
        }

        int level = ((!projectileSource.hasLevel) ? 1 : projectileSource.level.i);
        EquipmentEntity equipmentEntity = IDUtility.GetEquipmentEntity(projectileSource.parentPart.equipmentID);
        EquipmentEntity equipmentEntity2 = IDUtility.GetEquipmentEntity(projectileSource.parentSubsystem.equipmentID);
        bool isOwnerAllied = projectileSource.isOwnerAllied;
        float angle = fragmentationDelayed.angle;
        int num4 = fragmentationDelayed.count;
        if (dataBlockSubsystemProjectile_V.fragmentation != null && dataBlockSubsystemProjectile_V.fragmentation.count > 1)
        {
            num4 *= dataBlockSubsystemProjectile_V.fragmentation.count;
        }

        bool guided = dataBlockSubsystemProjectile_V.guidanceData != null;
        bool damageDispersed = dataBlockSubsystemProjectile_V.parentSubsystem.IsFlagPresent("damage_dispersed");
        bool deactivateBeforeRange = dataBlockSubsystemProjectile_V.range != null && dataBlockSubsystemProjectile_V.range.deactivateBeforeRange;
        int num5 = Mathf.FloorToInt(DataHelperStats.GetCachedStatForPart("wpn_penetration_charges", equipmentEntity));
        int penetrationUnitCost = Mathf.FloorToInt(DataHelperStats.GetCachedStatForPart("wpn_penetration_unitcost", equipmentEntity));
        int penetrationGeomCost = Mathf.FloorToInt(DataHelperStats.GetCachedStatForPart("wpn_penetration_geomcost", equipmentEntity));
        float cachedStatForPart = DataHelperStats.GetCachedStatForPart("wpn_penetration_damagek", equipmentEntity);
        bool flag = num5 > 0;
        int scatterDistribution = ((dataBlockSubsystemProjectile_V.distribution == null) ? 1 : dataBlockSubsystemProjectile_V.distribution.samples);
        float cachedStatForPart2 = DataHelperStats.GetCachedStatForPart("wpn_proj_ricochet", equipmentEntity);
        float lifetime = (projectileSource.hasTimeToLive ? projectileSource.timeToLive.f : 0.5f);
        if (fragmentationDelayed.lifetimeReset)
        {
            lifetime = DataHelperStats.GetCachedStatForPart("wpn_proj_lifetime", equipmentEntity);
        }

        string text = null;
        Vector3 bodyAssetScale = Vector3.one;
        bool flag2 = false;
        DataBlockColorInterpolated bodyAssetColorOverride = null;
        if (dataBlockSubsystemProjectile_V.visual != null && dataBlockSubsystemProjectile_V.visual.body != null)
        {
            DataBlockAssetProjectile body = dataBlockSubsystemProjectile_V.visual.body;
            bodyAssetScale = new Vector3(Mathf.Clamp(body.scale.x, 0.75f, 4f), Mathf.Clamp(body.scale.y, 0.75f, 4f), Mathf.Clamp(body.scale.z, 0.5f, 4f));
            text = ((isOwnerAllied || string.IsNullOrEmpty(body.keyEnemy)) ? body.key : body.keyEnemy);
            flag2 = !string.IsNullOrEmpty(text);
            bodyAssetColorOverride = ((isOwnerAllied || body.colorOverrideEnemy == null) ? body.colorOverride : body.colorOverrideEnemy);
        }

        float damage = 0f;
        float impact = 0f;
        float concussion = 0f;
        float heat = 0f;
        float stagger = 0f;
        if (projectileSource.hasInflictedDamage)
        {
            damage = projectileSource.inflictedDamage.f / (float)num4;
        }

        if (projectileSource.hasInflictedImpact)
        {
            impact = projectileSource.inflictedImpact.f / (float)num4;
        }

        if (projectileSource.hasInflictedConcussion)
        {
            concussion = projectileSource.inflictedConcussion.f / (float)num4;
        }

        if (projectileSource.hasInflictedHeat)
        {
            heat = projectileSource.inflictedHeat.f / (float)num4;
        }

        if (projectileSource.hasInflictedStagger)
        {
            stagger = projectileSource.inflictedStagger.f / (float)num4;
        }

        float fragmentationDelay = 0f;
        if (dataBlockSubsystemProjectile_V.fragmentationDelayed != null && dataBlockSubsystemProjectile_V.fragmentationDelayed.time > 0f && dataBlockSubsystemProjectile_V.fragmentationDelayed.count >= 1)
        {
            fragmentationDelay = dataBlockSubsystemProjectile_V.fragmentationDelayed.time * Random.Range(0.9f, 1.1f);
        }

        List<CombatEntity> list = null;
        if (fragmentationDelayed != null && fragmentationDelayed.targetUnitFiltered != null && fragmentationDelayed.targetUnitFiltered != null)
        {
            int desiredFactionOverride = -1;
            if (combatEntity != null)
            {
                desiredFactionOverride = ((!CombatUIUtility.IsUnitFriendly(combatEntity)) ? 1 : 0);
            }

            List<CombatEntity> filteredUnitsUsingSettings = fragmentationDelayed.targetUnitFiltered.GetFilteredUnitsUsingSettings(v, vector, desiredFactionOverride);
            if (filteredUnitsUsingSettings != null && filteredUnitsUsingSettings.Count > 0)
            {
                list = filteredUnitsUsingSettings;
            }
        }

        for (int j = 0; j < num4; j++)
        {
            CombatEntity targetEntity = combatEntity2;
            if (list != null)
            {
                combatEntity2 = UtilityCollections.GetRandomEntry<CombatEntity>(list);
            }

            Vector2Int projectileIndexes = new Vector2Int(j, num);
            CombatEntity combatEntity3 = ScheduledAttackSystem.CreateProjectile(combatEntity, equipmentEntity, equipmentEntity2, dataContainerSubsystem, parentAction, targetEntity, vector2, v, vector, level, isOwnerAllied, projectileIndexes, guided, angle, scatterDistribution, num2, currentTime, overshootTime, lifetime, fragmentationDelay, damageDispersed, deactivateBeforeRange, cachedStatForPart2);
            combatEntity3?.ReplaceFlightInfo(0f, newDistance, v, v);
            ScheduledAttackSystem.AddInflictedDamageComponents(combatEntity3, damage, impact, concussion, heat, stagger);
            if (flag)
            {
                ScheduledAttackSystem.AttachProjectilePenetrationData(combatEntity3, num5, penetrationGeomCost, penetrationUnitCost, cachedStatForPart);
            }

            if (flag2)
            {
                ScheduledAttackSystem.AttachProjectileBodyAsset(combatEntity3, text, bodyAssetScale, bodyAssetColorOverride);
            }
        }
    }
}