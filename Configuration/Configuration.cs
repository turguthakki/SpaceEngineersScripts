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
  public class Configuration
  {
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public class Option
    {
      public delegate void HandlerDelegate(params string[]values);
      public delegate void OnChangedDelegate();
      public string name {get; private set;}
      public string[] parameters {get; protected set;} = new string[] {};
      public HandlerDelegate handlers {get; set;} = delegate{};
      public OnChangedDelegate onChanged {get; set;} = delegate{};

      // ---------------------------------------------------------------------
      public Option(string name, HandlerDelegate handlers = null, params string []parameters)
      {
        this.name = name;
        if (handlers != null)
          this.handlers = handlers;
        setParameters(parameters);
      }     

      // ---------------------------------------------------------------------
      public void setParameters(params string [] parameters)
      {
        handlers(this.parameters = parameters);
      }
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public class Boolean : Option
    {
      const string @true = "true";
      const string @false = "false";
      bool _value;

      // ---------------------------------------------------------------------
      public static implicit operator bool(Boolean b)
      {
        return b._value;
      }

      // ---------------------------------------------------------------------
      public bool @value
      {
        get {return _value;}
        set {
          if (value != _value) {
            _value = value;
            parameters[0] = _value ? @true : @false;
            onChanged();
          }
        }
      }

      // ---------------------------------------------------------------------
      public Boolean(string name, bool defaultValue = false) : base(name)
      {
        parameters = new string[] {defaultValue ? @true : @false};
        handlers = handler;
        value = defaultValue;
      }

      // ---------------------------------------------------------------------
      void handler(string[] parameters)
      {
        if (parameters.Length == 1) {
          switch(parameters[0]) {
            case "on" :
            case @true : value = true; break;
            case "off" :
            case @false : value = false; break;
            case "toggle" : value = !value; break;
            default : throw new ArgumentException("Invalid argument for boolean : " + name + "(" + parameters[0] + ")");
          }
        }
        else {
          throw new ArgumentException("Invalid number of arguments for boolean : " + name + "(" + string.Join(", ", parameters) + ")");
        }
      }

    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public class Integer : Option
    {
      int _value;

      // ---------------------------------------------------------------------
      public static implicit operator int(Integer b)
      {
        return b._value;
      }

      // ---------------------------------------------------------------------
      public int @value
      {
        get {return _value;}
        set {
          if (value != _value) {
            _value = value;
            parameters[0] = _value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            onChanged();
          }
        }
      }

      // ---------------------------------------------------------------------
      public Integer(string name, int defaultValue = default(int)) : base(name)
      {
        parameters = new string[] {defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture)};
        handlers = handler;
        value = defaultValue;
      }

      // ---------------------------------------------------------------------
      void handler(string[] parameters)
      {
        if (parameters.Length == 1) {
          _value = Int32.Parse(parameters[0]);
        }
        else {
          throw new ArgumentException("Invalid number of arguments for integer : " + name + "(" + string.Join(", ", parameters) + ")");
        }
      }

    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public class Numeric : Option
    {
      double _value;

      // ---------------------------------------------------------------------
      public static implicit operator double(Numeric b)
      {
        return b._value;
      }

      // ---------------------------------------------------------------------
      public static implicit operator float(Numeric b)
      {
        return (float) b._value;
      }

      // ---------------------------------------------------------------------
      public double @value
      {
        get {return _value;}
        set {
          if (value != _value) {
            _value = value;
            parameters[0] = _value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            onChanged();
          }
        }
      }

      // ---------------------------------------------------------------------
      public Numeric(string name, double defaultValue = default(int)) : base(name)
      {
        parameters = new string[] {defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture)};
        handlers = handler;
        value = defaultValue;
      }

      // ---------------------------------------------------------------------
      void handler(string[] parameters)
      {
        if (parameters.Length == 1) {
          _value = Int32.Parse(parameters[0]);
        }
        else {
          throw new ArgumentException("Invalid number of arguments for numeric : " + name + "(" + string.Join(", ", parameters) + ")");
        }
      }
    }

    public MyGridProgram program {get; private set;}
    public Dictionary<string, Option> options = new Dictionary<string, Option>();

    // -----------------------------------------------------------------------
    public Configuration(MyGridProgram program, params Option[] handlers)
    {
      this.program = program;

      for (int i = 0; i < handlers.Length; i++) {
        Option handler = handlers[i];
        if (!string.IsNullOrEmpty(handler.name))
          this.options[handler.name] = handler;
      }

      readConfig();
    }

    // -----------------------------------------------------------------------
    public void readConfig()
    {
      MyIni ini = new MyIni();
      
      if (ini.TryParse(program.Me.CustomData)) {
        List<MyIniKey> configKeys = new List<MyIniKey>();
        ini.GetKeys("Configuration", configKeys);
        foreach(MyIniKey key in configKeys) {
          Option handler;
          if (options.TryGetValue(key.Name, out handler)) {
            string[] parameters = ini.Get(key).ToString().Split(',');
            for (int p = 0; p < parameters.Length; p++) {
              parameters[p] = parameters[p].Trim();
            }
            handler.setParameters(parameters);
          }

        }
      }
      else {
        program.Echo("Warning : Failed to parse configuration");
      }
    }

    // -----------------------------------------------------------------------
    public void writeConfig()
    {
      string iniString = "[Configuration]\n";
      foreach(KeyValuePair<string, Option> o in options) {
        iniString += o.Key + "=" + string.Join(", ", o.Value.parameters) + '\n';
      }
      program.Me.CustomData = iniString;
    }

    // -----------------------------------------------------------------------
    public void processCommands(string commandLine)
    {
      try {
        string[] commands = commandLine.Split(';');

        for (int a = 0; a < commands.Length; a++) {
          string command = commands[a];
          int paramStart = command.IndexOf('(');
          int paramEnd = command.IndexOf(')');

          if (command.Length == 0)
            continue;

          if (paramStart < 0 || paramEnd < 0) {
            throw new Exception("Bad command : " + command);
          }

          string name = command.Substring(0, paramStart).Trim();

          Option handler;
          if (options.TryGetValue(name, out handler)) {
            string[] parameters = command.Substring(paramStart+1, paramEnd-paramStart-1).Split(',');
            for (int p = 0; p < parameters.Length; p++) {
              parameters[p] = parameters[p].Trim();
            }
            handler.setParameters(parameters);
          }
          else {
            switch(name) {
              case "writeConfig" : writeConfig(); break;
              case "readConfig" : readConfig(); break;
              default : throw new Exception("Unknown command : " + name);
            }
          }
        }
        program.Echo("Ok");
      }
      catch(System.Exception e) {
        program.Echo("Error : " + Environment.NewLine + "  " + e.Message);
      }

    }
  }
 
  #endregion Library
} // End of namespace SpaceEngineers
