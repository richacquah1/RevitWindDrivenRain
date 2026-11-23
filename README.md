# RevitWindDrivenRain

**RevitWindDrivenRain** is an Autodesk Revit add-in for geometry-resolved wind-driven rain (WDR) exposure assessment on building façades. It is designed to combine building geometry with a trajectory-based method to estimate and visualise spatial differences in façade exposure, within a workflow informed by ISO 15927-3.

> **Source availability:** The complete Visual Studio source project is available as browsable files in [`source/RevitWindDrivenRain/`](source/RevitWindDrivenRain/). The ZIP archive has been removed from this repository; the archived v1 release remains available through Zenodo.

## Repository and archived release

- **Source repository:** https://github.com/richacquah1/RevitWindDrivenRain
- **Source project folder:** [source/RevitWindDrivenRain/](source/RevitWindDrivenRain/)
- **Visual Studio solution:** [RevitWindDrivenRain.sln](source/RevitWindDrivenRain/RevitWindDrivenRain.sln)
- **Visual Studio project:** [RevitWindDrivenRain.csproj](source/RevitWindDrivenRain/RevitWindDrivenRain.csproj)
- **Archived release (Zenodo):** https://doi.org/10.5281/zenodo.17678839
- **License:** MIT (see [LICENSE](LICENSE))

The Zenodo record identifies the archived software version as **v1**. For clear version provenance, a GitHub release tagged `v1` should identify the repository snapshot containing the corresponding software distribution and link to the Zenodo record above. The tag must not be reused for later source changes; subsequent versions should receive new tags and their own archived records.

## Source code

The browsable C# and WPF/XAML project files are grouped under [`source/RevitWindDrivenRain/`](source/RevitWindDrivenRain/). The main source folders include:

- [WDR command implementation](source/RevitWindDrivenRain/Command_WDR/)
- [Ray-casting implementation](source/RevitWindDrivenRain/Command_Ray%20Casting/)
- [Simulation commands](source/RevitWindDrivenRain/Command_Simulations/)
- [Forms and user interface](source/RevitWindDrivenRain/Forms/)
- [Utilities](source/RevitWindDrivenRain/Utilities/)
- [Resources](source/RevitWindDrivenRain/Resources/)
- [Highlight components](source/RevitWindDrivenRain/Highlight/)
- [Test codes](source/RevitWindDrivenRain/Test%20Codes/)

The [Visual Studio solution](source/RevitWindDrivenRain/RevitWindDrivenRain.sln) and [project file](source/RevitWindDrivenRain/RevitWindDrivenRain.csproj) are in the source folder. You can browse individual source files directly on GitHub or clone the repository to inspect and build the project. Generated Visual Studio folders and local build outputs are excluded.

## Main purpose

The add-in is intended to support façade-level WDR exposure assessment by:

- using building geometry to derive spatial differences in exposure across façade surfaces;
- applying a trajectory-based, ray-tracing approach to represent wind-driven rain exposure;
- combining geometry-derived exposure with climatic inputs and relevant ISO 15927-3 calculation factors; and
- displaying calculated exposure results in the Revit environment.

For the exact methods, assumptions, evaluation, and scientific interpretation, consult the associated publication:

Acquah, R., Niklewski, J., & Sandak, J. (2026). *BIM-based wind-driven rain modelling using ISO 15927-3 and geometry-based exposure*. **Building and Environment, 295**, 114447. https://doi.org/10.1016/j.buildenv.2026.114447

## Requirements and compatibility

The documented development environment for the archived software version is:

- **Operating system:** Windows
- **BIM application:** Autodesk Revit 2023
- **Development language:** C#
- **Target framework:** .NET Framework 4.8.1
- **UI technology:** Windows Presentation Foundation (WPF)
- **Revit dependencies:** `RevitAPI.dll` and `RevitAPIUI.dll`
- **Additional dependencies:** External .NET libraries declared in the project configuration

The source code is provided to support inspection, modification and adaptation to other Autodesk Revit releases. RevitWindDrivenRain relies on the Revit API and the .NET runtime supported by the target Revit version. Consequently, adapting the project to another release may require changes to the project configuration, API references, target framework, external dependencies and deployment settings.

## Installation

**This release contains source code only. It does not include a precompiled add-in assembly (`.dll`) ready to install in Revit.** No ZIP archive is distributed in this repository. The Zenodo record provides the archived v1 software release.

