//this works. 3rd update of 16.03.24. Works perfect adds a WPF user interface
//works perfec. this is before inspecting commulative and totals

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System;
using System.IO;


namespace RevitWindDrivenRain
{
    using System;
    using System.Net;
    using System.Runtime.ConstrainedExecution;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using static IronPython.Modules.PythonWeakRef;
    using static RevitWindDrivenRain.WDRSimulationUtilities;

    public static class WDRSimulationUtilities
    {
        public static double CalculateDropsPerRayForSimulationTime(double rainfallIntensityMmPerHr, double raindropRadiusM, double spacingInMillimeters, double simulationTimeInMinutes)
        {
            // Step 1: Convert rainfall intensity to m³/m²h
            double rainfallIntensityM3PerM2H = rainfallIntensityMmPerHr * 0.001;

            // Step 2: Calculate raindrop volume
            double raindropVolumeM3 = (4.0 / 3.0) * Math.PI * Math.Pow(raindropRadiusM, 3);

            // Step 3: Determine area covered by each ray (convert spacing to meters first)
            double spacingInMeters = spacingInMillimeters * 0.001;
            double areaPerRayM2 = Math.Pow(spacingInMeters, 2);

            // Step 4: Convert simulation time from minutes to hours
            double simulationTimeInHours = simulationTimeInMinutes / 60.0;

            // Step 5: Calculate volume of rain per area for the specified simulation time
            double volumeOfRainPerRayAreaForSimulationTime = rainfallIntensityM3PerM2H * areaPerRayM2 * simulationTimeInHours;

            // Step 6: Calculate drops per ray for the simulation time
            double dropsPerRay = volumeOfRainPerRayAreaForSimulationTime / raindropVolumeM3;

            return dropsPerRay; // Number of drops that each ray represents. This is calculated from the rainfall intensity and ray spacing in mm
        }

        public class WDRIntensityCalculator
        {
            // Static fields to store the last calculated rainfall intensities
            public static double RainfallIntensityInCellAdditionalMmPerHr { get; private set; }
            public static double RainfallIntensityInCellOriginalMmPerHr { get; private set; }

            // Method to calculate rainfall intensity from the number of raindrops per square meter, considering the simulation time in minutes
            public static void CalculateAndStoreRainfallIntensityFromDropsPerSquareMeter(
                    int additionalRaysInCellPerSquareMeter, int originalRaysInCellPerSquareMeter, double raindropRadiusM, double simulationTimeInMinutes)
            {
                // Calculate the volume of a single raindrop in cubic meters
                double dropVolumeM3 = (4.0 / 3.0) * Math.PI * Math.Pow(raindropRadiusM, 3);

                // Calculate the depth per square meter for additional and original rays (since it's already divided by AreaM2)
                double depthInCellAdditionalM = dropVolumeM3 * additionalRaysInCellPerSquareMeter;
                double depthInCellOriginalM = dropVolumeM3 * originalRaysInCellPerSquareMeter;

                // Convert depth to millimeters (1 m = 1000 mm)
                double depthInCellAdditionalMm = depthInCellAdditionalM * 1000; // Converted directly to mm
                double depthInCellOriginalMm = depthInCellOriginalM * 1000; // Converted directly to mm

                // Convert simulation time from minutes to hours
                double simulationTimeInHours = simulationTimeInMinutes / 60.0;//simulationTimeInMinutes

                // Adjusting for time to get mm/hr
                RainfallIntensityInCellAdditionalMmPerHr = depthInCellAdditionalMm / simulationTimeInHours;
                RainfallIntensityInCellOriginalMmPerHr = depthInCellOriginalMm / simulationTimeInHours;
            }
        }


        // I Can later improve this by calculating the orientation of each geridcell to account for faces that are not one plane 
        // Calculation for theta
        public static double GetFaceOrientationRelativeToNorth(Face face)
        {
            // Get the normal vector of the face
            XYZ faceNormal = face.ComputeNormal(new UV(0, 0));

            // Project the normal onto the XY plane to ignore the Z component
            XYZ faceNormalXY = new XYZ(faceNormal.X, faceNormal.Y, 0).Normalize();

            // Calculate the angle between the face normal and the north direction (Y-axis in Revit)
            double angleRadians = Math.Atan2(faceNormalXY.Y, faceNormalXY.X);

            // Convert radians to degrees
            double faceOrientation = angleRadians * (180.0 / Math.PI);

            // Ensure the angle is in the range [0, 360)
            if (faceOrientation < 0)
            {
                faceOrientation += 360;
            }

            // Adjust the angle to map the correct compass directions
            // In Revit, Y-axis is North (0 degrees), X-axis is East (90 degrees)
            faceOrientation = (450 - faceOrientation) % 360;

            return faceOrientation;
        }



        public static double CalculateWDR(double C_T, double C_R, double O, double W, double v, double R_h, double D, double theta)
        {
            // Convert degrees to radians for cosine calculation
            double D_rad = D * (Math.PI / 180.0);
            double theta_rad = theta * (Math.PI / 180.0);

            // Calculate the cosine of the angle difference
            double cosTerm = Math.Cos(D_rad - theta_rad);

            // Calculate wind-driven rain (WDR) using the provided formula
            double R_wdr = (2.0 / 9.0) * C_T * C_R * O * W * v * Math.Pow(R_h, 8.0 / 9.0) * cosTerm;

            return R_wdr;
        }

        public static double CalculateWindSpeedatHeightofFirstIntersection(double heightatPoint, double v_ref, double h_ref, double alpha)
        {
            return v_ref * Math.Pow((heightatPoint / h_ref), alpha);
        }


    }


    [Transaction(TransactionMode.Manual)]
    public class PickedFaceRayProjectionCode_new : IExternalCommand
    {
        private DeleteAnalysisDisplayStylesHandler _deleteHandler = new DeleteAnalysisDisplayStylesHandler();
        private ExternalEvent _deleteEvent;
        private UIApplication _uiApp;

        private ExternalEvent _deleteStylesEvent;
        private string _selectedStyle;

        private Dictionary<string, GridCellInfo> gridCells;

        private readonly double[] directionAngles = new double[] {
        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
    };

        private StringBuilder logBuilder = new StringBuilder(); // For accumulating log messages

        bool generateOriginalRays = false;
        bool generateAdditionalRays = true;

        private double windSpeed; // Wind speed in m/s
        private double raindropRadiusM; // Raindrop radius in meters
        private double spacingInMillimeters;
        private double simulationPlaneHeight;
        private double rainfallIntensityMmPerHr;
        private double rotationDegrees;
        private int maxHitCount = 0;
        private double dropsPerRay;

        private int maxOriginalRayHits = 0;
        private int maxAdditionalRayHits = 0;

