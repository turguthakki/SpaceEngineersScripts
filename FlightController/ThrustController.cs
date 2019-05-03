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
    
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    class ThrustController
    {
      delegate Vector3 ThrustInput(Vector3 gravityDirection);

      public Configuration.Option[] hanlders {get; private set;}
      MyGridProgram program;
      FlightComputer fc;

      Configuration.Boolean alignToGravity = new Configuration.Boolean("gravityAlignedThrust", true);
      ThrustInput thrustInput;

      // -------------------------------------------------------------------------
      public ThrustController(MyGridProgram program, FlightComputer fc)
      {
        this.program = program;
        this.fc = fc;
        hanlders = new Configuration.Option[] {
          alignToGravity
        };

        alignToGravity.onChanged += onGravityAlignmentChanged;

        onGravityAlignmentChanged();
      }

      // ------------------------------------------------------------------------
      void onGravityAlignmentChanged()
      {
        thrustInput = alignToGravity ? (ThrustInput) gravityAlignedThrust : (ThrustInput) controllerAlignedThrust;
      }

      // ------------------------------------------------------------------------
      Vector3 gravityAlignedThrust(Vector3 vertical)
      {
        Vector3 lateral = Vector3.Cross(fc.controller.WorldMatrix.Backward, vertical);
        Vector3 longitudal = Vector3.Cross(fc.controller.WorldMatrix.Left, vertical);

        lateral.Normalize();
        lateral *= fc.controller.MoveIndicator.X;

        longitudal.Normalize();
        longitudal *= fc.controller.MoveIndicator.Z;

        vertical *= -fc.controller.MoveIndicator.Y;

        return lateral + vertical + longitudal;
      }

      // ------------------------------------------------------------------------
      Vector3 controllerAlignedThrust(Vector3 gravityDirection)
      {
        return Vector3.TransformNormal(fc.controller.MoveIndicator, fc.controller.WorldMatrix);
      }

      // ------------------------------------------------------------------------
      public void run(Vector3 gravity, Vector3 gravityDirection)
      {
        Vector3 input = thrustInput(gravityDirection);
        Vector3 velocityVector = Vector3.Zero;


        double acceleration = input.Length();
        if (input.Length() > 0) {
          input.Normalize();
          input *= (float) (fc.maxEffectiveThrustInDirection(input) / fc.mass);
        }

        if (fc.controller.DampenersOverride) {
          velocityVector = ((Vector3) fc.controller.GetShipVelocities().LinearVelocity);
        }

        float descentCommand = Math.Max(0, Vector3.Down.Dot(fc.controller.MoveIndicator));
        fc.thrustW((-gravity * (1.0f - descentCommand)) - velocityVector + input);
      }
    }

    #endregion Program
  }
} // End of namespace SpaceEngineers
