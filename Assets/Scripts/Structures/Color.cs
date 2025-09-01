using UnityEngine;
using System;

using Unity.Mathematics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Color
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

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

	public color(Color color) {
		data = 0u;
		r = color.r;
		g = color.g;
		b = color.b;
		a = color.a;
	}

	public color(float r, float g, float b, float a = 1f) {
		data = 0u;
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}

	public color(uint hexcode) {
		data = (hexcode << 8) | 0xFF;
	}



	// Operators

	public static implicit operator color(Color color) {
		return new(color);
	}

	public static implicit operator Color(color color) {
		return new(color.r, color.g, color.b, color.a);
	}

	public static implicit operator color(Color32 color) {
		uint data = 0u;
		data |= (uint)(color.r << RShift);
		data |= (uint)(color.g << GShift);
		data |= (uint)(color.b << BShift);
		data |= (uint)(color.a << AShift);
		return new color { data = data };
	}

	public static implicit operator Color32(color color) {
		byte r = (byte)((color.data & RMask) >> RShift);
		byte g = (byte)((color.data & GMask) >> GShift);
		byte b = (byte)((color.data & BMask) >> BShift);
		byte a = (byte)((color.data & AMask) >> AShift);
		return new(r, g, b, a);
	}

	public static implicit operator color(float4 value) {
		return new color(value.x, value.y, value.z, value.w);
	}

	public static implicit operator float4(color value) {
		return new(value.r, value.g, value.b, value.a);
	}
}



public static class ColorExtensions {

	// Methods

	public static Color ToRGB(this Color hsv) {
		float h = hsv.r;
		float s = hsv.g;
		float v = hsv.b;
		float a = hsv.a;
		float c = v * s;
		float p = h % 360f / 60f;
		float x = c * (1f - math.abs(p % 2f - 1f));
		float m = v - c;
		Color rgb = new(0f, 0f, 0f, a);
		switch ((int)p) {
			case 0: rgb.r = c; rgb.g = x; rgb.b = 0; break;
			case 1: rgb.r = x; rgb.g = c; rgb.b = 0; break;
			case 2: rgb.r = 0; rgb.g = c; rgb.b = x; break;
			case 3: rgb.r = 0; rgb.g = x; rgb.b = c; break;
			case 4: rgb.r = x; rgb.g = 0; rgb.b = c; break;
			case 5: rgb.r = c; rgb.g = 0; rgb.b = x; break;
		}
		rgb.r += m;
		rgb.g += m;
		rgb.b += m;
		return rgb;
	}

	public static Color ToHSV(this Color rgb) {
		float max = math.max(math.max(rgb.r, rgb.g), rgb.b);
		float min = math.min(math.min(rgb.r, rgb.g), rgb.b);
		float delta = max - min;
		float h = 0f;
		float s = 0f;
		float v = max;
		float a = rgb.a;
		if (max != 0f) s = delta / max;
		if (delta != 0f) {
			switch (max) {
				case float n when n == rgb.r: h = (rgb.g - rgb.b) / delta + 0f; break;
				case float n when n == rgb.g: h = (rgb.b - rgb.r) / delta + 2f; break;
				case float n when n == rgb.b: h = (rgb.r - rgb.g) / delta + 4f; break;
			}
			h *= 60f;
			if (h < 0f) h += 360f;
		}
		return new(h, s, v, a);
	}
}
