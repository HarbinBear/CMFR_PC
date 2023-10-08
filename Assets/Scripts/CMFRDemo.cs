using UnityEngine;

namespace Framework.CMFR
{
    public class CMFRDemo : Architecture<CMFRDemo>
    {
        protected override void Init()
        {
            Debug.Log("[CMFRDemo] Init");
            RegisterSystem<IRenderSystem>( new RenderSystem() );
            RegisterModel<ICMFRModel>( new CMFRModel() );
        }
    }
}