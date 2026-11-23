#if false
#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SolarAngles;
#endregion

namespace RevitWindDrivenRain
{
    static double CalculateRelativePosition(XYZ point, Face face)
    {
        BoundingBoxUV boundingBox = face.GetBoundingBox();

        double umin = boundingBox.Min.U;
        double umax = boundingBox.Max.U;
        double vmin = boundingBox.Min.V;
        double vmax = boundingBox.Max.V;

        double u = (point.X - umin) / (umax - umin); // Calculate the relative U position
        double v = (point.Y - vmin) / (vmax - vmin); // Calculate the relative V position

        return (u + v) / 2.0; // Return the average of the relative U and V positions as the overall relative position
    }

    static double MapRelativePositionToValue(double relativePosition)
    {
        // Define the range of values or colors that you want to map the relative position to
        double minValue = 0.0;
        double maxValue = 1.0;

        // Map the relative position to the desired value or color range
        double mappedValue = minValue + (relativePosition * (maxValue - minValue));

        return mappedValue;
    }

    static void GetFieldPointsAndValues(ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints, Face face)
    {
        BoundingBoxUV boundingBox = face.GetBoundingBox();

        // Iterate over the points on the face
        foreach (XYZ point in face.Triangulate().Vertices)
        {
            // Calculate the relative position of the point on the face
            double relativePosition = CalculateRelativePosition(point, face);

            // Map the relative position to a value or color
            double value = MapRelativePositionToValue(relativePosition);

            // Add the point and value to the lists
            UV uv = new UV(point.X, point.Y);
            pts.Add(uv);
            valuesAtPoints.Add(new ValueAtPoint(new List<double> { value }));
        }
    }

}

#endif