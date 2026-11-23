//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWindDrivenRain
//{
//    public static class WDRBasedAVF
//    {
//        public static void ApplyWDRBasedAVFToSelectedFaces(Document doc, Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue)>> faceIntersectionPoints, string selectedStyle)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<(XYZ IntersectionPoint, double WDRValue)> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyWDRBasedAVF(doc, face, faceRef, intersections, selectedStyle);
//                }
//            }
//        }

//        public static AnalysisDisplayStyle CreateWDRBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange)
//        {
//            // Attempt to find an existing style that matches the provided styleName
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            // If an existing style is found, return it
//            if (existingStyle != null) return existingStyle;

//            // Common settings for both versions
//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = useSolidColorRange ? 2 : 256,
//                ShowDataDescription = true,
//                Rounding = 1
//            };

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = useSolidColorRange ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor,
//                MinColor = new Autodesk.Revit.DB.Color(0, 0, 255), // Blue
//                MaxColor = new Autodesk.Revit.DB.Color(255, 0, 0)   // Red
//            };

//            if (useSolidColorRange)
//            {
//                colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry> {
//                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255)), // Blue
//                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0))   // Red
//                });
//            }

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        public static int GetOrCreateWDRBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Wind-Driven Rain" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }

//        public static void SetUpAndApplyWDRBasedAVF(Document doc, Face face, Reference faceRef, List<(XYZ IntersectionPoint, double WDRValue)> intersections, string selectedStyle)
//        {
//            if (face == null) throw new ArgumentNullException(nameof(face));
//            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
//            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
//            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

//            bool useSolidColorRange = selectedStyle == "SolidColor";
//            AnalysisDisplayStyle analysisDisplayStyle = CreateWDRBasedAnalysisDisplayStyle(doc, "Custom WDR-Based AVF Style", useSolidColorRange);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            int schemaIndex = GetOrCreateWDRBasedAnalysisResultSchemaIndex(sfm, "WDR-Based Schema", "Description");
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForWDRIntersections(uvPoints, intersections, face);

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        public static Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.SelectedElementId);
//            return element?.GetGeometryObjectFromReference(faceRef) as Face;
//        }

//        public static FieldDomainPointsByUV GetIntersectionFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv)) uvPoints.Add(uv);
//                }

//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        public static FieldValues GetFieldValuesForWDRIntersections(IList<UV> uvPoints, List<(XYZ IntersectionPoint, double WDRValue)> intersections, Face face)
//        {
//            List<ValueAtPoint> values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26; // Adjust this threshold as needed

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                var nearestIntersection = intersections
//                    .Select(intersect => new { Distance = point.DistanceTo(intersect.IntersectionPoint), intersect.WDRValue })
//                    .OrderBy(x => x.Distance)
//                    .FirstOrDefault();

//                double value = nearestIntersection != null && nearestIntersection.Distance <= proximityThreshold ? nearestIntersection.WDRValue : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }
//    }
//}

//////better that the top approach
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWindDrivenRain
//{
//    public static class WDRBasedAVF
//    {
//        public static void ApplyWDRBasedAVFToSelectedFaces(Document doc, Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue)>> faceIntersectionPoints, string selectedStyle)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<(XYZ IntersectionPoint, double WDRValue)> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyWDRBasedAVF(doc, face, faceRef, intersections, selectedStyle);
//                }
//            }
//        }

//        public static AnalysisDisplayStyle CreateWDRBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange)
//        {
//            // Attempt to find an existing style that matches the provided styleName
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            // If an existing style is found, return it
//            if (existingStyle != null) return existingStyle;

//            // Common settings for both versions
//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 5, // 5 steps for 5 different color ranges
//                ShowDataDescription = true,
//                Rounding = 0.03,
//                ShowUnits = true,
//                ShowDataName = true,
//                NumberForScale = 5,
//            };

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = AnalysisDisplayStyleColorSettingsType.GradientColor
//            };

//            // Define 5 color ranges based on WDR values
//            colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry>
//            {
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), 0),  // Blue for lowest WDR values
//                new AnalysisDisplayColorEntry( new Autodesk.Revit.DB.Color(0, 255, 255), 0.25), // Cyan
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), 0.5),  // Green
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), 0.75), // Yellow
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), 1)   // Red for highest WDR values
//            });

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        public static int GetOrCreateWDRBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Wind-Driven Rain" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }

