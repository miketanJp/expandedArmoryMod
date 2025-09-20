## [DISMISSED CODE] - MIRV Missile (AKA FragmentMod)
The present mod is a library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/), a Hybrid Turn-based strategy game by [Brace Yourself Games](https://braceyourselfgames.com).+

The main purpose of this mod is to introduce [MIRV Missiles](https://en.wikipedia.org/wiki/Multiple_independently_targetable_reentry_vehicle) with different splitting patterns to choose from. Actually, wareheads are not yet truly independent as they still track and strike one or more targets around it.

NOTE: Library Mod is dismissed since Phantom Brigade version 1.2.0, as it introduces a better and more flexible implementation of delayed fragmentation. Therefore, mod version 1.2.0 onward the mod is using the official implementation provided by the game while the library mod project is still available.

## Basic introduction
The main idea to develop this kind of mod came when I tried, through weapon config files, to apply the same fragmentation properties used by shotguns as a similar system weren't available to Missile Launchers. This made me realized that approach wasn't technically possible as I needed to split the projectile after a set time. The system used in shotguns works in order to apply instant fragmentation (immediately after the projectiles are fired) and the fragmentation delay algorithm works specifically for that kind of weapons, which is why I decided to develop a fragmentation system specifically for missiles.

The mod uses custom properties in the subsystem YAML file, each one of them can be customized with desired values.
New projectiles created in that istant will then inherit the properties of the original warhead (guidance data, projectile speed, lifetime, etc.), since the child projectiles will either cause conflicts or empty assets with no properties attached ( = mere game props with no "soul").

<b>Custom values used</b> (Library code version)
- fragment_count (ints) → The number of fragments created from the original warhead while splitted;
- fragment_delay (floats) → The interval of the missile before splitting into several fragments from the moment it has been fired;
- fragment_scatter_angle (floats) → Scatter angle of the splitting missiles. It relies on wpn_scatter_angle property;
- fragment_key (strings) → The subsystem (actual weapon) where the injection should happen.
- fragment_hardpoint (strings) → The weapon hardpoint where the fragmentation will be performed;
- fragment_fanout (strings) → Fanout pattern to perform the desired splitting mode;
- fragment_scale (vectors) → New projectiles scale express in Vector3 coordinates (X,Y,Z)

<b>WARNING:</b> in order to avoid issues, it is strongly adviced to set an amount between 18 and 23 as Unity Engine seems to make the exceeding amount disappear.
However, the mod does also provided custom AssetPools limits to the projectiles just for testing if you want to fire lots of missiles at the cost of degraded performance on GPU.

## Images
![starbust](https://user-images.githubusercontent.com/88181255/188744853-dbecbb07-64be-403b-95ce-61c9e719f7cf.png)
"Starburst" Pattern

![circular](https://user-images.githubusercontent.com/88181255/188744872-4971d4ce-05b7-419c-ae5d-b48cb301d5f8.png)
"Circular" pattern

## Live Demo
https://user-images.githubusercontent.com/88181255/188748556-9b0dc88f-db61-4542-816c-647e1e9dcfc7.mp4

## Changelog

(2024-30-05) - Dismissed custom code in 1.1.4 as the stock delayed fragmentation is more flexible (featured introduced in Phantom Brigade 1.2.0). Handled the damage control over child projectiles.
New Manufacturer added as well as its related icon to make it distinguish from other manufacturers, making it unique.

(2022-09-07) - The actual work for creating functional child projectiles from a original warhead is done and the mod is considered functional with no basic issues at runtime, Therefore I can consider it as a operational version 1.0.0 of the mod

(2022-09-07) - At the moment, only 'Starbust' and 'Circular' patterns can be used

(2023-03-09) - Fitted the whole mod structure of part preset format and refactored for PB 1.0
