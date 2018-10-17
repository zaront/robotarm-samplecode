using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Specialized
{
    public interface ISoundProducer
    {
        void Beep(int frequency, int duration);
    }
}
