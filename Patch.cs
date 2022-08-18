using UnityEngine;
using HarmonyLib;
using Entitas;
using PhantomBrigade;
using PhantomBrigade.Data;
using System.Collections.Generic;
using PhantomBrigade.Combat.Systems;
using System;

namespace fragmentMod
{
    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(GameController), "Initialize", MethodType.Normal)]
        [HarmonyPatch(typeof(ScheduledAttackSystem), "Execute", MethodType.Normal)]

        [HarmonyPostfix]
        static void Sas_fragment(List <CombatEntity> entities)
        {

            foreach (var projectile in Contexts.sharedInstance.combat.GetEntities(CombatMatcher.DataLinkSubsystemProjectile))
            {
                CombatContext combat = Contexts.sharedInstance.combat;

                var part = IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID);
                var subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
                var blueprint = subsystem.dataLinkSubsystem.data;
                var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out float fragmentDelay, 0);
                var fragmentKeyFound = blueprint.TryGetString("fragment_key", out string fragmentKey, null);

                Vector3 bodyAssetScale = Vector3.Scale(Vector3.zero, Vector3.one);
                float lifetime = projectile.timeToLive.f;
                float projSpeed = projectile.movementSpeedCurrent.f;

                ActionEntity parentAction = null;
                CombatEntity bodyAssetKey = null;
                var level = CombatComponentsLookup.Level;
                bool bodyAssetUsed = true;

                if (fragmentDelayFound && fragmentKeyFound)
                {
                        Debug.Log("entered into first if (fragment delay and key found!)");
                    
                    if (projectile.flightInfo.time > fragmentDelay)
                    {

                        Debug.Log("entered into second if (missile logic). It works!");
                        var fragmentBlueprint = DataMultiLinkerSubsystem.GetEntry(fragmentKey);
                        var projectileData = fragmentBlueprint.projectileProcessed;

                        //Destroy Current projectile
                        projectile.TriggerProjectile();

                        // Create new projectile.
                        var projectileNew = combat.CreateEntity();

                        // Attach the new projectile to the subsystem and part (parent).
                        projectileNew.AddDataLinkSubsystemProjectile(projectileData);
                        projectileNew.AddParentPart(part.id.id);

                        if (projectile.HasComponent(148))
                        {
                            projectile.ReplaceComponent(148, projectile.projectileStartPosition);
                            projectile.GetComponent(148);
                        }


                        if (projectileNew.HasComponent(151))
                        {
                            projectile.ReplaceComponent(151, projectile.projectileTargetPosition);
                            projectile.GetComponent(151);
                        }

                        projectile.AddParentSubsystem(subsystem.id.id);
                        projectile.ReplaceScale(bodyAssetScale);
                        projectile.ReplaceLevel(level);

                        projectile.AddProjectileCollision(LayerMasks.projectileMask, 0.0f);
                        projectile.AddInflictedDamage(0);
                        projectile.ReplaceProjectileTargetPosition(projectile.projectileTargetPosition.v.normalized);

                        projectile.ReplaceMovementSpeedCurrent(projSpeed);
                        projectile.ReplaceRicochetChance(0);
                        projectile.ReplaceFlightInfo(0.0f, 0.0f, projectile.projectileStartPosition.v.normalized, projectile.projectileStartPosition.v.normalized);

                        projectile.SimpleMovement = true;
                        projectile.SimpleFaceMotion = true;

                        projectile.ReplacePosition(projectile.projectileStartPosition.v.normalized);
                        projectile.ReplaceRotation(Quaternion.LookRotation((projectile.projectileStartPosition.v - projectile.projectileTargetPosition.v).normalized));
                        projectile.ReplaceSourceEntity(parentAction.actionOwner.combatID);
                        projectile.ReplaceTimeToLive(lifetime);


                        if (bodyAssetUsed)
                            AssetPoolUtility.AttachInstance(bodyAssetKey.assetKey.key, projectile, true);

                        
                    }
                }   
            }
        }
    }
}