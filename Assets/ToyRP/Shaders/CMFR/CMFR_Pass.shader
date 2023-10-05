Shader "CMFR/CMFR_Pass"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_eyeX("_eyeX", float) = 0.5
		_eyeY("_eyeY", float) = 0.5
		_scaleRatio("_scaleRatio", float) = 2.0
		_fx("_fx", float) = 1.0
		_fy("_fy", float) = 1.0
		_iApplyLogMap1("_iApplyRFRMap1", int) = 1
		_SquelchedGridMappingBeta("_SquelchedGridMappingBeta", float ) = 0.0
		_MappingStrategy("_MappingStrategy" , int ) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Assets/ToyRP/Shaders/globaluniform.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			uniform float _iResolutionX;
			uniform float _iResolutionY;
			uniform float _eyeX;
			uniform float _eyeY;
			uniform float _scaleRatio;
			uniform float _fx;
			uniform float _fy;
			uniform int _iApplyRFRMap1;
			uniform float _SquelchedGridMappingBeta;
			uniform int _MappingStrategy;
			uniform int _DebugMode;

			fixed4 frag(v2f i) : SV_Target
			{
				// return tex2D( _GT1 , i.uv );
				// return tex2D( _MainTex , i.uv );

				if (_iApplyRFRMap1 < 0.5)
					return tex2D(_MainTex, i.uv);

				float2 cursorPos = float2(_eyeX, _eyeY); //(0,1)

				float maxDxPos = 1.0 - cursorPos.x;
				float maxDyPos = 1.0 - cursorPos.y;
				float maxDxNeg = cursorPos.x;
				float maxDyNeg = cursorPos.y;

				float norDxPos = _fx * maxDxPos / (_fx + maxDxPos);
				float norDyPos = _fy * maxDyPos / (_fy + maxDyPos);
				float norDxNeg = _fx * maxDxNeg / (_fx + maxDxNeg);
				float norDyNeg = _fy * maxDyNeg / (_fy + maxDyNeg);

				float2 tc = (i.uv - cursorPos); //i.uv.x > cursorPos.x : [0,maxDxPos] i.uv.x < cursorPos.x : [-maxDxNeg, 0]

				float x = tc.x > 0 ? tc.x / maxDxPos : tc.x / maxDxNeg;//[0,1], [-1,0]
				float y = tc.y > 0 ? tc.y / maxDyPos : tc.y / maxDyNeg; 
				if (tc.x >= 0) {
					x = x * norDxPos; //[0,norDxPos]
					x = _fx * x / (_fx - x); //[0, 1]
					x = x + cursorPos.x;
				}
				else {
					x = x * norDxNeg;
					x = _fx * x / (_fx + x);
					x = x + cursorPos.x;
				}

				if (tc.y >= 0) {
					y = y * norDyPos;
					y = _fy * y / (_fy - y);
					y = y + cursorPos.y;
				}
				else {
					y = y * norDyNeg;
					y = _fy * y / (_fy + y);
					y = y + cursorPos.y;
				}

				float2 pq = (float2(x, y)); //0,1 --> 0-1
				fixed4 col = tex2D(_MainTex, pq);


				float u,v;
				x = ( x * 2 ) - 1 ;
				y = ( y * 2 ) - 1 ;
				float xx = pow( x , 2 );
				float yy = pow( y , 2 );



				// elliplical Grid mapping
				// Spuare to Disc
				if( _MappingStrategy == 1 )
				{
					u = x * sqrt( 1 - yy / 2 );
					v = y * sqrt( 1 - xx / 2 );

				}

				//  Squelched Grid Open Mapping
				if( _MappingStrategy == 2 )
				{
					float b = 1.0;
					u = x * sqrt( ( 1 - b * yy ) / ( 1 - b * xx * yy ) );
					v = y * sqrt( ( 1 - b * xx ) / ( 1 - b * xx * yy ) );
					
				}


				// Blended E-Grid mapping
				if ( _MappingStrategy == 3 )
				{
	
					float beta = _SquelchedGridMappingBeta;
					float a = beta + 1 - beta * xx;	
					float b = beta + 1 - beta * yy;	
					u = x * sqrt( ( yy * b - a*b ) / ( xx*yy - a*b ) );
					v = y * sqrt( ( xx * b - a*b ) / ( xx*yy - a*b ) );
						
				}


				// FG Squiricle Mapping
				// Square to Disk
				if( _MappingStrategy == 4 )
				{
					float var1 = sqrt( xx + yy - xx * yy );
					float var2 = sqrt( xx + yy );
					u = x * var1 / var2 ;
					v = y * var1 / var2 ;
				}


				// 2-Squircular mapping
				// Square to Disk
				if( _MappingStrategy == 5 )
				{
					float var1 = sqrt( 1 + xx * yy );
					u = x / var1 ;
					v = y / var1 ;
				}



				// hyperbolic
				// Spuare to Disk
				if( _MappingStrategy == 8 )
				{
					float a = 0.8;
					float aa = a * a;
					float bb = aa  / ( 1 - aa );
					if( abs(x) > abs(y) )
					{
						u = sign(x) * sqrt( aa*xx + aa/bb*yy );
						v = y;
					}
					if( abs(y) >= abs(x) )
					{
						v = sign(y) * sqrt( aa*yy + aa/bb*xx );
						u = x;
					}
				}


				// Simple Strech
				// square to disk
				if(_MappingStrategy == 11 )
				{
					if( xx >= yy )
					{
						u = sign(x) * xx    / ( sqrt( xx + yy ));
						v = sign(x) * x * y / ( sqrt( xx + yy ));
					}
					else
					{
						u = sign(y) * x * y / ( sqrt( xx + yy ));
						v = sign(y) * yy    / ( sqrt( xx + yy ));
					}
				}


					
				if( _MappingStrategy > 0 )
				{
					u *= sqrt(2);
					v *= sqrt(2);
				}
				
				u = ( u + 1 ) / 2 ;
				v = ( v + 1 ) / 2 ;


				if( _DebugMode == 0 )
				{

					if( u < 0.0 || u > 1.0 ){ return fixed4(0,0,0,0); }	
					if( v < 0.0 || v > 1.0 ){ return fixed4(0,0,0,0); }
					
					u = max( 0 , u );
					u = min( 1 , u );
					v = max( 0 , v );
					v = min( 1 , v );
				}
				else if( _DebugMode == 1 )
				{
					if( u < 0.0 || u > 1.0 ){ return fixed4(1,1,1,1); }	
					if( v < 0.0 || v > 1.0 ){ return fixed4(1,1,1,1); }
					
				}

					
				if( _DebugMode == 0 )
				{
					if( _MappingStrategy > 0 )
					{
						return tex2D(_MainTex, fixed2( u , v ) );
					}
					else if (_MappingStrategy == 0 )
					{
						return col;
					}
				}
				else if( _DebugMode == 1 )
				{
					if( _MappingStrategy > 0 )
					{
						return fixed4( u , v , 0 , 1 );
					}
					else if( _MappingStrategy == 0 )
					{
						return fixed4( pq , 0 , 1 );
					}
				}

				return col;





				
			
			}
			ENDCG
		}
	}
}