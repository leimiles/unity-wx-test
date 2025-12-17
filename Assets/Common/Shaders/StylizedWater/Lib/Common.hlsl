#ifndef DINO_WATER_COMMON_INCLUDED
#define DINO_WATER_COMMON_INCLUDED

float _CustomTime;
#define TIME_FRAG_INPUT _CustomTime > 0 ? _CustomTime : input.uv.z
#define TIME_VERTEX_OUTPUT _CustomTime > 0 ? _CustomTime : output.uv.z

#define TIME ((TIME_FRAG_INPUT * _Speed) * -_Direction.xy)
#define TIME_VERTEX ((TIME_VERTEX_OUTPUT * _Speed) * -_Direction.xy)

#define HORIZONTAL_DISPLACEMENT_SCALAR 0.01
#define UP_VECTOR float3(0,1,0)

struct WaterSurface
{
	uint vFace;
	float3 positionWS;
	float3 viewDelta;
	float3 viewDir;
	float3 vertexNormal;
	float3 waveNormal;	
	half3x3 tangentToWorldMatrix;
	float3 tangentNormal;
	float3 tangentWorldNormal;
	float3 diffuseNormal;
	float4 refractionOffset;
	
	float3 albedo;
	float3 reflections;
	float3 caustics;
	float3 specular;
	half reflectionMask;
	half reflectionLighting;
	
	float3 offset;
	float slope;
	
	float fog;
	float intersection;
	float foam;

	float alpha;
	float edgeFade;
	float shadowMask;
};

//#define PIXELIZE_UV

float2 GetSourceUV(float2 uv, float2 wPos, float state) 
{
	float2 output =  lerp(uv, wPos, state);

	//Pixelize
	#ifdef PIXELIZE_UV
	output.x = (int)((output.x / 0.5) + 0.5) * 0.5;
	output.y = (int)((output.y / 0.5) + 0.5) * 0.5;
	#endif
	
	return output;
}

float4 GetVertexColor(float4 inputColor, float4 mask)
{
	return inputColor * mask;
}

float DepthDistance(float3 wPos, float3 viewPos, float3 normal)
{
	return length((wPos - viewPos) * normal);
}

float4 PackedUV(float2 sourceUV, float2 time, float speed, float subTiling, float subSpeed)
{
	float2 uv1 = sourceUV.xy + (time.xy * speed);
	float2 uv2 = (sourceUV.xy * subTiling) + ((time.xy) * speed * subSpeed);
	
	return float4(uv1.xy, uv2.xy);
}

struct SurfaceNormalData
{
	float3 geometryNormalWS;
	float3 pixelNormalWS;
	float lightingStrength;
	float mask;
};

struct SceneDepth
{
	float raw;
	float linear01;
	float eye;
};

#define FAR_CLIP _ProjectionParams.z
#define NEAR_CLIP _ProjectionParams.y
//Scale linear values to the clipping planes for orthographic projection (unity_OrthoParams.w = 1 = orthographic)
#define DEPTH_SCALAR lerp(1.0, FAR_CLIP - NEAR_CLIP, unity_OrthoParams.w)

//Linear depth difference between scene and current (transparent) geometry pixel
float SurfaceDepth(SceneDepth depth, float4 positionCS)
{
	const float sceneDepth = (unity_OrthoParams.w == 0) ? depth.eye : LinearDepthToEyeDepth(depth.raw);
	const float clipSpaceDepth = (unity_OrthoParams.w == 0) ? LinearEyeDepth(positionCS.z, _ZBufferParams) : LinearDepthToEyeDepth(positionCS.z / positionCS.w);

	return sceneDepth - clipSpaceDepth;
}

//Return depth based on the used technique (buffer, vertex color, baked texture)
SceneDepth SampleDepth(float4 screenPos)
{
	SceneDepth depth = (SceneDepth)0;
	
#ifndef _DISABLE_DEPTH_TEX
	screenPos.xyz /= screenPos.w;

	depth.raw = SampleSceneDepth(screenPos.xy);
	depth.eye = LinearEyeDepth(depth.raw, _ZBufferParams); 
	depth.linear01 = Linear01Depth(depth.raw, _ZBufferParams) * DEPTH_SCALAR;
#else
	depth.raw = 1.0;
	depth.eye = 1.0;
	depth.linear01 = 1.0;
#endif

	return depth;
}

//Reconstruct world-space position from depth.
float3 ReconstructWorldPosition(float4 screenPos, float3 viewDir, SceneDepth sceneDepth)
{
	#if UNITY_REVERSED_Z
	real rawDepth = sceneDepth.raw;
	#else
	// Adjust z to match NDC for OpenGL
	real rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneDepth.raw);
	#endif
	//Projection to world position
	float3 camPos = GetCurrentViewPosition().xyz;
	float3 worldPos = sceneDepth.eye * (viewDir/screenPos.w) - camPos;
	float3 perspWorldPos = -worldPos;
	return perspWorldPos;
}
#endif