//        public static void SetUpAndApplyWDRBasedAVF(Document doc, Face face, Reference faceRef, List<(XYZ IntersectionPoint, double WDRValue)> intersections, string selectedStyle)
//        {
//            if (face == null) throw new ArgumentNullException(nameof(face));
//            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
//            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
//            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

//            bool useSolidColorRange = selectedStyle == "SolidColor";
//            AnalysisDisplayStyle analysisDisplayStyle = CreateWDRBasedAnalysisDisplayStyle(doc, "Custom WDR-Based AVF Style", useSolidColorRange);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            int schemaIndex = GetOrCreateWDRBasedAnalysisResultSchemaIndex(sfm, "WDR-Based Schema", "Description");
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForWDRIntersections(uvPoints, intersections, face);

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        public static Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.SelectedElementId);
//            return element?.GetGeometryObjectFromReference(faceRef) as Face;
//        }

//        public static FieldDomainPointsByUV GetIntersectionFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv)) uvPoints.Add(uv);
//                }

//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        public static FieldValues GetFieldValuesForWDRIntersections(IList<UV> uvPoints, List<(XYZ IntersectionPoint, double WDRValue)> intersections, Face face)
//        {
//            List<ValueAtPoint> values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26; // Adjust this threshold as needed

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                var nearestIntersection = intersections
//                    .Select(intersect => new { Distance = point.DistanceTo(intersect.IntersectionPoint), intersect.WDRValue })
//                    .OrderBy(x => x.Distance)
//                    .FirstOrDefault();

//                double value = nearestIntersection != null && nearestIntersection.Distance <= proximityThreshold ? nearestIntersection.WDRValue : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }
//    }
//}


//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWindDrivenRain
//{
//    public static class WDRBasedAVF
//    {
//        public static void ApplyWDRBasedAVFToSelectedFaces(Document doc, Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue)>> faceIntersectionPoints, string selectedStyle)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<(XYZ IntersectionPoint, double WDRValue)> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyWDRBasedAVF(doc, face, faceRef, intersections, selectedStyle);
//                }
//            }
//        }

//        public static AnalysisDisplayStyle CreateWDRBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange, List<double> binEdges)
//        {
//            // Attempt to find an existing style that matches the provided styleName
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            // If an existing style is found, return it
//            if (existingStyle != null) return existingStyle;

//            // Common settings for both versions
//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = binEdges.Count - 1, // Number of bins
//                ShowDataDescription = true,
//                Rounding = 0.05
//            };

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = useSolidColorRange ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor,
//            };

//            // Define colors for the bins
//            colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry>
//            {
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), binEdges[0]),  // Blue for the lowest bin
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 255), binEdges[1]), // Cyan
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), binEdges[2]),  // Green
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), binEdges[3]), // Yellow
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), binEdges[4])   // Red for the highest bin
//            });

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        public static int GetOrCreateWDRBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Wind-Driven Rain" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }

//        public static void SetUpAndApplyWDRBasedAVF(Document doc, Face face, Reference faceRef, List<(XYZ IntersectionPoint, double WDRValue)> intersections, string selectedStyle)
//        {
//            if (face == null) throw new ArgumentNullException(nameof(face));
//            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
//            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
//            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

//            bool useSolidColorRange = selectedStyle == "SolidColor";

//            // Calculate histogram bins for the WDR values
//            List<double> wdrValues = intersections.Select(i => i.WDRValue).ToList();
//            List<double> binEdges = CalculateHistogramBins(wdrValues, 5); // 5 bins

//            AnalysisDisplayStyle analysisDisplayStyle = CreateWDRBasedAnalysisDisplayStyle(doc, "Custom WDR-Based AVF Style", useSolidColorRange, binEdges);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            int schemaIndex = GetOrCreateWDRBasedAnalysisResultSchemaIndex(sfm, "WDR-Based Schema", "Description");
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForWDRIntersections(uvPoints, intersections, face);

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        public static Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.SelectedElementId);
//            return element?.GetGeometryObjectFromReference(faceRef) as Face;
//        }

