this repository contains code samples for a windows and android app to control a robot arm

*what I've included is shared with permission from Instinctive Robotics*

## About the code samples ##
In this repo are samples of code from a greater repository of 100K+ lines of code.  All of which is my own work.  The intent of sharing this is **only** to provide an idea of my architecture and coding style for people interested in my work.
The larger codebase, which this samples, was designed to allow building cross platform apps to work with an arduino based robot arm.

* `Mimic` contains a few select XAML and MVVM examples
* `zArm.Api` contains a few select API classes used to control the robot arm
* `zArm.ApiTest` contains a few select unit-tests for zArm.Api
* `zArm.Simulation` contains a few select classes used in running a 3D interactive simulator of the robot arm


## UI Wire-up ##
the UI consists of modules that plug together.  the following is the dependency wire-up for the entire UI.  It also happens to be the only code-behind in the entire WPF application.  all the rest of the code is orchestrated through pure XAML and MVVM design patterns.
```C#
            //create application entities
            var armListener = new ArmListener(new UsbConnection(this), new SimulationConnection());
            var connection = new ConnectionVM(armListener);
            var settings = new SettingsVM();
            var modules = new ModulesVM(connection, settings,
                new ModuleVM(null, typeof(Menu)) { Transition = TransitionType.Right },
                new ModuleVM("Build Instructions", typeof(BuildInstructionsControl), typeof(BuildInstructionsVM)) { BackgroundColor = ModuleColors.Green, IconName = "appbar_tools" },
                new ModuleVM("Flash Firmware", typeof(FirmwareControl), typeof(FirmwareVM)) { BackgroundColor = ModuleColors.Teal, IconName = "appbar_lightning"  },
                new ModuleVM("Calibration", typeof(CalibrationControl), typeof(CalibrationVM)) { BackgroundColor = ModuleColors.Purple, RobotRequired = true, IconName = "appbar_axis_three" },
                new ModuleVM("Motion Studio", typeof(MotionStudioControl), typeof(MotionStudioVM)) { LargeTile = true, RobotRequired = true, CalibrationRequired = true, BackgroundColor = ModuleColors.Orange, ImageName = "Record.png" },
				new ModuleVM("Robot Settings", typeof(RobotSettingsControl), typeof(RobotSettingsVM)) { BackgroundColor = ModuleColors.Blue, RobotRequired = true, IconName = "appbar_cogs" },
                new ModuleVM("Testing & Troubleshooting", typeof(TestingControl), typeof(TestingVM)) { BackgroundColor = ModuleColors.Purple, RobotRequired = true, ImageName = "Testing.png" },
				new ModuleVM("Record & Playback", typeof(RecordAndPlaybackControl), typeof(RecordAndPlaybackVM)) { BackgroundColor = ModuleColors.Teal, ImageName = "RecordAndPlayback.png", RobotRequired = true, CalibrationRequired = true },
				new ModuleVM("Learning & Education", null) { BackgroundColor = ModuleColors.Green },
                new ModuleVM("Program with Scratch", typeof(Scratch), typeof(ScratchVM)) { BackgroundColor = ModuleColors.Blue, ImageName = "Scratch.png", RobotRequired = true, CalibrationRequired = true },
                new ModuleVM("Simulator", typeof(LaunchSimControl), typeof(LaunchSimVM)) { BackgroundColor = ModuleColors.Blue, ImageName = "Simulator.png" },
                new ModuleVM("Maestro", null) { LargeTile = true, BackgroundColor = ModuleColors.Blue, ImageName= "Maestro.png" },
                new ModuleVM("Sketch Artist", typeof(DrawingControl), typeof(DrawingVM)) { LargeTile = true, BackgroundColor = ModuleColors.Orange, ImageName = "SketchArtist.png", RobotRequired = true, CalibrationRequired = true },
                new ModuleVM("Tic-Tac-Toe", null) { BackgroundColor = ModuleColors.Purple },
                new ModuleVM("Sock Puppet", null) { LargeTile = true, BackgroundColor = ModuleColors.Teal, ImageName = "SockPuppet.png" },
                new ModuleVM("Program with If-Then", null) { BackgroundColor = ModuleColors.Purple },
                new ModuleVM("Pick & Place", null) { BackgroundColor = ModuleColors.Purple },
                new ModuleVM("Virtual Reality", null) { BackgroundColor = ModuleColors.Purple }
                );
            App.Instance = new MainWindowVM(this, connection, settings, modules, new StorageUI());

            //databind
            this.DataContext = App.Instance;
```


## Folder Structure ##
this is a simplified folder structure of the project.  the root levels indicate .NET assemblies and sub-folder are name-spaces.
I believe you can tell a lot from a well structured project.  The main WPF presentation project is called Mimic.  Its clear to see separation of concerns at the assembly level between the robots API and its presentation layer, tests, simulations, and platform specific implementation
```
├───Mimic
│   ├───Adorners
│   ├───Behaviors
│   ├───Converters
│   ├───Images
│   ├───Properties
│   ├───Resources
│   └───ViewModel
├───Mimic.Setup
│   ├───Debug
│   └───Release
├───Mimic.Simulator
│   └───ViewModel
├───packages
│   ├───ArduinoUploader.2.1.0
│   ├───Castle.Core.4.0.0
│   ├───CefSharp.Wpf.57.0.0
│   ├───gong-wpf-dragdrop.1.1.0
│   ├───IntelHexFormatReader.2.1.0
│   ├───MahApps.Metro.1.5.0
│   ├───MahApps.Metro.Resources.0.6.1.0
│   ├───Moq.4.7.10
│   ├───Moq.Sequences.NoNUnit.1.0.1
│   ├───NamedPipeWrapper.1.5.0
│   ├───Newtonsoft.Json.10.0.3
│   ├───NLog.4.4.11
│   ├───PropertyTools.Wpf.2.0.1
│   ├───SharpDX.3.1.1
│   ├───SharpDX.XInput.3.1.1
│   ├───Svg.2.3.0
│   ├───System.Reactive.3.1.1
│   ├───System.Reactive.Core.3.1.1
│   ├───System.Reactive.Interfaces.3.1.1
│   ├───System.Reactive.Linq.3.1.1
│   ├───System.Reactive.PlatformServices.3.1.1
│   ├───System.Reactive.Windows.Threading.3.1.1
│   ├───UrhoSharp.1.5.20
│   ├───UrhoSharp.Wpf.1.5.20
│   ├───WindowsFirewallHelper.1.0.0.0
│   ├───Xamarin.Forms.2.4.0.280
│   └───XBoxController.1.0.5.0
├───Scratch
├───TestResults
├───zArm.Api
│   ├───Behaviors
│   ├───Commands
│   ├───Motion
│   └───Specialized
├───zArm.Api.Upload
├───zArm.Api.Windows
├───zArm.ApiTests
├───zArm.Behaviors
├───zArm.Graphics
│   ├───Interpolation
├───zArm.IK
│   ├───Caliko
├───zArm.Simulation
│   ├───Actions
├───zArm.Simulation.Emulator
├───zArm.Simulation.TestEnv
├───zArm.Simulation.UITests
├───zArm.Simulation.WPF
├───zArm.UI
│   ├───TicTacToe
│   └───ViewModel
├───zArm.UI.Android
│   ├───Assets
└───zArm.UITests
```
