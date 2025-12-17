#define COLLAPSIBLE_GROUP 1
#define PLANAR_REFLECTION_DISTORTION_MULTIPLIER 0.25

struct SceneData
{
	float4 positionSS;
	float2 screenPos; 
	float3 positionWS;
	float3 color;
	
	float viewDepth;
	float verticalDepth;
	float skyMask;

	//More easy debugging
	half refractionMask;
};

void PopulateSceneData(inout SceneData scene, Varyings input, WaterSurface water, float4 shadowCoords)
{	
	scene.positionSS = input.screenPos;
	scene.screenPos = scene.positionSS.xy / scene.positionSS.w;
	
	scene.viewDepth = 1;
	scene.verticalDepth = 1;

	scene.refractionMask = 1.0;
	#if !_DISABLE_DEPTH_TEX
	SceneDepth depth = SampleDepth(scene.positionSS);
	scene.positionWS = ReconstructWorldPosition(scene.positionSS, water.viewDelta, depth);
	
	//Invert normal when viewing backfaces
	float normalSign = ceil(dot(water.viewDir, water.waveNormal));
	normalSign = normalSign == 0 ? -1 : 1;
	
	scene.viewDepth = SurfaceDepth(depth, input.positionCS);
	scene.verticalDepth = DepthDistance(water.positionWS, scene.positionWS, water.waveNormal * normalSign);
	#endif

	//Skybox mask is used for backface (underwater) reflections, to blend between refraction and reflection probes
	scene.skyMask = 0;
	#ifdef DEPTH_MASK
		#if !_DISABLE_DEPTH_TEX
		float depthSource = depth.linear01;
		scene.skyMask = depthSource > 0.99 ? 1 : 0;
		#endif
	#endif
}

float GetWaterDensity(SceneData scene, float mask, float heightScalar, float viewDepthScalar, bool exponential)
{
	float density = 1.0;
	#if !_DISABLE_DEPTH_TEX
	float viewDepth = scene.viewDepth;
	float verticalDepth = scene.verticalDepth;

	float depthAttenuation = 1.0 - exp(-viewDepth * viewDepthScalar * 0.1);
	float heightAttenuation = saturate(lerp(verticalDepth * heightScalar, 1.0 - exp(-verticalDepth * heightScalar), exponential));
	
	density = max(depthAttenuation, heightAttenuation);
	#endif
	
	//Use green vertex color channel to subtract density
	density = saturate(density - mask);
	return density;
}

#define FRONT_FACE_SEMANTIC_REAL FRONT_FACE_SEMANTIC
float4 ForwardPassFragment(Varyings input, FRONT_FACE_TYPE vertexFace : FRONT_FACE_SEMANTIC_REAL) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
	InputData inputData = (InputData)0;
	SurfaceData surfaceData = (SurfaceData)0; 
	WaterSurface water = (WaterSurface)0;
	SceneData scene = (SceneData)0;

	water.alpha = 1.0;
	// water.vFace = IS_FRONT_VFACE(vertexFace, true, false); //0 = back face
	water.vFace = 1.0; // some devices don't support
	int faceSign = water.vFace > 0 ? 1 : -1;
	
	/* ========
	// GEOMETRY DATA
	=========== */
	#if COLLAPSIBLE_GROUP

	float4 vertexColor = input.color;
	float3 normalWS = normalize(input.normalWS.xyz);
#ifdef _NORMALMAP
	float3 WorldTangent = input.tangent.xyz;
	float3 WorldBiTangent = input.bitangent.xyz;
	float3 positionWS = float3(input.normalWS.w, input.tangent.w, input.bitangent.w);
#else
	float3 positionWS = input.positionWS;
#endif
	water.positionWS = positionWS;
	water.viewDelta = GetCurrentViewPosition() - positionWS;
	water.viewDir = normalize(water.viewDelta);
	half VdotN = 1.0 - saturate(dot(water.viewDir * faceSign, normalWS));
	
	water.vertexNormal = normalWS;
	//Returns mesh or world-space UV
	float2 uv = GetSourceUV(input.uv.xy, positionWS.xz, _WorldSpaceUV);;
	#endif

	/* ========
	// WAVES
	=========== */
	#if COLLAPSIBLE_GROUP
	water.waveNormal = normalWS;