//        public static FieldDomainPointsByUV GetIntersectionFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv)) uvPoints.Add(uv);
//                }

//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        public static FieldValues GetFieldValuesForWDRIntersections(IList<UV> uvPoints, List<(XYZ IntersectionPoint, double WDRValue)> intersections, Face face)
//        {
//            List<ValueAtPoint> values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26; // Adjust this threshold as needed

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                var nearestIntersection = intersections
//                    .Select(intersect => new { Distance = point.DistanceTo(intersect.IntersectionPoint), intersect.WDRValue })
//                    .OrderBy(x => x.Distance)
//                    .FirstOrDefault();

//                double value = nearestIntersection != null && nearestIntersection.Distance <= proximityThreshold ? nearestIntersection.WDRValue : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }

//        private static List<double> CalculateHistogramBins(List<double> values, int binCount)
//        {
//            double min = values.Min();
//            double max = values.Max();
//            double binWidth = (max - min) / binCount;

//            List<double> binEdges = new List<double>();
//            for (int i = 0; i <= binCount; i++)
//            {
//                binEdges.Add(min + i * binWidth);
//            }

//            return binEdges;
//        }
//    }
//}





////beased on Percentile-Based Binning:it is good
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWindDrivenRain
//{
//    public static class WDRBasedAVF
//    {
//        public static void ApplyWDRBasedAVFToSelectedFaces(Document doc, Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue)>> faceIntersectionPoints, string selectedStyle)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<(XYZ IntersectionPoint, double WDRValue)> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyWDRBasedAVF(doc, face, faceRef, intersections, selectedStyle);
//                }
//            }
//        }

//        public static AnalysisDisplayStyle CreateWDRBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange, List<double> percentiles)
//        {
//            // Attempt to find an existing style that matches the provided styleName
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            // If an existing style is found, return it
//            if (existingStyle != null) return existingStyle;

//            // Common settings for both versions
//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 5, // 5 steps for 5 different color ranges
//                ShowDataDescription = true,
//                Rounding = 0.05
//            };

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                //ColorSettingsType = AnalysisDisplayStyleColorSettingsType.GradientColor
//                ColorSettingsType = useSolidColorRange ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor,
//            };

//            // Define 5 color ranges based on calculated percentiles
//            colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry>
//            {
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), percentiles[0]),  // Blue for lowest WDR values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 255), percentiles[1]), // Cyan
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), percentiles[2]),  // Green
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), percentiles[3]), // Yellow
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), percentiles[4])   // Red for highest WDR values
//            });

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        public static int GetOrCreateWDRBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Wind-Driven Rain" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }

//        public static void SetUpAndApplyWDRBasedAVF(Document doc, Face face, Reference faceRef, List<(XYZ IntersectionPoint, double WDRValue)> intersections, string selectedStyle)
//        {
//            if (face == null) throw new ArgumentNullException(nameof(face));
//            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
//            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
//            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

//            bool useSolidColorRange = selectedStyle == "SolidColor";

//            // Calculate percentiles for the WDR values
//            List<double> wdrValues = intersections.Select(i => i.WDRValue).ToList();
//            List<double> percentiles = CalculatePercentiles(wdrValues, new List<double> { 0, 25, 50, 75, 100 });

//            AnalysisDisplayStyle analysisDisplayStyle = CreateWDRBasedAnalysisDisplayStyle(doc, "Custom WDR-Based AVF Style", useSolidColorRange, percentiles);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            int schemaIndex = GetOrCreateWDRBasedAnalysisResultSchemaIndex(sfm, "WDR-Based Schema", "Description");
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForWDRIntersections(uvPoints, intersections, face);

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        public static Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.SelectedElementId);
//            return element?.GetGeometryObjectFromReference(faceRef) as Face;
//        }

//        public static FieldDomainPointsByUV GetIntersectionFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv)) uvPoints.Add(uv);
//                }

//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        public static FieldValues GetFieldValuesForWDRIntersections(IList<UV> uvPoints, List<(XYZ IntersectionPoint, double WDRValue)> intersections, Face face)
//        {
//            List<ValueAtPoint> values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26; // Adjust this threshold as needed

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                var nearestIntersection = intersections
//                    .Select(intersect => new { Distance = point.DistanceTo(intersect.IntersectionPoint), intersect.WDRValue })
//                    .OrderBy(x => x.Distance)
//                    .FirstOrDefault();

