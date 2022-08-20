using UnityEngine;
using HarmonyLib;
using Entitas;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Combat.Components;
using YamlDotNet.Core.Tokens;

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

            foreach (var projectile in Contexts.sharedInstance.combat.GetEntities(CombatMatcher.DataLinkSubsystemProjectile))
            {
                CombatContext combat = Contexts.sharedInstance.combat;
                    Debug.Log("[1]Context Shared Instance (combat): " + combat.ToString());

                var part = IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID);
                    Debug.Log("[2]parent part: " + part.ToString());

                var subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
                    Debug.Log("[3]parent subsystem: " + subsystem.ToString());

                var blueprint = subsystem.dataLinkSubsystem.data;
                    Debug.Log("[4]Blueprint: " + blueprint.ToString());

                var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out float fragmentDelay, 0);
                    Debug.Log("[5]Fragment Delay: " + fragmentDelayFound.ToString());

                var fragmentKeyFound = blueprint.TryGetString("fragment_key", out string fragmentKey, null);
                    Debug.Log("[6]Fragment Key: " + fragmentKeyFound.ToString());

                Vector3 bodyAssetScale = Vector3.Scale(Vector3.one, Vector3.zero);
                    Debug.Log("[7]BodyAssetScale(x,y,z): " + bodyAssetScale.ToString());

                AssetLink bodyAssetKey = null;
                bool bodyAssetUsed = true;

                if (fragmentDelayFound && fragmentKeyFound)
                {
                    if (projectile.flightInfo.time > fragmentDelay)
                    {
                        var fragmentBlueprint = DataMultiLinkerSubsystem.GetEntry(fragmentKey);
                            Debug.Log("[8]fragmentBlueprint | " + fragmentBlueprint.ToString());

                        var projectileData = fragmentBlueprint.projectileProcessed;
                            Debug.Log("[9]projectileData | " + projectileData.ToString());

                        //Destroy Current projectile
                        projectile.TriggerProjectile();

                        // Create new projectile.
                        var projectileNew = combat.CreateEntity();
                            Debug.Log("[10]ProjectileNew (createEntity) | " + projectileNew.ToString());

                        // Attach the new projectile to the subsystem and part (parent).
                        if (projectileNew.hasDataLinkSubsystemProjectile)
                        {
                            projectileNew.AddComponent(248, projectile.dataLinkSubsystemProjectile);
                            projectileNew.AddDataLinkSubsystemProjectile(projectileData);
                            projectileNew.GetComponent(248);
                            Debug.Log("[11]Projectile Data added/replaced! - result | " + " ProjectileData: ");
                        }
                        else
                        {
                            Debug.Log("[11]cannot add/replace projectile Data - result | " + " ProjectileData : ");
                        }

                        if (projectileNew.HasComponent(112))
                        {

                            projectileNew.AddParentPart(part.id.id);
                            projectileNew.GetComponent(112);
                            Debug.Log("[12]parent part added | value: " + projectile.parentPart.equipmentID.ToString() + "(" + part.id.id.ToString() + ")");
                        }
                        else
                        {
                            Debug.Log("[12]cannot add/replace parent part | value: " + projectile.parentPart.equipmentID.ToString() + "(" + part.id.id + ")");
                        }

                        if (projectileNew.HasComponent(113))
                        {
                            projectile.AddParentSubsystem(subsystem.id.id);
                            projectile.GetComponent(113);
                            Debug.Log("[13]parent subsystem added/replaced | value: " + projectile.parentSubsystem.equipmentID.ToString() + "(" + subsystem.id.id.ToString() + ")");
                        }
                        else
                        {
                            Debug.Log("[13]cannot add/replace parent subsystem | value: " + projectile.parentSubsystem.equipmentID.ToString() + "(" + subsystem.id.id.ToString() + ")");
                        }

                        if (projectile.HasComponent(148))
                        {

                            projectile.ReplaceComponent(148, projectile.projectileStartPosition);
                            projectile.GetComponent(148);
                            Debug.Log("[14]ProjectileStartPosition added | value: " + projectile.projectileStartPosition.v.ToString());
                        }
                        else
                        {
                            Debug.Log("[14]error startPosition");
                        }

                        if (projectile.HasComponent(112))
                        {

                            projectile.ReplaceParentPart(projectile.id.id);
                            projectile.GetComponent(112);
                            Debug.Log("[15]parent part added #2 | value: " + projectile.id.id.ToString());

                        }
                        else
                        {
                            Debug.Log("[15]cannot add/replace ParentPart projectile #2 | value: " + projectile.id.id.ToString());
                        }

                        if (projectile.HasComponent(113))
                        {

                            projectile.ReplaceParentSubsystem(projectile.id.id);
                            projectile.GetComponent(113);
                            Debug.Log("[16]parent subsystem added #2 | value: " + projectile.id.id.ToString());

                        }
                        else
                        {
                            Debug.Log("[16]cannot add/replace ParentSubsystem projectile #2 | value: " + projectile.id.id.ToString());
                        }

                        if (projectile.HasComponent(246))
                        {
                            projectile.ReplaceScale(bodyAssetScale);
                            projectile.GetComponent(246);
                            Debug.Log("[17]Projectile Scale added | value: " + bodyAssetScale.ToString()); 
                        } else
                        {
                            Debug.Log("[17]error Projectile scale | value: " + bodyAssetScale.ToString());
                        }

                        if (projectile.HasComponent(91))
                        {
                            projectile.ReplaceLevel(projectile.level.i);
                            projectile.GetComponent(91);
                            Debug.Log("[18]Projectile level added | value: " + projectile.level.i.ToString());
                        } else
                        {
                            Debug.Log("[18]error projectile level | value: " + projectile.level.i.ToString());
                        }

                        if (!projectile.HasComponent(132))
                        {
                            projectile.AddProjectileCollision(LayerMasks.projectileMask, 1.0f);
                            projectile.GetComponent(132);
                            Debug.Log("[19]Projectile collision added | value: " + LayerMasks.projectileMask.ToString());

                        } else
                        {
                            Debug.Log("[19]error Projectile collision | value: " + LayerMasks.projectileMask.ToString());
                        }
                        

                        if (projectile.HasComponent(83))
                        {
                            projectile.ReplaceInflictedDamage(projectile.inflictedDamage.f);
                            projectile.GetComponent(83);
                            Debug.Log("[20]InflictedDamage added | value: " + projectile.inflictedConcussion.f.ToString());
                        }
                        else
                        {
                            Debug.Log("[20]error InflictedDamage | value " + projectile.inflictedDamage.f.ToString());
                        }

                        

                        if (projectile.HasComponent(141))
                        {
                            projectile.ReplaceProjectileGuidanceTargetPosition(projectile.projectileGuidanceTargetPosition.v);
                            projectile.GetComponent(141);
                            Debug.Log("[23]ProjectileGuidanceTargetPosition added | value: " + projectile.projectileGuidanceTargetPosition.v.ToString());
                        }
                        else
                        {
                            Debug.Log("[23]error ProjectileGuidanceTargetPosition | value: " + projectile.projectileGuidanceTargetPosition.v.ToString());
                        }

                        if (!projectile.HasComponent(97))
                        {
                            projectile.ReplaceMovementSpeedCurrent(1f);
                            projectile.GetComponent(97);
                            Debug.Log("[24]MovementSpeedCurrent added | value: " + projectile.movementSpeedCurrent.f.ToString());
                        }
                        else
                        {
                            Debug.Log("[25]error MovementSpeedCurrent | value: " + projectile.movementSpeedCurrent.f.ToString());
                        }

                        if (!projectile.hasRicochetChance)
                        {
                            projectile.AddRicochetChance(1f);

                            Debug.Log("[25]RicochetChance added | value: " + projectile.ricochetChance.f.ToString());
                        }
                        else
                        {
                            Debug.Log("[26]error RicochetChance | value: " + projectile.ricochetChance.f.ToString());
                        }

                        if (projectile.HasComponent(75))
                        {
                            projectile.ReplaceFlightInfo(0.0f, 0.0f, projectile.projectileStartPosition.v, projectile.projectileStartPosition.v);
                            projectile.GetComponent(75);
                            Debug.Log("[26]FlightInfo added | value: " + projectile.projectileStartPosition.v.ToString() + ", " + projectile.projectileStartPosition.v.ToString());
                        }
                        else
                        {
                            Debug.Log("[26]error FlightInfo | value: " + projectile.projectileStartPosition.v.ToString() + ", " + projectile.projectileStartPosition.v.ToString());
                        }

                        if (!projectile.HasComponent(190))
                        {

                            projectile.SimpleMovement = true;
                            projectile.GetComponent(190);
                            Debug.Log("[27]SimpleMovement added | set to: true");
                        }
                        else
                        {
                            Debug.Log("[27]error SimpleMovement | set to: false");
                        }

                        if (!projectile.HasComponent(188))
                        {

                            projectile.SimpleFaceMotion = true;
                            projectile.GetComponent(188);
                            Debug.Log("[28]SimpleFaceMotion added | set to: true");

                        }
                        else
                        {
                            Debug.Log("[28]error SimpleFaceMotion | set to: false");
                        }

                        if (projectile.HasComponent(148))
                        {

                            projectile.ReplacePosition(projectile.position.v);
                            projectile.GetComponent(148);
                            Debug.Log("[29]ProjectilePosition added | value: " + projectile.position.v.ToString());
                        }
                        else
                        {
                            Debug.Log("[29]error ProjectilePosition | value: " + projectile.position.v.ToString());
                        }

                        if (projectile.HasComponent(245))
                        {
                            projectile.ReplaceRotation(projectile.rotation.q);
                            projectile.GetComponent(245);
                            Debug.Log("[30]ProjectileRotation added | value: " + projectile.rotation.q.ToString());
                        }
                        else
                        {
                            Debug.Log("[30]error ProjectileRotation | value: " + projectile.rotation.q.ToString());
                        }

                        if (projectile.HasComponent(198))
                        {

                            projectile.ReplaceSourceEntity(projectile.sourceEntity.combatID);
                            projectile.GetComponent(198);
                            Debug.Log("[31]SourceEntity (parentAction) Added | Parent Action: " + projectile.sourceEntity.combatID.ToString());

                        }
                        else
                        {
                            Debug.Log("[31]error replaceSourceEntity | Parent Action: " + projectile.sourceEntity.combatID.ToString());
                        }

                        if (projectile.HasComponent(209))
                        {

                            projectile.ReplaceTimeToLive(projectile.timeToLive.f);
                            projectile.GetComponent(209);
                            Debug.Log("[32]HasTimeToLive Added | value: " + projectile.timeToLive.f.ToString());
                        }
                        else
                        {
                            Debug.Log("[32]error TimeToLive | value: " + projectile.timeToLive.f.ToString());
                        }


                        if (bodyAssetUsed)
                        {
                            AssetPoolUtility.AttachInstance(projectile.assetKey.key, projectile, true);
                            Debug.Log("[33]entered into BodyAssetKey | Asset Key: " + projectile.assetKey.key.ToString());
                        }
                        else
                        {
                            Debug.Log("[33]error BodyAssetKey | Asset Key: " + projectile.assetKey.key.ToString());
                        }

                        Debug.Log("[!!!FINAL!!!] - It works! -");

                    }
                }
            }
        }
    }
}