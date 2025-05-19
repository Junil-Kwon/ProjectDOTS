using UnityEngine;
using System;

using Unity.Mathematics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Constraints
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Serializable]
public struct constraints {

	// Constants

	const int PositionXMask = 0x01;
	const int PositionYMask = 0x02;
	const int PositionZMask = 0x04;
	const int RotationXMask = 0x08;
	const int RotationYMask = 0x10;
	const int RotationZMask = 0x20;



	// Fields

	[SerializeField] byte data;



	// Properties

	public bool positionX {
		get => (data & PositionXMask) != 0;
		set => data = (byte)((data & ~PositionXMask) | (value ? PositionXMask : 0));
	}
	public bool positionY {
		get => (data & PositionYMask) != 0;
		set => data = (byte)((data & ~PositionYMask) | (value ? PositionYMask : 0));
	}
	public bool positionZ {
		get => (data & PositionZMask) != 0;
		set => data = (byte)((data & ~PositionZMask) | (value ? PositionZMask : 0));
	}
	public bool rotationX {
		get => (data & RotationXMask) != 0;
		set => data = (byte)((data & ~RotationXMask) | (value ? RotationXMask : 0));
	}
	public bool rotationY {
		get => (data & RotationYMask) != 0;
		set => data = (byte)((data & ~RotationYMask) | (value ? RotationYMask : 0));
	}
	public bool rotationZ {
		get => (data & RotationZMask) != 0;
		set => data = (byte)((data & ~RotationZMask) | (value ? RotationZMask : 0));
	}

	public bool3 position {
		get {
			bool x = (data & PositionXMask) != 0;
			bool y = (data & PositionYMask) != 0;
			bool z = (data & PositionZMask) != 0;
			return new(x, y, z);
		}
		set {
			int i = data & ~(PositionXMask | PositionYMask | PositionZMask);
			i |= value.x ? PositionXMask : 0;
			i |= value.y ? PositionYMask : 0;
			i |= value.z ? PositionZMask : 0;
			data = (byte)i;
		}
	}
	public bool3 rotation {
		get {
			bool x = (data & RotationXMask) != 0;
			bool y = (data & RotationYMask) != 0;
			bool z = (data & RotationZMask) != 0;
			return new(x, y, z);
		}
		set {
			int i = data & ~(RotationXMask | RotationYMask | RotationZMask);
			i |= value.x ? RotationXMask : 0;
			i |= value.y ? RotationYMask : 0;
			i |= value.z ? RotationZMask : 0;
			data = (byte)i;
		}
	}



	// Constructors

	public constraints(bool3 position, bool3 rotation) {
		data = 0;
		this.position = position;
		this.rotation = rotation;
	}
}