To use the add-in, first build the source project in Visual Studio as described in [Building from source](#building-from-source). After a successful build, the resulting assembly and its dependencies must be installed and registered with Revit using a correctly configured `.addin` manifest.

For Revit 2023, the per-user add-ins folder is normally:

`%AppData%\Autodesk\Revit\Addins\2023\`

The manifest must point to the actual built assembly and identify the correct add-in entry point. Do not assume that the repository's manifest is ready to use unchanged: inspect its assembly path and class/entry-point details, and update the path to match the location of your build output. Copy any required runtime dependencies alongside the assembly as appropriate.

The repository does not provide a precompiled binary, so users must have a compatible Revit installation and development environment to build and run the software.

## Building from source

The repository contains the browsable Visual Studio solution/project and source files under [`source/RevitWindDrivenRain/`](source/RevitWindDrivenRain/) required to inspect and build the add-in.

1. Install Visual Studio with the **.NET desktop development** workload.
2. Clone or download this repository and open [RevitWindDrivenRain.sln](source/RevitWindDrivenRain/RevitWindDrivenRain.sln) in Visual Studio.
3. Confirm that the target framework matches the release requirements.
4. Set the references to `RevitAPI.dll` and `RevitAPIUI.dll` from the local Autodesk Revit 2023 installation.
5. Restore NuGet dependencies as described below.
6. Build the project and resolve any missing references or dependency errors.
7. Test the resulting add-in in Revit 2023 before using it for research or project decisions.

### Dependencies and the removed `packages` folder

The repository does **not** include the NuGet `packages` folder. For a project that declares its NuGet dependencies correctly, this is usually acceptable: the packages can be restored when the project is built. It reduces the need to commit downloaded package files to source control.

To restore packages in Visual Studio:

1. Open the solution.
2. Use **Restore NuGet Packages** from the solution's context menu if that option is available, or build the solution to trigger automatic restore.
3. Wait for restore to finish, then build again and check the Error List for missing assemblies or unresolved references.

Package restoration generally requires internet access unless the required packages are already available in a local package cache or source. If restore fails, check that the project still contains its NuGet dependency declarations (for example, `packages.config` or `PackageReference`) and that package sources are configured. If the project file directly references DLLs inside a deleted `packages` directory without a working restore configuration, the project file or references will need to be repaired.

**Important distinction:** NuGet packages and Autodesk's Revit API assemblies are not the same thing. Restoring NuGet packages does not install Revit or provide `RevitAPI.dll` and `RevitAPIUI.dll`; users building the add-in need a compatible local Revit installation and the corresponding API references. Runtime dependencies required by the compiled add-in must also be available when the add-in is loaded.

These instructions describe the general workflow. They do not by themselves establish that the supplied project builds successfully; a clean restore/build/test should be performed before treating it as verified.

## Compatibility with other Revit versions

The archived project is configured for Revit 2023. The source code may be adapted to other Revit versions, subject to the API and runtime requirements of the target release.

For Revit versions that use .NET Framework, adaptation generally involves:

1. Installing the target Revit version and its corresponding API development components.
2. Updating the `RevitAPI.dll` and `RevitAPIUI.dll` references to the assemblies supplied with that version.
3. Updating the project configuration and target framework where necessary.
4. Reviewing API calls and external dependencies for version compatibility.
5. Updating the add-in manifest and deployment paths.
6. Rebuilding the project and testing its commands, calculations, geometry processing, data loading and visualisation in the target Revit version.

Revit 2025 introduced a transition from .NET Framework to .NET 8. Add-ins targeting Revit 2025 or later therefore require a .NET 8-compatible build. Migration may involve converting the project configuration, reviewing WPF and Windows Forms components, updating external libraries and addressing changes in .NET and assembly loading. A single codebase can be maintained for multiple Revit releases using appropriate version-specific build configurations.

These adaptation procedures describe how the source project can be prepared for other Revit versions. They do not imply that every version has been independently tested or that the original Revit 2023 build can be used unchanged in newer releases.

## Operating-system support

RevitWindDrivenRain uses Autodesk Revit and Windows-specific desktop technologies, including Windows Presentation Foundation (WPF). The software is intended for Windows environments that support the corresponding Autodesk Revit release and its required .NET runtime. Native macOS support is not provided. Running Windows and Revit through a virtualised environment on an Apple computer is a separate configuration and requires its own compatibility assessment.

## Inputs, outputs, and scientific use

The add-in is intended for geometry-resolved WDR estimation and visualisation. Results depend on the building geometry, selected analysis settings, climatic inputs, discretisation/sampling choices, and the assumptions of the implemented calculation method. It should not be interpreted as a detailed CFD simulation of airflow, turbulence, flow separation, or aerodynamic droplet transport.

Use the accompanying research publication and the source code to understand the implemented method and its limitations. Check inputs, units, assumptions, and results for the intended use case.

## Citation

When using the software, please cite the archived release:

Acquah, R., Niklewski, J., & Sandak, J. (2025). *RevitWindDrivenRain* [Software]. Zenodo. https://doi.org/10.5281/zenodo.17678839

Also cite the associated methods/application publication where relevant:

Acquah, R., Niklewski, J., & Sandak, J. (2026). *BIM-based wind-driven rain modelling using ISO 15927-3 and geometry-based exposure*. **Building and Environment, 295**, 114447. https://doi.org/10.1016/j.buildenv.2026.114447

## Support

For questions about the software, contact Richard Acquah at richard.acquah@innorenew.eu.
