using UnityEngine;
using HarmonyLib;
using Entitas;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;
using System;
using UnityEngine.Assertions;
using PhantomBrigade.Combat.Components;

namespace fragmentMod
{
    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(GameController), "Initialize", MethodType.Normal)]
        [HarmonyPatch(typeof(ScheduledAttackSystem), "Execute", MethodType.Normal)]

        [HarmonyPostfix]

        static void Sas_fragment()
        {
            foreach (CombatEntity projectile in Contexts.sharedInstance.combat.GetEntities(CombatMatcher.DataLinkSubsystemProjectile))
            {
                CombatContext combat = Contexts.sharedInstance.combat;
                    Debug.Log("[1] - Context Shared Instance (combat): " + combat.ToString());

                EquipmentEntity part = IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID);
                    Debug.Log("[2] - parent part: " + projectile.parentPart.equipmentID.ToString());

                EquipmentEntity subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
                    Debug.Log("[3] - parent subsystem: " + projectile.parentSubsystem.equipmentID.ToString());

                DataContainerSubsystem blueprint = subsystem.dataLinkSubsystem.data;
                    Debug.Log("[4] - Blueprint: " + blueprint.ToString());

                var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out float fragmentDelay, 0);
                    Debug.Log("[5] - Fragment Delay: " + fragmentDelayFound.ToString());

                var fragmentKeyFound = blueprint.TryGetString("fragment_key", out string fragmentKey, null);
                    Debug.Log("[6] - Fragment Key: " + fragmentKeyFound.ToString());

                Vector3 bodyAssetScale = Vector3.one.normalized;
                    Debug.Log("[7] - BodyAssetScale(x,y,z): " + bodyAssetScale.ToString());

                DataContainerSubsystem fragmentBlueprint = DataMultiLinkerSubsystem.GetEntry(fragmentKey);
                    Debug.Log("[8] - fragmentBlueprint | " + fragmentBlueprint.ToString());

                DataBlockSubsystemProjectile_V2 projectileData = fragmentBlueprint.projectileProcessed;
                    Debug.Log("[9] - projectileData | " + projectileData.ToString());

                //ONLY FOR TESTING PURPOSE.
                //var stat = DataHelperStats.GetCachedStatForPart(UnitStats.weaponConcussion, part);

                if (fragmentDelayFound && fragmentKeyFound)
                {
                    if (projectile.flightInfo.time > fragmentDelay)
                    {
                        //Destroy the projectile.
                        //projectile.TriggerProjectile();

                        CombatEntity projectileNew = combat.CreateEntity();
                        

                        //Experimental: cloning missiles.
                        for (var totalNum = 0; totalNum < 10; totalNum += 4)
                        {
                            AssetPoolUtility.AttachInstance(projectile.assetKey.key, projectile, true);

                            if (projectileNew.HasComponent(248))
                            {
                                projectileNew.ReplaceDataLinkSubsystemProjectile(projectileData);
                                Debug.Log("[11] - Projectile Data Link (root) added! - result | " + " ProjectileData: " + projectileData.ToString());
                            }
                            else
                            {
                                Debug.Log("[11] - Projectile Data Link (root) not added");
                            }

                            if (projectileNew.HasComponent(112))
                            {
                                projectileNew.ReplaceParentPart(part.id.id);
                                Debug.Log("[12] - parent part (root) added | value: " + projectileNew.parentPart.equipmentID.ToString() + "(" + part.id.id.ToString() + ")");
                            }
                            else
                            {
                                Debug.Log("[12] - parent part (root) not added");
                            }

                            if (projectile.HasComponent(113))
                            {
                                projectile.ReplaceParentSubsystem(subsystem.id.id);
                                Debug.Log("[13] - parent subsystem (root) added | value: " + projectile.parentSubsystem.equipmentID.ToString() + "(" + subsystem.id.id.ToString() + ")");
                            }
                            else
                            {
                                Debug.Log("[13] - parent subsystem (root) not added | value: " + projectile.parentSubsystem.equipmentID.ToString() + "(" + subsystem.id.id.ToString() + ")");
                            }

                            if (projectile.hasLevel)
                            {
                                projectile.ReplaceLevel(projectile.level.i);
                                Debug.Log("[18] - Projectile level added | value: " + projectile.level.i.ToString());
                            }
                            else
                            {
                                Debug.Log("[18] - Projectile level not added");
                            }

                            if (projectile.hasProjectileCollision)
                            {
                                projectile.ReplaceProjectileCollision(LayerMasks.projectileMask, 1f);
                                Debug.Log("[19] - Projectile collision added | value: " + LayerMasks.projectileMask);
                            }
                            else
                            {
                                Debug.Log("[19] - Projectile collision not added");
                            }

                            if (projectile.hasInflictedDamage)
                            {
                                projectile.ReplaceInflictedDamage(1.0f);
                                Debug.Log("[20] - InflictedDamage added | value: " + projectile.inflictedDamage.f.ToString());
                            }
                            else
                            {
                                Debug.Log("[20] - InflictedDamage not added");
                            }

                            if (projectile.hasProjectileTargetPosition)
                            {
                                projectile.ReplaceProjectileTargetPosition(projectile.projectileTargetPosition.v.normalized);
                                Debug.Log("[21] - ProjectileTargetPosition added | value: " + projectile.projectileGuidanceTargetPosition.v.normalized.ToString());
                            }
                            else
                            {
                                Debug.Log("[21] - ProjectileTargetPosition not added");
                            }

                            if (projectile.HasComponent(141))
                            {
                                projectile.ReplaceProjectileGuidanceTargetPosition(projectile.projectileGuidanceTargetPosition.v.normalized);
                                Debug.Log("[22] - ProjectileGuidanceTargetPosition added | value: " + projectile.projectileGuidanceTargetPosition.v.normalized.ToString());
                            }
                            else
                            {
                                Debug.Log("[22] ProjectileGuidanceTargetPosition not added");
                            }


                            if (projectile.hasMovementSpeedCurrent)
                            {
                                projectile.ReplaceMovementSpeedCurrent(3f);
                                Debug.Log("[23] - MovementSpeedCurrent added | value: " + projectile.movementSpeedCurrent.f.ToString());
                            }
                            else
                            {
                                Debug.Log("[23] - MovementSpeedCurrent not added");
                            }

                            if (projectile.hasRicochetChance)
                            {
                                projectile.ReplaceRicochetChance(0.5f);
                                Debug.Log("[24] - RicochetChance added | value: " + projectile.ricochetChance.f.ToString());
                            }
                            else
                            {
                                Debug.Log("[24] - RicochetChance not added");
                            }

                            if (projectile.hasFlightInfo)
                            {
                                projectile.ReplaceFlightInfo(2f, 1f, projectile.flightInfo.origin.normalized, projectile.previousPosition.v.normalized);
                                Debug.Log("[25] - FlightInfo added | value: " + projectile.projectileStartPosition.v.ToString() + ", " + projectile.projectileStartPosition.v.ToString());
                            }
                            else
                            {
                                Debug.Log("[25] - FlightInfo not added");
                            }

                            if (projectile.SimpleMovement)
                            {
                                projectile.SimpleMovement = true;
                                Debug.Log("[26] - SimpleMovement: " + projectile.SimpleMovement.ToString());
                            }
                            else
                            {
                                Debug.Log("[26] - SimpleMovement: " + projectile.SimpleMovement.ToString());
                            }

                            if (projectile.SimpleFaceMotion)
                            {
                                projectile.SimpleFaceMotion = true;
                                Debug.Log("[27] - SimpleFaceMotion: " + projectile.SimpleFaceMotion.ToString());
                            }
                            else
                            {
                                Debug.Log("[27] - SimpleFaceMotion: " + projectile.SimpleFaceMotion.ToString());
                            }

                            if (projectile.hasPosition)
                            {
                                projectile.ReplacePosition(projectile.position.v.normalized);
                                Debug.Log("[28] - ProjectilePosition added | value: " + projectile.position.v.normalized.ToString());
                            }
                            else
                            {
                                Debug.Log("[28] - ProjectilePosition not added");
                            }

                            if (projectile.hasRotation)
                            {
                                projectile.ReplaceRotation(projectile.rotation.q);
                                Debug.Log("[29] - ProjectileRotation added | value: " + projectile.rotation.q.ToString());
                            }
                            else
                            {
                                Debug.Log("[29] - ProjectileRotation not added");
                            }

                            if (projectile.hasFacing)
                            {
                                projectile.ReplaceFacing(projectile.facing.v);
                                Debug.Log("[30] - Facing added | value: " + projectile.facing.v.ToString());
                            }
                            else
                            {
                                Debug.Log("[30] - Facing not added");
                            }

                            if (projectile.hasSourceEntity)
                            {
                                projectile.ReplaceSourceEntity(projectile.sourceEntity.combatID);
                                Debug.Log("[31] - SourceEntity (parentAction) Added | Parent Action: " + projectile.sourceEntity.combatID.ToString());
                            }
                            else
                            {
                                Debug.Log("[31] - SourceEntity (parentAction) not Added");
                            }

                            if (projectile.hasTimeToLive)
                            {
                                projectile.ReplaceTimeToLive(projectile.timeToLive.f);
                                Debug.Log("[32] - HasTimeToLive Added | value: " + projectile.timeToLive.f.ToString());
                            }
                            else
                            {
                                Debug.Log("[32] - HasTimeToLive Not Added");
                            }

                        }

                        Debug.Log("[!!!THE END!!!] It works! -");
                    }
                }
            }
        }
    }
}