using System.Collections;
using System.Collections.Generic;
using Kino;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.Profiling;

namespace Framework.CMFR
{
    public class ToyRenderPipeline : RenderPipeline
    {

        private IRenderSystem _renderSys;
        public IRenderSystem RenderSys
        {
            get => _renderSys;
            set { _renderSys = value; }
        }
        
        RenderTexture _gdepth; // depth attachment
        RenderTexture _gdepth_CMFR; // depth attachment
        RenderTexture tempDepthRT2;

        public RenderTexture GDepth
        {
            get { return RenderSys.GetModel<ICMFRModel>().outputTex != 0 ? _gdepth_CMFR : _gdepth; }
        }

        RenderTexture[] _gbuffers = new RenderTexture[4]; // color attachments
        RenderTexture[] _gbuffers_CMFR = new RenderTexture[4]; // color attachments

        public RenderTexture[] GBuffers
        {
            get { return RenderSys.GetModel<ICMFRModel>().outputTex != 0 ? _gbuffers_CMFR : _gbuffers; }
        }

        RenderTargetIdentifier _gdepthID;
        RenderTargetIdentifier _gdepthID_CMFR;

        public RenderTargetIdentifier GDepthID
        {
            get { return RenderSys.GetModel<ICMFRModel>().outputTex != 0 ? _gdepthID_CMFR : _gdepthID; }
        }

        RenderTargetIdentifier[] _gbufferID = new RenderTargetIdentifier[4]; // tex ID 
        RenderTargetIdentifier[] _gbufferID_CMFR = new RenderTargetIdentifier[4]; // tex ID 

        public RenderTargetIdentifier[] GBufferID
        {
            get { return RenderSys.GetModel<ICMFRModel>().outputTex != 0 ? _gbufferID_CMFR : _gbufferID; }
        }

        public Material CMFR_Mat;
        public Material CMFR_Depth_Mat;
        public Material Inv_CMFR_Mat;
        public Material Inv_CMFR_Depth_Mat;

        RenderTexture lightPassTex; // 存储 light pass 的结果
        RenderTexture largeLightPassTex; // 存储 light pass 的结果
        RenderTexture InvCMFRTex; // 存储 light pass 的结果
        RenderTexture TAATex; // 存储 light pass 的结果
        RenderTexture BokehTex; // 存储 light pass 的结果

        Matrix4x4 vpMatrix;
        Matrix4x4 vpMatrixInv;
        Matrix4x4 vpMatrixPrev; // 上一帧的 vp 矩阵
        Matrix4x4 vpMatrixInvPrev;

        // 噪声图
        public Texture blueNoiseTex;

        // IBL 贴图
        public Cubemap diffuseIBL;
        public Cubemap specularIBL;
        public Texture brdfLut;

        // 阴影管理
        public int shadowMapResolution = 1024;
        public float orthoDistance = 500.0f;
        public float lightSize = 2.0f;
        CSM csm;
        public CsmSettings csmSettings;
        RenderTexture[] shadowTextures = new RenderTexture[4]; // 阴影贴图
        RenderTexture shadowMask;
        RenderTexture shadowStrength;

        // 光照管理
        ClusterLight clusterLight;

        // instance data 数组
        public InstanceData[] instanceDatas;
        
