namespace fragmentMod
{
	public partial class Patch
	{
		private static void DestroyProjectile(CombatEntity projectile)
		{
			// Lifted from ProjectileUtility.TriggerProjectile()

			if (projectile.hasProjectileCollision)
			{
				projectile.RemoveProjectileCollision();
			}

			if (projectile.hasProjectileGuidancePID)
			{
				projectile.RemoveProjectileGuidancePID();
			}

			if (projectile.hasProjectileGuidanceProgress)
			{
				projectile.RemoveProjectileGuidanceProgress();
			}

			if (projectile.hasProjectileGuidanceTargetBlend)
			{
				projectile.RemoveProjectileGuidanceTargetBlend();
			}

			if (projectile.hasProjectileGuidanceTargetOffset)
			{
				projectile.RemoveProjectileGuidanceTargetOffset();
			}

			if (projectile.hasProjectileRigidbodyDriverInput)
			{
				projectile.RemoveProjectileRigidbodyDriverInput();
			}

			if (projectile.hasAssetLink)
			{
				projectile.assetLink.instance.Stop();
				CombatReplayHelper.OnProjectileEnd(projectile);
			}

			projectile.isDestroyed = true;
		}
	}
}
