using System;

using Unity.Mathematics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Constraints
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Serializable]
public struct constraints {

	// Constants

	const int PositionXMask = 0b000001;
	const int PositionYMask = 0b000010;
	const int PositionZMask = 0b000100;
	const int RotationXMask = 0b001000;
	const int RotationYMask = 0b010000;
	const int RotationZMask = 0b100000;



	// Fields

	public byte data;



	// Properties

	public bool positionX {
		get => (data & PositionXMask) != 0;
		set => data = value ? (byte)(data | PositionXMask) : (byte)(data & ~PositionXMask);
	}
	public bool positionY {
		get => (data & PositionYMask) != 0;
		set => data = value ? (byte)(data | PositionYMask) : (byte)(data & ~PositionYMask);
	}
	public bool positionZ {
		get => (data & PositionZMask) != 0;
		set => data = value ? (byte)(data | PositionZMask) : (byte)(data & ~PositionZMask);
	}
	public bool rotationX {
		get => (data & RotationXMask) != 0;
		set => data = value ? (byte)(data | RotationXMask) : (byte)(data & ~RotationXMask);
	}
	public bool rotationY {
		get => (data & RotationYMask) != 0;
		set => data = value ? (byte)(data | RotationYMask) : (byte)(data & ~RotationYMask);
	}
	public bool rotationZ {
		get => (data & RotationZMask) != 0;
		set => data = value ? (byte)(data | RotationZMask) : (byte)(data & ~RotationZMask);
	}

	public bool3 position {
		get => new(positionX, positionY, positionZ);
		set => (positionX, positionY, positionZ) = (value.x, value.y, value.z);
	}
	public bool3 rotation {
		get => new(rotationX, rotationY, rotationZ);
		set => (rotationX, rotationY, rotationZ) = (value.x, value.y, value.z);
	}



	// Constructors

	public constraints(bool3 position, bool3 rotation) {
		data = default;
		this.position = position;
		this.rotation = rotation;
	}
}