        private bool considerOriginalRays;
        private bool considerAdditionalRays;

        private string avfApproach;
        private string selectedStyle;

        private double[] rotationAngles;
        private double[] windSpeeds;
        private double[] RainfallIntensityMmPerHr;

        private double C_T = 1.0; // Topography coefficient
        private double C_R = 1.0; // Roughness coefficient
        private double O = 1.0;   // Obstruction factor
        private double h_ref = 10.0; // Reference height in meters
        private double alpha = 0.14; // Power law exponent
        private int simulationNumber = 1; // Initialize simulation number

        double rayLength = 100.0; // Define the length of the rays
        bool visualizeRays = false; // Set to 'true' to enable visualization by default

        private XYZ right;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _uiApp = commandData.Application;
            UIDocument uiDoc = _uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            if (_deleteStylesEvent == null)
            {
                _deleteHandler = new DeleteAnalysisDisplayStylesHandler();
                _deleteStylesEvent = ExternalEvent.Create(_deleteHandler);
            }

            // Define default values
            double defaultRotationAngle = 90.0;
            double defaultWindSpeed = 3.8;
            double defaultRaindropRadius = 0.001;
            double defaultRainfallIntensity = 1.0;
            double defaultSimulationTimeInMinutes = 5.0;
            double defaultSpacingInMillimeters = 5000.0;

            try
            {
                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces for AVF application.");
                if (selectedFaceRefs.Count == 0)
                {
                    message = "No faces selected.";
                    return Result.Cancelled;
                }

                string faceDetails = GetFaceDetails(doc, selectedFaceRefs);
                View3D view3D = doc.ActiveView as View3D;
                if (view3D == null || !view3D.IsSectionBoxActive)
                {
                    message = "Please make sure you are in a 3D view with an active section box.";
                    return Result.Failed;
                }

                BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
                XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
                double width = sectionBox.Max.X - sectionBox.Min.X;
                double depth = sectionBox.Max.Y - sectionBox.Min.Y;
                double defaultHeightBasedOnSectionTopCenter = sectionTopCenter.Z;

                ComprehensiveInputForm inputForm = new ComprehensiveInputForm(defaultHeightBasedOnSectionTopCenter, defaultRotationAngle, defaultWindSpeed, defaultRaindropRadius, defaultRainfallIntensity, defaultSimulationTimeInMinutes, defaultSpacingInMillimeters, doc, _deleteStylesEvent);
                if (inputForm.ShowDialog() != true)
                {
                    message = "User cancelled the operation.";
                    return Result.Cancelled;
                }

                // Assign form values to variables
                simulationPlaneHeight = inputForm.Height;
                rotationAngles = inputForm.RotationAngles;
                windSpeeds = inputForm.WindSpeeds;
                raindropRadiusM = inputForm.RaindropRadiusM;
                generateOriginalRays = inputForm.GenerateOriginalRays;
                generateAdditionalRays = inputForm.GenerateAdditionalRays;
                RainfallIntensityMmPerHr = inputForm.RainfallIntensityMmPerHr;
                double simulationTimeInMinutes = inputForm.SimulationTimeInMinutes;
                spacingInMillimeters = inputForm.SpacingInMillimeters;
                avfApproach = inputForm.AVFApproach;
                selectedStyle = inputForm.SelectedStyle;
                bool useSolidColorRange = selectedStyle == "SolidColor";

                if (string.IsNullOrEmpty(avfApproach))
                {
                    message = "AVF approach not selected.";
                    return Result.Failed;
                }

                using (Transaction tx = new Transaction(doc, "Ray Projection and AVF Application"))
                {
                    tx.Start();

                    try
                    {
                        int totalSimulations = Math.Min(rotationAngles.Length, Math.Min(windSpeeds.Length, RainfallIntensityMmPerHr.Length));

                        // Run the simulation for the total number of values in the arrays
                        for (int s = 0; s < totalSimulations; s++)
                        {
                            double rotationAngle = rotationAngles[s];
                            double windSpeed = windSpeeds[s];
                            double rainfallIntensity = RainfallIntensityMmPerHr[s];

                            // Run the simulation
                            RunSimulation(doc, sectionTopCenter, width, depth, view3D, selectedFaceRefs, rotationAngle, windSpeed, rainfallIntensity, spacingInMillimeters, simulationPlaneHeight, rayLength, visualizeRays, generateOriginalRays, generateAdditionalRays, avfApproach, selectedStyle, totalSimulations);

                            // Accumulate results across simulations
                            AccumulateAcrossSimulations(gridCells);

                        }
                        // Calculate final totals after all simulations are complete
                        CalculateFinalTotals();
                    }
                    catch (Exception ex)
                    {
                        message = $"Error during simulation: {ex.Message}\n{ex.StackTrace}";
                        tx.RollBack();
                        return Result.Failed;
                    }

                    tx.Commit();
                }



                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                message = "Operation canceled by the user.";
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
                TaskDialog.Show("Error", message);
                return Result.Failed;
            }
        }

        private void RunSimulation(Document doc, XYZ sectionTopCenter, double width, double depth, View3D view3D, IList<Reference> selectedFaceRefs,
            double rotationDegrees, double windSpeed, double rainfallIntensityMmPerHr, double spacingInMillimeters, double simulationPlaneHeight,
            double rayLength, bool visualizeRays, bool generateOriginalRays, bool generateAdditionalRays, string avfApproach, string selectedStyle, int totalSimulations)
        {
            double simulationPlaneArea = width * depth; // Assuming this is the area of the simulation plane in square meters

            // Calculate the number of raindrops per square meter using the provided raindrop radius
            double raindropsPerSquareMeter = 1.0 / (Math.PI * Math.Pow(raindropRadiusM, 2));

            var results = CalculateInclinationAngle(windSpeed, raindropRadiusM, true);
            double inclinationAngleRadians = results.InclinationRadians;
            double inclinationAngleDegrees = results.InclinationDegrees;

            double angleRadians = rotationDegrees * (Math.PI / 180.0);
            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);

            _deleteEvent = ExternalEvent.Create(_deleteHandler);
            SimulationPlaneInfo simulationPlaneInfo = CreateSimulationPlane(doc, sectionTopCenter, width, depth, simulationPlaneHeight, rotationDegrees, view3D);
            PlanarFace planeFace = simulationPlaneInfo.PlaneFace as PlanarFace;

            if (planeFace == null)
            {
                TaskDialog.Show("Error", "Failed to obtain the simulation plane face.");
                throw new InvalidOperationException("Failed to obtain the simulation plane face.");
            }

