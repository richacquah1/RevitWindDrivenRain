# RevitWindDrivenRain

**RevitWindDrivenRain** is an Autodesk Revit add-in for geometry-resolved wind-driven rain (WDR) exposure assessment on building façades. It is designed to combine building geometry with a trajectory-based method to estimate and visualise spatial differences in façade exposure, within a workflow informed by ISO 15927-3.

> **Status and scope:** This repository is being prepared to accompany the archived software release and associated research publication. Please check the archived release and its included files for the source code and build artefacts. The documentation does not imply that the software has been independently tested on every Revit release or Windows configuration.

## Repository and archived release

- **Source repository:** https://github.com/richacquah1/RevitWindDrivenRain
- **Archived release (Zenodo):** https://doi.org/10.5281/zenodo.17678839
- **License:** MIT (see [LICENSE](LICENSE))

The Zenodo archive is the permanent, versioned record of the software. Once a matching GitHub release/tag is published, it should correspond to the same source snapshot as the archived version.

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

**Compatibility limitations:** RevitWindDrivenRain is a Revit add-in and relies on Autodesk Revit APIs and Windows-specific desktop UI technology. Native macOS support is not provided. Compatibility with Revit versions other than Revit 2023, or with other Windows/.NET configurations, has not been established here and should not be assumed without testing.

## Installation

The exact installation procedure depends on the files included in the archived release. Use the following general workflow only if the archive contains a compiled add-in and its required dependencies:

1. Close Autodesk Revit.
2. Download and extract the software archive from [Zenodo](https://doi.org/10.5281/zenodo.17678839).
3. Locate the compiled add-in assembly (`.dll`) and any files required by the release. Do not copy only the main DLL if the release includes dependencies or data files.
4. Create or use the appropriate Revit add-ins folder for Revit 2023, normally:
   `%AppData%\Autodesk\Revit\Addins\2023\`
5. Place the add-in assembly and required supporting files in the installation location, and create a Revit `.addin` manifest in that folder that points to the assembly and the add-in's entry-point class. The manifest values must match the actual release; do not use placeholder values.
6. Start Revit and check whether the add-in appears and loads. If Revit reports a load error, review the assembly path, manifest, referenced assemblies, and target .NET framework.

**Important:** This repository's installation notes must be checked against the actual archive and project structure before being treated as a definitive installation guide. If the Zenodo record contains source files only, build the solution in the appropriate Visual Studio environment and configure the references to the installed Revit 2023 API assemblies before preparing the add-in manifest.

## Building from source

A buildable Visual Studio solution/project and all required source/dependency files must be present in the repository or archive to compile the add-in. If included:

1. Install Visual Studio with the .NET desktop development workload.
2. Open the supplied solution/project.
3. Confirm the project target framework matches the release requirements.
4. Set references to the `RevitAPI.dll` and `RevitAPIUI.dll` assemblies from the local Autodesk Revit 2023 installation.
5. Build the project and resolve any missing references or dependency errors.
6. Test the resulting add-in in Revit 2023 before using it for research or project decisions.

No claim is made that the project will build from this README alone; the actual source project and all required files must be included and verified.

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
