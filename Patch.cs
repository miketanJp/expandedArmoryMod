using Entitas;
using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace fragmentMod
{
	using FanoutFunc = Func<Vector3, float, int, int, Vector3>;

	[HarmonyPatch]
    public partial class Patch
    {
		private static readonly Dictionary<string, FanoutFunc> fanouts = new Dictionary<string, FanoutFunc>()
		{
			["circular"] = CircularFanout,
			["umbrella"] = UmbrellaFanout,
			["starburst"] = StarburstFanout,
		};

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
				if (fragmentCount <= 0)
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
				if (!fragmentHardpointFound)
				{
					continue;
				}

				var fragmentFanoutFound = blueprint.TryGetString("fragment_fanout", out var fragmentFanout, null);
				if (!fragmentFanoutFound || !fanouts.TryGetValue(fragmentFanout, out var fanout))
				{
					// No fanout so all fragments start out clumped together.
					fanout = (f, s, n, t) => f;
				}

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


				var scatterAngleFromPart = DataHelperStats.GetCachedStatForPart("wpn_scatter_angle", partMirv);
				blueprint.TryGetFloat("fragment_scatter_angle", out var scatterAngle, scatterAngleFromPart);
				scatterAngle = Mathf.Max(0f, scatterAngle);

				blueprint.TryGetVector("fragment_scale", out var scale, projectileData.visual.body.scale);

				// This statement can be moved around depending on how often you want to trace.
				// With the statement here, only when we find suitable projectiles will the code
				// write the trace to the log.
				trace = true;

				for (var n = 0; n < fragmentCount; n += 1)
				{
					var projectileNew = combat.CreateEntity();

					if (projectileData.falloff != null && projectileData.falloff.animated)
					{
						projectileNew.isProjectileFalloffAnimated = true;
					}

					if (projectileData.fuseProximity != null)
					{
						projectileNew.isProjectileProximityFuse = true;
					}

					if (projectileData.splashDamage != null)
					{
						projectileNew.isDamageSplash = true;
					}

					if (projectileData.splashImpact != null)
					{
						projectileNew.isImpactSplash = true;
						projectileNew.ImpactSplashOnDamage = projectileData.splashImpact.triggerOnDamage;
					}

					if (fragmentBlueprint.IsFlagPresent("damage_dispersed"))
					{
						projectileNew.isDamageDispersed = true;
					}

					var deactivateBeforeRange = projectileData.range != null && projectileData.range.deactivateBeforeRange;
					projectileNew.isProjectilePrimed = !deactivateBeforeRange;

					projectileNew.ReplaceDataLinkSubsystemProjectile(projectileData);
					projectileNew.ReplaceParentPart(partMirv.id.id);
					projectileNew.ReplaceParentSubsystem(subsystemMirv.id.id);

					projectileNew.ReplaceScale(scale);

					if (projectile.hasLevel)
					{
						projectileNew.ReplaceLevel(projectile.level.i);
					}

					var (position, rotation, facing) = Fanout(
						projectile,
						scatterAngle,
						fanout,
						n,
						fragmentCount);

					if (projectile.hasFacing)
					{
						projectileNew.ReplaceFacing(facing);
					}

					if (projectile.hasPosition)
					{
						projectileNew.ReplacePosition(position);
					}

					if (projectile.hasRotation)
					{
						projectileNew.ReplaceRotation(rotation);
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
						projectileNew.AddProjectileTargetEntity(projectile.projectileTargetEntity.combatID);

						if (projectile.hasProjectileTargetEntity)
						{
                            projectileNew.ReplaceProjectileTargetEntity(projectile.projectileTargetEntity.combatID);
                        }

					}

                    // In v1.0, the mod will not detect this component for whatever reason.
					// We try to get the missing component by recreating it, then replacing as originally intended.
					// Continue statement is used to prevent the mod from breaking up the game.
                    if (projectile.hasProjectileTargetPosition)
                    {
                        projectileNew.AddProjectileTargetPosition(projectile.projectileTargetPosition.v.normalized);

                        if (projectile.hasProjectileTargetPosition)
                        {
                            projectileNew.ReplaceProjectileTargetPosition(projectile.projectileTargetPosition.v.normalized);
                        }

                    } else
                    {
                        continue;
                    }

                    if (projectile.hasProjectileIndex)
					{
						projectileNew.ReplaceProjectileIndex(projectile.projectileIndex.i);
					}

					AssetPoolUtility.AttachInstance(projectile.assetKey.key, projectileNew, true);
					var assetLinker = projectileNew.hasAssetLink ? projectileNew.assetLink.instance : null;
					if (projectileNew.isProjectilePrimed && assetLinker != null && assetLinker.fxHelperProjectile != null)
					{
						assetLinker.fxHelperProjectile.SetRange(1f);
					}

					ScheduledAttackSystem.AddInflictedDamageComponents(partMirv, projectileNew);
					ScheduledAttackSystem.AttachGuidedProjectileData(
						projectileNew,
						projectile,
						partMirv,
						projectileData,
						projectile.position.v,
						facing,
						rigidbody.velocity.magnitude,
						projectile.projectileGuidanceTargetPosition.v,
						addedVelocity: default);

					if (projectile.hasFlightInfo)
					{
						projectileNew.ReplaceFlightInfo(
							projectile.flightInfo.time,
							projectile.flightInfo.distance,
							projectileNew.flightInfo.origin,
							projectileNew.flightInfo.positionLast);
					}

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
					}

					CombatReplayHelper.OnProjectileTransform(projectileNew, true);
				}

                    DestroyProjectile(projectile);
				
            }

			if (trace)
			{
				Report(debugInfo);
				Debug.Log("[!!!THE END!!!] It works! -");
			}
		}
	}
}