using System.Collections;
using System.Collections.Generic;
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
        RenderTexture InvCMFRTex; // 存储 light pass 的结果
        RenderTexture TAATex; // 存储 light pass 的结果
        RenderTexture hizBuffer; // hi-z buffer

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
                RenderSys = gameComp?.GetArchitecture().GetSystem<IRenderSystem>();
            }
            else
            {
                Debug.LogError("[ToyRenderPipeline] GameComp is null !");
            }
            QualitySettings.vSyncCount = 0; // 关闭垂直同步
            Application.targetFrameRate = 60; // 帧率

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
            
            _gdepth_CMFR      = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.RFloat     );
            _gbuffers_CMFR[0] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB32     );
            _gbuffers_CMFR[1] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB2101010);
            _gbuffers_CMFR[2] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB64     );
            _gbuffers_CMFR[3] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGBFloat  );
            
            lightPassTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            InvCMFRTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            TAATex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
            

            tempDepthRT2 =
                new RenderTexture(_gdepth_CMFR.width, _gdepth_CMFR.height, 0, RenderTextureFormat.RFloat);

            // Hi-z buffer
            int hSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height)); // 大小必须是 2 的次幂
            hizBuffer = new RenderTexture(hSize, hSize, 0, RenderTextureFormat.RHalf);
            hizBuffer.autoGenerateMips = false;
            hizBuffer.useMipMap = true;
            hizBuffer.filterMode = FilterMode.Point;

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
            
            _gdepth_CMFR.Release();
            foreach (var buffer in _gbuffers_CMFR)
            {
                buffer.Release();
            }

            _gdepth_CMFR      = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.RFloat     );
            _gbuffers_CMFR[0] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB32     );
            _gbuffers_CMFR[1] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB2101010);
            _gbuffers_CMFR[2] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGB64     );
            _gbuffers_CMFR[3] = new RenderTexture(sigmaWidth, sigmaHeight, 0, RenderTextureFormat.ARGBFloat  );
        }
        

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // 主相机
            Camera camera = cameras[0];
            
            PreCullPass(context , camera);


            // 全局变量设置
            Shader.SetGlobalFloat("_far", camera.farClipPlane);
            Shader.SetGlobalFloat("_near", camera.nearClipPlane);
            Shader.SetGlobalFloat("_screenWidth", Screen.width);
            Shader.SetGlobalFloat("_screenHeight", Screen.height);
            Shader.SetGlobalTexture("_noiseTex", blueNoiseTex);
            Shader.SetGlobalFloat("_noiseTexResolution", blueNoiseTex.width);

            //  gbuffer 
            Shader.SetGlobalTexture("_gdepth", GDepth);
            Shader.SetGlobalTexture("_hizBuffer", hizBuffer);
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

            
            ClusterLightingPass(context, camera);

            ShadowCastingPass(context, camera);

            GbufferPass(context, camera);

            InstanceDrawPass(context, Camera.main);
            
            // only generate for main camera
            if (!isEditor)
            {
                HizPass(context, camera);
                vpMatrixPrev = vpMatrix;
            }
            
            if( RenderSys.GetModel<ICMFRModel>().outputTex != 0 ) 
                CMFRPass(context , camera );
            
            ShadowMappingPass(context, camera);
            
            LightPass(context, camera);
            
            if( RenderSys.GetModel<ICMFRModel>().outputTex != 0 ) 
                InvCMFRPass( context ,camera);

            TAAPass(context, camera);

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
            jitter.PreCull( context  );
        }
        

        void ClusterLightingPass(ScriptableRenderContext context, Camera camera)
        {
            // 裁剪光源
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);

            // 更新光源
            clusterLight.UpdateLightBuffer(cullingResults.visibleLights.ToArray());

            // 划分 cluster
            clusterLight.ClusterGenerate(camera);

            // 分配光源
            clusterLight.LightAssign();

            // 传递参数
            clusterLight.SetShaderParameters();
        }

        // 阴影贴图 pass
        void ShadowCastingPass(ScriptableRenderContext context, Camera camera)
        {
            Profiler.BeginSample("MyPieceOfCode");

            // 获取光源信息
            Light light = RenderSettings.sun;
            Vector3 lightDir = light.transform.rotation * Vector3.forward;

            // 更新 shadowmap 分割
            csm.Update(camera, lightDir, csmSettings);
            csmSettings.Set();

            csm.SaveMainCameraSettings(ref camera);
            for (int level = 0; level < 4; level++)
            {
                // 将相机移到光源方向
                csm.ConfigCameraToShadowSpace(ref camera, lightDir, level, orthoDistance, shadowMapResolution);

                // 设置阴影矩阵, 视锥分割参数
                Matrix4x4 v = camera.worldToCameraMatrix;
                Matrix4x4 p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
                Shader.SetGlobalMatrix("_shadowVpMatrix" + level, p * v);
                Shader.SetGlobalFloat("_orthoWidth" + level, csm.orthoWidths[level]);

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "shadowmap" + level;

                // 绘制前准备
                context.SetupCameraProperties(camera);
                cmd.SetRenderTarget(shadowTextures[level]);
                cmd.ClearRenderTarget(true, true, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // 剔除
                camera.TryGetCullingParameters(out var cullingParameters);
                var cullingResults = context.Cull(ref cullingParameters);
                // config settings
                ShaderTagId shaderTagId = new ShaderTagId("depthonly");
                SortingSettings sortingSettings = new SortingSettings(camera);
                DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
                FilteringSettings filteringSettings = FilteringSettings.defaultValue;

                // 绘制
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
                context.Submit(); // 每次 set camera 之后立即提交
            }

            csm.RevertMainCameraSettings(ref camera);

            Profiler.EndSample();
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

            SetCMFRMatParams( CMFR_Depth_Mat );
            SetCMFRMatParams( CMFR_Mat );



            cmd.Blit(_gdepth, _gdepth_CMFR, CMFR_Depth_Mat);
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


        // 阴影计算 pass : 输出阴影强度 texture
        void ShadowMappingPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "shadowmappingpass";

            RenderTexture tempTex1 = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0,
                RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            RenderTexture tempTex2 = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0,
                RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            RenderTexture tempTex3 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear);

            if (csmSettings.usingShadowMask)
            {
                // 生成 Mask, 模糊 Mask
                cmd.Blit(GBufferID[0], tempTex1, new Material(Shader.Find("ToyRP/preshadowmappingpass")));
                cmd.Blit(tempTex1, tempTex2, new Material(Shader.Find("ToyRP/blurNx1")));
                cmd.Blit(tempTex2, shadowMask, new Material(Shader.Find("ToyRP/blur1xN")));
            }

            // 生成阴影, 模糊阴影
            cmd.Blit(GBufferID[0], tempTex3, new Material(Shader.Find("ToyRP/shadowmappingpass")));
            cmd.Blit(tempTex3, shadowStrength, new Material(Shader.Find("ToyRP/blurNxN")));

            RenderTexture.ReleaseTemporary(tempTex1);
            RenderTexture.ReleaseTemporary(tempTex2);
            RenderTexture.ReleaseTemporary(tempTex3);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }

        // 光照 Pass : 计算 PBR 光照并且存储到 lightPassTex 纹理
        void LightPass(ScriptableRenderContext context, Camera camera)
        {
            // 使用 Blit  
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "lightpass";

            Material mat = new Material(Shader.Find("ToyRP/lightpass"));
            cmd.Blit(GBufferID[0], lightPassTex, mat);
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
            SetCMFRMatParams( Inv_CMFR_Depth_Mat );
            cmd.Blit(null,tempDepthRT2 , Inv_CMFR_Depth_Mat );
            cmd.Blit(tempDepthRT2, _gdepth_CMFR);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();

            Profiler.EndSample();

        }



        void TAAPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "TAA";

            TemporalReprojection taa = camera.GetComponentInParent<TemporalReprojection>();
            switch (RenderSys.GetModel<ICMFRModel>().outputTex )
            {
                case (int)OutputTex.Original:
                { 
                    taa.RenderTAA(lightPassTex,TAATex);
                    break;
                }
                case (int)OutputTex.CMFRPass1_Albedo:
                {
                    taa.RenderTAA(_gbuffers_CMFR[0],TAATex);
                    break;
                }
                case (int)OutputTex.CMFRPass2:
                {
                    taa.RenderTAA(InvCMFRTex,TAATex);
                    break;
                }
                case (int)OutputTex.SampleDensity:
                {
                    taa.RenderTAA(InvCMFRTex,TAATex);
                    break;
                }
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
            if (RenderSys.GetModel<ICMFRModel>().TAA_On == true)
            {
                cmd.Blit(TAATex,BuiltinRenderTextureType.CameraTarget,mat);

            }
            else
            {
                switch (RenderSys.GetModel<ICMFRModel>().outputTex )
                {
                    case (int)OutputTex.Original:
                    {
                        cmd.Blit(lightPassTex, BuiltinRenderTextureType.CameraTarget, mat);
                        break;
                    }
                    case (int)OutputTex.CMFRPass1_Albedo:
                    {
                        cmd.Blit(_gbuffers_CMFR[0], BuiltinRenderTextureType.CameraTarget, mat);

                        break;
                    }
                    case (int)OutputTex.CMFRPass2:
                    {
                        cmd.Blit(InvCMFRTex, BuiltinRenderTextureType.CameraTarget, mat);
                        break;
                    }
                    case (int)OutputTex.SampleDensity:
                    {
                        cmd.Blit(InvCMFRTex, BuiltinRenderTextureType.CameraTarget, mat);
                        break;
                    }
                }
            }
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }
        

        // 绘制 instanceData 列表中的所有 instance
        void InstanceDrawPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "instance gbuffer";
            cmd.SetRenderTarget(_gbufferID, _gdepthID);

            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 vp = projMatrix * viewMatrix;

            // 绘制 instance
            ComputeShader cullingCs = FindComputeShader("InstanceCulling");
            for (int i = 0; i < instanceDatas.Length; i++)
            {
                InstanceDrawer.Draw(instanceDatas[i], Camera.main, cullingCs, vpMatrixPrev, hizBuffer, ref cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }

        // hiz pass
        void HizPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "hizpass";

            // 创建纹理
            int size = hizBuffer.width;
            int nMips = (int)Mathf.Log(size, 2);
            RenderTexture[] mips = new RenderTexture[nMips];
            for (int i = 0; i < mips.Length; i++)
            {
                int mSize = size / (int)Mathf.Pow(2, i);
                mips[i] = RenderTexture.GetTemporary(mSize, mSize, 0, RenderTextureFormat.RHalf,
                    RenderTextureReadWrite.Linear);
                mips[i].filterMode = FilterMode.Point;
            }

            // 生成 mipmap
            Material mat = new Material(Shader.Find("ToyRP/hizBlit"));
            cmd.Blit(_gdepth, mips[0]);
            for (int i = 1; i < mips.Length; i++)
            {
                cmd.Blit(mips[i - 1], mips[i], mat);
            }

            // 拷贝到 hizBuffer 的各个 mip
            for (int i = 0; i < mips.Length; i++)
            {
                cmd.CopyTexture(mips[i], 0, 0, hizBuffer, 0, i);
                RenderTexture.ReleaseTemporary(mips[i]);
            }

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }
        
        static ComputeShader FindComputeShader(string shaderName)
        {
            ComputeShader[] css = Resources.FindObjectsOfTypeAll(typeof(ComputeShader)) as ComputeShader[];
            for (int i = 0; i < css.Length; i++)
            {
                if (css[i].name == shaderName)
                    return css[i];
            }

            return null;
        }
        
    }

}