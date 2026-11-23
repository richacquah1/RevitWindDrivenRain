using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Windows.Controls;
using Autodesk.Revit.DB.Structure;
using System.Collections.Generic;
using System.Security.Cryptography;
using Autodesk.Revit.DB.Analysis;
using System.Linq;
using Autodesk.Revit.DB.Visual;

namespace RevitWindDrivenRain
{
    public class SimulationSetup
    {
        private double windSpeed;
        private double windDirection; // In degrees
        private double rainfallIntensity;
        private UIDocument uiDoc;

        // Class-level variables for plane properties
        private double planeHeight;
        private double planeLength;
        private double planeWidth;
        private XYZ planeOrigin;

        // Flag to control visualization of rays
        private bool visualizeRays = true;

        private XYZ originalSectionBoxCenter;


        private SpatialFieldManager sfm;
        private int schemaIndex;

        public SimulationSetup(UIDocument uiDoc, ExternalCommandData commandData, double windSpeed, double windDirection, double rainfallIntensity)
        {
            this.uiDoc = uiDoc;
            this.windSpeed = windSpeed;
            this.windDirection = windDirection;
            this.rainfallIntensity = rainfallIntensity;

            sfm = null; // Initialize when needed
            schemaIndex = -1; // Initialize when needed
        }

        public void Initialize(ExternalCommandData commandData)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Attempt to get section box dimensions
            var dimensions = GetCurrentViewSectionBoxDimensions(doc);
            double buildingLength = dimensions.Sectionboxlength;
            double buildingWidth = dimensions.Sectionboxwidth;
            double buildingHeight = dimensions.Sectionboxheight;
            XYZ buildingLocation = dimensions.center;
            originalSectionBoxCenter = dimensions.center;

            CreateAndVisualizeSimulationPlane(doc, buildingLocation, buildingHeight, buildingLength, buildingWidth);

            // Use the calculated planeHeight to set planeOrigin
            planeOrigin = new XYZ(buildingLocation.X, buildingLocation.Y, planeHeight);

            // Now call the method to project and optionally visualize rays
            ProjectAndVisualizeRays(doc);

            InitializeAVF(doc);
        }

        public (double Sectionboxlength, double Sectionboxwidth, double Sectionboxheight, XYZ center) GetCurrentViewSectionBoxDimensions(Document doc)
        {
            View currentView = doc.ActiveView;

            if (currentView is View3D view3D && view3D.IsSectionBoxActive)
            {
                BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
                double Sectionboxlength = sectionBox.Max.X - sectionBox.Min.X;
                double Sectionboxwidth = sectionBox.Max.Y - sectionBox.Min.Y;
                double Sectionboxheight = sectionBox.Max.Z - sectionBox.Min.Z;
                XYZ center = (sectionBox.Max + sectionBox.Min) * 0.5;
                return (Sectionboxlength, Sectionboxwidth, Sectionboxheight, center);
            }
            else
            {
                throw new InvalidOperationException("Current view is not a 3D view with an active section box.");
            }
        }

        public double CalculatePlaneHeight(double buildingHeight, double windSpeed)
        {
            double g = 9.81; // Gravity
            double k_height = 0.5; // Scaling factor for height
            double t_fall = Math.Sqrt(2 * buildingHeight / g); // Time for raindrop to fall

            double additionalHeight = k_height * (Math.Pow(windSpeed, 2) / (2 * g) + windSpeed * t_fall);
            return buildingHeight + additionalHeight;
        }



        public (double area, double totalLength, double totalWidth) CalculatePlaneDimensions(double buildingLength, double buildingWidth)
        {
            // Using the dimensions of the section box directly
            double totalLength = buildingLength;
            double totalWidth = buildingWidth;
            double area = totalLength * totalWidth;

            return (area, totalLength, totalWidth);
        }


