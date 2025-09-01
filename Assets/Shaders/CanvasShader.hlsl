// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Canvas Shader
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#ifndef WORLDUI_SHADER_H
#define WORLDUI_SHADER_H

// Constants

struct CanvasDrawData {
	float3 Position;
	float2 Scale;
	float2 Pivot;
	float2 Tiling;
	float2 Offset;
	float3 Center;
	float4 BaseColor;
	float4 MaskColor;
};



// Fields

StructuredBuffer<CanvasDrawData> _CanvasDrawData;



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
	CanvasDrawData data = _CanvasDrawData[unity_InstanceID];
	unity_ObjectToWorld = CreateTransform(data.Position, float4(0, 0, 0, 1), float3(1, 1, 1));
	unity_WorldToObject = InvertTransform(unity_ObjectToWorld);
	#endif
}

void Passthrough_float(
	in float2 In_Scale,
	in float2 In_Pivot,
	in float2 In_Tiling,
	in float2 In_Offset,
	in float3 In_Center,
	in float4 In_BaseColor,
	in float4 In_MaskColor,

	out float2 Out_Scale,
	out float2 Out_Pivot,
	out float2 Out_Tiling,
	out float2 Out_Offset,
	out float3 Out_Center,
	out float4 Out_BaseColor,
	out float4 Out_MaskColor) {

	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	CanvasDrawData data = _CanvasDrawData[unity_InstanceID];
	Out_Scale     = data.Scale;
	Out_Pivot     = data.Pivot;
	Out_Tiling    = data.Tiling;
	Out_Offset    = data.Offset;
	Out_Center    = data.Center;
	Out_BaseColor = data.BaseColor;
	Out_MaskColor = data.MaskColor;
	#else
	Out_Scale     = In_Scale;
	Out_Pivot     = In_Pivot;
	Out_Tiling    = In_Tiling;
	Out_Offset    = In_Offset;
	Out_Center    = In_Center;
	Out_BaseColor = In_BaseColor;
	Out_MaskColor = In_MaskColor;
	#endif
}

#endif
