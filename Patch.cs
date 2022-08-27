using UnityEngine;
using HarmonyLib;
using Entitas;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;

using System;
using System.Collections.Generic;

namespace fragmentMod
{
    [HarmonyPatch]
    public partial class Patch
    {
		[HarmonyPatch(typeof(ScheduledAttackSystem), "Execute", MethodType.Normal)]
		[HarmonyPostfix]
        static void Sas_fragment()
        {
			var trace = false;
			var debugInfo = new List<(Func<int, int, object, string>, int, int, object)>();

			var combat = Contexts.sharedInstance.combat;
			debugInfo.Add((ReportCombatContext, 0, 0, combat));

			var projectiles = Contexts.sharedInstance.combat.GetEntities(CombatMatcher.DataLinkSubsystemProjectile);
			for (var i = 0; i < projectiles.Length; i += 1)
			{
				var projectile = projectiles[i];

				if (projectile.isDestroyed)
				{
					continue;
				}

				if (projectile.hasProjectileDestructionPosition)
				{
					// Projectile has been triggered but not destroyed yet.
					continue;
				}

				if (!projectile.hasPosition)
				{
					// Must have a position to know where the fragments start.
					continue;
				}

				if (!projectile.hasAuthoritativeRigidbody)
				{
					// No rigidbody --> not a guided projectile
					continue;
				}

				var rigidbody = projectile.authoritativeRigidbody?.rb;
				if (rigidbody == null)
				{
					// Defensive programming: component exists so someone stuffed a null.
					continue;
				}

				var speed = rigidbody.velocity.magnitude;
				var targetPosition = GetTargetPosition(projectile, speed);

				var part = IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID);
				debugInfo.Add((ReportPart, i, 0, part));

				var subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
				debugInfo.Add((ReportSubsystem, i, 0, subsystem));

				var blueprint = subsystem.dataLinkSubsystem.data;
				debugInfo.Add((ReportBlueprint, i, 0, blueprint));

				var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out var fragmentDelay, 0);
				debugInfo.Add((ReportFragmentDelay, i, 0, (fragmentDelayFound, fragmentDelay)));
				if (!fragmentDelayFound)
				{
					continue;
				}
				if (projectile.flightInfo.time < fragmentDelay)
				{
					continue;
				}

				var fragmentCountFound = blueprint.TryGetInt("fragment_count", out var fragmentCount, 0);
				debugInfo.Add((ReportFragmentCount, i, 0, (fragmentCountFound, fragmentCount)));
				if (!fragmentCountFound)
				{
					continue;
				}

				var fragmentKeyFound = blueprint.TryGetString("fragment_key", out var fragmentKey, null);
				debugInfo.Add((ReportFragmentKey, i, 0, (fragmentKeyFound, fragmentKey)));
				if (!fragmentKeyFound)
				{
					continue;
				}

				var fragmentHardpointFound = blueprint.TryGetString("fragment_hardpoint", out var fragmentHardpoint, null);
				debugInfo.Add((ReportFragmentHardpoint, i, 0, (fragmentHardpointFound, fragmentHardpoint)));
				if (!fragmentKeyFound)
				{
					continue;
				}

				var bodyAssetScale = Vector3.one.normalized;
				debugInfo.Add((ReportBodyAssetScale, i, 0, bodyAssetScale));

				var presetMirv = DataMultiLinkerPartPreset.GetEntry(fragmentKey);
				debugInfo.Add((ReportPresetMirv, i, 0, presetMirv));
				if (presetMirv == null)
				{
					continue;
				}

				var level = projectile.hasLevel ? projectile.level.i : 1;
				var partMirv = UnitUtilities.CreatePartEntityFromPreset(presetMirv, level: level);
				debugInfo.Add((ReportPartMirv, i, 0, partMirv));
				if (partMirv == null)
				{
					continue;
				}

				var subsystemMirv = EquipmentUtility.GetSubsystemInPart(partMirv, fragmentHardpoint);
				debugInfo.Add((ReportSubsystemMirv, i, 0, subsystemMirv));
				if (subsystemMirv == null)
				{
					continue;
				}
				if (!subsystemMirv.hasDataLinkSubsystem)
				{
					continue;
				}

				var fragmentBlueprint = subsystemMirv.dataLinkSubsystem.data;
				debugInfo.Add((ReportFragmentBlueprint, i, 0, fragmentBlueprint));

				var projectileData = fragmentBlueprint.projectileProcessed;
				debugInfo.Add((ReportProjectileData, i, 0, projectileData));

				// This statement can be moved around depending on how often you want to trace.
				// With the statement here, only when we find suitable projectiles will the code
				// write the trace to the log.
				trace = true;