            // Display values before calling GenerateAndVisualizeRaysFromFace
            //TaskDialog.Show("Debug - Values Before", $"Plane Face: {simulationPlaneInfo.PlaneFace as PlanarFace}\n" +
            //                                          $"Document: {doc.Title}\n" +
            //                                          $"Spacing in Millimeters: {spacingInMillimeters}\n" +
            //                                          $"Ray Length: {rayLength}\n" +
            //                                          $"Visualize Rays: {visualizeRays}\n" +
            //                                          $"Right Vector: {right}\n" +
            //                                          $"Rotation Degrees: {rotationDegrees}\n" +
            //                                          $"Generate Original Rays: {generateOriginalRays}\n" +
            //                                          $"Generate Additional Rays: {generateAdditionalRays}\n" +
            //                                          $"Face References Count: {selectedFaceRefs.Count}");


            List<IdentifiedRay> rays = GenerateAndVisualizeRaysFromFace(planeFace, doc, spacingInMillimeters, rayLength, visualizeRays, right, rotationDegrees, generateOriginalRays, generateAdditionalRays, selectedFaceRefs, windSpeed, raindropRadiusM);

            var faceIntersectionMap = GetAllandFilterFirstRayFaceIntersections(doc, rays, selectedFaceRefs);
            var adaptedIntersections = AdaptIntersectionData(faceIntersectionMap);

            double gridSizeInMeters = spacingInMillimeters / 1000;
            CreateGridOnSurfaces(doc, gridSizeInMeters, selectedFaceRefs);

            Dictionary<Reference, double> faceOrientations = selectedFaceRefs.ToDictionary(faceRef => faceRef, faceRef => WDRSimulationUtilities.GetFaceOrientationRelativeToNorth(doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face));

            AssignIntersectionsToGridCells(gridCells, faceIntersectionMap, windSpeed, h_ref, alpha, C_T, C_R, O, rainfallIntensityMmPerHr, rotationDegrees, faceOrientations);

