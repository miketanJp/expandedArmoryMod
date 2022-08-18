using UnityEngine;
using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Data;
using System.Collections.Generic;
using PhantomBrigade.Combat.Systems;

namespace fragmentMod
{
    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(GameController), "Initialize", MethodType.Normal)]
        [HarmonyPatch(typeof(ScheduledAttackSystem), "Execute", MethodType.Normal)]

        [HarmonyPostfix]
        static void Sas_fragment(List <CombatEntity> entities, CombatEntity projectile, CombatContext combat)
        {

            for (int i = 0; i < entities.Count; i++)
            {
                projectile = entities[i];
                combat = Contexts.sharedInstance.combat;

                Vector3 firingPoint = Vector3.one;
                Vector3 targetPoint = Vector3.MoveTowards(Vector3.zero, Vector3.forward, 1.0f);
                Vector3 bodyAssetScale = Vector3.Scale(Vector3.zero, Vector3.one);
                Transform scatteredDirection = null;
                var bak = CombatComponentsLookup.AssetKey;
                var level = CombatComponentsLookup.Level;
                var parentAction = ActionComponentsLookup.ActionOwner;
                bool bodyAssetUsed = true;

                var part = IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID);
                var subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
                var blueprint = subsystem.dataLinkSubsystem.data;
                var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out float fragmentDelay, 0);
                var fragmentKeyFound = blueprint.TryGetString("fragment_key", out string fragmentKey, null);
                var fragmentBlueprint = DataMultiLinkerSubsystem.GetEntry(fragmentKey);
                var projectileData = fragmentBlueprint.projectileProcessed;

                if (fragmentDelayFound && fragmentKeyFound)
                {
                        Debug.Log("entered into first if (fragment delay and key found!)");

                    if (projectile.hasParentPart)
                    {
                        part.GetComponent(112);

                    } else if (true)
                    {

                    } 
                    if (projectile.hasParentSubsystem)
                    {
                        subsystem.GetComponent(113);

                    } else if (true)
                    {

                    }


                    if (projectile.flightInfo.time > fragmentDelay)
                    {

                        // Destroy current projectile
                        projectile.TriggerProjectile();

                        //create new projectiles.
                        var projectileNew = combat.CreateEntity();

                        //Add the new projectile and linking to the actual part and projectile data 
                        projectileNew.AddDataLinkSubsystemProjectile(projectileData);
                        projectileNew.AddParentPart(part.id.id);
                        projectile.AddParentSubsystem(subsystem.id.id);

                        //Replace projectile properties (collision, damage and other subsystem stats.
                        projectile.ReplaceScale(bodyAssetScale);
                        projectile.ReplaceLevel(level);

                        projectile.AddProjectileCollision(LayerMasks.projectileMask, 0.5f);
                        projectile.AddInflictedDamage(2);
                        projectile.ReplaceProjectileTargetPosition(targetPoint);

                        projectile.ReplaceMovementSpeedCurrent(1.5f);
                        projectile.ReplaceRicochetChance(0);
                        projectile.ReplaceFlightInfo(1.0f, 0.5f, firingPoint, firingPoint);

                        projectile.SimpleMovement = true;
                        projectile.SimpleFaceMotion = true;

                        projectile.ReplacePosition(firingPoint);
                        projectile.ReplaceRotation(Quaternion.LookRotation(scatteredDirection.TransformDirection(2, 2, 2)));
                        projectile.ReplaceFacing(scatteredDirection.TransformDirection(1, 1, 1));
                        projectile.ReplaceSourceEntity(parentAction);
                        projectile.ReplaceTimeToLive(0.0f);

                        if (bodyAssetUsed)
                            AssetPoolUtility.AttachInstance(bak.ToString(), projectile, true);

                        Debug.Log("entered into second if (missile logic). It works!");
                    }
                }   
            }
        }
    }
}