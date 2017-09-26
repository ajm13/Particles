Shader "Custom/Particle"
{
	Properties
	{
		_Sprite("Sprite", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Overlay+100" "RenderType" = "Transparent" }

		LOD 100
		Blend SrcAlpha One

		Cull off
		ZWrite off

		Pass
		{

			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#pragma multi_compile_fog
			
      #include "UnityCG.cginc"
      #include "Particle.cginc"

			sampler2D _Sprite;
			float2 _size = float2(1,1);
			float3 _worldPos;
			float4x4 _worldRot;

			//The buffer containing the points we want to draw.
			StructuredBuffer<Particle> particles;

			struct input
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 col : COLOR;
				UNITY_FOG_COORDS(1)
			};

			input vert(uint id : SV_VertexID)
			{
				input o = (input) 0;

				float3 pos = mul(particles[id].pos, _worldRot);

				o.pos = float4(pos + _worldPos, 1.0f);
				o.col = particles[id].col;
				return o;
			}

			float4 RotPoint(float4 p, float3 offset, float3 sideVector, float3 upVector)
			{
				float3 finalPos = p.xyz;

				finalPos += offset.x * sideVector;
				finalPos += offset.y * upVector;

				return float4(finalPos,1);
			}

			[maxvertexcount(4)]
			void geom(point input p[1], inout TriangleStream<input> triStream)
			{
				float4 v[4];

				float3 up = float3(0, 1, 0);
				float3 look = _WorldSpaceCameraPos - p[0].pos.xyz;

				look = normalize(look);
				float3 right = normalize(cross(look, up));
				up = normalize(cross(right, look));

				v[0] = RotPoint(p[0].pos, float3(-_size.x, -_size.y, 0), right, up);
				v[1] = RotPoint(p[0].pos, float3(-_size.x, _size.y, 0), right, up);
				v[2] = RotPoint(p[0].pos, float3(_size.x, -_size.y, 0), right, up);
				v[3] = RotPoint(p[0].pos, float3(_size.x, _size.y, 0), right, up);
		
				input pIn;
				pIn.col = p[0].col;

				pIn.pos = mul(UNITY_MATRIX_VP, v[0]);
				pIn.uv = float2(0.0f, 0.0f);
				UNITY_TRANSFER_FOG(pIn, pIn.pos);
				triStream.Append(pIn);

				pIn.pos = mul(UNITY_MATRIX_VP, v[1]);
				pIn.uv = float2(0.0f, 1.0f);
				UNITY_TRANSFER_FOG(pIn, pIn.pos);
				triStream.Append(pIn);

				pIn.pos = mul(UNITY_MATRIX_VP, v[2]);
				pIn.uv = float2(1.0f, 0.0f);
				UNITY_TRANSFER_FOG(pIn, pIn.pos);
				triStream.Append(pIn);

				pIn.pos = mul(UNITY_MATRIX_VP, v[3]);
				pIn.uv = float2(1.0f, 1.0f);
				UNITY_TRANSFER_FOG(pIn, pIn.pos);
				triStream.Append(pIn);
			}

			float4 frag(input i) : COLOR
			{
				fixed4 col = tex2D(_Sprite, i.uv) * float4(i.col, 1.0f);
				clip(col.a - 0.0001f);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}

			ENDCG
		}
	}
	Fallback Off
}