        public void CreateAndVisualizeSimulationPlane(Document doc, XYZ buildingLocation, double buildingHeight, double buildingLength, double buildingWidth)
        {
            double additionalHeight = CalculateAdditionalHeight(buildingHeight, windSpeed);

            var planeDims = CalculatePlaneDimensions(buildingLength, buildingWidth);
            this.planeLength = buildingLength;
            this.planeWidth = buildingWidth;
            this.planeHeight = buildingHeight + additionalHeight;

            // Aligning the center of the simulation plane with the center of the section box
            this.planeOrigin = new XYZ(originalSectionBoxCenter.X, originalSectionBoxCenter.Y, this.planeHeight);


            // Visualizing the simulation plane as a rectangle at the calculated height
            using (Transaction t = new Transaction(doc, "Create and Visualize Simulation Plane"))
            {
                t.Start();

                // Create a rectangle geometry representing the plane
                List<Curve> planeEdges = new List<Curve>
        {
            Line.CreateBound(new XYZ(planeOrigin.X - this.planeLength / 2, planeOrigin.Y - this.planeWidth / 2, planeOrigin.Z), new XYZ(planeOrigin.X + this.planeLength / 2, planeOrigin.Y - this.planeWidth / 2, planeOrigin.Z)),
            Line.CreateBound(new XYZ(planeOrigin.X + this.planeLength / 2, planeOrigin.Y - this.planeWidth / 2, planeOrigin.Z), new XYZ(planeOrigin.X + this.planeLength / 2, planeOrigin.Y + this.planeWidth / 2, planeOrigin.Z)),
            Line.CreateBound(new XYZ(planeOrigin.X + this.planeLength / 2, planeOrigin.Y + this.planeWidth / 2, planeOrigin.Z), new XYZ(planeOrigin.X - this.planeLength / 2, planeOrigin.Y + this.planeWidth / 2, planeOrigin.Z)),
            Line.CreateBound(new XYZ(planeOrigin.X - this.planeLength / 2, planeOrigin.Y + this.planeWidth / 2, planeOrigin.Z), new XYZ(planeOrigin.X - this.planeLength / 2, planeOrigin.Y - this.planeWidth / 2, planeOrigin.Z))
        };

                CurveLoop planeLoop = CurveLoop.Create(planeEdges);
                Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { planeLoop }, XYZ.BasisZ, 0.01); // Small extrusion for visualization

                // Create a DirectShape element in the document
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { planeSolid });
                ds.Name = "Simulation Plane";

