using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api;

namespace zArm.Api.Behaviors
{
    public interface IBehavior
    {
        void Enable(Arm arm);
        void Disable();
    }
}