//                double value = nearestIntersection != null && nearestIntersection.Distance <= proximityThreshold ? nearestIntersection.WDRValue : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }

//        private static List<double> CalculatePercentiles(List<double> values, List<double> percentiles)
//        {
//            values.Sort();
//            List<double> result = new List<double>();
//            foreach (double percentile in percentiles)
//            {
//                int index = (int)(percentile / 100.0 * (values.Count - 1));
//                result.Add(values[index]);
//            }
//            return result;
//        }
//    }
//}





//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWindDrivenRain
//{
//    public static class WDRBasedAVF
//    {
//        public static void ApplyWDRBasedAVFToSelectedFaces(Document doc, Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue)>> faceIntersectionPoints, string selectedStyle)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<(XYZ IntersectionPoint, double WDRValue)> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyWDRBasedAVF(doc, face, faceRef, intersections, selectedStyle);
//                }
//            }
//        }

//        public static AnalysisDisplayStyle CreateWDRBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange, List<double> percentiles)
//        {
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            if (existingStyle != null) return existingStyle;

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 5,
//                ShowDataDescription = true,
//                Rounding = 0.05
//            };

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = useSolidColorRange ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor,
//            };

//            colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry>
//            {
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), percentiles[0]),  // Blue for lowest WDR values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 255), percentiles[1]), // Cyan
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), percentiles[2]),  // Green
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), percentiles[3]), // Yellow
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), percentiles[4])   // Red for highest WDR values
//            });

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        public static int GetOrCreateWDRBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Wind-Driven Rain" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }

//        public static void SetUpAndApplyWDRBasedAVF(Document doc, Face face, Reference faceRef, List<(XYZ IntersectionPoint, double WDRValue)> intersections, string selectedStyle)
//        {
//            if (face == null) throw new ArgumentNullException(nameof(face));
//            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
//            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
//            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

//            bool useSolidColorRange = selectedStyle == "SolidColor";

//            List<double> wdrValues = intersections.Select(i => i.WDRValue).ToList();
//            List<double> percentiles = CalculatePercentiles(wdrValues, new List<double> { 0, 25, 50, 75, 100 });

//            AnalysisDisplayStyle analysisDisplayStyle = CreateWDRBasedAnalysisDisplayStyle(doc, "Custom WDR-Based AVF Style", useSolidColorRange, percentiles);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            int schemaIndex = GetOrCreateWDRBasedAnalysisResultSchemaIndex(sfm, "WDR-Based Schema", "Description");
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForWDRIntersections(uvPoints, intersections, face);

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        public static Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.SelectedElementId);
//            return element?.GetGeometryObjectFromReference(faceRef) as Face;
//        }

//        public static FieldDomainPointsByUV GetIntersectionFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10;
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv)) uvPoints.Add(uv);
//                }

//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        public static FieldValues GetFieldValuesForWDRIntersections(IList<UV> uvPoints, List<(XYZ IntersectionPoint, double WDRValue)> intersections, Face face)
//        {
//            List<ValueAtPoint> values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26;

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                var nearestIntersection = intersections
//                    .Select(intersect => new { Distance = point.DistanceTo(intersect.IntersectionPoint), intersect.WDRValue })
//                    .OrderBy(x => x.Distance)
//                    .FirstOrDefault();

//                double value = nearestIntersection != null && nearestIntersection.Distance <= proximityThreshold ? nearestIntersection.WDRValue : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }

//        private static List<double> CalculatePercentiles(List<double> values, List<double> percentiles)
//        {
//            values.Sort();
//            List<double> result = new List<double>();
//            foreach (double percentile in percentiles)
//            {
//                int index = (int)(percentile / 100.0 * (values.Count - 1));
//                result.Add(values[index]);
//            }
//            return result;
//        }
//    }
//}