                t.Commit();
            }
        }



        //public void ProjectAndVisualizeRays(Document doc)
        //{
        //    List<XYZ> rayOrigins = CalculateRayOrigins(this.planeLength, this.planeWidth, CalculateRaySpacing(this.planeLength, this.planeWidth, CalculateRayDensity()), this.planeOrigin);

        //    using (Transaction t = new Transaction(doc, "Project and Visualize Rays"))
        //    {
        //        t.Start();

        //        foreach (var rayOrigin in rayOrigins)
        //        {
        //            // Set the direction of the ray to be directly downwards
        //            XYZ directionDownwards = new XYZ(0, 0, -1);

        //            // Calculate a reasonable endpoint for the ray
        //            double rayLength = 50; // Example length, adjust as needed
        //            XYZ rayEndPoint = rayOrigin + directionDownwards * rayLength;

        //            // Now create a line from the ray's origin to the calculated end point
        //            Line rayLine = Line.CreateBound(rayOrigin, rayEndPoint);

        //            // Visualization code...
        //            if (visualizeRays && rayLine.Length > 0.001) // Check line length
        //            {
        //                DirectShape rayShape = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
        //                rayShape.SetShape(new GeometryObject[] { rayLine });
        //                rayShape.Name = "Ray Visualization";
        //            }
        //        }

        //        t.Commit();
        //    }
        //}


        public void ProjectAndVisualizeRays(Document doc)
        {
            // Ensure the active view is a 3D view
            View3D view3d = doc.ActiveView as View3D;
            if (view3d == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view to visualize rays.");
                return;
            }

            List<XYZ> intersectionPointsList = new List<XYZ>();

            using (Transaction t = new Transaction(doc, "Visualize Ray Intersections"))
            {
                t.Start();

                ReferenceIntersector referenceIntersector = new ReferenceIntersector(view3d.Id, FindReferenceTarget.Face, view3d);
                List<XYZ> rayOrigins = CalculateRayOrigins(this.planeLength, this.planeWidth, CalculateRaySpacing(this.planeLength, this.planeWidth, CalculateRayDensity()), this.planeOrigin);

                foreach (var rayOrigin in rayOrigins)
                {
                    XYZ directionDownwards = new XYZ(0, 0, -1);
                    ReferenceWithContext refWithContext = referenceIntersector.FindNearest(rayOrigin, directionDownwards);

                    if (refWithContext != null && refWithContext.Proximity > 0)
                    {
                        XYZ intersectionPoint = refWithContext.GetReference().GlobalPoint;
                        intersectionPointsList.Add(intersectionPoint);

                        // Visualization code for the ray (if needed)
                        // For example, creating a line from the ray origin to the intersection point
                        CreateDirectShape(doc, rayOrigin, intersectionPoint, "Ray Visualization");
                    }
                }

                t.Commit();
            }

            // Optional: Use the collected intersection points as needed
            // For example, displaying them or using them for further analysis
            ShowIntersectionPoints(intersectionPointsList);
        }

        private void CreateDirectShape(Document doc, XYZ startPoint, XYZ endPoint, string name)
        {
            // Create a line from the ray's origin to the intersection point
            Line line = Line.CreateBound(startPoint, endPoint);

            // Create a DirectShape element in the document for visualization
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.SetShape(new GeometryObject[] { line });
            ds.Name = name;
        }



        private void ShowIntersectionPoints(List<XYZ> intersectionPointsList)
        {
            // For a simple demonstration, we're using a TaskDialog to show the intersection points
            TaskDialog dialog = new TaskDialog("Intersection Points")
            {
                TitleAutoPrefix = false,
                MainInstruction = "Intersection Points",
                MainContent = $"Found {intersectionPointsList.Count} intersection points."
            };

            // Build a string that lists the intersection points
            string pointList = string.Join("\n", intersectionPointsList.Select(p => $"X: {p.X}, Y: {p.Y}, Z: {p.Z}"));
            dialog.ExpandedContent = pointList;

            // Show the dialog
            dialog.Show();
        }




        //ALL HELPING METHODS

        public RayParameters ConfigureRayParameters()
        {
            RayParameters rayParams = new RayParameters();

            // Calculate ray direction based on wind direction
            rayParams.Direction = new XYZ(Math.Cos(windDirection * Math.PI / 180), Math.Sin(windDirection * Math.PI / 180), 0);

            // Configure ray density based on rainfall intensity
            rayParams.Density = (int)(rainfallIntensity * 10); // Example conversion

            return rayParams;
        }

        public int CalculateRayDensity()
        {
            // Example calculation (modify as needed for your simulation)
            // This could be a fixed value or calculated based on simulation parameters
            return (int)(rainfallIntensity * 10); // Just an example
        }

        public double CalculateRaySpacing(double planeLength, double planeWidth, int rayDensity)
        {
            // Calculate the total area of the plane
            double planeArea = planeLength * planeWidth;

            // Determine the spacing needed to achieve the desired density
            // Assuming a uniform distribution of rays over the plane
            return Math.Sqrt(planeArea / rayDensity);
        }

        private List<XYZ> CalculateRayOrigins(double planeLength, double planeWidth, double spacing, XYZ planeOrigin)
        {
            List<XYZ> rayOrigins = new List<XYZ>();
            // Calculate the center of the plane
            XYZ centerOfPlane = new XYZ(planeOrigin.X, planeOrigin.Y, planeHeight);

            for (double x = centerOfPlane.X - planeLength / 2; x <= centerOfPlane.X + planeLength / 2; x += spacing)
            {
                for (double y = centerOfPlane.Y - planeWidth / 2; y <= centerOfPlane.Y + planeWidth / 2; y += spacing)
                {
                    // Ray origins should be on the bottom surface of the simulation plane
                    rayOrigins.Add(new XYZ(x, y, centerOfPlane.Z));
                }
            }
            return rayOrigins;
        }


        public double CalculateAdditionalHeight(double buildingHeight, double windSpeed)
        {
            double g = 9.81; // Gravity
            double k_height = 0.5; // Scaling factor for height (adjust based on empirical data)
            double t_fall = Math.Sqrt(2 * buildingHeight / g); // Time for raindrop to fall

            // Calculate additional height (ΔH) based on wind
            return k_height * (Math.Pow(windSpeed, 2) / (2 * g) + windSpeed * t_fall);
        }


        public void VisualizeRayIntersections(Document doc, List<IntersectingFaceandPointInfo> intersectingFacesInfo)
        {
            View activeView = doc.ActiveView;
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(activeView) ?? SpatialFieldManager.CreateSpatialFieldManager(activeView, 1);

            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Ray Intersection Visualization", "Points where rays intersect elements");
            int schemaIndex = sfm.RegisterResult(resultSchema);

            foreach (var faceInfo in intersectingFacesInfo)
            {
                List<UV> uvPoints = new List<UV>();
                List<ValueAtPoint> valueAtPoints = new List<ValueAtPoint>();

                foreach (var intersectionPoint in faceInfo.IntersectionPoints)
                {
                    IntersectionResult result = faceInfo.Face.Project(intersectionPoint);
                    if (result != null)
                    {
                        uvPoints.Add(result.UVPoint);
                        valueAtPoints.Add(new ValueAtPoint(new List<double> { 1.0 })); // You can modify this value as needed
                    }
                }

                if (uvPoints.Count > 0)
                {
                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);
                    FieldValues fieldValues = new FieldValues(valueAtPoints);

                    int primitiveId = sfm.AddSpatialFieldPrimitive(faceInfo.FaceReference);
                    sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
                }
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Analysis Visualization Framework (AVF) Methods

        private void InitializeAVF(Document doc)
        {
            // Initialize AVF components
            sfm = GetOrCreateSpatialFieldManager(doc, doc.ActiveView);
            schemaIndex = RegisterResultSchema(doc, doc.ActiveView, "Ray Intersection Visualization", "Points where rays intersect elements");
        }
        private SpatialFieldManager GetOrCreateSpatialFieldManager(Document doc, View view)
        {
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1); // 1 indicates one value per point
            }
            return sfm;
        }

        private int RegisterResultSchema(Document doc, View view, string baseSchemaName, string description)
        {
            SpatialFieldManager sfm = GetOrCreateSpatialFieldManager(doc, view);

            string uniqueSchemaName = baseSchemaName;
            int iteration = 1;

            // Check if the schema name is unique and modify if necessary
            while (SchemaNameExists(sfm, uniqueSchemaName))
            {
                uniqueSchemaName = $"{baseSchemaName}_{iteration}";
                iteration++;
            }

            AnalysisResultSchema resultSchema = new AnalysisResultSchema(uniqueSchemaName, description);
            return sfm.RegisterResult(resultSchema);
        }

        private bool SchemaNameExists(SpatialFieldManager sfm, string schemaName)
        {
            foreach (var schemaIndex in sfm.GetRegisteredResults())
            {
                AnalysisResultSchema schema = sfm.GetResultSchema(schemaIndex);
                if (schema.Name == schemaName)
                {
                    return true;
                }
            }
            return false;
        }

        private void ConvertSimulationDataToAVF(Document doc, List<IntersectingFaceandPointInfo> intersectingFacesInfo, SpatialFieldManager sfm, int schemaIndex)
        {
            foreach (var faceInfo in intersectingFacesInfo)
            {
                List<UV> uvPoints = new List<UV>();
                List<ValueAtPoint> valueAtPoints = new List<ValueAtPoint>();

                foreach (var intersectionPoint in faceInfo.IntersectionPoints)
                {
                    UV uvPoint = ConvertXYZToUV(intersectionPoint, faceInfo.Face); // Implement this conversion
                    uvPoints.Add(uvPoint);
                    valueAtPoints.Add(new ValueAtPoint(new List<double> { 1.0 })); // Example value
                }

                if (uvPoints.Any())
                {
                    UpdateSpatialFieldManager(doc, sfm, faceInfo.FaceReference, uvPoints, valueAtPoints, schemaIndex);
                }
            }
        }

        private void UpdateSpatialFieldManager(Document doc, SpatialFieldManager sfm, Reference faceRef, List<UV> uvPoints, List<ValueAtPoint> valueAtPoints, int schemaIndex)
        {
            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);
            FieldValues fieldValues = new FieldValues(valueAtPoints);
            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        }

        private UV ConvertXYZToUV(XYZ point, Face face)
        {
            // Project the 3D point onto the face
            IntersectionResult result = face.Project(point);

            // Check if the projection was successful
            if (result != null && result.XYZPoint != null)
            {
                // Get the UV coordinates of the projection
                UV uvPoint = result.UVPoint;

                // Ensure that the UV coordinates are within the face's bounds
                if (IsUVWithinFaceBounds(uvPoint, face))
                {
                    return uvPoint;
                }
            }

            // If the projection fails or the UV point is outside the face bounds, return UV.Zero
            return UV.Zero;
        }

        private bool IsUVWithinFaceBounds(UV uvPoint, Face face)
        {
            // Get the bounding box of the face in UV coordinates
            BoundingBoxUV uvBounds = face.GetBoundingBox();

            // Check if the UV point is within the UV bounds
            return uvPoint.U >= uvBounds.Min.U && uvPoint.U <= uvBounds.Max.U &&
                   uvPoint.V >= uvBounds.Min.V && uvPoint.V <= uvBounds.Max.V;
        }


        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------


    }
    public class RayParameters
    {
        public XYZ Direction { get; set; }
        public int Density { get; set; }
    }

    public class RayIntersectingFaceandPointInfo
    {
        public Face Face { get; set; }
        public ElementId ElementId { get; set; }
        public List<XYZ> IntersectionPoints { get; set; } = new List<XYZ>();
        public Reference FaceReference { get; set; }
    }


}
