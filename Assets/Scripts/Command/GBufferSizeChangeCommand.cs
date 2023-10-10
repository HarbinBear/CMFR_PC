using System.Windows.Input;
using Framework;
using Framework.CMFR;

namespace Command
{
    public class GBufferSizeChangeCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.SendEvent<GBufferSizeChangeEvent>();
        }
    }
}