# RevitWindDrivenRain

**RevitWindDrivenRain** is an Autodesk Revit add-in for geometry-resolved wind-driven rain (WDR) exposure assessment on building façades. It is designed to combine building geometry with a trajectory-based method to estimate and visualise spatial differences in façade exposure, within a workflow informed by ISO 15927-3.

> **Status and scope:** The repository includes a ZIP archive of the software project. The instructions below describe the expected workflow, but should be checked against the exact files in the archive. Compatibility has not been established for every Revit release or Windows configuration.

## Repository and archived release

- **Source repository:** https://github.com/richacquah1/RevitWindDrivenRain
- **Project ZIP:** [RevitWindDrivenRain.zip](RevitWindDrivenRain.zip)
- **Archived release (Zenodo):** https://doi.org/10.5281/zenodo.17678839
- **License:** MIT (see [LICENSE](LICENSE))

The Zenodo archive is intended to serve as the permanent, versioned record of the software. Once a matching GitHub release/tag is published, it should correspond to the same source snapshot as the archived version.

## Main purpose

The add-in is intended to support façade-level WDR exposure assessment by:

- using building geometry to derive spatial differences in exposure across façade surfaces;
- applying a trajectory-based, ray-tracing approach to represent wind-driven rain exposure;
- combining geometry-derived exposure with climatic inputs and relevant ISO 15927-3 calculation factors; and
- displaying calculated exposure results in the Revit environment.

For the exact methods, assumptions, evaluation, and scientific interpretation, consult the associated publication:

Acquah, R., Niklewski, J., & Sandak, J. (2026). *BIM-based wind-driven rain modelling using ISO 15927-3 and geometry-based exposure*. **Building and Environment, 295**, 114447. https://doi.org/10.1016/j.buildenv.2026.114447

## Requirements and compatibility

The software metadata for the archived version identifies the following target environment:

- Windows
- Autodesk Revit 2023
- .NET Framework 4.8.1
- Autodesk Revit API assemblies: `RevitAPI.dll` and `RevitAPIUI.dll`
- C# and WPF/XAML

**Compatibility limitations:** RevitWindDrivenRain is a Revit add-in and relies on Autodesk Revit APIs and Windows-specific desktop UI technology. Native macOS support is not provided. Compatibility with Revit versions other than Revit 2023, or with other Windows/.NET configurations, has not been established and should not be assumed without testing.

## Installation

First determine whether the downloaded archive contains a **compiled add-in** or only the **source project**. These require different workflows.

### If the archive includes a compiled add-in

1. Close Autodesk Revit.
2. Download and extract [RevitWindDrivenRain.zip](RevitWindDrivenRain.zip) or the matching archived release from [Zenodo](https://doi.org/10.5281/zenodo.17678839).
3. Check for the add-in assembly (`.dll`), the Revit `.addin` manifest, and all required supporting DLLs/data files. Do not copy only the main DLL if the release includes dependencies.
4. Follow the instructions supplied with the release for installing the assembly and manifest. For Revit 2023, the user add-ins folder is normally:
   `%AppData%\Autodesk\Revit\Addins\2023\`
5. Start Revit and check whether the add-in appears and loads. If it does not, review the assembly path, manifest, required DLLs, and target .NET framework.

Do not create a manifest using guessed class names or paths; its values must match the actual compiled add-in.

### If the archive contains source code only

Follow the steps in [Building from source](#building-from-source). A source project must be compiled and installed before it can run in Revit.

## Building from source

A buildable Visual Studio solution/project and the required source files must be present in the archive to compile the add-in.

1. Install Visual Studio with the **.NET desktop development** workload.
2. Extract the ZIP archive and open the supplied solution (`.sln`) or project (`.csproj`) file.
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
