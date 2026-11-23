#if false
#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;
using System.Threading;
#endregion

namespace RevitWindDrivenRain
{
    class SurfaceColorMappingHandler : IExternalEventHandler
    {
        #region Member data
        const int _width = 200; // Requested image width
        const int _height = 200; // Requested image height
        static int _intervalMs = 200; // Time interval for image update in milliseconds

        static Reference _faceReference = null; // Reference to user-selected face to display the image onto
        static int _sfp_index = -1; // Spatial field primitive index
        static bool _running = false; // Flag indicating if the webcam event is running
        static ExternalEvent _event; // External event driving this handler
        #endregion // Member data

        #region CompareBytes - Compare two byte arrays
        static int CompareBytes(byte[] a, byte[] b)
        {
            int n = a.Length;
            int d = n - b.Length;

            if (d == 0)
            {
                for (int i = 0; i < n && d == 0; ++i)
                {
                    d = a[i] - b[i];
                }
            }
            return d;
        }
        #endregion // CompareBytes

        #region Log - Display a message with a time stamp in the debug output
        static void Log(string msg)
        {
            string dt = DateTime.Now.ToString("u");
            Debug.Print(dt + " " + msg);
        }
        #endregion // Log

        #region Start - Initialize the external webcam event
        public static void Start(View view, Reference r)
        {
            _faceReference = r;

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);

            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
            }
            else if (_sfp_index > 0)
            {
                sfm.RemoveSpatialFieldPrimitive(_sfp_index);
                _sfp_index = -1;
            }

            _sfp_index = sfm.AddSpatialFieldPrimitive(_faceReference);
            _event = ExternalEvent.Create(new SurfaceColorMappingHandler());

            Thread thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }
        #endregion // Start

        #region Run - External webcam event driver
        static void Run()
        {
            _running = true;

            while (_running)
            {
                _event.Raise();
                Thread.Sleep(_intervalMs);
            }
        }
        #endregion // Run

        #region Execute - External webcam event handler
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;

            if (uidoc == null)
            {
                _running = false;
            }
            else
            {
                Document doc = uidoc.Document;

                Log("OnIdling event triggered, active document: " + doc.Title);

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Revit Webcam Update");

                    View view = doc.ActiveView;

                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);

                    int nPoints = _width * _height;

                    IList<UV> pts = new List<UV>(nPoints);
                    IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>(nPoints);

                    Element eFace = doc.GetElement(_faceReference.ElementId);
                    Face face = eFace.GetGeometryObjectFromReference(_faceReference) as Face;

                    GetFieldPointsAndValues(ref pts, ref valuesAtPoints, face);

                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(pts);
                    FieldValues fieldValues = new FieldValues(valuesAtPoints);

                    int resultIndex;
                    IList<int> registeredResults = sfm.GetRegisteredResults();

                    if (registeredResults.Count == 0)
                    {
                        AnalysisResultSchema resultSchema = new AnalysisResultSchema("Schema 1", "Schema 1 Description");
                        resultIndex = sfm.RegisterResult(resultSchema);
                    }
                    else
                    {
                        resultIndex = registeredResults[0];
                    }

                    sfm.UpdateSpatialFieldPrimitive(_sfp_index, fieldPoints, fieldValues, resultIndex);

                    tx.Commit();
                }
            }
        }
        #endregion // Execute

        #region GetName - Return the name of the external event handler
        public string GetName()
        {
            return "RevitWindDrivenRain external event handler";
        }
        #endregion // GetName

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
}


#endif

#if false

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;
using System.Linq;
using System.Threading;

namespace RevitWindDrivenRain
{
    class SurfaceColorMappingHandler : IExternalEventHandler
    {
        private const int _width = 200;
        private const int _height = 200;
        private const int _intervalMs = 200;

        private static Document _doc;
        private static IList<Reference> _faceReferences;
        private static IList<UV> _pts;
        private static IList<ValueAtPoint> _valuesAtPoints;
        private static ExternalEvent _event;
        private static bool _running;

        public static void Start(View view, IList<Reference> faceReferences)
        {
            _doc = view.Document;
            _faceReferences = faceReferences;
            _pts = new List<UV>();
            _valuesAtPoints = new List<ValueAtPoint>();
            _event = ExternalEvent.Create(new SurfaceColorMappingHandler());

            Thread thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }

        private static void Run()
        {
            _running = true;

            while (_running)
            {
                GetFieldPointsAndValues();

                _event.Raise();
                Thread.Sleep(_intervalMs);
            }
        }

        private static void GetFieldPointsAndValues()
        {
            _pts.Clear();
            _valuesAtPoints.Clear();

            foreach (Reference faceReference in _faceReferences)
            {
                ElementId elementId = faceReference.ElementId;
                Element element = _doc.GetElement(elementId);
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(faceReference);
                Face face = geometryObject as Face;

                if (face != null)
                {
                    BoundingBoxUV boundingBox = face.GetBoundingBox();
                    double umin = boundingBox.Min.U;
                    double umax = boundingBox.Max.U;
                    double vmin = boundingBox.Min.V;
                    double vmax = boundingBox.Max.V;

                    double ustep = (umax - umin) / _width;
                    double vstep = (vmax - vmin) / _height;

                    List<double> values = new List<double>(1);

                    for (int y = 0; y < _height; ++y)
                    {
                        double v = vmin + y * vstep;

                        for (int x = 0; x < _width; ++x)
                        {
                            double u = umin + x * ustep;

                            XYZ point = face.Evaluate(new UV(u, v));
                            double relativePosition = CalculateRelativePosition(point, face);
                            double mappedValue = MapRelativePositionToValue(relativePosition);

                            _pts.Add(new UV(u, v));

                            values.Clear();
                            values.Add(mappedValue);
                            _valuesAtPoints.Add(new ValueAtPoint(values));
                        }
                    }
                }
            }
        }

        private static double CalculateRelativePosition(XYZ point, Face face)
        {
            BoundingBoxUV boundingBox = face.GetBoundingBox();

            double umin = boundingBox.Min.U;
            double umax = boundingBox.Max.U;
            double vmin = boundingBox.Min.V;
            double vmax = boundingBox.Max.V;

            double u = (point.X - umin) / (umax - umin);
            double v = (point.Y - vmin) / (vmax - vmin);

            return (u + v) / 2.0;
        }

        private static double MapRelativePositionToValue(double relativePosition)
        {
            // Define the range of values or colors that you want to map the relative position to
            double minValue = 0.0;
            double maxValue = 1.0;

            // Map the relative position to the desired value or color range
            double mappedValue = minValue + (relativePosition * (maxValue - minValue));

            return mappedValue;
        }

        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;

            if (uidoc == null)
            {
                _running = false;
            }
            else
            {
                Document doc = uidoc.Document;

                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Revit Surface Color Mapping");

                    View view = doc.ActiveView;
                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);

                    int resultIndex;
                    IList<int> registeredResults = sfm.GetRegisteredResults();

                    if (registeredResults.Count == 0)
                    {
                        AnalysisResultSchema resultSchema = new AnalysisResultSchema("Schema", "Schema Description");
                        resultIndex = sfm.RegisterResult(resultSchema);
                    }
                    else
                    {
                        resultIndex = registeredResults[0];
                    }

                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(_pts);
                    FieldValues fieldValues = new FieldValues(_valuesAtPoints);

                    int sfpIndex = sfm.AddSpatialFieldPrimitive(_faceReferences.First());

                    sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);

                    tx.Commit();
                }
            }
        }

        public string GetName()
        {
            return "RevitWindDrivenRain Surface Color Mapping";
        }
    }
}
#endif