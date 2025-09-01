// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Opaque Shader
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#ifndef TERRAIN_SHADER_H
#define TERRAIN_SHADER_H

// Constants

struct OpaqueDrawData {
	float3 Position;
	float4 Rotation;
	float2 Tiling;
	float2 Offset;
};



// Fields

StructuredBuffer<OpaqueDrawData> _OpaqueDrawData;



// Methods

inline float4x4 CreateTransform(float3 position, float4 rotation, float3 scale) {
	float4x4 m;
	m[0][0] = (1 - 2 * (rotation.y * rotation.y + rotation.z * rotation.z)) * scale.x;
	m[1][0] = (0 + 2 * (rotation.x * rotation.y + rotation.z * rotation.w)) * scale.x;
	m[2][0] = (0 + 2 * (rotation.x * rotation.z - rotation.y * rotation.w)) * scale.x;
	m[3][0] = 0;
	m[0][1] = (0 + 2 * (rotation.x * rotation.y - rotation.z * rotation.w)) * scale.y;
	m[1][1] = (1 - 2 * (rotation.x * rotation.x + rotation.z * rotation.z)) * scale.y;
	m[2][1] = (0 + 2 * (rotation.y * rotation.z + rotation.x * rotation.w)) * scale.y;
	m[3][1] = 0;
	m[0][2] = (0 + 2 * (rotation.x * rotation.z + rotation.y * rotation.w)) * scale.z;
	m[1][2] = (0 + 2 * (rotation.y * rotation.z - rotation.x * rotation.w)) * scale.z;
	m[2][2] = (1 - 2 * (rotation.x * rotation.x + rotation.y * rotation.y)) * scale.z;
	m[3][2] = 0;
	m[0][3] = position.x;
	m[1][3] = position.y;
	m[2][3] = position.z;
	m[3][3] = 1;
	return m;
}

inline float4x4 InvertTransform(float4x4 m) {
	float3x3 rotation;
	rotation[0] = m[1].yzx * m[2].zxy - m[1].zxy * m[2].yzx;
	rotation[1] = m[0].zxy * m[2].yzx - m[0].yzx * m[2].zxy;
	rotation[2] = m[0].yzx * m[1].zxy - m[0].zxy * m[1].yzx;
	rotation = transpose(rotation) * rcp(dot(m[0].xyz, rotation[0]));
	float3 position = mul(rotation, -m._14_24_34);

	m._11_21_31_41 = float4(rotation._11_21_31, 0);
	m._12_22_32_42 = float4(rotation._12_22_32, 0);
	m._13_23_33_43 = float4(rotation._13_23_33, 0);
	m._14_24_34_44 = float4(position, 1);
	return m;
}

void Setup() {
	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	OpaqueDrawData data = _OpaqueDrawData[unity_InstanceID];
	unity_ObjectToWorld = CreateTransform(data.Position, data.Rotation, float3(1, 1, 1));
	unity_WorldToObject = InvertTransform(unity_ObjectToWorld);
	#endif
}

void Passthrough_float(
	in float3 In_Position,
	in float2 In_Tiling,
	in float2 In_Offset,

	out float3 Out_Position,
	out float2 Out_Tiling,
	out float2 Out_Offset) {

	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	OpaqueDrawData data = _OpaqueDrawData[unity_InstanceID];
	Out_Position = In_Position;
	Out_Tiling   = data.Tiling;
	Out_Offset   = data.Offset;
	#else
	Out_Position = In_Position;
	Out_Tiling   = In_Tiling;
	Out_Offset   = In_Offset;
	#endif
}

#endif
