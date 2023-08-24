using PhantomBrigade.Data;

/* as for Phantom Brigade v.1.1.2, the actual mod structure was unable to find the wrapper method of DataBlockSubsystemProjectileVisual.body despite it's clearly present on the codebase.
 * With the said game version's codebase being updated, the mod wasn't then unable to get the field of the visual asset used (the projectile) in order to be scaled from subsystem config
 * as per specified value inserted.
 * By implementing DataBlockSubsystemProjectileVisual directly, when instantiated, is then able to be called correctly (Patch.cs - Line 167/168) */

namespace fragmentMod
{
	public partial class Patch
    {
        public class DataBlockSubsystemProjectileVisual
        {
            public DataBlockAssetProjectile body = new DataBlockAssetProjectile();
            public DataBlockAssetFactionBased impact;
            public DataBlockAssetFactionBased deactivation;
        }
    }
}