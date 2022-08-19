using UnityEngine;
using HarmonyLib;
using Entitas;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Action.Components;

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

                var part = IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID);
                var subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
                var blueprint = subsystem.dataLinkSubsystem.data;
                var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out float fragmentDelay, 0);
                var fragmentKeyFound = blueprint.TryGetString("fragment_key", out string fragmentKey, null);

                Vector3 bodyAssetScale = Vector3.Scale(Vector3.zero, Vector3.one);

                ActionEntity parentAction = null;
                AssetLink bodyAssetKey = null;
                var level = CombatComponentsLookup.Level;
                bool bodyAssetUsed = true;

                if (fragmentDelayFound && fragmentKeyFound)
                {
                    if (projectile.flightInfo.time > fragmentDelay)
                    {
                        var fragmentBlueprint = DataMultiLinkerSubsystem.GetEntry(fragmentKey);
                        var projectileData = fragmentBlueprint.projectileProcessed;

                        //Destroy Current projectile
                        projectile.TriggerProjectile();

                        // Create new projectile.
                        var projectileNew = combat.CreateEntity();

                        // Attach the new projectile to the subsystem and part (parent).
                        if (projectileNew.HasComponent(75)) {

                            projectile.ReplaceDataLinkSubsystemProjectile(projectileData);
                            projectileNew.GetComponent(75);
                            Debug.Log("data link added");

                        } else
                        {
                            Debug.Log("cannot get or add/replace projectile Data.");
                        }

                        if (projectileNew.HasComponent(112))
                        {
                            projectileNew.AddParentPart(part.id.id);
                            projectileNew.GetComponent(112);
                            Debug.Log("parent part added");
                        }
                        else
                        {
                            Debug.Log("cannot get or add/replace parent part");
                        }

                        if (projectileNew.HasComponent(113))
                        {
                            projectileNew.AddParentSubsystem(subsystem.id.id);
                            projectileNew.GetComponent(113);
                            Debug.Log("parent subsystem added");
                        } else
                        {
                            Debug.Log("cannot get or add/replace parent subsystem");
                        }

                        if (projectile.HasComponent(148))
                        {
                            
                            projectile.ReplaceComponent(148, projectile.projectileStartPosition);
                            projectile.GetComponent(148);
                            Debug.Log("ProjectileStartPosition added (true)");
                        } else
                        {
                            Debug.Log("error startPosition");
                        }

                        if (projectileNew.HasComponent(151))
                        {
                            
                            projectile.ReplaceComponent(151, projectile.projectileTargetPosition);
                            projectile.GetComponent(151);
                            Debug.Log("ProjectileTargetPosition added (true)");
                        } else
                        {
                            Debug.Log("error TargetPosition");
                        }

                        if (projectile.HasComponent(112))
                        {
                            
                            projectile.ReplaceParentPart(projectile.id.id);
                            projectile.GetComponent(112);
                            Debug.Log("parent part added #2");

                        } else
                        {
                            Debug.Log("cannot get or add/replace ParentPart projectile #2");
                        }

                        if (projectile.HasComponent(113))
                        {
                            
                            projectile.ReplaceParentSubsystem(projectile.parentSubsystem.equipmentID);
                            projectile.GetComponent(113);
                            Debug.Log("parent subsystem added #2");

                        } else
                        {
                            Debug.Log("cannot get or add/replace ParentSubsystem projectile #2");
                        }

                        projectile.ReplaceScale(bodyAssetScale);
                        projectile.ReplaceLevel(level);

                        projectile.AddProjectileCollision(LayerMasks.projectileMask, 0.0f);

                        if (projectile.HasComponent(83))
                        {
                            projectile.ReplaceInflictedDamage(projectile.inflictedDamage.f);
                            projectile.GetComponent(83);
                            Debug.Log("InflictedDamage added");
                        } else
                        {
                            Debug.Log("error InflictedDamage");
                        }

                        if (projectile.HasComponent(151))
                        {
                            projectile.ReplaceProjectileTargetPosition(projectile.projectileTargetPosition.v);
                            projectile.GetComponent(151);
                            Debug.Log("ProjectileTargetPosition added.");
                        } else
                        {
                            Debug.Log("error TargetPosition");
                        }

                        if (projectile.HasComponent(97))
                        {
                            projectile.ReplaceMovementSpeedCurrent(0.0f);
                            projectile.GetComponent(97);
                            Debug.Log("MovementSpeedCurrent added");
                        } else
                        {
                            Debug.Log("error MovementSpeedCurrent");
                        }

                        if (projectile.HasComponent(157))
                        {
                            projectile.ReplaceRicochetChance(projectile.ricochetChance.f);
                            projectile.GetComponent(157);
                            Debug.Log("RicochetChance added");
                        } else
                        {
                            Debug.Log("error RicochetChance");
                        }

                        if (projectile.HasComponent(75))
                        {
                            projectile.ReplaceFlightInfo(0.0f, 0.0f, projectile.projectileStartPosition.v, projectile.projectileStartPosition.v);
                            projectile.GetComponent(75);
                            Debug.Log("FlightInfo added");
                        } else
                        {
                            Debug.Log("error FlightInfo");
                        }

                        if (projectile.HasComponent(190)) {

                            projectile.SimpleMovement = true;
                            projectile.GetComponent(190);
                            Debug.Log("SimpleMovement added");
                        } else
                        {
                            Debug.Log("error SimpleMovement");
                        }

                        if (projectile.HasComponent(188))
                        {
                            
                            projectile.SimpleFaceMotion = true;
                            projectile.GetComponent(188);
                            Debug.Log("SimpleFaceMotion added");

                        }
                        else
                        {
                            Debug.Log("error SimpleFaceMotion");
                        }

                        if (projectile.HasComponent(148))
                        {
                            
                            projectile.ReplacePosition(projectile.position.v);
                            projectile.GetComponent(148);
                            Debug.Log("ProjectilePosition added");
                        } else
                        {
                            Debug.Log("error ProjectilePosition");
                        }
                        
                        if (projectile.HasComponent(245))
                        {
                            projectile.ReplaceRotation(projectile.rotation.q);
                            projectile.GetComponent(245);
                            Debug.Log("ProjectileRotation added");
                        } else
                        {
                            Debug.Log("error ProjectileRotation");
                        }

                        if (projectile.HasComponent(198))
                        {

                            projectile.ReplaceSourceEntity(parentAction.actionOwner.combatID);
                            projectile.GetComponent(198);
                            Debug.Log("SourceEntity (parentAction) Added");

                        } else
                        {
                            Debug.Log("error replaceSourceEntity");
                        }

                        if (projectile.HasComponent(209)) 
                        {
                            
                            projectile.AddTimeToLive(0.0f);
                            projectile.GetComponent(209);
                            Debug.Log("HasTimeToLive Added");
                        }
                        else
                        {
                            Debug.Log("error TimeToLive");
                        }


                        if (bodyAssetUsed) { 
                            Debug.Log("entered into BodyAssetKey");
                            AssetPoolUtility.AttachInstance(bodyAssetKey.key, projectile, true);
                        } else
                        {
                            Debug.Log("error BodyAssetKey");
                        }

                        Debug.Log("entered into second if (projectile logic). It works!");

                    }
                }   
            }
        }
    }
}