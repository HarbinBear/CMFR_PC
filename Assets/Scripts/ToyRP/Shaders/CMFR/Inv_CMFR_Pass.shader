Shader "CMFR/Inv_CMFR_Pass"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MidTex("Texture", 2D) = "white" {}
		_NoiseTex("_NoiseTex" , 2D) = "white" {}
		_eyeX("_eyeX", float) = 0.5
		_eyeY("_eyeY", float) = 0.5
		_scaleRatio("_scaleRatio", float) = 2.0
		_fx("_fx", float) = 1.0
		_fy("_fy", float) = 1.0
		_iApplyLogMap2("_iApplyRFRMap2", int) = 1
		_SquelchedGridMappingBeta("_SquelchedGridMappingBeta", float ) = 0.0
		_MappingStrategy("_MappingStrategy" , int ) = 0
		
		_bSampleDensityWithNoise("_bSampleDensityWithNoise" , float ) = 0
		_DebugMode("_DebugMode", int ) = 0
		_validPercent("_validPercent" , float ) = 0.78 

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
			sampler2D _MidTex;
			sampler2D _NoiseTex;
			uniform float _eyeX;
			uniform float _eyeY;
			uniform float _scaleRatio;
			uniform float _kernel;
			uniform float _fx;
			uniform float _fy;
			uniform int _iApplyRFRMap2;
			uniform float _SquelchedGridMappingBeta;
			uniform int _MappingStrategy;
			uniform float _bSampleDensityWithNoise;
			uniform int _DebugMode;
			uniform int _OutputMode;
			uniform float _validPercent;

			fixed4 frag(v2f i) : SV_Target
			{

				if( _OutputMode == 3 || _OutputMode == 4  )
				{
					_DebugMode = 1 ;
				}
				else
				{
					_DebugMode = 0 ; 
				}
				
				if (_iApplyRFRMap2 < 0.5)
					return tex2D(_MidTex, i.uv);

				float2 cursorPos = float2(_eyeX, _eyeY); //0-1 -> -1,1 (0,0)
				float2 tc = (i.uv - cursorPos);
				


				float u,v;
				float2 tt = tc;
				tt /= sqrt(2);
				tt = tt * 2 ;      //  [ -1 , 1 ]
				float xx = pow( tt.x , 2 );
				float yy = pow( tt.y , 2 );



				
				// elliptical Grid Mapping
				// Disc to Square
				if( _MappingStrategy == 1 )
				{
					if(2 + xx - yy + 2 * sqrt(2) * tt.x < 0 ) return fixed4(0,1,0,1);
					if(2 + xx - yy - 2 * sqrt(2) * tt.x < 0 ) return fixed4(0,1,0,1);
					if(2 - xx + yy + 2 * sqrt(2) * tt.y < 0 ) return fixed4(0,1,0,1);
					if(2 - xx + yy - 2 * sqrt(2) * tt.y < 0 ) return fixed4(0,1,0,1);
						
					u = 0.5 * sqrt( 2 + xx - yy + 2 * sqrt(2) * tt.x ) - 0.5 * sqrt( 2 + xx - yy - 2 * sqrt(2) * tt.x ) ;
					v = 0.5 * sqrt( 2 - xx + yy + 2 * sqrt(2) * tt.y ) - 0.5 * sqrt( 2 - xx + yy - 2 * sqrt(2) * tt.y ) ;
					
				}

				// Squelched Grid Open Mapping
				// Disc to Square
				if(_MappingStrategy == 2 )
				{
					u = tt.x / sqrt( 1- yy ) ;
					v = tt.y / sqrt( 1- xx ) ;
				}


				// Blended E-Grid mapping
				if( _MappingStrategy == 3 )
				{

					float beta = _SquelchedGridMappingBeta;
					float ax = beta + 1 + beta*xx - yy ;
					float ay = beta + 1 + beta*yy - xx ;
					float c = 4 * beta * ( beta + 1 ) ;
					u = sign( tt.x ) / sqrt( 2 * beta ) * sqrt( ax - sqrt( ax*ax - c*xx )); 
					v = sign( tt.y ) / sqrt( 2 * beta ) * sqrt( ay - sqrt( ay*ay - c*yy )); 
						
				}


				// FG-Spuircular Mapping with & without S and K
				// Disc to Square Mapping
				if( _MappingStrategy == 4 )
				{

					float S = 1.0f;
					float SS = S * S;
					if( tt.x == 0 || tt.y == 0 ) discard;
						
					float temp = ( xx + yy ) * ( xx + yy - 4 * SS * xx * yy ) ;   // float temp = ( xx + yy ) * ( xx + yy - 4 * SS * xx * yy / ( xx + yy )) ;
					if( temp < 0 ) return fixed4(0,1,0,1);
					temp = sqrt( temp );
					temp = xx + yy - temp ;
					if( temp < 0 ) return fixed4(0,1,0,1);
					temp = sqrt( temp );
					temp = sign( tt.x * tt.y ) / S / sqrt(2) * temp ;     // temp = sign( x * y ) / S / sqrt(2) * temp * sqrt( xx + yy ) ;
						
					u = temp / tt.y;
					v = temp / tt.x;
					
				}


				// 2-Squircular mapping
				// Disc to Square mapping
				if( _MappingStrategy == 5 )
				{
					float var1 = sqrt( 1 - sqrt( 1 - 4 * xx * yy ) );
					var1 = var1 * sign( tt.x * tt.y ) / ( sqrt(2) );
					u = var1 / tt.y ;
					v = var1 / tt.x ;
				}
				

				// hyperbolic
				// Disc to Square
				if( _MappingStrategy == 8 )
				{
					float a = 0.5;
					float aa = a * a;

					float bb = aa  / ( 1 - aa );
					if( abs(tt.x) > abs(tt.y) )
					{
						u = sign(tt.x) * sqrt( xx/aa - yy/bb );
						v = tt.y;
					}
					if( abs(tt.y) >= abs(tt.x) )
					{
						v = sign(tt.y) * sqrt( yy/aa - xx/bb );
						u = tt.x;
					}
				}

				
				// cornerific tapered2 mapping
				// Disc to Square
				if( _MappingStrategy == 9 )
				{					
					float var0 = xx + yy ;
					float var1 = 2 - var0 ;
					float var2 = 4 * xx * yy * var1;
					var2 = var0 - var2 ;
					var2 = var0 * var2 ;
					var2 = sqrt(var2) ;
					var2 = var0 - var2 ;
					var2 /= 2 * var1 ;
					var2 = sqrt(var2) ;
					var2 *= sign( tt.x * tt.y );
					
					u = var2 / tt.y;
					v = var2 / tt.x;
				}

				
				// Non-axial 2-pinch mapping
				// Disc to Square
				if( _MappingStrategy == 10 )
				{
					
					float var0 = xx + yy ;
					float var1 = var0 - 4*xx*yy;
					var1 *= var0;
					var1 = pow( var1 , 0.5 );
					var1 = var0 - 2 * xx * yy - var1;
					var1 = pow( var1 , 0.25 );
					var1 *= sign( tt.x * tt.y ) / (  pow( 2 , 0.25 ) );
					
					u = var1 / tt.y ;
					v = var1 / tt.x ;
				}

				// Simple Strech
				// Disk To Square
				if( _MappingStrategy == 11  )
				{
					float var = sqrt( xx + yy );
					if( xx > yy )
					{
						u = sign( tt.x ) * var;
						v = sign( tt.x ) * var * tt.y / tt.x;
					}
					else
					{
						u = sign( tt.y ) * var * tt.x / tt.y;
						v = sign( tt.y ) * var;
					}
				} 
				

					
				// --------------------------
				
				if( _MappingStrategy > 0 )
				{
					u/=2;
					v/=2;

					tc = fixed2(u,v);  //  [ -0.5 , 0.5 ] 
					
				}

				// --------------------------




				float maxDxPos = 1.0 - cursorPos.x; // >= 0.5
				float maxDyPos = 1.0 - cursorPos.y;
				float maxDxNeg = cursorPos.x;
				float maxDyNeg = cursorPos.y;

				float norDxPos = _fx * maxDxPos / (_fx + maxDxPos);
				float norDyPos = _fy * maxDyPos / (_fy + maxDyPos);
				float norDxNeg = _fx * maxDxNeg / (_fx + maxDxNeg);
				float norDyNeg = _fy * maxDyNeg / (_fy + maxDyNeg);

				float x = 0.0;
				float y = 0.0;
				float2 pq = float2(0.0, 0.0);
				if (tc.x >= 0) {
					x = _fx * tc.x / (_fx + tc.x); //>0
					x = x / norDxPos;
					pq.x = x * maxDxPos + cursorPos.x;
				}
				else {
					x = _fx * tc.x / (_fx - tc.x); //<0
					x = x / norDxNeg;
					pq.x = x * maxDxNeg + cursorPos.x;
				}

				if (tc.y >= 0) {
					y = _fy * tc.y / (_fy + tc.y);
					y = y / norDyPos;
					pq.y = y * maxDyPos + cursorPos.y;
				}
				else {
					y = _fy * tc.y / (_fy - tc.y);
					y = y / norDyNeg;
					pq.y = y * maxDyNeg + cursorPos.y;
				}
					
				fixed4 col = tex2D(_MidTex, pq);


				if( _DebugMode == 1 )
				{
					
					float res = 800 / _scaleRatio ;
					float OriginRes = 800;
				
				
				
					pq *= res ;  // pos of RectMapping Tex   [ 0 , 800 / Scale ]   // pq = fixed2( col.x ,col.y );        /// value( pos in Original Tex ) in the RectMappingTex, not the pos of it.
				   
					// fixed2 pt1 = fixed2( floor(pq.x) / res , floor(pq.y) / res ) ;
					// fixed2 pt3 = fixed2( ceil(pq.x)  / res , floor(pq.y) / res ) ;   
					// fixed2 pt2 = fixed2( ceil(pq.x)  / res , ceil(pq.y)  / res ) ;
					// fixed2 pt4 = fixed2( floor(pq.x) / res , ceil(pq.y)  / res ) ;
					//
					//

					if( (pq.x - 0.5) / res < 0 || (pq.x + 0.5) / res > 1 ) discard;
					if( (pq.y - 0.5) / res < 0 || (pq.y + 0.5) / res > 1 ) discard;

					fixed2 pt1 = fixed2( (pq.x - 0.5) / res ,  (pq.y - 0.5) / res ) ;
					fixed2 pt3 = fixed2( (pq.x + 0.5)  / res , (pq.y - 0.5) / res ) ;   
					fixed2 pt2 = fixed2( (pq.x + 0.5)  / res , (pq.y + 0.5)  / res ) ;
					fixed2 pt4 = fixed2( (pq.x - 0.5) / res ,  (pq.y + 0.5)  / res ) ;

					

					
					//
					pt1 += ( tex2D( _NoiseTex , fixed2(x,y)).x - 0.5 ) * 0.01 * ( _bSampleDensityWithNoise ) ; 
					pt2 += ( tex2D( _NoiseTex , fixed2(x,y)).x - 0.5 ) * 0.01 * ( _bSampleDensityWithNoise ) ; 
					pt3 += ( tex2D( _NoiseTex , fixed2(x,y)).x - 0.5 ) * 0.01 * ( _bSampleDensityWithNoise ) ; 
					pt4 += ( tex2D( _NoiseTex , fixed2(x,y)).x - 0.5 ) * 0.01 * ( _bSampleDensityWithNoise ) ; 

					
					if( pt1.x < 0 || pt1.x > 1 || pt1.y < 0 || pt1.y > 1 ) return fixed4(0,0,0,1);
					if( pt2.x < 0 || pt2.x > 1 || pt2.y < 0 || pt2.y > 1 ) return fixed4(0,0,0,1);
					if( pt3.x < 0 || pt3.x > 1 || pt3.y < 0 || pt3.y > 1 ) return fixed4(0,0,0,1);
					if( pt4.x < 0 || pt4.x > 1 || pt4.y < 0 || pt4.y > 1 ) return fixed4(0,0,0,1);
					
					pt1 = tex2D( _MidTex, pt1 ).xy ;
					pt2 = tex2D( _MidTex, pt2 ).xy ;
					pt3 = tex2D( _MidTex, pt3 ).xy ;
					pt4 = tex2D( _MidTex, pt4 ).xy ;
				
				
					if( pt1.x < 0 || pt1.x > 1 || pt1.y < 0 || pt1.y > 1 ) return fixed4(0,0,0,1);
					if( pt2.x < 0 || pt2.x > 1 || pt2.y < 0 || pt2.y > 1 ) return fixed4(0,0,0,1);
					if( pt3.x < 0 || pt3.x > 1 || pt3.y < 0 || pt3.y > 1 ) return fixed4(0,0,0,1);
					if( pt4.x < 0 || pt4.x > 1 || pt4.y < 0 || pt4.y > 1 ) return fixed4(0,0,0,1);
				
					pt1 *= OriginRes ;
					pt2 *= OriginRes ;
					pt3 *= OriginRes ;
					pt4 *= OriginRes ;
				
					float s1 = 0.5 * ( pt1.x * pt2.y - pt1.y * pt2.x + pt2.x * pt3.y - pt2.y * pt3.x + pt3.x * pt1.y - pt3.y * pt1.x);
					float s2 = 0.5 * ( pt1.x * pt2.y - pt1.y * pt2.x + pt2.x * pt4.y - pt2.y * pt4.x + pt4.x * pt1.y - pt4.y * pt1.x);

					float density;
					if( _MappingStrategy > 0 )
					{
						density = 1 / ( ( abs(s1) + abs(s2) ) * _validPercent );
						
					}
					else if (_MappingStrategy == 0 )
					{
						density = 1 / ( ( abs(s1) + abs(s2) )  );
						
					}
					return fixed4( density ,density,density ,1);
				}

				// return fixed4(0,1,0,1);
				return col;
			}
		ENDCG
		}
	}
}