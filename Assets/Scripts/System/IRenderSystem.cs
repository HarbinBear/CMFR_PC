namespace Framework.CMFR
{
    public interface IRenderSystem : ISystem
    {
    }
    
    public class RenderSystem : AbstractSystem , IRenderSystem 
    {
        protected override void OnInit()
        {
        }
    }
}