#if _WAVES
	WaveInfo waves = GetWaveInfo(uv, TIME * _WaveSpeed, _WaveHeight,  lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);
	waves.normal = lerp(waves.normal, normalWS, lerp(0, 1, vertexColor.b));
	water.waveNormal = waves.normal;
	water.offset.y += waves.position.y;
	water.offset.xz += waves.position.xz * 0.5;
#endif
	#endif

	#if _WAVES
	if(_WorldSpaceUV == 1) uv = GetSourceUV(input.uv.xy, positionWS.xz + water.offset.xz, _WorldSpaceUV);
	#endif
	
	/* ========
	// SHADOWS
	=========== */
	#if COLLAPSIBLE_GROUP
	water.shadowMask = 1.0;
	float4 shadowCoords = float4(0, 0, 0, 0);
	#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	shadowCoords = TransformWorldToShadowCoord(water.positionWS);
	#endif
	half4 shadowMask = 1.0;
	shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
	Light mainLight = GetMainLight(shadowCoords, water.positionWS, shadowMask);
	
	#if _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
	#endif
	{
		water.shadowMask = mainLight.shadowAttenuation;
	}
	
	half backfaceShadows = 1;
	#endif

	/* ========
	// NORMALS
	=========== */
	#if COLLAPSIBLE_GROUP
	water.tangentNormal = float3(0.5, 0.5, 1);
	water.tangentWorldNormal = water.waveNormal;
	
#if _NORMALMAP
	//Tangent-space
	water.tangentNormal = SampleNormals(uv, _NormalTiling, _NormalSubTiling, positionWS, TIME, _NormalSpeed, _NormalSubSpeed, water.slope, water.vFace);
	water.tangentToWorldMatrix = half3x3(WorldTangent, WorldBiTangent, water.waveNormal);
	//World-space
	water.tangentWorldNormal = normalize(TransformTangentToWorld(water.tangentNormal, water.tangentToWorldMatrix));	
#endif
	#endif
	PopulateSceneData(scene, input, water, shadowCoords);
	
	/* =========
	// COLOR + FOG
	============ */
	#if COLLAPSIBLE_GROUP
	water.fog = GetWaterDensity(scene, vertexColor.g, _DepthHorizontal, _DepthVertical, _DepthExp);
	//Albedo
	float4 baseColor = lerp(_ShallowColor, _BaseColor, water.fog);
	baseColor.rgb += saturate(_WaveTint * water.offset.y);

	water.fog *= baseColor.a;
	water.alpha = baseColor.a;
	water.albedo.rgb = baseColor.rgb;	
	#endif

	/* ========
	// INTERSECTION FOAM
	=========== */
	#if COLLAPSIBLE_GROUP

	water.intersection = 0;
#ifdef _INTERSECTION_ON

	float interSecGradient = 0;
	
	#if !_DISABLE_DEPTH_TEX
	interSecGradient = 1-saturate(exp(scene.verticalDepth) / _IntersectionLength);	
	#endif
	
	if (_IntersectionSource == 1) interSecGradient = vertexColor.r;
	if (_IntersectionSource == 2) interSecGradient = saturate(interSecGradient + vertexColor.r);
	water.intersection = SampleIntersection(uv.xy, interSecGradient, TIME * _IntersectionSpeed) * _IntersectionColor.a;
	
	#if _WAVES
	if(positionWS.y < scene.positionWS.y) water.intersection = 0;
	#endif
	
	water.waveNormal = lerp(water.waveNormal, normalWS, water.intersection);
