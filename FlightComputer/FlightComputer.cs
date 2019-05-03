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
  #region Library

  // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
  public class FlightComputer
  {
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public class ThrusterGroup : List<IMyThrust>
    {
      List<IMyThrust> thrusters;

      public double maxEffectiveThrust {get; protected set;}
      public MatrixD worldMatrix => this[0].WorldMatrix;
      public Vector3D forward => worldMatrix.Forward;
      public Vector3D backward => worldMatrix.Backward;
      public Vector3D left => worldMatrix.Left;
      public Vector3D right => worldMatrix.Right;
      public Vector3I direction {get; private set;}

      // ---------------------------------------------------------------------
      public static implicit operator bool(ThrusterGroup group)
      {
        return group != null && group.Count > 0;
      }

      // ---------------------------------------------------------------------
      public ThrusterGroup(Vector3I direction, List<IMyThrust> allThrusters)
      {
        this.direction = direction;
        this.thrusters = allThrusters;
        updateMaxEffectiveThrust();
      }

      // ---------------------------------------------------------------------
      public void updateThrusters()
      {
        Clear();
        foreach(IMyThrust thruster in thrusters) {
          if (thruster.GridThrustDirection == -direction)
            Add(thruster);
        }
        updateMaxEffectiveThrust();
      }

      // ---------------------------------------------------------------------
      public void updateMaxEffectiveThrust()
      {
        maxEffectiveThrust = 0;
        foreach(IMyThrust thruster in this)
          maxEffectiveThrust += thruster.MaxEffectiveThrust;
      }

      // ---------------------------------------------------------------------
      public void setOverride(float v) 
      {
        v = Math.Max(0.00001f, v);
        foreach(IMyThrust thruster in this)
          thruster.ThrustOverride = v;
      }     

      // ---------------------------------------------------------------------
      public void setOverridePercentage(float v) 
      {
        v = Math.Max(0.00001f, v);
        foreach(IMyThrust thruster in this) {
          thruster.CustomName = thruster.ThrustOverridePercentage.ToString();
          thruster.ThrustOverridePercentage = v;
        }
      }     
    }

    public IMyShipController controller {get; private set;}
    public List<ThrusterGroup> thrusterGroups {get; private set;}
    public List<IMyGyro> gyros {get; private set;} = new List<IMyGyro>();
    public double mass {get; private set;} = 0;

    protected MyGridProgram program {get; private set;}
    List<IMyThrust> thrusters = new List<IMyThrust>();

    // -----------------------------------------------------------------------
    public FlightComputer(MyGridProgram program, IMyShipController controller = null)
    {
      this.program = program;

      if (controller == null) {
        List<IMyShipController> controllers = new List<IMyShipController>();
        program.GridTerminalSystem.GetBlocksOfType(controllers);
        int highScore = 0;
        foreach(IMyShipController c in controllers) {
          int score = (c.IsMainCockpit ? 10 : 0) + (c.CanControlShip ? 5 : 0 ) + (c.ControlThrusters ? 3 : 0) + (c.ControlWheels ? 1 : 0);
          if (controller == null || score > highScore) {
            controller = c;
            highScore = score;
          }
        }
      }

      this.controller = controller;
      update();
    }

    // -----------------------------------------------------------------------
    public void updateMass()
    {
      mass = controller.CalculateShipMass().PhysicalMass;
    }

    // -----------------------------------------------------------------------
    public void updateThrusters()
    {
      if (thrusterGroups == null) {
        thrusterGroups = new List<ThrusterGroup>() {
          new ThrusterGroup(Vector3I.Forward, thrusters),
          new ThrusterGroup(Vector3I.Backward, thrusters),
          new ThrusterGroup(Vector3I.Left, thrusters),
          new ThrusterGroup(Vector3I.Right, thrusters),
          new ThrusterGroup(Vector3I.Up, thrusters),
          new ThrusterGroup(Vector3I.Down, thrusters),
        };
      }
      thrusters.Clear();
      program.GridTerminalSystem.GetBlocksOfType(thrusters);
      foreach(ThrusterGroup group in thrusterGroups) {
        group.updateThrusters();
        group.updateMaxEffectiveThrust();
      }
    }

    // -----------------------------------------------------------------------
    public void updateGyros()
    {
      gyros.Clear();
      program.GridTerminalSystem.GetBlocksOfType(gyros);
      foreach (IMyGyro gyro in gyros) {
        gyro.GyroOverride = true;
        gyro.GyroPower = 1.0f;
      }
    }

    // -----------------------------------------------------------------------
    public void updateMaxEffectiveThrust()
    {
      foreach(ThrusterGroup group in thrusterGroups) {
        group.updateMaxEffectiveThrust();
      }
    }

    // -----------------------------------------------------------------------
    public void update()
    {
      updateMass();
      updateThrusters();
      updateGyros();
    }

    // -----------------------------------------------------------------------
    public double maxEffectiveThrustInDirection(Vector3 direction)
    {
      double rv = 0;
      foreach(ThrusterGroup group in thrusterGroups) if (group) {
        rv += Math.Max(0, group.backward.Dot(direction)) * group.maxEffectiveThrust;
      }
      return rv;
    }
  
    // -----------------------------------------------------------------------
    // Thrust vector is a world space direction vector
    public void thrustW(Vector3D vector)
    {
      double acceleration = vector.Normalize();
      foreach(ThrusterGroup group in thrusterGroups) if (group) {
        group.setOverridePercentage((float) (mass * acceleration * Math.Max(0, group.backward.Dot(vector)) / group.maxEffectiveThrust));
      }
    }

    // -----------------------------------------------------------------------
    // Thrust vector is a direction vector in local coordinates to vessel cocpit
    public void thrustL(Vector3D vector)
    {
      thrustW(Vector3D.TransformNormal(vector, controller.WorldMatrix));
    }

    // -----------------------------------------------------------------------
    // Turn rate in worldspace angles in radians per second
    public void turnW(Vector3D rotation)
    {
      foreach (IMyGyro gyro in gyros) {
        // Worldspace to gyro local
        Vector3 localInput = Vector3D.TransformNormal(rotation, Matrix.Transpose(gyro.WorldMatrix));
        gyro.Pitch = localInput.X;
        gyro.Yaw = localInput.Y;
        gyro.Roll = localInput.Z;
      }
    }

    // -----------------------------------------------------------------------
    // Turn rate in radians per second local to cockpit
    public void turnL(Vector3D rotation)
    {
      turnW(Vector3D.TransformNormal(rotation, controller.WorldMatrix));
    }
  }

  #endregion Library
} // End of namespace SpaceEngineers
