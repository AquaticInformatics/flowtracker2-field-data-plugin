# flowtracker2-field-data-plugin

[![Build status](https://ci.appveyor.com/api/projects/status/vfsxbalu9pafgeab/branch/master?svg=true)](https://ci.appveyor.com/project/SystemsAdministrator/flowtracker2-field-data-plugin/branch/master)

An AQTS field data plugin supporting FlowTracker2 measurement files, for AQTS 2017.4-or-newer systems.

## Want to install this plugin?

- Use of this plugin implies agreement with the [license terms of this repository](./LICENSE.txt) and with license terms for software components provided by [SonTek/YSI](src/External/SonTek.StandaloneDataParser.License.md).
- Download the latest release of the plugin [here](../../releases/latest)
- Install it using the [SystemConfig page](https://github.com/AquaticInformatics/aquarius-field-data-framework#need-to-install-a-plugin-on-your-aqts-app-server)

## Plugin Compatibility Matrix

It is recommended that you use the most recent version of the plugin which matches your AQTS server version.

| AQTS Version | Latest compatible plugin Version |
| --- | --- |
| AQTS 2020.2+ | [v20.2.0](https://github.com/AquaticInformatics/flowtracker2-field-data-plugin/releases/download/v20.2.0/FlowTracker2Plugin.plugin) - Adds configurable ISO Uncertainty Scalar |
| AQTS 2019.2 - 2020.1 | [v19.2.13](https://github.com/AquaticInformatics/flowtracker2-field-data-plugin/releases/download/v19.2.13/FlowTracker2Plugin.plugin) - Adds Party and Primary Meter to channel measurement<br/>v19.2.12 - Adds incrementing vertical numbers |
| 2017.4 - 2019.1 | [v17.4.44](https://github.com/AquaticInformatics/flowtracker2-field-data-plugin/releases/download/v17.4.44/FlowTracker2Plugin.plugin) |

## Configuring the plugin

Starting with version 20.2.0, there are some configurable plugin settings which can be set in the Settings tab of the System Config Page.

| Group | Key | Value | Description |
| --- |--- |--- | --- |
| `FieldDataPluginConfig-FlowTracker2Plugin` | `IsoUncertaintyScalar` | `1.0` | This value will be used to scale the FlowTracker2 ISO uncertainty measurement, which defaults to a single standard devation (68% confidence interval).<br/><br/>A value of `1.96` will scale the uncertainty into a two-standard-deviation value (95% confidence interval).<br/><br/>Defaults to `1.0` if the value is missing or is not a valid number. |

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

Use the SystemConfig page to install/enable this plugin on your AQTS app server.
