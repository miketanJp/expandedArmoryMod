using PhantomBrigade;
using PhantomBrigade.Data;
using UnityEngine;

namespace fragmentMod
{
	public partial class Patch
	{
		private static Vector3 GetTargetPosition(CombatEntity projectile, float speed)
		{
			if (projectile.hasProjectileTargetPosition)
			{
				return projectile.projectileTargetPosition.v;
			}

			if (!projectile.hasProjectileTargetEntity)
			{
				return Vector3.zero;
			}

			var targetID = projectile.projectileTargetEntity.combatID;
			var targetEntity = IDUtility.GetCombatEntity(targetID);
			if (targetEntity == null)
			{
				return Vector3.zero;
			}

			// From here down is taken from ScheduledAttackSystem.ProcessProjectiles().

			var targetVector = Vector3.zero;
			if (targetEntity.hasPosition)
			{
				targetVector = targetEntity.position.v;
				if (targetEntity.hasLocalCenterPoint)
				{
					targetVector += targetEntity.localCenterPoint.v;
				}
			}

			if (targetEntity.hasVelocity && (double)targetEntity.velocity.v.sqrMagnitude > 0.0 && (double)speed > 50.0)
			{
				var position = projectile.position.v;
				var current = targetVector;
				var v = targetEntity.velocity.v;
				var velocityModifier = DataLinker<DataContainerSettingsSimulation>.data.targetVelocityModifier;
				var trackingIterations = DataLinker<DataContainerSettingsSimulation>.data.targetTrackingIterations;
				for (var index = 0; index < trackingIterations; index += 1)
				{
					var deltaT = (targetVector - position).magnitude / speed;
					targetVector = v * (deltaT * velocityModifier) + current;
				}
			}

			return targetVector;
		}
	}
}