//dynamic 
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWindDrivenRain
{
    public static class WDRBasedAVF
    {
        public static void ApplyWDRBasedAVFToSelectedFaces(
            Document doc,
            Dictionary<Reference, List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)>> faceIntersectionPoints,
            string selectedStyle)
        {
            foreach (var entry in faceIntersectionPoints)
            {
                Reference faceRef = entry.Key;
                List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)> intersections = entry.Value;

                Face face = GetFaceFromReference(doc, faceRef);
                if (face != null)
                {
                    SetUpAndApplyWDRBasedAVF(doc, face, faceRef, intersections, selectedStyle);
                }
            }
        }

        public static AnalysisDisplayStyle CreateWDRBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange, List<double> percentiles)
        {
            var existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle))
                .Cast<AnalysisDisplayStyle>()
                .FirstOrDefault(style => style.Name.Equals(styleName));

            if (existingStyle != null) return existingStyle;

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = true,
                NumberOfSteps = percentiles.Count,
                ShowDataDescription = true,
                Rounding = 0.05
            };

            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                ColorSettingsType = useSolidColorRange ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor,
            };

            colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry>
            {
                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), percentiles[0]),  // Blue for lowest WDR values
                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 255), percentiles[1]), // Cyan
                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), percentiles[2]),  // Green
                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), percentiles[3]), // Yellow
                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), percentiles[4])   // Red for highest WDR values
            });

            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
        }

        public static int GetOrCreateWDRBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
        {
            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
            foreach (int index in existingSchemaIndices)
            {
                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
                {
                    return index;
                }
            }

            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
            schema.SetUnits(new List<string> { "Wind-Driven Rain" }, new List<double> { 1.0 });

            return sfm.RegisterResult(schema);
        }

        public static void SetUpAndApplyWDRBasedAVF(
          Document doc,
          Face face,
          Reference faceRef,
          List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)> intersections,
          string selectedStyle)
        {
            if (face == null) throw new ArgumentNullException(nameof(face));
            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            bool useSolidColorRange = selectedStyle == "SolidColor";

            List<double> wdrValues = intersections.Select(i => i.WDRValue).ToList();
            List<double> percentiles = CalculatePercentiles(wdrValues, new List<double> { 0, 25, 50, 75, 100 });

            AnalysisDisplayStyle analysisDisplayStyle = CreateWDRBasedAnalysisDisplayStyle(doc, "Custom WDR-Based AVF Style", useSolidColorRange, percentiles);
            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

            int schemaIndex = GetOrCreateWDRBasedAnalysisResultSchemaIndex(sfm, "WDR-Based Schema", "Description");
            IList<UV> uvPoints;
            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
            FieldValues fieldValues = GetFieldValuesForWDRIntersections(uvPoints, intersections, face);

            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        }

        public static Face GetFaceFromReference(Document doc, Reference faceRef)
        {
            Element element = doc.GetElement(faceRef.ElementId);
            return element?.GetGeometryObjectFromReference(faceRef) as Face;
        }

        public static FieldDomainPointsByUV GetIntersectionFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
        {
            uvPoints = new List<UV>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            double uStep = (bbox.Max.U - bbox.Min.U) / 10;
            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
                {
                    UV uv = new UV(u, v);
                    if (face.IsInside(uv)) uvPoints.Add(uv);
                }

            return new FieldDomainPointsByUV(uvPoints);
        }

        public static FieldValues GetFieldValuesForWDRIntersections(
      IList<UV> uvPoints,
      List<(XYZ IntersectionPoint, double WDRValue, string GridCellId)> intersections,
      Face face)
        {
            List<ValueAtPoint> values = new List<ValueAtPoint>();
            double proximityThreshold = 0.26;

            foreach (UV uv in uvPoints)
            {
                XYZ point = face.Evaluate(uv);
                var nearestIntersection = intersections
                    .Select(intersect => new { Distance = point.DistanceTo(intersect.IntersectionPoint), intersect.WDRValue })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                double value = nearestIntersection != null && nearestIntersection.Distance <= proximityThreshold ? nearestIntersection.WDRValue : 0;

                values.Add(new ValueAtPoint(new List<double> { value }));
            }

            return new FieldValues(values);
        }

        private static List<double> CalculatePercentiles(List<double> values, List<double> percentiles)
        {
            values.Sort();
            List<double> result = new List<double>();
            foreach (double percentile in percentiles)
            {
                int index = (int)(percentile / 100.0 * (values.Count - 1));
                result.Add(values[index]);
            }
            return result;
        }
    }
}
