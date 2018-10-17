using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api
{
    public class DataEventArg<T> : EventArgs
    {
        public DataEventArg(T data)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
