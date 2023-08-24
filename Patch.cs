using Entitas;
using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Data;
using PhantomBrigade.Game.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
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
			//var debugInfo = new Queue<(Func<int, int, object, string>, int, int, object)>();

			var combat = Contexts.sharedInstance.combat;

			//debugInfo.Enqueue((ReportCombatContext, 0, 0, combat));

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
				//debugInfo.Enqueue((ReportPart, i, 0, part));

				var subsystem = IDUtility.GetEquipmentEntity(projectile.parentSubsystem.equipmentID);
                //debugInfo.Enqueue((ReportSubsystem, i, 0, subsystem));

                var blueprint = subsystem.dataLinkSubsystem.data;
                //debugInfo.Enqueue((ReportBlueprint, i, 0, blueprint));

                var fragmentDelayFound = blueprint.TryGetFloat("fragment_delay", out var fragmentDelay, 0);
                //debugInfo.Enqueue((ReportFragmentDelay, i, 0, (fragmentDelayFound, fragmentDelay)));
                if (!fragmentDelayFound)
				{
					continue;
				}
				if (projectile.flightInfo.time < fragmentDelay)
				{
					continue;
				}

				var fragmentCountFound = blueprint.TryGetInt("fragment_count", out var fragmentCount, 0);
                //debugInfo.Enqueue((ReportFragmentCount, i, 0, (fragmentCountFound, fragmentCount)));
                if (!fragmentCountFound)
				{
					continue;
				}
				if (fragmentCount <= 0)
				{
					continue;
				}

				var fragmentKeyFound = blueprint.TryGetString("fragment_key", out var fragmentKey, null);
                //debugInfo.Enqueue((ReportFragmentKey, i, 0, (fragmentKeyFound, fragmentKey)));
                if (!fragmentKeyFound)
				{
					continue;
				}

				var fragmentHardpointFound = blueprint.TryGetString("fragment_hardpoint", out var fragmentHardpoint, null);
                //debugInfo.Enqueue((ReportFragmentHardpoint, i, 0, (fragmentHardpointFound, fragmentHardpoint)));
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
                //debugInfo.Enqueue((ReportPresetMirv, i, 0, presetMirv));
                if (presetMirv == null)
				{
					continue;
				}

				var level = projectile.hasLevel ? projectile.level.i : 1;
				var partMirv = UnitUtilities.CreatePartEntityFromPreset(presetMirv, level: level);
                //debugInfo.Enqueue((ReportPartMirv, i, 0, partMirv));
                if (partMirv == null)
				{
					continue;
				}

				var subsystemMirv = EquipmentUtility.GetSubsystemInPart(partMirv, fragmentHardpoint);
                //debugInfo.Enqueue((ReportSubsystemMirv, i, 0, subsystemMirv));
                if (subsystemMirv == null)
				{
					continue;
				}
				if (!subsystemMirv.hasDataLinkSubsystem)
				{
					continue;
				}

				var fragmentBlueprint = subsystemMirv.dataLinkSubsystem.data;
                //debugInfo.Enqueue((ReportFragmentBlueprint, i, 0, fragmentBlueprint));

                var projectileData = fragmentBlueprint.projectileProcessed;
                //debugInfo.Enqueue((ReportProjectileData, i, 0, projectileData));


                var scatterAngleFromPart = DataHelperStats.GetCachedStatForPart("wpn_scatter_angle", partMirv);
				blueprint.TryGetFloat("fragment_scatter_angle", out var scatterAngle, scatterAngleFromPart);
				scatterAngle = Mathf.Max(0f, scatterAngle);


				var projScale = new DataBlockSubsystemProjectileVisual();
                blueprint.TryGetVector("fragment_scale", out var scale, projScale.body.scale);

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
						projectileNew.ReplaceProjectileTargetEntity(projectile.projectileTargetEntity.combatID);

                    }

                    if (projectile.hasProjectileTargetPosition)
                    {
                        projectileNew.ReplaceProjectileTargetPosition(projectile.projectileTargetPosition.v);
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

					/*if (trace)
					{
						debugInfo.Enqueue((ReportCloneFlightInfo, i, n, (projectile.hasFlightInfo, projectileNew)));
						debugInfo.Enqueue((ReportCloneProjectileData, i, n, (true, projectileData)));
						debugInfo.Enqueue((ReportCloneParentPart, i, n, (true, projectileNew, partMirv)));
						debugInfo.Enqueue((ReportCloneParentSubsystem, i, n, (true, projectileNew, subsystemMirv)));
						debugInfo.Enqueue((ReportCloneScale, i, n, (projectile.hasScale, projectileNew)));
						debugInfo.Enqueue((ReportCloneLevel, i, n, (projectile.hasLevel, projectileNew)));
						debugInfo.Enqueue((ReportClonePosition, i, n, (projectile.hasPosition, projectileNew)));
						debugInfo.Enqueue((ReportCloneRotation, i, n, (projectile.hasRotation, projectileNew)));
						debugInfo.Enqueue((ReportCloneFacing, i, n, (projectile.hasFacing, projectileNew)));
						debugInfo.Enqueue((ReportCloneTimeToLive, i, n, (projectile.hasTimeToLive, projectileNew)));
						debugInfo.Enqueue((ReportCloneProjectileCollision, i, n, (projectile.hasProjectileCollision, LayerMasks.projectileMask)));
						debugInfo.Enqueue((ReportCloneSourceEntity, i, n, (projectile.hasSourceEntity, projectileNew)));
                        debugInfo.Enqueue((ReportCloneProjectileTargetEntity, i, n, (projectile.hasProjectileTargetEntity, projectileNew)));
                        debugInfo.Enqueue((ReportCloneProjectileTargetPosition, i, n, (projectile.hasProjectileTargetPosition, projectileNew)));
                        debugInfo.Enqueue((ReportCloneProjectileIndex, i, n, (projectile.hasProjectileIndex, projectileNew)));
						debugInfo.Enqueue((ReportCloneInflictedDamage, i, n, (projectile.hasInflictedDamage, 1.0f)));
					}*/

					CombatReplayHelper.OnProjectileTransform(projectileNew, true);
				}

				DestroyProjectile(projectile);
			}

            if (trace)
			{
				//Report(debugInfo);
				Debug.Log("[!!!THE END!!!] It works! -");
			}
		}
	}
}