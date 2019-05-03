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
    public class AttitudeController
    {
      delegate float BankHandler(float bankAngle, float input);
      delegate float PitchHandler(float pitchAngle, float input);
      public Configuration.Option[] hanlders {get; private set;}

      MyGridProgram program;
      FlightComputer fc;
      Configuration.Boolean bankCorrection = null;
      Configuration.Numeric bankCorrectionMultiplier = null;
      Configuration.Option maximumBankAngleHandler = null;
      float maxBankLeft = 0.25f;
      float maxBankRight = 0.25f;

      Configuration.Boolean pitchCorrection = null;
      Configuration.Numeric pitchCorrectionMultiplier = null;
      Configuration.Option maximumPitchAngleHandler = null;
      float maxPitchDown = 0.25f;
      float maxPitchUp = 0.25f;

      BankHandler bankHandler = null;
      PitchHandler pitchHandler = null;

      // ---------------------------------------------------------------------
      public AttitudeController(MyGridProgram program, FlightComputer fc)
      {
        this.program = program;
        this.fc = fc;

        hanlders = new Configuration.Option[] {
          bankCorrection = new Configuration.Boolean("bankCorrection", true),
          bankCorrectionMultiplier = new Configuration.Numeric("bankCorrectionMultiplier", 4.0f),
          maximumBankAngleHandler = new Configuration.Option("maximumBankAngle", onMaximumBankAngleChanged, "45", "45"),
          pitchCorrection = new Configuration.Boolean("pitchCorrection", true),
          pitchCorrectionMultiplier = new Configuration.Numeric("pitchCorrectionMultiplier", 4.0f),
          maximumPitchAngleHandler = new Configuration.Option("maximumPitchAngle", onMaximumPitchAngleChanged, "45", "45"),
        };

        bankCorrection.onChanged += onBankCorrectionChanged;
        pitchCorrection.onChanged += onPitchCorrectionChanged;

        onBankCorrectionChanged();
        onPitchCorrectionChanged();
      }

      // ---------------------------------------------------------------------
      void onBankCorrectionChanged()
      {
        bankHandler = bankCorrection ? (BankHandler) applyBankCorrectedInput : (BankHandler) applyBankInput;
      }

      // ---------------------------------------------------------------------
      void onPitchCorrectionChanged()
      {
        pitchHandler = pitchCorrection ? (PitchHandler) applyPitchCorrectedInput : (PitchHandler) applyPitchInput;
      }

      // ---------------------------------------------------------------------
      void onMaximumBankAngleChanged(params string[] parameters)
      {
        if (parameters.Length != 2) 
          throw new ArgumentException("Invalid number of arguments for maximumBankAngle");
        maxBankLeft = (float) (double.Parse(parameters[0]) * (Math.PI / 180.0f) / Math.PI);
        maxBankRight = (float) (double.Parse(parameters[1]) * (Math.PI / 180.0f) / Math.PI);
      }

      // ---------------------------------------------------------------------
      void onMaximumPitchAngleChanged(params string[] parameters)
      {
        if (parameters.Length != 2) 
          throw new ArgumentException("Invalid number of arguments for maximumPitchAngle");
        maxPitchDown = (float) (double.Parse(parameters[0]) * (Math.PI / 180.0f) / Math.PI);
        maxPitchUp = (float) (double.Parse(parameters[1]) * (Math.PI / 180.0f) / Math.PI);
      }

      // ---------------------------------------------------------------------
      float applyBankCorrectedInput(float bankAngle, float input)
      {
        if (bankAngle < -maxBankLeft) {
          return (bankAngle+maxBankLeft) * bankCorrectionMultiplier + Math.Min(input, 0);
        }
        else if (bankAngle > maxBankRight) {
          return (bankAngle-maxBankRight) * bankCorrectionMultiplier + Math.Max(input, 0);
        }

        return input;
      }

      // ---------------------------------------------------------------------
      float applyPitchCorrectedInput(float pitchAngle, float input)
      {
        if (pitchAngle < -maxPitchDown) {
          return (pitchAngle+maxPitchDown) * pitchCorrectionMultiplier + Math.Min(input, 0);
        }
        else if (pitchAngle > maxPitchUp) {
          return (pitchAngle - maxPitchUp) * pitchCorrectionMultiplier + Math.Max(input, 0);
        }
        return input;
      }

      // ---------------------------------------------------------------------
      float applyBankInput(float bankAngle, float input)
      {
        return input;
      }

      // ---------------------------------------------------------------------
      float applyPitchInput(float pitchAngle, float input)
      {
        return input;
      }

      // ---------------------------------------------------------------------
      public void run(Vector3 gravityDirection)
      {
        float yawInput   = fc.controller.RotationIndicator.Y;
        float pitchInput = pitchHandler(gravityDirection.Dot(fc.controller.WorldMatrix.Backward), fc.controller.RotationIndicator.X);
        float bankInput  = bankHandler(gravityDirection.Dot(fc.controller.WorldMatrix.Left), fc.controller.RollIndicator);
        
        fc.turnL(new Vector3D(pitchInput, yawInput, bankInput));
      }
    }
    #endregion Program
  }
} // End of namespace SpaceEngineers
