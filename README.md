# flowtracker2-field-data-plugin

[![Build status](https://ci.appveyor.com/api/projects/status/vfsxbalu9pafgeab/branch/master?svg=true)](https://ci.appveyor.com/project/SystemsAdministrator/flowtracker2-field-data-plugin/branch/master)

An AQTS field data plugin supporting FlowTracker2 measurement files, for AQTS 2017.4-or-newer systems.

## Want to install this plugin?

- Use of this plugin implies agreement with the [license terms of this repository](./LICENSE.txt) and with license terms for software components provided by [SonTek/YSI](src/External/SonTek.StandaloneDataParser.License.md).
- Download the latest release of the plugin [here](../../releases/latest)
- Install it using the [FieldVisitPluginTool](https://github.com/AquaticInformatics/aquarius-field-data-framework/tree/master/src/FieldDataPluginTool)

## Requirements for building the plugin from source

- Requires Visual Studio 2017 (Community Edition is fine)
- .NET 4.7 runtime

## Building the plugin

- Load the `src\FlowTracker2.sln` file in Visual Studio and build the `Release` configuration.
- The `src\FlowTracker2Plugin\deploy\Release\FlowTracker2Plugin.plugin` file can then be installed on your AQTS app server.

## Testing the plugin within Visual Studio

Use the included `PluginTester.exe` tool from the `Aquarius.FieldDataFramework` package to test your plugin logic on the sample files.

1. Open the FlowTracker2Plugin project's **Properties** page
2. Select the **Debug** tab
3. Select **Start external program:** as the start action and browse to `"src\packages\Aquarius.FieldDataFramework.17.4.3\tools\PluginTester.exe`
4. Enter the **Command line arguments:** to launch your plugin

```
/Plugin=FlowTracker2Plugin.dll /Json=AppendedResults.json /Data=..\..\..\..\data\DemoData.ft
```

The `/Plugin=` argument can be the filename of your plugin assembly, without any folder. The default working directory for a start action is the bin folder containing your plugin.

5. Set a breakpoint in the plugin's `ParseFile()` methods.
6. Select your plugin project in Solution Explorer and select **"Debug | Start new instance"**
7. Now you're debugging your plugin!

See the [PluginTester](https://github.com/AquaticInformatics/aquarius-field-data-framework/tree/master/src/PluginTester) documentation for more details.

## Installation of the plugin

Use the [FieldDataPluginTool](https://github.com/AquaticInformatics/aquarius-field-data-framework/tree/master/src/FieldDataPluginTool) to install the plugin on your AQTS app server.
