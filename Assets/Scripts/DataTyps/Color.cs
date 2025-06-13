using UnityEngine;
using System;

using Unity.Mathematics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Color
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Serializable]
public struct color {

	// Constants

	const uint RMask = 0xFF000000u;
	const uint GMask = 0x00FF0000u;
	const uint BMask = 0x0000FF00u;
	const uint AMask = 0x000000FFu;

	const int RShift = 24;
	const int GShift = 16;
	const int BShift = 08;
	const int AShift = 00;

	public static readonly color white = new(Color.white);
	public static readonly color black = new(Color.black);
	public static readonly color red = new(Color.red);
	public static readonly color green = new(Color.green);
	public static readonly color blue = new(Color.blue);
	public static readonly color cyan = new(Color.cyan);
	public static readonly color magenta = new(Color.magenta);
	public static readonly color yellow = new(Color.yellow);
	public static readonly color clear = new(Color.clear);



	// Fields

	public uint data;



	// Properties

	public float r {
		get => ((data & RMask) >> RShift) * 0.00392157f;
		set => data = (data & ~RMask) | ((uint)(math.saturate(value) * 255f) << RShift);
	}
	public float g {
		get => ((data & GMask) >> GShift) * 0.00392157f;
		set => data = (data & ~GMask) | ((uint)(math.saturate(value) * 255f) << GShift);
	}
	public float b {
		get => ((data & BMask) >> BShift) * 0.00392157f;
		set => data = (data & ~BMask) | ((uint)(math.saturate(value) * 255f) << BShift);
	}
	public float a {
		get => ((data & AMask) >> AShift) * 0.00392157f;
		set => data = (data & ~AMask) | ((uint)(math.saturate(value) * 255f) << AShift);
	}



	// Constructors

	public color(uint data) => this.data = (data << 8) | 0xFF;

	public color(float value) {
		data = 0u;
		r = value;
		g = value;
		b = value;
		a = 1f;
	}

	public color(float r, float g, float b, float a = 1f) {
		data = 0u;
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}
	public color(Color color) {
		data = 0u;
		r = color.r;
		g = color.g;
		b = color.b;
		a = color.a;
	}



	// Operators

	public static implicit operator color(Color color) {
		return new(color);
	}
	public static implicit operator Color(color color) {
		return new(color.r, color.g, color.b, color.a);
	}
	public static implicit operator color(Color32 color) {
		float r = color.r * 0.00392157f;
		float g = color.g * 0.00392157f;
		float b = color.b * 0.00392157f;
		float a = color.a * 0.00392157f;
		return new(r, g, b, a);
	}
	public static implicit operator Color32(color color) {
		byte r = (byte)(color.r * 255f);
		byte g = (byte)(color.g * 255f);
		byte b = (byte)(color.b * 255f);
		byte a = (byte)(color.a * 255f);
		return new(r, g, b, a);
	}



	// Methods

	public static color HSVtoRGB(float h, float s, float v, float a = 1f) {
		float c = v * s;
		float p = h % 360f * 0.0166667f;
		float x = c * (1f - math.abs(p % 2f - 1f));
		float m = v - c;
		color color = new(0f, 0f, 0f, a);
		switch (p) {
			case < 1f: color.r = c; color.g = x; color.b = 0; break;
			case < 2f: color.r = x; color.g = c; color.b = 0; break;
			case < 3f: color.r = 0; color.g = c; color.b = x; break;
			case < 4f: color.r = 0; color.g = x; color.b = c; break;
			case < 5f: color.r = x; color.g = 0; color.b = c; break;
			default: color.r = c; color.g = 0; color.b = x; break;
		}
		color.r += m;
		color.g += m;
		color.b += m;
		return color;
	}

	public static (float, float, float, float) RGBtoHSV(color color) {
		float max = math.max(math.max(color.r, color.g), color.b);
		float min = math.min(math.min(color.r, color.g), color.b);
		float delta = max - min;
		float h = 0f;
		float s = 0f;
		float v = max;
		float a = color.a;
		if (max != 0f) s = delta / max;
		if (delta != 0f) {
			if (color.r == max) h = (color.g - color.b) / delta + 0f;
			if (color.g == max) h = (color.b - color.r) / delta + 2f;
			if (color.b == max) h = (color.r - color.g) / delta + 4f;
			h *= 60f;
			if (h < 0f) h += 360f;
		}
		return (h, s, v, a);
	}
}
