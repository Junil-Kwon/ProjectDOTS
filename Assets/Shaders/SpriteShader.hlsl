
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sprite Shader
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#ifndef SPRITE_SHADER_H
#define SPRITE_SHADER_H

	// Definition

	struct SpriteDrawData {
		float3 position;
		float2 scale;
		float2 pivot;

		float2 tiling;
		float2 offset;
		uint   basecolor;
		uint   maskcolor;
		uint   emission;
	};



	// Fields

	StructuredBuffer<SpriteDrawData> _SpriteDrawData;



	// Methods

	inline float4x4 TRS(float3 position, float2 scale, float2 pivot, float4x4 camera) {
		float3 right   = float3(camera[0][0], camera[1][0], camera[2][0]);
		float3 up      = float3(camera[0][1], camera[1][1], camera[2][1]);
		float3 forward = float3(camera[0][2], camera[1][2], camera[2][2]);
	
		float4x4 m = 0.0f;
		m[0][0] = right.x * scale.x;
		m[1][0] = right.y * scale.x;
		m[2][0] = right.z * scale.x;
		m[3][0] = 0.0f;
		m[0][1] = up.x * scale.y;
		m[1][1] = up.y * scale.y;
		m[2][1] = up.z * scale.y;
		m[3][1] = 0.0f;
		m[0][2] = forward.x;
		m[1][2] = forward.y;
		m[2][2] = forward.z;
		m[3][2] = 0.0f;
		m[0][3] = position.x + pivot.x * right.x + pivot.y * up.x;
		m[1][3] = position.y + pivot.x * right.y + pivot.y * up.y;
		m[2][3] = position.z + pivot.x * right.z + pivot.y * up.z;
		m[3][3] = 1.0f;
		return m;
	}

	inline float4x4 InverseAffineTransform(float4x4 m) {
		float3x3 rotation = float3x3(
			m[1].yzx * m[2].zxy - m[1].zxy * m[2].yzx,
			m[0].zxy * m[2].yzx - m[0].yzx * m[2].zxy,
			m[0].yzx * m[1].zxy - m[0].zxy * m[1].yzx);
		float det = dot(m[0].xyz, rotation[0]);
		rotation = transpose(rotation);
		rotation *= rcp(det);
		float3 position = mul(rotation, -m._14_24_34);

		m._11_21_31_41 = float4(rotation._11_21_31, 0.0f);
		m._12_22_32_42 = float4(rotation._12_22_32, 0.0f);
		m._13_23_33_43 = float4(rotation._13_23_33, 0.0f);
		m._14_24_34_44 = float4(position,           1.0f);

		return m;
	}



	// Lifecycle

	void Setup() {
		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			SpriteDrawData data = _SpriteDrawData[unity_InstanceID];
			unity_ObjectToWorld = TRS(data.position, data.scale, data.pivot, unity_CameraToWorld);
			unity_WorldToObject = InverseAffineTransform(unity_ObjectToWorld);
		#endif
	}

	void Passthrough_float(
		in float3 In,
		in float2 In_Tiling,
		in float2 In_Offset,
		in float4 In_BaseColor,
		in float4 In_MaskColor,
		in float4 In_Emission,

		out float3 Out,
		out float2 Out_Tiling,
		out float2 Out_Offset,
		out float4 Out_BaseColor,
		out float4 Out_MaskColor,
		out float4 Out_Emission) {

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			SpriteDrawData data = _SpriteDrawData[unity_InstanceID];
			Out           = In;
			Out_Tiling    = data.tiling;
			Out_Offset    = data.offset;
			Out_BaseColor = float4(
				((data.basecolor & 0xFF000000u) >> 24) * 0.00392157f,
				((data.basecolor & 0x00FF0000u) >> 16) * 0.00392157f,
				((data.basecolor & 0x0000FF00u) >>  8) * 0.00392157f,
				((data.basecolor & 0x000000FFu) >>  0) * 0.00392157f);
			Out_MaskColor = float4(
				((data.maskcolor & 0xFF000000u) >> 24) * 0.00392157f,
				((data.maskcolor & 0x00FF0000u) >> 16) * 0.00392157f,
				((data.maskcolor & 0x0000FF00u) >>  8) * 0.00392157f,
				((data.maskcolor & 0x000000FFu) >>  0) * 0.00392157f);
			Out_Emission  = float4(
				((data.emission  & 0xFF000000u) >> 24) * 0.00392157f,
				((data.emission  & 0x00FF0000u) >> 16) * 0.00392157f,
				((data.emission  & 0x0000FF00u) >>  8) * 0.00392157f,
				((data.emission  & 0x000000FFu) >>  0) * 0.00392157f);
		#else
			Out           = In;
			Out_Tiling    = In_Tiling;
			Out_Offset    = In_Offset;
			Out_BaseColor = In_BaseColor;
			Out_MaskColor = In_MaskColor;
			Out_Emission  = In_Emission;
		#endif
	}

#endif
