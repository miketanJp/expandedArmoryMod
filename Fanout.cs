using System;

using UnityEngine;

namespace fragmentMod
{
	using FanoutFunc = Func<Vector3, float, int, int, Vector3>;

	public partial class Patch
	{
		// A short explainer for those not familiar with Unity's coordinate systems. This group of functions
		// makes extensive use of rotations and it can be hard to understand what those rotations are doing
		// without a good grasp on the coordinate system.
		//
		// Unity uses a left-handed coordinate system with positive Z pointing into the screen, positive X
		// going to the right and positive Y up. By convention, missiles are always pointed in the positive
		// Z direction in the local transform. You will see that denoted as `Vector3.forward` below.
		//
		// Looking from the positive end of the axis toward the origin, positive rotation is clockwise.
		// To point a missile left or right (yaw), rotate it about the Y-axis. Positive angles angles will
		// point the missile to its right, negative ones to its left. To point a missile up or down (pitch),
		// rotate it about the X-axis. Positive angles will point it down, negatives ones up.
		//
		// There are times when the missile is rotated about its Z-axis, which at first doesn't make sense since
		// most missiles are cylinders. However, this rotation is mostly used as the first step in a compound
		// rotation for when the new facing isn't aligned with either the X- or Y-axis. The easiest way to do
		// this is rotate around the Z-axis first then pitch the missile up (negative angle on X-axis rotation).
		// See the `CircularFanout()` function for an example.

		private static (Vector3 Position, Quaternion Rotation, Vector3 Facing)
			Fanout(
				CombatEntity projectile,
				float scatterAngle,
				FanoutFunc fanout,
				int n,
				int totalFragments)
		{
			var position = projectile.hasPosition ? projectile.position.v : Vector3.zero;
			var rotation = projectile.hasRotation ? projectile.rotation.q : Quaternion.Euler(0f, 0f, 0f);
			var facing = projectile.hasFacing ? projectile.facing.v : Vector3.forward;

			if (!projectile.hasFacing)
			{
				return (position, rotation, facing);
			}

			if (scatterAngle > 0f)
			{
				facing = fanout(facing, scatterAngle, n, totalFragments);
				rotation = Quaternion.LookRotation(facing);
			}

			return (position, rotation, facing);
		}

		private static Vector3 CircularFanout(Vector3 facing, float scatterAngle, int n, int totalFragments)
		{
			switch (totalFragments)
			{
				case 1:
					return facing;
				case 2:
					return DiagonalFanout(facing, scatterAngle, 1, n, totalFragments);
				case 3:
					return TriangleFanout(facing, scatterAngle, n);
			}

			var odd = totalFragments % 2 == 1;

			// Back-roll the missile a quarter-turn before setting the roll angle for odd circles because 12 o'clock for
			// `Vector3.forward` is horizontal right with the missile coming at you.
			// Even circles are tipped because the 4-fragment ones look better with the fragments coming from the corners
			// (cross) rather than the compass points (tee).
			var roll = (odd ? -90 : 45) + n * 360 / totalFragments;

			var pitch = -scatterAngle / 2f;
			var orientation = Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.AngleAxis(pitch, Vector3.up);
			return Quaternion.LookRotation(facing) * orientation * Vector3.forward;
		}

		private static Vector3 DiagonalFanout(Vector3 facing, float scatterAngle, int rollDirection, int n, int totalFragments)
		{
			// Roll a horizontal fanout.
			var roll = Math.Sign(rollDirection) * 45f;
			return LinearFanout(
				facing,
				scatterAngle,
				a => Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.AngleAxis(a, Vector3.up),
				n,
				totalFragments);
		}

		private static Vector3 TriangleFanout(Vector3 facing, float scatterAngle, int n)
		{
			// Back-roll the missile a quarter-turn before setting the roll angle because 12 o'clock for
			// `Vector3.forward` is horizontal right with the missile coming at you.
			var roll = -90 + n * 120f;
			var pitch = -scatterAngle / 2f;
			var orientation = Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.AngleAxis(pitch, Vector3.up);
			return Quaternion.LookRotation(facing) * orientation * Vector3.forward;
		}

		private static Vector3 UmbrellaFanout(Vector3 facing, float scatterAngle, int n, int totalFragments)
		{
			if (n < 4)
			{
				return CircularFanout(facing, scatterAngle, n, totalFragments);
			}

			var odd = totalFragments % 2 == 1;
			if (odd && n == 0)
			{
				return facing;
			}

			var adj = odd ? 1 : 0;
			n -= adj;
			totalFragments -= adj;

			return CircularFanout(facing, scatterAngle, n, totalFragments);
		}

