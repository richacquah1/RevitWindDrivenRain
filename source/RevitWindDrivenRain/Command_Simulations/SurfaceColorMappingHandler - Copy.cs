#if false
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System.Collections.Generic;
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
        public static IList<Reference> References { get; set; }
        public static IList<UV> Points { get; set; }
        public static IList<ValueAtPoint> ValuesAtPoints { get; set; }
        private static ExternalEvent _event;
        private static bool _running;

        public static void Start(View view, Reference reference)
        {
            _doc = view.Document;
            References = new List<Reference> { reference };
            Points = new List<UV>();
            ValuesAtPoints = new List<ValueAtPoint>();
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
            Points.Clear();
            ValuesAtPoints.Clear();

            foreach (Reference reference in References)
            {
                ElementId elementId = reference.ElementId;
                Element element = _doc.GetElement(elementId);
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
                Face face = geometryObject as Face;

                // Check if the object is a face
                if (face != null)
                {
                    // Get the bounding box of the face
                    BoundingBoxUV boundingBox = face.GetBoundingBox();
                    double umin = boundingBox.Min.U;
                    double umax = boundingBox.Max.U;
                    double vmin = boundingBox.Min.V;
                    double vmax = boundingBox.Max.V;

                    // Calculate the step size for each point in the field
                    double ustep = (umax - umin) / _width;
                    double vstep = (vmax - vmin) / _height;

                    List<double> values = new List<double>(1);

                    // Iterate through each point in the field
                    for (int y = 0; y < _height; ++y)
                    {
                        double v = vmin + y * vstep;

                        for (int x = 0; x < _width; ++x)
                        {
                            double u = umin + x * ustep;

                            // Evaluate the point on the face
                            XYZ point = face.Evaluate(new UV(u, v));

                            // Calculate the relative position and map it to a value
                            double relativePosition = CalculateRelativePosition(point, face);
                            double mappedValue = MapRelativePositionToValue(relativePosition);

                            // Add the point and value to the lists
                            Points.Add(new UV(u, v));

                            values.Clear();
                            values.Add(mappedValue);
                            ValuesAtPoints.Add(new ValueAtPoint(values));
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

                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(Points);
                    FieldValues fieldValues = new FieldValues(ValuesAtPoints);

                    int sfpIndex = sfm.AddSpatialFieldPrimitive(References.First());

                    sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);

                    tx.Commit();
                }
            }
        }

        public string GetName()
        {
            return "Surface Color Mapping Handler";
        }
    }
}
#endif

#if false 
//not working 
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System.Collections.Generic;
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
        public static IList<Reference> References { get; set; }
        public static IList<UV> Points { get; set; }
        public static IList<ValueAtPoint> ValuesAtPoints { get; set; }
        private static ExternalEvent _event;
        private static bool _running;

        public static void Start(View view, Reference reference)
        {
            _doc = view.Document;
            References = new List<Reference> { reference };
            Points = new List<UV>();
            ValuesAtPoints = new List<ValueAtPoint>();
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
            Points.Clear();
            ValuesAtPoints.Clear();

            foreach (Reference reference in References)
            {
                ElementId elementId = reference.ElementId;
                Element element = _doc.GetElement(elementId);
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
                Face face = geometryObject as Face;

                // Check if the object is a face
                if (face != null)
                {
                    // Get the bounding box of the face
                    BoundingBoxUV boundingBox = face.GetBoundingBox();
                    double umin = boundingBox.Min.U;
                    double umax = boundingBox.Max.U;
                    double vmin = boundingBox.Min.V;
                    double vmax = boundingBox.Max.V;

                    // Calculate the step size for each point in the field
                    double ustep = (umax - umin) / _width;
                    double vstep = (vmax - vmin) / _height;

                    List<double> values = new List<double>(1);

                    // Iterate through each point in the field
                    for (int y = 0; y < _height; ++y)
                    {
                        double v = vmin + y * vstep;

                        for (int x = 0; x < _width; ++x)
                        {
                            double u = umin + x * ustep;

                            // Evaluate the point on the face
                            XYZ point = face.Evaluate(new UV(u, v));

                            // Calculate the relative position and map it to a value
                            double relativePosition = CalculateRelativePosition(point, face);
                            double mappedValue = MapRelativePositionToValue(relativePosition);

                            // Add the point and value to the lists
                            Points.Add(new UV(u, v));

                            values.Clear();
                            values.Add(mappedValue);
                            ValuesAtPoints.Add(new ValueAtPoint(values));
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

                    // Find a 3D view to access the SpatialFieldManager
                    View3D view3D = new FilteredElementCollector(doc)
                        .OfClass(typeof(View3D))
                        .Cast<View3D>()
                        .FirstOrDefault();

                    if (view3D == null)
                    {
                        _running = false;
                        return;
                    }

                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view3D);

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

                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(Points);
                    FieldValues fieldValues = new FieldValues(ValuesAtPoints);

                    int sfpIndex = sfm.AddSpatialFieldPrimitive(References.First());

                    sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);

                    tx.Commit();
                }
            }
        }

        public string GetName()
        {
            return "Surface Color Mapping Handler";
        }
    }
}
#endif 

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitWindDrivenRain
{
    class SurfaceColorMappingHandler0000 : IExternalEventHandler
    {
        private const int _width = 200;
        private const int _height = 200;
        private const int _intervalMs = 200;

        private static Document _doc;
        public static IList<Reference> References { get; set; }
        public static IList<UV> Points { get; set; }
        public static IList<ValueAtPoint> ValuesAtPoints { get; set; }
        private static ExternalEvent _event;
        private static bool _running;

        // New variables
        static Reference _faceReference = null; // Reference to user-selected face to display the image onto
        static int _sfp_index = -1; // Spatial field primitive index

        public static void Start(View view, Reference r)
        {
            _faceReference = r; // Set the face reference
            _doc = view.Document;
            References = new List<Reference> { r }; // Change the reference here
            Points = new List<UV>();
            ValuesAtPoints = new List<ValueAtPoint>();

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
            Points.Clear();
            ValuesAtPoints.Clear();

            foreach (Reference reference in References)
            {
                ElementId elementId = reference.ElementId;
                Element element = _doc.GetElement(elementId);
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
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

                            Points.Add(new UV(u, v));

                            values.Clear();
                            values.Add(mappedValue);
                            ValuesAtPoints.Add(new ValueAtPoint(values));
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
            double minValue = 0.0;
            double maxValue = 1.0;

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

                    View3D view3D = new FilteredElementCollector(doc)
                        .OfClass(typeof(View3D))
                        .Cast<View3D>()
                        .FirstOrDefault();

                    if (view3D == null)
                    {
                        _running = false;
                        return;
                    }

                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view3D);

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

                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(Points);
                    FieldValues fieldValues = new FieldValues(ValuesAtPoints);

                    sfm.UpdateSpatialFieldPrimitive(_sfp_index, fieldPoints, fieldValues, resultIndex);

                    tx.Commit();
                }
            }
        }

        public string GetName()
        {
            return "Surface Color Mapping Handler";
        }
    }
}

