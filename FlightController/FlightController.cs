using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using VRage;
using VRageMath;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;

using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript {
  partial class Program : MyGridProgram 
  {
    #region Program
    delegate float BankHandler(float bankAngle, float input);

    Configuration cfg;        
    FlightComputer fc;
    AttitudeController attc;
    ThrustController thrc;

    int tick = 0;

    // -----------------------------------------------------------------------
    public Program()
    {
      List<Configuration.Option> optionHandlers = new List<Configuration.Option>();

      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      fc = new FlightComputer(this);
      attc = new AttitudeController(this, fc);
      thrc = new ThrustController(this, fc);

      optionHandlers.AddRange(attc.hanlders);
      optionHandlers.AddRange(thrc.hanlders);

      cfg = new Configuration(this, optionHandlers.ToArray());
    }

    // -----------------------------------------------------------------------
    public void Main(string argument, UpdateType updateSource)
    {
      tick++;
      if (tick % 100 == 0) {
        fc.updateMaxEffectiveThrust();
      }

      if (!string.IsNullOrEmpty(argument)) {
        cfg.readConfig();
        cfg.processCommands(argument);
      }

      Vector3 gravity = fc.controller.GetTotalGravity();
      Vector3 gravityDirection = gravity;
      gravityDirection.Normalize();

      thrc.run(gravity, gravityDirection);
      attc.run(gravityDirection);
    }

    #endregion Program
  }
} // End of namespace SpaceEngineers