        public ToyRenderPipeline()
        {
            Debug.Log("[ToyRenderPipeline] constructor");
            GameObject game = GameObject.Find("Game");
            if (game != null)
            {
                Game gameComp = game.GetComponent<Game>() ;
                RenderSys = CMFRDemo.Interface.GetSystem<IRenderSystem>();
            }
            else
            {
                Debug.LogError("[ToyRenderPipeline] GameComp is null !");
            }
            // QualitySettings.vSyncCount = 0; // 关闭垂直同步
            // Application.targetFrameRate = 60; // 帧率

            // 创建纹理
            _gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
            _gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            _gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010,
                RenderTextureReadWrite.Linear);
            _gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64,
                RenderTextureReadWrite.Linear);
            _gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);

            float sigma = RenderSys.GetModel<ICMFRModel>().sigma;
            int sigmaWidth = Mathf.RoundToInt(Screen.width / sigma);
            int sigmaHeight = Mathf.RoundToInt(Screen.height / sigma);
            
            _gdepth_CMFR      = new RenderTexture(Screen.width , Screen.height, 0, RenderTextureFormat.RFloat );
            _gbuffers_CMFR[0] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB32     );
            _gbuffers_CMFR[1] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB2101010);
            _gbuffers_CMFR[2] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB64     );
            _gbuffers_CMFR[3] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGBFloat  );
            
            lightPassTex = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGBFloat);
            // lightPassTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            largeLightPassTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            InvCMFRTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            TAATex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            BokehTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            

            tempDepthRT2 =
                new RenderTexture(_gdepth_CMFR.width, _gdepth_CMFR.height, 0, RenderTextureFormat.RFloat);
            

            // 给纹理 ID 赋值
            _gdepthID = _gdepth;
            _gdepthID_CMFR = _gdepth_CMFR;
            for (int i = 0; i < 4; i++)
                _gbufferID[i] = _gbuffers[i];
            for (int i = 0; i < 4; i++)
                _gbufferID_CMFR[i] = _gbuffers_CMFR[i];

            // 创建阴影贴图
            shadowMask = new RenderTexture(Screen.width / 4, Screen.height / 4, 0, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear);
            shadowStrength = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear);
            for (int i = 0; i < 4; i++)
                shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24,
                    RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

            csm = new CSM();

            clusterLight = new ClusterLight();

            RenderSys.RegisterEvent<GBufferSizeChangeEvent>(OnGBufferSizeChanged);
        }

        private void OnGBufferSizeChanged(GBufferSizeChangeEvent e)
        {
            float sigma = RenderSys.GetModel<ICMFRModel>().sigma;
            int sigmaWidth = Mathf.RoundToInt(Screen.width / sigma);
            int sigmaHeight = Mathf.RoundToInt(Screen.height / sigma);
            
            // _gdepth_CMFR.Release();
            foreach (var buffer in _gbuffers_CMFR)
            {
                if(buffer) buffer.Release();
            }
            if( lightPassTex ) lightPassTex.Release();

            // _gdepth_CMFR      = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.RFloat     );
            _gbuffers_CMFR[0] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB32     );
            _gbuffers_CMFR[1] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB2101010);
            _gbuffers_CMFR[2] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB64     );
            _gbuffers_CMFR[3] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGBFloat  );
            
            lightPassTex = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGBFloat);
        }
        

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // Debug.Log("Time taken for this frame: " + Time.deltaTime);

            // 主相机
            Camera camera = cameras[0];
            
            if( RenderSys.GetModel<ICMFRModel>().FrustumJitter_On == true ) PreCullPass(context , camera);


            // 全局变量设置
            Shader.SetGlobalFloat("_far", camera.farClipPlane);
            Shader.SetGlobalFloat("_near", camera.nearClipPlane);
            Shader.SetGlobalFloat("_screenWidth", Screen.width);
            Shader.SetGlobalFloat("_screenHeight", Screen.height);
            Shader.SetGlobalTexture("_noiseTex", blueNoiseTex);
            Shader.SetGlobalFloat("_noiseTexResolution", blueNoiseTex.width);

            //  gbuffer 
            Shader.SetGlobalTexture("_gdepth", GDepth);
            for (int i = 0; i < 4; i++)
                Shader.SetGlobalTexture("_GT" + i, GBuffers[i]);

            // 设置相机矩阵
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            vpMatrix = projMatrix * viewMatrix;
            vpMatrixInv = vpMatrix.inverse;
            Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
            Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
            Shader.SetGlobalMatrix("_vpMatrixPrev", vpMatrixPrev);
            Shader.SetGlobalMatrix("_vpMatrixInvPrev", vpMatrixInvPrev);

            // 设置 IBL 贴图
            Shader.SetGlobalTexture("_diffuseIBL", diffuseIBL);
            Shader.SetGlobalTexture("_specularIBL", specularIBL);
            Shader.SetGlobalTexture("_brdfLut", brdfLut);

            // 设置 CSM 相关参数
            Shader.SetGlobalFloat("_orthoDistance", orthoDistance);
            Shader.SetGlobalFloat("_shadowMapResolution", shadowMapResolution);
            Shader.SetGlobalFloat("_lightSize", lightSize);
            Shader.SetGlobalTexture("_shadowStrength", shadowStrength);
            Shader.SetGlobalTexture("_shadoMask", shadowMask);
            for (int i = 0; i < 4; i++)
            {
                Shader.SetGlobalTexture("_shadowtex" + i, shadowTextures[i]);
                Shader.SetGlobalFloat("_split" + i, csm.splts[i]);
            }

            bool isEditor = Handles.ShouldRenderGizmos();

            // ------------------------ 管线各个 Pass ------------------------ //
            
            GbufferPass(context, camera);

            if( RenderSys.GetModel<ICMFRModel>().outputTex != 0 ) 
                CMFRPass(context , camera );
            
            // ShadowMappingPass(context, camera);
            
            LightPass(context, camera);
            
            if( RenderSys.GetModel<ICMFRModel>().outputTex != 0 ) 
                InvCMFRPass( context ,camera);

            if( RenderSys.GetModel<ICMFRModel>().TAA_On ) TAAPass(context, camera);
            
            if( RenderSys.GetModel<ICMFRModel>().Bokeh_On ) BokehPass( context , camera );

            FinalPass(context, camera);
            
            
            // ------------------------- Pass end -------------------------- //

            // skybox and Gizmos
            context.DrawSkybox(camera);
            if (isEditor)
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
            


            // 提交绘制命令
            context.Submit();
            
            

        }


        void PreCullPass(ScriptableRenderContext context, Camera camera)
        {
            FrustumJitter jitter =  camera.GetComponentInParent<FrustumJitter>();
            if (jitter)
            {
                jitter.PreCull( context  );
            }
        }
        

        // Gbuffer Pass
        void GbufferPass(ScriptableRenderContext context, Camera camera)
        {
            Profiler.BeginSample("gbufferDraw");

            context.SetupCameraProperties(camera);
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "gbuffer";

            // 清屏
            cmd.SetRenderTarget(_gbufferID, _gdepthID);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 剔除
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);

            // config settings
            ShaderTagId shaderTagId = new ShaderTagId("gbuffer"); // 使用 LightMode 为 gbuffer 的 shader
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            // 绘制一般几何体
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.Submit();

            Profiler.EndSample();
        }

        void CMFRPass(ScriptableRenderContext context, Camera camera)
        {
            // Debug.Log("[ToyRenderPipeline] CMFRPass");
            if (RenderSys == null) return;
            Profiler.BeginSample("CMFR");
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "CMFR";

            // SetCMFRMatParams( CMFR_Depth_Mat );
            SetCMFRMatParams( CMFR_Mat );



            cmd.Blit(_gdepth, _gdepth_CMFR);
            for (int i = 0; i < 4; i++)
            {
                // _gbuffers[i].filterMode = FilterMode.Point;
                cmd.Blit(_gbuffers[i], _gbuffers_CMFR[i], CMFR_Mat);
            }
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();

            Profiler.EndSample();

        }

        void SetCMFRMatParams(Material mat )
        {
            mat.SetFloat("_eyeX", RenderSys.GetModel<ICMFRModel>().eyeX );
            mat.SetFloat("_eyeY", RenderSys.GetModel<ICMFRModel>().eyeY);
            mat.SetFloat("_scaleRatio", RenderSys.GetModel<ICMFRModel>().sigma);
            mat.SetFloat("_fx", RenderSys.GetModel<ICMFRModel>().fx);
            mat.SetFloat("_fy", RenderSys.GetModel<ICMFRModel>().fy);
            mat.SetFloat("_SquelchedGridMappingBeta", RenderSys.GetModel<ICMFRModel>().squelchedGridMappingBeta);
            mat.SetInt("_MappingStrategy",RenderSys.GetModel<ICMFRModel>().mappingStrategy );
            // mat.SetInt("_DebugMode", (int)RenderSys.GetModel<ICMFRModel>().debugMode);
            mat.SetInt("_OutputMode", (int)RenderSys.GetModel<ICMFRModel>().outputTex);
            

            if (mat == CMFR_Mat)
            {
                mat.SetInt("_iApplyRFRMap1", RenderSys.GetModel<ICMFRModel>().iApplyRFRMap1);
            }

            if (mat == CMFR_Depth_Mat)
            {
                mat.SetInt("_iApplyRFRMap1", RenderSys.GetModel<ICMFRModel>().iApplyRFRMap1);

            }
            
            if (mat == Inv_CMFR_Mat)
            {
                if (RenderSys.GetModel<ICMFRModel>().outputTex >= 3)
                {
                    mat.SetTexture("_MidTex", _gbuffers_CMFR[3]);
                }
                else
                {
                    mat.SetTexture("_MidTex", lightPassTex);
                }
                mat.SetInt("_iApplyRFRMap2", RenderSys.GetModel<ICMFRModel>().iApplyRFRMap1);
                mat.SetFloat("_bSampleDensityWithNoise", RenderSys.GetModel<ICMFRModel>().sampleDensityWithNoise);
                mat.SetFloat("_validPercent" , RenderSys.GetModel<ICMFRModel>().validPercent);
            }

            if (mat == Inv_CMFR_Depth_Mat)
            {
                mat.SetTexture("_MidTex" , _gdepth_CMFR );
                mat.SetInt("_iApplyRFRMap2", RenderSys.GetModel<ICMFRModel>().iApplyRFRMap1);
                mat.SetFloat("_bSampleDensityWithNoise", RenderSys.GetModel<ICMFRModel>().sampleDensityWithNoise);
                mat.SetFloat("_validPercent" , RenderSys.GetModel<ICMFRModel>().validPercent);
            }
        }

        // 光照 Pass : 计算 PBR 光照并且存储到 lightPassTex 纹理
        void LightPass(ScriptableRenderContext context, Camera camera)
        {
            // 使用 Blit  
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "lightpass";

            Material mat = new Material(Shader.Find("ToyRP/lightpass"));
            switch (RenderSys.GetModel<ICMFRModel>().outputTex)
            {
                case (int)OutputTex.Original:
                {
                    cmd.Blit(GBufferID[0], largeLightPassTex, mat);
                    break;
                }
                case (int)OutputTex.CMFRPass2 or (int)OutputTex.CMFRPass1_Albedo or (int)OutputTex.SampleDensity :
                {
                    cmd.Blit(GBufferID[0], lightPassTex, mat);
                    cmd.Blit( lightPassTex, largeLightPassTex);
                    break;
                }
            }

            context.ExecuteCommandBuffer(cmd);

            context.Submit();
        }



        void InvCMFRPass(ScriptableRenderContext context, Camera camera)
        {
            // Debug.Log("[ToyRenderPipeline] InvCMFRPass");
            if (RenderSys == null) return;
            Profiler.BeginSample("InvCMFR");
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "InvCMFR";

            SetCMFRMatParams( Inv_CMFR_Mat );
            cmd.Blit(null, InvCMFRTex, Inv_CMFR_Mat);
            // SetCMFRMatParams( Inv_CMFR_Depth_Mat );
            // cmd.Blit(null,tempDepthRT2 , Inv_CMFR_Depth_Mat );
            // cmd.Blit(tempDepthRT2, _gdepth_CMFR);
            

            context.ExecuteCommandBuffer(cmd);
            context.Submit();

            Profiler.EndSample();

        }



        void TAAPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "TAA";

            TemporalReprojection taa = camera.GetComponentInParent<TemporalReprojection>();
            VelocityBuffer velocityBuffer = camera.GetComponent<VelocityBuffer>();
            if (taa == null) return;
            
            velocityBuffer.OnPreRender();
            taa.RenderTAA( GetCurBuffer(),TAATex , cmd);         
            velocityBuffer.OnPostRender();
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            
        }


        void BokehPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Bokeh";

            Bokeh bokeh = camera.GetComponentInParent<Bokeh>();

            // by raycast
            Ray ray = Camera.main.ScreenPointToRay( 
    new Vector2( 
            RenderSys.GetModel<ICMFRModel>().eyeX * _gdepth_CMFR.width ,
                RenderSys.GetModel<ICMFRModel>().eyeY  * _gdepth_CMFR.height 
                // ( 1.0f - RenderSys.GetModel<ICMFRModel>().eyeY ) * _gdepth_CMFR.height 
                )
            );
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 hit_ViewSpace = Camera.main.WorldToViewportPoint(hit.point);

                float depth = hit_ViewSpace.z;

                float trueDepth = depth + Camera.main.nearClipPlane;
                
                RenderSys.GetModel<ICMFRModel>().focusDistance.Value = trueDepth;
            }
            else
            {
                RenderSys.GetModel<ICMFRModel>().focusDistance.Value = 100000;
            }
            Debug.Log( RenderSys.GetModel<ICMFRModel>().focusDistance.Value );
            if (bokeh == null) return; 
            if (RenderSys.GetModel<ICMFRModel>().TAA_On == true)
            {
                bokeh.OnBokeh( TAATex , _gdepth  , BokehTex , cmd);
            }
            else
            {
                bokeh.OnBokeh( GetCurBuffer() , _gdepth ,  BokehTex , cmd );
             
            }
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();

        }
        
        
        
        // 后处理和最终合成 Pass
        void FinalPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "finalpass";

            Material mat = new Material(Shader.Find("ToyRP/finalpass"));

            if (RenderSys.GetModel<ICMFRModel>().Bokeh_On == false  )
            {
                if (RenderSys.GetModel<ICMFRModel>().TAA_On == true)
                {
                    cmd.Blit(TAATex,BuiltinRenderTextureType.CameraTarget,mat);

                }
                else
                {
                    cmd.Blit( GetCurBuffer() , BuiltinRenderTextureType.CameraTarget , mat );
                }
            }
            else
            {
                cmd.Blit(BokehTex,BuiltinRenderTextureType.CameraTarget,mat);

            }
            
            
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }


        RenderTexture GetCurBuffer()
        {
            switch (RenderSys.GetModel<ICMFRModel>().outputTex )
            {
                case (int)OutputTex.Original:
                {
                    return largeLightPassTex;
                }
                case (int)OutputTex.CMFRPass1_Albedo:
                {
                    return _gbuffers_CMFR[0];
                }
                case (int)OutputTex.CMFRPass2:
                {
                    return InvCMFRTex;
                }
                case (int)OutputTex.SampleDensity:
                {
                    return InvCMFRTex;
                }
            }
            
            return largeLightPassTex;
        }

    }

}