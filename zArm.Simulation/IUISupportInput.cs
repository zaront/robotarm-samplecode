using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Simulation
{
    public interface IUISupportInput
    {
        Tuple<int, int> HideMouse();
        void ShowMouse(Tuple<int, int> pos);
        void SetCursor(CursorType cursor);
    }

    public enum CursorType
    {
        Default,
        UpDown
    }
}