            var wdrValues = CalculateWDRForGridCells(gridCells, faceIntersectionMap, C_T, C_R, O, rainfallIntensityMmPerHr, rotationDegrees, h_ref, alpha, view3D, faceOrientations, windSpeed);

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView) ?? SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
            sfm.Clear();

            if (avfApproach == "ISO-WDR-Based")
            {
                WDRBasedAVF.ApplyWDRBasedAVFToSelectedFaces(doc, wdrValues, selectedStyle);
            }
            else if (avfApproach == "Ray-Intersection-Based")
            {
                var detailedIntersections = GetAllandFilterFirstRayFaceIntersections(doc, rays, selectedFaceRefs);
                Dictionary<Reference, List<XYZ>> simplifiedIntersections = detailedIntersections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => item.IntersectionPoint).ToList());
                IntegratedBasedAVF.ApplyIntersectionBasedAVFToSelectedFaces(doc, simplifiedIntersections, selectedStyle);
            }

            // Increment simulation number after each run
            simulationNumber++;
        }

        private string GetFaceDetails(Document doc, IList<Reference> faceRefs)
        {
            StringBuilder faceDetails = new StringBuilder();
            foreach (var faceRef in faceRefs)
            {
                Element faceElement = doc.GetElement(faceRef.ElementId);
                if (faceElement != null)

                {
                    faceDetails.AppendLine($"Element ID: {faceElement.Id}, Face Reference: {faceRef.ConvertToStableRepresentation(doc)}");
                }
            }
            return faceDetails.ToString();
        }

        private XYZ GetSectionBoxTopCenter(BoundingBoxXYZ sectionBox)
        {

            return new XYZ(
                (sectionBox.Min.X + sectionBox.Max.X) / 2,
                (sectionBox.Min.Y + sectionBox.Max.Y) / 2,
                sectionBox.Max.Z // Top Z coordinate
            );
        }
        private SimulationPlaneInfo CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle, View3D view3D)
        {

            SimulationPlaneInfo simulationInfo = new SimulationPlaneInfo();
            StringBuilder faceInfoBuilder = new StringBuilder();

            // Assume that the top view of the hexadecagon is flat on the XY plane
            // The 0 degree direction is aligned with the positive Y axis (North)
            // The 90 degree direction is aligned with the positive X axis (East)

            // Calculate the right vector (East) based on the user input direction angle
            // The direction angle is counterclockwise from the North (+Y axis)
            double angleRadians = directionAngle * (Math.PI / 180.0);
            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);
            XYZ up = new XYZ(-right.Y, right.X, 0); // Perpendicular to the right vector

            // Define the rectangle corners based on the right and up vectors
            XYZ p1 = center - right * width / 2 + up * depth / 2;
            XYZ p2 = center + right * width / 2 + up * depth / 2;
            XYZ p3 = center + right * width / 2 - up * depth / 2;
            XYZ p4 = center - right * width / 2 - up * depth / 2;

            // Create lines for the rectangle edges
            Line edge1 = Line.CreateBound(p1, p2);
            Line edge2 = Line.CreateBound(p2, p3);
            Line edge3 = Line.CreateBound(p3, p4);
            Line edge4 = Line.CreateBound(p4, p1);

            // Create and return a curve loop
            CurveLoop baseRectangle = new CurveLoop();
            baseRectangle.Append(edge1);
            baseRectangle.Append(edge2);
            baseRectangle.Append(edge3);
            baseRectangle.Append(edge4);

            // Define the plane geometry
            XYZ elevatedCenter = new XYZ(center.X, center.Y, center.Z + height);
            XYZ normal = new XYZ(0, 0, 1); // Normal pointing upwards (Z-axis)

            double extrusionDepth = 0.1; // Assuming a thin plane
            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, normal, extrusionDepth);

            // Create a DirectShape for visualization
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.ApplicationId = "SimulationPlane";
            ds.ApplicationDataId = Guid.NewGuid().ToString();

            // Calculate the endpoints for the existing line
            XYZ lineStartPoint = elevatedCenter + right * (width / 2) - up * (depth / 2);
            XYZ lineEndPoint = lineStartPoint + up * depth;

            // Create the model line for the existing line (as before)
            Plane plane = Plane.CreateByNormalAndOrigin(normal, elevatedCenter);
            SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
            Line existingLine = Line.CreateBound(lineStartPoint, lineEndPoint);
            ModelCurve existingModelCurve = doc.Create.NewModelCurve(existingLine, sketchPlane);

            PlanarFace simulationFace = null;
            Reference faceReference = null;

            foreach (Face face in planeSolid.Faces)
            {
                if (face is PlanarFace planarFace && planarFace.FaceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
                {
                    simulationFace = planarFace;
                    faceReference = face.Reference; // Get the reference of the face.

                    // Accumulate face info for visualization
                    faceInfoBuilder.AppendLine($"DirectShape Element ID: {ds.Id.IntegerValue}, " +
                                               $"DirectShape Unique ID: {ds.UniqueId}, " +
                                               $"Face: {face}");

                    // If you want to include the face reference stable representation
                    if (faceReference != null)
                    {
                        string stableReference = faceReference.ConvertToStableRepresentation(doc);
                        faceInfoBuilder.AppendLine($"Face Reference: {stableReference}");
                    }
                }
            }

            if (simulationFace != null)
            {
                XYZ extrusionDirection = new XYZ(0, 0, 0.01); // Small extrusion in the Z direction
                Solid extrudedSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { baseRectangle }, extrusionDirection, extrusionDepth);
                IList<GeometryObject> geometry = new List<GeometryObject> { extrudedSolid };
                ds.SetShape(geometry);
            }

            // Populate the SimulationPlaneInfo properties
            simulationInfo.PlaneFace = simulationFace;
            simulationInfo.SimulationShapeId = ds.Id;
            simulationInfo.PlaneOrigin = elevatedCenter;
            simulationInfo.PlaneLength = width;
            simulationInfo.PlaneWidth = depth;

            // Show all face info in a TaskDialog
            //if (faceInfoBuilder.Length > 0)
            //{
            //    TaskDialog.Show("Face Info", faceInfoBuilder.ToString());
            //}

            return simulationInfo;
        }


        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(
     Face planeFace,
     Document doc,
     double spacingInMillimeters,
     double rayLength,
     bool visualizeRays,
     XYZ right,
     double rotationDegrees,
     bool generateOriginalRays,
     bool generateAdditionalRays,
     IList<Reference> faceRefs,
     double windSpeed,
     double raindropRadiusM) // Added windSpeed and raindropRadiusM as parameters
        {
            // Convert spacing from millimeters to meters
            double spacingInMeters = spacingInMillimeters / 1000.0;

            ConcurrentBag<IdentifiedRay> concurrentIdentifiedRays = new ConcurrentBag<IdentifiedRay>();
            EdgeArrayArray edgeLoops = planeFace.EdgeLoops;
            EdgeArray edges = edgeLoops.get_Item(0); // Assuming the first loop is the outer loop

            XYZ p1 = edges.get_Item(0).AsCurve().GetEndPoint(0);
            XYZ p2 = edges.get_Item(1).AsCurve().GetEndPoint(0);
            XYZ p3 = edges.get_Item(2).AsCurve().GetEndPoint(0);
            XYZ p4 = edges.get_Item(3).AsCurve().GetEndPoint(0);

            XYZ side1 = p2 - p1;
            XYZ side2 = p3 - p2;
            double length = side1.GetLength();
            double width = side2.GetLength();

            int rayDensityLength = (int)Math.Round(length / spacingInMeters);
            int rayDensityWidth = (int)Math.Round(width / spacingInMeters);

            XYZ dir1 = side1.Normalize();
            XYZ dir2 = side2.Normalize();

            StringBuilder debugInfo = new StringBuilder();

            try
            {
                for (int i = 0; i <= rayDensityLength; i++)
                {
                    XYZ startPoint = p1 + i * dir1 * (length / rayDensityLength);
                    debugInfo.AppendLine($"i={i}, Start Point: {startPoint}");

                    for (int j = 0; j <= rayDensityWidth; j++)
                    {
                        XYZ gridPoint = startPoint + j * dir2 * (width / rayDensityWidth);
                        debugInfo.AppendLine($"j={j}, Grid Point: {gridPoint}, Direction: {dir2}, Segment Length: {(width / rayDensityWidth)}");

                        if (generateOriginalRays)
                        {
                            XYZ endPointDown = gridPoint - new XYZ(0, 0, rayLength);
                            debugInfo.AppendLine($"Original Ray Calculation:");
                            debugInfo.AppendLine($"  Grid Point: {gridPoint}");
                            debugInfo.AppendLine($"  Ray Length: {rayLength}");
                            debugInfo.AppendLine($"  End Point Down: {endPointDown}");

                            IdentifiedRay identifiedRayOriginal = CreateIdentifiedRay(gridPoint, endPointDown, XYZ.BasisZ.Negate(), "Original");
                            concurrentIdentifiedRays.Add(identifiedRayOriginal);
                            debugInfo.AppendLine($"Original Ray: Start={gridPoint}, End={endPointDown}, Direction=Down");
                        }

                        if (generateAdditionalRays)
                        {
                            var inclinationResults = CalculateInclinationAngle(windSpeed, raindropRadiusM, false);
                            double inclinationAngleRadians = inclinationResults.InclinationRadians;
                            double inclinationAngleDegrees = inclinationResults.InclinationDegrees;
                            debugInfo.AppendLine($"Additional Ray Inclination Calculation:");
                            debugInfo.AppendLine($"  Wind Speed: {windSpeed}");
                            debugInfo.AppendLine($"  Raindrop Radius: {raindropRadiusM}");
                            debugInfo.AppendLine($"  Inclination Angle (Radians): {inclinationAngleRadians}");
                            debugInfo.AppendLine($"  Inclination Angle (Degrees): {inclinationAngleDegrees}");

                            XYZ inclinedDirection = new XYZ(
                                Math.Sin(inclinationAngleRadians) * -right.X,
                                Math.Sin(inclinationAngleRadians) * -right.Y,
                                -Math.Cos(inclinationAngleRadians));
                            debugInfo.AppendLine($"  Calculated Inclined Direction:");
                            debugInfo.AppendLine($"    Sin(Inclination Angle) * -Right.X: {Math.Sin(inclinationAngleRadians) * -right.X}");
                            debugInfo.AppendLine($"    Sin(Inclination Angle) * -Right.Y: {Math.Sin(inclinationAngleRadians) * -right.Y}");
                            debugInfo.AppendLine($"    -Cos(Inclination Angle): {-Math.Cos(inclinationAngleRadians)}");
                            debugInfo.AppendLine($"    Inclined Direction: {inclinedDirection}");

                            XYZ endPointInclined = gridPoint + inclinedDirection * rayLength;
                            debugInfo.AppendLine($"  Additional Ray End Point Calculation:");
                            debugInfo.AppendLine($"    Grid Point: {gridPoint}");
                            debugInfo.AppendLine($"    Inclined Direction: {inclinedDirection}");
                            debugInfo.AppendLine($"    Ray Length: {rayLength}");
                            debugInfo.AppendLine($"    End Point Inclined: {endPointInclined}");

                            IdentifiedRay identifiedRayAdditional = CreateIdentifiedRay(gridPoint, endPointInclined, inclinedDirection, "Additional");
                            concurrentIdentifiedRays.Add(identifiedRayAdditional);
                            debugInfo.AppendLine($"Additional Ray: Start={gridPoint}, End={endPointInclined}, Direction={inclinedDirection}");
                        }
                    }
                }

                List<IdentifiedRay> identifiedRays = concurrentIdentifiedRays.ToList();

                if (identifiedRays.Count == 0)
                {
                    debugInfo.AppendLine("No rays generated.");
                }

                // Log to TaskDialog
                //TaskDialog.Show("Generated Rays", debugInfo.ToString());

                // Log to file
                //string logFileName = $"RayGenerationLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                //Logger.ClearLog(logFileName); // Clear the log file before writing new log
                //Logger.Log(debugInfo.ToString(), logFileName);

                if (visualizeRays)
                {
                    VisualizeRays(identifiedRays, doc, faceRefs);
                }

                return identifiedRays;
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    Debug.WriteLine($"Exception: {ex.Message}");
                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                }
                throw;
            }
        }


        private static int lastRayId = 0;

        private IdentifiedRay CreateIdentifiedRay(XYZ startPoint, XYZ endPoint, XYZ direction, string type)
        {
            // Ensure the type is always included in the RayId
            int newId = Interlocked.Increment(ref lastRayId); // Atomically increments the ID
            string rayId = $"Ray_{type}_{newId}"; // Formats the RayId to include the type explicitly
                                                  //delete
                                                  //TaskDialog.Show("CreateIdentifiedRay", $"Ray ID at CreateIdentifiedRay: {rayId}");
            return new IdentifiedRay
            {
                Ray = Line.CreateBound(startPoint, endPoint),
                RayId = rayId,
                RayType = type // Optionally store the type separately for easier access
            };
        }

        private void VisualizeRays(List<IdentifiedRay> rays, Document doc, IList<Reference> faceRefs)
        {
            foreach (IdentifiedRay identifiedRay in rays)
            {
                Line rayLine = identifiedRay.Ray;
                if (rayLine != null && rayLine.Length > doc.Application.ShortCurveTolerance)
                {
                    // Create a DirectShape to visualize the ray
                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Lines));
                    ds.ApplicationId = "Rays Visualization";
                    ds.ApplicationDataId = identifiedRay.RayId;
                    ds.SetShape(new List<GeometryObject> { rayLine });
                }
            }
        }

        private (double InclinationRadians, double InclinationDegrees, double TerminalVelocity) CalculateInclinationAngle(double windSpeed, double raindropRadiusM, bool showDebug = false)
        {
            try
            {
                // Constants
                double g = 9.81; // Acceleration due to gravity in m/s^2
                double rhoW = 1000; // Density of water in kg/m^3
                double rhoA = 1.225; // Density of air at sea level in kg/m^3
                double Cd = 0.47; // Drag coefficient for a sphere

                // Convert raindrop radius from millimeters to meters
                //raindropRadius = raindropRadius / 1000.0; // If raindropRadius was initially in mm

                double volume = (4.0 / 3.0) * Math.PI * Math.Pow(raindropRadiusM, 3);
                double mass = rhoW * volume;

                // Calculate cross-sectional area of the raindrop
                double area = Math.PI * Math.Pow(raindropRadiusM, 2);

                // Calculate terminal velocity
                double vT = Math.Sqrt((2 * mass * g) / (rhoA * area * Cd));

                // Calculate inclination angle in radians
                double inclinationAngleRadians = Math.Atan(windSpeed / vT);

                // Convert to degrees
                double inclinationDegrees = inclinationAngleRadians * (180.0 / Math.PI);

                // Show debug information if requested
                //if (showDebug)
                //{
                //    //TaskDialog debugDialog = new TaskDialog("Debug Information")
                //    //{
                //    //    MainInstruction = "Debug Information for Inclination Calculation",
                //    //    AllowCancellation = true,
                //    //    ExpandedContent = $"Calculating the inclination angle...\n" +
                //    //                      $"Volume: {volume} m^3\n" +
                //    //                      $"Mass: {mass} kg\n" +
                //    //                      $"Cross-sectional Area: {area} m^2\n" +
                //    //                      $"Terminal Velocity vT: {vT} m/s\n" +
                //    //                      $"Inclination Angle Radians: {inclinationAngleRadians}\n" +
                //    //                      $"Inclination Angle Degrees: {inclinationDegrees}"
                //    //};

                //  //  debugDialog.Show();
                //}

                return (inclinationAngleRadians, inclinationDegrees, vT);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                throw; // Re-throw the exception for higher-level handling
            }
        }


        private Dictionary<Reference, List<IntersectionInfo>> GetAllandFilterFirstRayFaceIntersections(Document doc, List<IdentifiedRay> rays, IList<Reference> faceRefs)
        {
            var faceIntersectionMap = new Dictionary<Reference, List<IntersectionInfo>>();
            var rayFirstIntersectionMap = new Dictionary<string, IntersectionInfo>(); // To track the first intersection for each ray

            foreach (IdentifiedRay ray in rays)
            {
                foreach (Reference faceRef in faceRefs)
                {
                    Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
                    if (face == null) continue;

                    IntersectionResultArray results;
                    SetComparisonResult intersectResult = face.Intersect(ray.Ray, out results);

                    if (intersectResult == SetComparisonResult.Overlap)
                    {
                        if (!faceIntersectionMap.ContainsKey(faceRef))
                        {
                            faceIntersectionMap[faceRef] = new List<IntersectionInfo>();
                        }

                        foreach (IntersectionResult result in results)
                        {
                            var distance = result.XYZPoint.DistanceTo(ray.Ray.GetEndPoint(0));
                            var intersectionInfo = new IntersectionInfo
                            {
                                ElementId = faceRef.ElementId,
                                Face = face,
                                FirstRayFaceIntersectionPoint = result.XYZPoint, // 3D coordinates of the first intersection point
                                IntersectionPoint = result.XYZPoint, // 3D coordinates of the intersection point
                                Distance = distance,
                                FaceReference = faceRef,
                                RayId = ray.RayId,
                                GridCellId = Guid.NewGuid().ToString() // Generate a unique ID for the grid cell
                            };

                            // Check if this is the first intersection for the ray
                            if (!rayFirstIntersectionMap.ContainsKey(ray.RayId) || distance < rayFirstIntersectionMap[ray.RayId].Distance)
                            {
                                // Update the previous first intersection to not be the first (if any)
                                if (rayFirstIntersectionMap.ContainsKey(ray.RayId))
                                {
                                    var previousFirst = rayFirstIntersectionMap[ray.RayId];
                                    previousFirst.IntersectionValue = 0; // Mark as not first
                                }

                                intersectionInfo.IntersectionValue = 1; // Mark as first intersection
                                rayFirstIntersectionMap[ray.RayId] = intersectionInfo; // Update the first intersection map
                            }
                            else
                            {
                                intersectionInfo.IntersectionValue = 0; // Mark as subsequent intersection
                            }

                            faceIntersectionMap[faceRef].Add(intersectionInfo);
                        }
                    }
                }
            }

            // Optionally, show debug information
            StringBuilder debugInfo = new StringBuilder();
            foreach (var kvp in faceIntersectionMap)
            {
                debugInfo.AppendLine($"Face: {kvp.Key.ConvertToStableRepresentation(doc)}");
                foreach (var intersection in kvp.Value)
                {
                    debugInfo.AppendLine($"RayId: {intersection.RayId}, Point: {intersection.IntersectionPoint}, Value: {intersection.IntersectionValue}, GridCellId: {intersection.GridCellId}");
                }
                debugInfo.AppendLine("-----");
            }
            //TaskDialog.Show("Debug", debugInfo.ToString());

            return faceIntersectionMap;
        }

        private Dictionary<Reference, List<IntersectionInfo>> AdaptIntersectionData(
                Dictionary<Reference, List<IntersectionInfo>> originalData)
        {
            var adaptedData = new Dictionary<Reference, List<IntersectionInfo>>();

            foreach (var kvp in originalData)
            {
                var reference = kvp.Key;
                var intersections = kvp.Value;
                var adaptedList = new List<IntersectionInfo>();

                foreach (var intersectionInfo in intersections)
                {
                    // Assuming no further adaptation is required since it's already in the desired format
                    adaptedList.Add(intersectionInfo);
                }

                adaptedData.Add(reference, adaptedList);
            }

            return adaptedData;
        }

        private void CreateGridOnSurfaces(Document doc, double gridSizeInMeters, IList<Reference> selectedFaceRefs)
        {
            gridCells = new Dictionary<string, GridCellInfo>();

            foreach (var faceRef in selectedFaceRefs)
            {
                Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
                if (face == null) continue;

                BoundingBoxUV faceBoundingBox = face.GetBoundingBox();
                double faceWidth = faceBoundingBox.Max.U - faceBoundingBox.Min.U;
                double faceHeight = faceBoundingBox.Max.V - faceBoundingBox.Min.V;

                int numGridCellsU = (int)Math.Ceiling(faceWidth / gridSizeInMeters);
                int numGridCellsV = (int)Math.Ceiling(faceHeight / gridSizeInMeters);

                for (int i = 0; i < numGridCellsU; i++)
                {
                    for (int j = 0; j < numGridCellsV; j++)
                    {
                        double gridCenterU = faceBoundingBox.Min.U + (i + 0.5) * gridSizeInMeters;
                        double gridCenterV = faceBoundingBox.Min.V + (j + 0.5) * gridSizeInMeters;

                        UV gridCellCenterUV = new UV(gridCenterU, gridCenterV);
                        XYZ gridCellCenter = face.Evaluate(gridCellCenterUV);

                        string gridCellId = $"GridCell_{faceRef.ElementId.IntegerValue}_{i}_{j}";

                        GridCellInfo gridCellInfo = new GridCellInfo
                        {
                            FaceReference = faceRef, // Add face reference
                            GridCellId = gridCellId,
                            GridCellCenter = gridCellCenter,
                            GridCellIntersectionCount = 0,
                            GridCellSumIntersectionValues = 0,
                            GridCellAverageCount = 0,
                            GridCellHeight = gridCellCenter.Z,
                            GridCellWindSpeed = 0.0,
                            GridCellWDR = 0.0,

                        };

                        gridCells.Add(gridCellId, gridCellInfo);
                    }
                }
            }
        }

        private void AssignIntersectionsToGridCells(
    Dictionary<string, GridCellInfo> gridCells,
    Dictionary<Reference, List<IntersectionInfo>> faceIntersectionMap,
    double v_ref,
    double h_ref,
    double alpha,
    double C_T,
    double C_R,
    double O,
    double rainfallIntensityMmPerHr,
    double rotationDegrees,
    Dictionary<Reference, double> faceOrientations) // Add this parameter
        {
            // Initialize grid cell sums
            foreach (var gridCell in gridCells.Values)
            {
                gridCell.GridCellSumIntersectionValues = 0;
                gridCell.GridCellIntersectionCount = 0;
                gridCell.GridCellWindSpeed = WDRSimulationUtilities.CalculateWindSpeedatHeightofFirstIntersection(
                    gridCell.GridCellHeight, v_ref, h_ref, alpha); // Calculate once and store
            }

            // Assign intersections to grid cells and calculate sums
            foreach (var faceIntersections in faceIntersectionMap.Values)
            {
                foreach (var intersectionInfo in faceIntersections)
                {
                    XYZ intersectionPoint = intersectionInfo.IntersectionPoint;

                    string closestGridCellId = null;
                    double minDistance = double.MaxValue;

                    foreach (var gridCell in gridCells.Values)
                    {
                        double distance = intersectionPoint.DistanceTo(gridCell.GridCellCenter);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestGridCellId = gridCell.GridCellId;
                        }
                    }

                    if (closestGridCellId != null)
                    {
                        GridCellInfo gridCell = gridCells[closestGridCellId];
                        gridCell.GridCellIntersectionCount++;
                        gridCell.GridCellSumIntersectionValues += intersectionInfo.IntersectionValue;
                        gridCell.FaceReference = intersectionInfo.FaceReference; // Add face reference
                    }
                }
            }

            // Calculate average intersection count for each grid cell and accumulate data
            foreach (var gridCell in gridCells.Values)
            {
                if (gridCell.GridCellIntersectionCount > 0)
                {
                    gridCell.GridCellAverageCount = (double)gridCell.GridCellSumIntersectionValues / gridCell.GridCellIntersectionCount;
                }
                else
                {
                    gridCell.GridCellAverageCount = 0;
                }

                // Calculate and update WDR for the grid cell
                foreach (var faceRef in faceIntersectionMap.Keys)
                {
                    double orientation = faceOrientations[faceRef];
                    gridCell.GridCellWDR = WDRSimulationUtilities.CalculateWDR(
                        C_T, C_R, O, gridCell.GridCellAverageCount, gridCell.GridCellWindSpeed, rainfallIntensityMmPerHr, rotationDegrees, orientation);
                }

                // Accumulate the data
                gridCell.CumulativeGridCellIntersectionCount += gridCell.GridCellIntersectionCount;
                gridCell.CumulativeSumIntersectionValues += gridCell.GridCellSumIntersectionValues;
                gridCell.CumulativeWindSpeed += gridCell.GridCellWindSpeed;
                gridCell.CumulativeWDR += gridCell.GridCellWDR;
            }
        }


        private Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)>> CalculateWDRForGridCells(
      Dictionary<string, GridCellInfo> gridCells,
      Dictionary<Reference, List<IntersectionInfo>> faceIntersectionMap,
      double C_T,
      double C_R,
      double O,
      double rainfallIntensityMmPerHr,
      double rotationDegrees,
      double h_ref,
      double alpha,
      View3D view3D,
      Dictionary<Reference, double> faceOrientations,
      double windSpeed)
        {
            double sectionBoxBaseZ = view3D.GetSectionBox().Min.Z;
            double v_ref = windSpeed;

            var faceWDRValues = new Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)>>();
            StringBuilder logBuilder = new StringBuilder();

            logBuilder.AppendLine($"Simulation Number: {simulationNumber}");
            logBuilder.AppendLine($"Rotation Angle: {rotationDegrees}");
            logBuilder.AppendLine($"Wind Speed: {windSpeed} m/s");
            logBuilder.AppendLine($"Rainfall Intensity: {rainfallIntensityMmPerHr} mm/hr");
            logBuilder.AppendLine();

            foreach (var gridCell in gridCells.Values)
            {

                logBuilder.AppendLine($"GridCellId: {gridCell.GridCellId}");
                logBuilder.AppendLine($"FaceReference: {gridCell.FaceReference.ConvertToStableRepresentation(_uiApp.ActiveUIDocument.Document)}");
                logBuilder.AppendLine($"  Height: {gridCell.GridCellHeight:F3} m");
                logBuilder.AppendLine($"  v_ref: {v_ref:F3} m/s");
                logBuilder.AppendLine($"  h_ref: {h_ref:F3} m");
                logBuilder.AppendLine($"  alpha: {alpha:F3}");
                logBuilder.AppendLine($"  Precomputed Wind Speed: {gridCell.GridCellWindSpeed:F3} m/s");

                foreach (var faceRef in faceIntersectionMap.Keys)
                {
                    double orientation = faceOrientations[faceRef];
                    double wallFactor = CalculateWallFactor(gridCell);
                    double wdr = WDRSimulationUtilities.CalculateWDR(C_T, C_R, O, wallFactor, gridCell.GridCellWindSpeed, rainfallIntensityMmPerHr, rotationDegrees, orientation);

                    logBuilder.AppendLine($"  Orientation: {orientation:F3} degrees");
                    logBuilder.AppendLine($"  WallFactor: {wallFactor:F3}");
                    logBuilder.AppendLine($"  Calculated WDR: {wdr:F3}");

                    gridCell.GridCellWDR = wdr;

                    foreach (var intersectionInfo in faceIntersectionMap[faceRef])
                    {
                        if (intersectionInfo.GridCellId == gridCell.GridCellId)
                        {
                            if (!faceWDRValues.ContainsKey(faceRef))
                            {
                                faceWDRValues[faceRef] = new List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)>();
                            }

                            faceWDRValues[faceRef].Add((intersectionInfo.IntersectionPoint, wdr, gridCell.GridCellId));
                        }
                    }
                }
            }

            //string logFileName = $"GridCell_WDR_Log_Simulation_{simulationNumber}.txt";
            //Logger.ClearLog(logFileName);

            //foreach (var gridCell in gridCells.Values)
            //{
            //    string logMessage = $"GridCellId: {gridCell.GridCellId}\n" +
            //                        $"FaceReference: {gridCell.FaceReference.ConvertToStableRepresentation(_uiApp.ActiveUIDocument.Document)}\n" +
            //                        $"GridCellCenter: {gridCell.GridCellCenter}\n" +
            //                        $"GridCellIntersectionCount: {gridCell.GridCellIntersectionCount}\n" +
            //                        $"GridCellSumIntersectionValues: {gridCell.GridCellSumIntersectionValues}\n" +
            //                        $"GridCellAverageCount: {gridCell.GridCellAverageCount:F3}\n" +
            //                        $"GridCellHeight: {gridCell.GridCellHeight:F3} m\n" +
            //                        $"GridCellWindSpeed: {gridCell.GridCellWindSpeed:F3} m/s\n" +
            //                        $"WallFactor: {gridCell.GridCellAverageCount:F3}\n" +
            //                        $"GridCellWDR: {gridCell.GridCellWDR:F3}\n" +
            //                        "-----";

            //    logBuilder.AppendLine(logMessage);
            //}

            //Logger.Log(logBuilder.ToString(), logFileName);

            return faceWDRValues;
        }


        public static double CalculateWallFactor(GridCellInfo gridCell)
        {
            double wallFactor = gridCell.GridCellAverageCount;
            return wallFactor;
        }

        private Dictionary<string, GridCellTotalInfo> totalGridCellData = new Dictionary<string, GridCellTotalInfo>();

        private void AccumulateAcrossSimulations(Dictionary<string, GridCellInfo> gridCells)
        {
            foreach (var gridCell in gridCells.Values)
            {
                if (!totalGridCellData.ContainsKey(gridCell.GridCellId))
                {
                    totalGridCellData[gridCell.GridCellId] = new GridCellTotalInfo
                    {
                        GridCellId = gridCell.GridCellId
                    };
                }

                var totalGridCell = totalGridCellData[gridCell.GridCellId];
                totalGridCell.GridCellIntersectionCountAcrossSimulations += gridCell.CumulativeGridCellIntersectionCount;
                totalGridCell.GridcellSumIntersectionValuesAcrossSimulations += gridCell.CumulativeSumIntersectionValues;
                totalGridCell.GridcellAverageCountAcrossSimulations += gridCell.CumulativeAverageCount;
                totalGridCell.GridcellWindSpeedAcrossSimulations += gridCell.CumulativeWindSpeed;
                totalGridCell.GridcellWDRAcrossSimulations += gridCell.CumulativeWDR;
                totalGridCell.SimulationCount++;
            }
        }

        private void CalculateFinalTotals()
        {
            foreach (var totalGridCell in totalGridCellData.Values)
            {
                totalGridCell.GridcellSumIntersectionValuesAcrossSimulations /= totalGridCell.SimulationCount;
                totalGridCell.GridcellAverageCountAcrossSimulations /= totalGridCell.SimulationCount;
                totalGridCell.GridcellWindSpeedAcrossSimulations /= totalGridCell.SimulationCount;
                //totalGridCell.GridcellWDRAcrossSimulations /= totalGridCell.SimulationCount;

            }

            // Log the final accumulated data
            StringBuilder totalLogBuilder = new StringBuilder();
            totalLogBuilder.AppendLine("Final Accumulated Values Across Simulations:");

            foreach (var totalGridCell in totalGridCellData.Values)
            {
                totalLogBuilder.AppendLine($"GridCellId: {totalGridCell.GridCellId}");
                totalLogBuilder.AppendLine($"  GridCellIntersectionCountAcrossSimulations: {totalGridCell.GridCellIntersectionCountAcrossSimulations}");
                totalLogBuilder.AppendLine($"  GridcellSumIntersectionValuesAcrossSimulations: {totalGridCell.GridcellSumIntersectionValuesAcrossSimulations}");
                totalLogBuilder.AppendLine($"  GridcellAverageCountAcrossSimulations: {totalGridCell.GridcellAverageCountAcrossSimulations:F3}");
                totalLogBuilder.AppendLine($"  GridcellWindSpeedAcrossSimulations: {totalGridCell.GridcellWindSpeedAcrossSimulations:F3} m/s");
                totalLogBuilder.AppendLine($"  GridcellWDRAcrossSimulations: {totalGridCell.GridcellWDRAcrossSimulations:F3}");
                totalLogBuilder.AppendLine("-----");
            }

            //string totalLogFileName = "Final_Total_GridCell_Data_Log.txt";
            //Logger.ClearLog(totalLogFileName);
            //Logger.Log(totalLogBuilder.ToString(), totalLogFileName);
        }

  
    }


    // Updated IntersectionInfo class to include distance
    public class IntersectionInfo
    {
        public ElementId ElementId { get; set; }
        public XYZ FirstRayFaceIntersectionPoint { get; set; } // Stores the 3D coordinates of the first intersection point
        public XYZ IntersectionPoint { get; set; } // Stores the 3D coordinates of the intersection point (both first and subsequent)
        public Face Face { get; set; }
        public Reference FaceReference { get; set; }
        public double Distance { get; set; }
        public string RayId { get; set; }
        public int IntersectionValue { get; set; } // 1 for the first intersection, 0 for subsequent intersections
        public string GridCellId { get; set; } // Unique ID for the grid cell
    }

    public class SimulationPlaneInfo
    {
        public Face PlaneFace { get; set; }
        public XYZ PlaneOrigin { get; set; }
        public double PlaneLength { get; set; }
        public double PlaneWidth { get; set; }
        public ElementId SimulationShapeId { get; set; } // Add this line
    }

    public class IdentifiedRay
    {
        public Line Ray { get; set; }
        public string RayId { get; set; }
        public string Type { get; set; }
        public string RayType { get; set; }
        public ElementId ElementId { get; set; } // Add this line if SelectedElementId should be a part of IdentifiedRay
    }

    public class RayHitInfo
    {
        public string RayId { get; set; }
        public string RayType { get; set; } // "Original" or "Additional"
                                            // Add the missing properties here
        public string ElementId { get; set; }
        public double Distance { get; set; }
        public XYZ IntersectionPoint { get; set; }
    }

    public class GridCellInfo
    {
        public string GridCellId { get; set; }
        public XYZ GridCellCenter { get; set; }
        public int GridCellIntersectionCount { get; set; }
        public double GridCellSumIntersectionValues { get; set; }
        public double GridCellAverageCount { get; set; }
        public double GridCellHeight { get; set; }
        public double GridCellWindSpeed { get; set; }
        public double GridCellWDR { get; set; }
        public Reference FaceReference { get; set; } // Add face reference

        // Cumulative variables
        public int CumulativeGridCellIntersectionCount { get; set; }
        public double CumulativeSumIntersectionValues { get; set; }
        public double CumulativeAverageCount { get; set; }
        public double CumulativeWindSpeed { get; set; }
        public double CumulativeWDR { get; set; }

        // Total variables
        public double TotalGridCellWDR { get; set; }
        public double TotalAverageWDR { get; set; }
        public int TotalGridCellIntersectionCount { get; set; }
        public double TotalSumIntersectionValues { get; set; }
        public double TotalAverageCount { get; set; }
        public double TotalAverageWindSpeed { get; set; }
        public int NumberOfSimulations { get; set; }

        // Constructor to initialize with default values
        public GridCellInfo()
        {
            GridCellIntersectionCount = 0;
            GridCellSumIntersectionValues = 0;
            GridCellAverageCount = 0;
            GridCellHeight = 0.0;
            GridCellWindSpeed = 0.0;
            GridCellWDR = 0.0;
            CumulativeGridCellIntersectionCount = 0;
            CumulativeSumIntersectionValues = 0.0;
            CumulativeAverageCount = 0.0;
            CumulativeWindSpeed = 0.0;
            CumulativeWDR = 0.0;
            TotalGridCellWDR = 0.0;
            TotalAverageWDR = 0.0;
            TotalGridCellIntersectionCount = 0;
            TotalSumIntersectionValues = 0.0;
            TotalAverageCount = 0.0;
            TotalAverageWindSpeed = 0.0;
            NumberOfSimulations = 0;
        }
    }

    public class GridCellTotalInfo
    {
        public string GridCellId { get; set; }
        public int GridCellIntersectionCountAcrossSimulations { get; set; }
        public double GridcellSumIntersectionValuesAcrossSimulations { get; set; }
        public double GridcellAverageCountAcrossSimulations { get; set; }
        public double GridcellWindSpeedAcrossSimulations { get; set; }
        public double GridcellWDRAcrossSimulations { get; set; }
        public int SimulationCount { get; set; }

        // Initialize with default values
        public GridCellTotalInfo()
        {
            GridCellIntersectionCountAcrossSimulations = 0;
            GridcellSumIntersectionValuesAcrossSimulations = 0;
            GridcellAverageCountAcrossSimulations = 0;
            GridcellWindSpeedAcrossSimulations = 0;
            GridcellWDRAcrossSimulations = 0;
            SimulationCount = 0;
        }
    }

    public class IntersectionResults
    {
        public Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>> FaceToFirstIntersectionsMap { get; set; }
        public Dictionary<Reference, List<XYZ>> FaceIntersectionPoints { get; set; }
    }


    // Handler for deleting analysis display styles
    public class DeleteAnalysisDisplayStylesHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            using (Transaction tx = new Transaction(doc, "Delete Analysis Display Styles"))
            {
                tx.Start();
                // Retrieve all AnalysisDisplayStyle elements in the document
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<ElementId> analysisDisplayStyles = collector
                    .OfClass(typeof(AnalysisDisplayStyle))
                    .ToElementIds();

                // Delete all found AnalysisDisplayStyle elements
                foreach (ElementId styleId in analysisDisplayStyles)
                {
                    doc.Delete(styleId);
                }
                tx.Commit();
            }
        }

        public string GetName() => "Delete Analysis Display Styles";
    }

}