		private static Vector3 StarburstFanout(Vector3 facing, float scatterAngle, int n, int totalFragments)
		{
			// Example of a large AoE MIRV. Probably would look pretty good with a high-apogee primary.
			//
			// This works best with high fragment counts (18+) and wide scatter angles (60). The fragments form concentric
			// hexagon shells that get progressively more dense with increasing fragment count. Be careful about creating
			// huge numbers of projectiles: this mod is not optimized in any way.

			const int shellFragments = 6;

			if (totalFragments < 6)
			{
				return CrossFanout(facing, scatterAngle, n, totalFragments);
			}

			if (totalFragments < 8)
			{
				return UmbrellaFanout(facing, scatterAngle, n, totalFragments);
			}

			if (n == 0)
			{
				return facing;
			}

			// Take out center fragment;
			n -= 1;

			// We're using zero-based math here, so we have to do some fiddling with the total number of fragments.
			// Remove one fragment from the count for the center and then another to account for being zero-based.
			var shells = (totalFragments - 2) / shellFragments;
			var nshell = n / shellFragments;
			// Ensure that fragments in outer shell are evenly spaced when it's not fully populated.
			var fragmentsPerShell = nshell == shells ? (totalFragments - 2) % shellFragments + 1 : shellFragments;
			var roll = n * 360f / fragmentsPerShell;
			if (fragmentsPerShell != shellFragments)
			{
				// Partial outer shell, offset 12 o'clock so it isn't exactly in line with the inner shells.
				roll -= 360f / fragmentsPerShell * 2f / fragmentsPerShell;
			}
			var pitch = -scatterAngle / Mathf.Pow(Mathf.Sqrt(2), shells - nshell) / 2f;
			var orientation = Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.AngleAxis(pitch, Vector3.up);
			return Quaternion.LookRotation(facing) * orientation * Vector3.forward;
		}

		private static Vector3 HorizontalFanout(Vector3 facing, float scatterAngle, int n, int totalFragments) =>
			LinearFanout(
				facing,
				scatterAngle,
				// Horizontal rotation so use Y-axis.
				a => Quaternion.AngleAxis(a, Vector3.up),
				n,
				totalFragments);

		private static Vector3 VerticalFanout(Vector3 facing, float scatterAngle, int n, int totalFragments) =>
			LinearFanout(
				facing,
				scatterAngle,
				// Vertical rotation so use X-axis.
				a => Quaternion.AngleAxis(a, Vector3.right),
				n,
				totalFragments);

		private static Vector3 LinearFanout(
			Vector3 facing,
			float scatterAngle,
			Func<float, Quaternion> orientation,
			int n,
			int totalFragments)
		{
			var fanAngle = LinearFanAngle(scatterAngle, n, totalFragments);
			return Quaternion.LookRotation(facing) * orientation(fanAngle) * Vector3.forward;
		}

		private static float LinearFanAngle(float scatterAngle, int n, int totalFragments)
		{
			// XXX find a different spacing algorithm so spread looks better.

			if (n == 0)
			{
				if (totalFragments % 2 == 1)
				{
					return 0f;
				}
			}
			n += totalFragments % 2 == 0 ? 1 : 0;

			var even = n % 2 == 0;
			var n1 = n / 2 + 1 - (even ? 1 : 0);
			return (even ? 1 : -1) * scatterAngle / Mathf.Pow(Mathf.Sqrt(2), n1) / 2f;
		}

		private static Vector3 LeftDiagonalFanout(Vector3 facing, float scatterAngle, int n, int totalFragments) =>
			// Looking at the missile coming toward you, the fragments should form a line at a 45-degree angle to
			// the horizontal with the left side toward the top of the monitor and the right side toward the bottom.
			DiagonalFanout(facing, scatterAngle, 1, n, totalFragments);

		private static Vector3 RightDiagonalFanout(Vector3 facing, float scatterAngle, int n, int totalFragments) =>
			// Looking at the missile coming toward you, the fragments should form a line at a 45-degree angle to
			// the horizontal with the left side toward the bottom of the monitor and the right side toward the top.
			DiagonalFanout(facing, scatterAngle, -1, n, totalFragments);

		private static Vector3 CrossFanout(Vector3 facing, float scatterAngle, int n, int totalFragments) =>
			IntersectionFanout(
				facing,
				scatterAngle,
				RightDiagonalFanout,
				LeftDiagonalFanout,
				n,
				totalFragments);

		private static Vector3 IntersectionFanout(
			Vector3 facing,
			float scatterAngle,
			FanoutFunc dominantAxis,
			FanoutFunc recessiveAxis,
			int n,
			int totalFragments)
		{
			var odd = totalFragments % 2 == 1;
			if (odd)
			{
				if (n < 3)
				{
					return dominantAxis(facing, scatterAngle, n, totalFragments);
				}
				n -= 3;
				totalFragments -= 1;
			}

			var pair = n / 2;
			if (pair % 2 == 0)
			{
				return recessiveAxis(facing, scatterAngle, n, totalFragments);
			}
			n += odd ? 2 : 0;
			return dominantAxis(facing, scatterAngle, n, totalFragments);
		}

		private static Vector3 TeeFanout(Vector3 facing, float scatterAngle, int n, int totalFragments) =>
			IntersectionFanout(
				facing,
				scatterAngle,
				HorizontalFanout,
				VerticalFanout,
				n,
				totalFragments);
	}
}
