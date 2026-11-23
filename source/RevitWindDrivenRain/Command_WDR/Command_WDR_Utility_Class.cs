using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWindDrivenRain
{
    public static class IntegratedBasedAVF
    {
        public static void ApplyIntersectionBasedAVFToSelectedFaces(Document doc, Dictionary<Reference, List<XYZ>> faceIntersectionPoints, string selectedStyle)
        {
            foreach (var entry in faceIntersectionPoints)
            {
                Reference faceRef = entry.Key;
                List<XYZ> intersections = entry.Value;

                Face face = GetFaceFromReference(doc, faceRef);
                if (face != null)
                {
                    SetUpAndApplyIntersectionBasedAVF(doc, face, faceRef, intersections, selectedStyle);
                }
            }
        }

        public static AnalysisDisplayStyle CreateIntersectionBasedAnalysisDisplayStyle(Document doc, string styleName, bool useSolidColorRange)
        {
            // Attempt to find an existing style that matches the provided styleName
            var existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle))
                .Cast<AnalysisDisplayStyle>()
                .FirstOrDefault(style => style.Name.Equals(styleName));

            // If an existing style is found, return it
            if (existingStyle != null) return existingStyle;

            // Common settings for both versions
            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = true,
                NumberOfSteps = useSolidColorRange ? 2 : 2,
                ShowDataDescription = true,
                Rounding = 1
            };

            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                ColorSettingsType = useSolidColorRange ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor,
                MinColor = new Autodesk.Revit.DB.Color(0, 0, 255), // Blue
                MaxColor = new Autodesk.Revit.DB.Color(255, 0, 0)   // Red
            };

            if (useSolidColorRange)
            {
                colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry> {
                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255)), // Blue
                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0))   // Red
                });
            }

            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
        }

        public static int GetOrCreateIntersectionBasedAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
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

        public static void SetUpAndApplyIntersectionBasedAVF(Document doc, Face face, Reference faceRef, List<XYZ> intersections, string selectedStyle)
        {
            if (face == null) throw new ArgumentNullException(nameof(face));
            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            bool useSolidColorRange = selectedStyle == "SolidColor";
            AnalysisDisplayStyle analysisDisplayStyle = CreateIntersectionBasedAnalysisDisplayStyle(doc, "Custom Intersection-Based AVF Style", useSolidColorRange);
            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

            int schemaIndex = GetOrCreateIntersectionBasedAnalysisResultSchemaIndex(sfm, "Intersection-Based Schema", "Description");
            IList<UV> uvPoints;
            FieldDomainPointsByUV fieldPoints = GetIntersectionFieldDomainPointsByUV(face, out uvPoints);
            FieldValues fieldValues = GetFieldValuesForIntersections(uvPoints, intersections, face);

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
            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
                {
                    UV uv = new UV(u, v);
                    if (face.IsInside(uv)) uvPoints.Add(uv);
                }

            return new FieldDomainPointsByUV(uvPoints);
        }

        public static FieldValues GetFieldValuesForIntersections(IList<UV> uvPoints, List<XYZ> intersections, Face face)
        {
            List<ValueAtPoint> values = new List<ValueAtPoint>();
            double proximityThreshold = 0.26; // Adjust this threshold as needed

            foreach (UV uv in uvPoints)
            {
                XYZ point = face.Evaluate(uv);
                double nearestDistance = intersections.Min(intersect => point.DistanceTo(intersect));
                double value = nearestDistance <= proximityThreshold ? 1 : 0;

                values.Add(new ValueAtPoint(new List<double> { value }));
            }

            return new FieldValues(values);
        }
    }
}
