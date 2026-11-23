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
    class WebcamEventHandler : IExternalEventHandler
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
            _event = ExternalEvent.Create(new WebcamEventHandler());

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

        #region GetFieldPointsAndValues - Determine field points and values for displaying the mapping
        static void GetFieldPointsAndValues(ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints, Face face)
        {
            BoundingBoxUV bb = face.GetBoundingBox();

            double umin = bb.Min.U;
            double umax = bb.Max.U;
            double ustep = (umax - umin) / _width;
            double u = umin;

            double v = bb.Min.V;
            double vmax = bb.Max.V;
            double vstep = (vmax - v) / _height;

            for (int y = 0; y < _height; ++y, v += vstep)
            {
                u = umin;

                for (int x = 0; x < _width; ++x, u += ustep)
                {
                    UV uv = new UV(u, v);

                    pts.Add(uv);

                    double relativePosition = CalculateRelativePosition(u, v, face);
                    double mappedValue = MapRelativePositionToValue(relativePosition);

                    List<double> values = new List<double> { mappedValue };
                    valuesAtPoints.Add(new ValueAtPoint(values));
                }
            }
        }
        #endregion // GetFieldPointsAndValues

        #region CalculateRelativePosition - Calculate the relative position of a point on the face
        static double CalculateRelativePosition(double u, double v, Face face)
        {
            // Implement your logic here to calculate the relative position of the point
            // based on its distance to the edges or other reference points on the face
            // and return the relative position value
            // You can use the u and v coordinates and the face geometry to perform calculations

            return 0.0; // Placeholder value, replace with your calculation logic
        }
        #endregion // CalculateRelativePosition

        #region MapRelativePositionToValue - Map the relative position to a value or color
        static double MapRelativePositionToValue(double relativePosition)
        {
            // Implement your logic here to map the relative position to a value or color
            // based on your desired mapping technique
            // You can use conditional statements, mathematical formulas, or lookup tables

            // Return the mapped value
            return 0.0; // Placeholder value, replace with your mapping logic
        }
        #endregion // MapRelativePositionToValue
    }
}