				for (var n = 0; n < fragmentCount; n += 1)
				{
					// When ScheduledAttackSystem.ProcessProjectiles() creates a new projectile,
					// will create a new CombatEntity and assign a number of properties (components) to
					// it and then call AddInflictedDamageComponents() followed by
					// AttachTypeSpecificProjectileData().

					var projectileNew = combat.CreateEntity();
					ScheduledAttackSystem.AttachGuidedProjectileData(
						projectileNew,
						projectile,
						partMirv,
						projectileData,
						projectile.position.v,
						rigidbody.transform.forward,
						rigidbody.velocity.magnitude,
						projectile.projectileGuidanceTargetPosition.v);

					if (projectile.hasFlightInfo)
					{
						projectileNew.ReplaceFlightInfo(
							projectile.flightInfo.time,
							projectile.flightInfo.distance,
							projectileNew.flightInfo.origin,
							projectileNew.flightInfo.positionLast);
					}

					// The following assignments are in the same order as in ScheduledAttackSystem.ProcessProjectiles()
					// but a few properties are missing: isDamageSplash, isImpactSplash, isDamageDispersed,
					// isProjectileProximityFuse, isProjectileFalloffAnimated. These are booleans that are
					// easily added if necessary.

					projectileNew.ReplaceDataLinkSubsystemProjectile(projectileData);
					projectileNew.ReplaceParentPart(partMirv.id.id);
					projectileNew.ReplaceParentSubsystem(subsystemMirv.id.id);

					if (projectile.hasScale)
					{
						projectileNew.ReplaceScale(projectile.scale.v);
					}

					if (projectile.hasLevel)
					{
						projectileNew.ReplaceLevel(projectile.level.i);
					}

					if (projectile.hasPosition)
					{
						projectileNew.ReplacePosition(projectile.position.v.normalized);
					}

					if (projectile.hasRotation)
					{
						projectileNew.ReplaceRotation(projectile.rotation.q);
					}

					if (projectile.hasFacing)
					{
						projectileNew.ReplaceFacing(projectile.facing.v);
					}

					if (projectile.hasTimeToLive)
					{
						projectileNew.ReplaceTimeToLive(projectile.timeToLive.f);
					}

					if (projectile.hasProjectileCollision)
					{
						projectileNew.ReplaceProjectileCollision(LayerMasks.projectileMask, projectile.projectileCollision.radius);
					}

					if (projectile.hasSourceEntity)
					{
						projectileNew.ReplaceSourceEntity(projectile.sourceEntity.combatID);
					}

					if (projectile.hasProjectileTargetEntity)
					{
						projectileNew.ReplaceProjectileTargetEntity(projectile.projectileTargetEntity.combatID);
					}

					if (projectile.hasProjectileTargetPosition)
					{
						projectileNew.ReplaceProjectileTargetPosition(projectile.projectileTargetPosition.v.normalized);
					}

					if (projectile.hasProjectileIndex)
					{
						projectileNew.ReplaceProjectileIndex(projectile.projectileIndex.i);
					}

					if (projectile.hasInflictedDamage)
					{
						ScheduledAttackSystem.AddInflictedDamageComponents(partMirv, projectileNew);
						projectileNew.ReplaceInflictedDamage(1.0f);
					}

					// MovementSpeedCurrent, FlightInfo, RicochetChange, SimpleMovement, SimpleFaceMotion appear
					// to be used with only ballistic projectiles.

					if (projectile.hasMovementSpeedCurrent)
					{
						projectileNew.ReplaceMovementSpeedCurrent(3f);
					}

					if (projectile.hasRicochetChance)
					{
						projectileNew.ReplaceRicochetChance(0.5f);
					}

					projectileNew.SimpleMovement = projectile.SimpleMovement;
					projectileNew.SimpleFaceMotion = projectile.SimpleFaceMotion;

					AssetPoolUtility.AttachInstance(projectile.assetKey.key, projectileNew, true);

					if (trace)
					{
						debugInfo.Add((ReportCloneFlightInfo, i, n, (projectile.hasFlightInfo, projectileNew)));
						debugInfo.Add((ReportCloneProjectileData, i, n, (true, projectileData)));
						debugInfo.Add((ReportCloneParentPart, i, n, (true, projectileNew, partMirv)));
						debugInfo.Add((ReportCloneParentSubsystem, i, n, (true, projectileNew, subsystemMirv)));
						debugInfo.Add((ReportCloneScale, i, n, (projectile.hasScale, projectileNew)));
						debugInfo.Add((ReportCloneLevel, i, n, (projectile.hasLevel, projectileNew)));
						debugInfo.Add((ReportClonePosition, i, n, (projectile.hasPosition, projectileNew)));
						debugInfo.Add((ReportCloneRotation, i, n, (projectile.hasRotation, projectileNew)));
						debugInfo.Add((ReportCloneFacing, i, n, (projectile.hasFacing, projectileNew)));
						debugInfo.Add((ReportCloneTimeToLive, i, n, (projectile.hasTimeToLive, projectileNew)));
						debugInfo.Add((ReportCloneProjectileCollision, i, n, (projectile.hasProjectileCollision, LayerMasks.projectileMask)));
						debugInfo.Add((ReportCloneSourceEntity, i, n, (projectile.hasSourceEntity, projectileNew)));
						debugInfo.Add((ReportCloneProjectileTargetEntity, i, n, (projectile.hasProjectileTargetEntity, projectileNew)));
						debugInfo.Add((ReportCloneProjectileTargetPosition, i, n, (projectile.hasProjectileTargetPosition, projectileNew)));
						debugInfo.Add((ReportCloneProjectileIndex, i, n, (projectile.hasProjectileIndex, projectileNew)));
						debugInfo.Add((ReportCloneInflictedDamage, i, n, (projectile.hasInflictedDamage, 1.0f)));
						debugInfo.Add((ReportCloneMovementSpeedCurrent, i, n, (projectile.hasMovementSpeedCurrent, projectileNew)));
						debugInfo.Add((ReportCloneRicochetChance, i, n, (projectile.hasRicochetChance, projectileNew)));
						debugInfo.Add((ReportCloneSimpleMovement, i, n, projectileNew.SimpleMovement));
						debugInfo.Add((ReportCloneSimpleFaceMotion, i, n, projectileNew.SimpleFaceMotion));
					}

					CombatReplayHelper.OnProjectileTransform(projectileNew, true);
				}

				//Destroy the projectile.
				projectile.TriggerProjectile(true);
			}

			if (trace)
			{
				Report(debugInfo);
				Debug.Log("[!!!THE END!!!] It works! -");
			}
		}
	}
}