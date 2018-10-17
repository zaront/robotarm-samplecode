using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Simulation.Entities;

namespace zArm.Simulation.Enviorment
{
    public abstract class ArmSim : ArmSim<ArmApp>
    {
    }

    public abstract class ArmSim<T> : Sim<T>
        where T : ArmApp
    {
        public SimzArmB1 SimArm { get; private set; }

        protected internal override void Started()
        {
            //set fields
            SimArm = App.SimArm;
        }
    }
}
