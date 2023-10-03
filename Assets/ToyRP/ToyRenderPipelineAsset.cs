using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Framework.CMFR
{



    [CreateAssetMenu(menuName = "RenderPipeline/ToyRenderPipeline")]
    public class ToyRenderPipelineAsset : RenderPipelineAsset
    {

        public Cubemap diffuseIBL;
        public Cubemap specularIBL;
        public Texture brdfLut;
        public Texture blueNoiseTex;

        [SerializeField] public CsmSettings csmSettings;
        public InstanceData[] instanceDatas;

        public bool CMFR_On;
        public Material CMFR_Mat;
        public Material CMFR_Depth_Mat;
        
        protected override RenderPipeline CreatePipeline()
        {
            ToyRenderPipeline rp = new ToyRenderPipeline();

            rp.diffuseIBL = diffuseIBL;
            rp.specularIBL = specularIBL;
            rp.brdfLut = brdfLut;
            rp.blueNoiseTex = blueNoiseTex;
            rp.csmSettings = csmSettings;
            rp.instanceDatas = instanceDatas;
            rp.CMFR_On = CMFR_On;
            rp.CMFR_Mat = CMFR_Mat;
            rp.CMFR_Depth_Mat = CMFR_Depth_Mat;

            return rp;
        }
    }
}