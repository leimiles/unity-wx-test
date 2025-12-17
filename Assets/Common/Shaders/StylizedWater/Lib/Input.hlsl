TEXTURE2D(_IntersectionNoise);
SAMPLER(sampler_IntersectionNoise);
TEXTURE2D(_DepthTex);

CBUFFER_START(UnityPerMaterial)
	float4 _ShallowColor;
	float4 _BaseColor;
	half _ColorAbsorption;

	//float _Smoothness;
	//float _Metallic;

	float4 _IntersectionColor;
	float _DepthVertical;
	float _DepthHorizontal;
	float _DepthExp;
	float _WorldSpaceUV;
	float _NormalTiling;
	float _NormalSubTiling;
	float _NormalSpeed;
	float _NormalSubSpeed;
	half _NormalStrength;

	half _EdgeFade;
	float _WaveSpeed;
	float4 _HorizonColor;
	half _HorizonDistance;
	half _SunReflectionDistortion;
	half _SunReflectionSize;
	float _SunReflectionStrength;
	float _PointSpotLightReflectionStrength;
	half _PointSpotLightReflectionSize;
	half _PointSpotLightReflectionDistortion;

	half _ShadowStrength;
	float2 _Direction;
	float _Speed;
	float _SparkleIntensity;
	half _SparkleSize;

	//Intersection
	half _IntersectionSource;
	half _IntersectionLength;
	half _IntersectionFalloff;
	half _IntersectionTiling;
	half _IntersectionRippleDist;
	half _IntersectionRippleStrength;
	half _IntersectionClipping;
	float _IntersectionSpeed;

	//Waves
	half _WaveHeight;
	half _WaveNormalStr;
	float _WaveDistance;
	half2 _WaveFadeDistance;
	float _WaveSteepness;
	uint _WaveCount;
	half4 _WaveDirection;

	half _ShoreLineWaveStr;
	half _ShoreLineWaveDistance;
	half _ShoreLineLength;

	//Underwater
	half _CausticsBrightness;
	float _CausticsTiling;
	half _CausticsSpeed;
	half _CausticsDistortion;

	half _VertexColorDepth;
	half _VertexColorWaveFlattening;
	half _VertexColorFoam;

	half _WaveTint;
CBUFFER_END