#endif

	#if _NORMALMAP
	water.tangentWorldNormal = lerp(water.tangentWorldNormal, water.vertexNormal, water.intersection);
	#endif
	#endif
	
	/* ========
	// EMISSION (Caustics + Specular)
	=========== */
	#if COLLAPSIBLE_GROUP
	#ifdef _CAUSTICS
	float2 causticsProjection = scene.positionWS.xz;
	#if _DISABLE_DEPTH_TEX
	causticsProjection = water.positionWS.xz;
	#endif
	water.caustics = SampleCaustics(causticsProjection + lerp(water.waveNormal.xz, water.tangentWorldNormal.xz, _CausticsDistortion), TIME * _CausticsSpeed, _CausticsTiling);
	// float causticsMask = saturate((1-water.fog) - water.intersection - water.foam - scene.skyMask) * water.vFace;
	float causticsMask = saturate((1-water.fog) - water.intersection - water.foam - scene.skyMask);
	water.caustics *= causticsMask * _CausticsBrightness;
	#endif

	#if _NORMALMAP
	if(_SparkleIntensity > 0)
	{
		//Can piggyback on the tangent normal
		half3 sparkles = mainLight.color * saturate(step(_SparkleSize, (water.tangentNormal.y))) * _SparkleIntensity;
	
		#if !_UNLIT
		//Fade out the effect as the sun approaches the horizon
		float sunAngle = saturate(dot(water.vertexNormal, mainLight.direction));
		float angleMask = saturate(sunAngle * 10); /* 1.0/0.10 = 10 */
		sparkles *= angleMask;
		#endif
		
		water.specular += sparkles.rgb;
	}
	#endif
	
#ifndef _SPECULARHIGHLIGHTS_OFF
	float3 lightReflectionNormal = water.tangentWorldNormal;
	half specularMask = saturate((1-water.foam * 2.0) * (1-water.intersection) * water.shadowMask);
	half3 sunSpec = SpecularReflection(mainLight, water.viewDir, water.waveNormal, lightReflectionNormal, _SunReflectionDistortion, lerp(8196, 64, _SunReflectionSize),
		/* Mask: */ _SunReflectionStrength * specularMask);
	water.specular += sunSpec;
#endif
	#endif

	/* ========
	// COMPOSITION
	=========== */
	#ifdef COLLAPSIBLE_GROUP

	#ifdef _INTERSECTION_ON
	water.albedo.rgb = lerp(water.albedo.rgb, _IntersectionColor.rgb, water.intersection);
	#endif

	#ifdef _INTERSECTION_ON
	water.alpha = saturate(water.alpha + water.intersection + water.foam);
	#endif

	#if !_UNLIT
	water.diffuseNormal = lerp(water.waveNormal, water.tangentWorldNormal, _NormalStrength);
	#endif
	
	float fresnel = saturate(pow(VdotN, _HorizonDistance)) * _HorizonColor.a;
	water.albedo.rgb = lerp(water.albedo.rgb, _HorizonColor.rgb, fresnel);
	
	//Final alpha
	water.edgeFade = saturate(scene.verticalDepth / (_EdgeFade * 0.01));
	water.alpha *= water.edgeFade;
	// return water.edgeFade;
	#endif
	
	/* ========
	// UNITY SURFACE & INPUT DATA
	=========== */
	#if COLLAPSIBLE_GROUP
	surfaceData.albedo = water.albedo.rgb;
	surfaceData.specular = water.specular.rgb;
	surfaceData.metallic = 0;
	surfaceData.smoothness = 0;
	surfaceData.normalTS = water.tangentNormal;
	surfaceData.emission = 0; //To be populated with translucency+caustics
	surfaceData.occlusion = 1.0;
	surfaceData.alpha = water.alpha;
	
	inputData.positionWS = positionWS;
	inputData.viewDirectionWS = water.viewDir;
	inputData.shadowCoord = shadowCoords;
	inputData.normalWS = water.tangentWorldNormal;
	// inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
	// inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, water.waveNormal);
	// inputData.shadowMask = shadowMask;
	#endif
	
	float4 finalColor = float4(ApplyLighting(surfaceData, scene.color, mainLight, inputData, water, _ShadowStrength, water.vFace), water.alpha);

	// half fogMask = 1.0;
	// finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);
	finalColor.a = water.alpha;
	
	return finalColor;
	
}