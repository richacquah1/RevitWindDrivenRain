
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
        private static IList<Reference> _references;
        private static IList<UV> _pts;
        private static IList<ValueAtPoint> _valuesAtPoints;
        private static ExternalEvent _event;
        private static bool _running;
        private static SpatialFieldManager _sfm;

        public static void Start(View view, Reference reference)
        {
            _doc = view.Document;
            _references = new List<Reference> { reference };
            _pts = new List<UV>();
            _valuesAtPoints = new List<ValueAtPoint>();
            _event = ExternalEvent.Create(new SurfaceColorMappingHandler());
            _sfm = SpatialFieldManager.GetSpatialFieldManager(view);

            if (_sfm == null)
            {
                _sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
            }

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

            foreach (Reference reference in _references)
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

                    int resultIndex;
                    IList<int> registeredResults = _sfm.GetRegisteredResults();

                    if (registeredResults.Count == 0)
                    {
                        AnalysisResultSchema resultSchema = new AnalysisResultSchema("Schema", "Schema Description");
                        resultIndex = _sfm.RegisterResult(resultSchema);
                    }
                    else
                    {
                        resultIndex = registeredResults[0];
                    }

                    FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(_pts);
                    FieldValues fieldValues = new FieldValues(_valuesAtPoints);

                    int sfpIndex = _sfm.AddSpatialFieldPrimitive(_references.First());

                    _sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);

                    tx.Commit();
                }
            }
        }

        public string GetName()
        {
            return "SurfaceColorMappingHandler";
        }
    }
}