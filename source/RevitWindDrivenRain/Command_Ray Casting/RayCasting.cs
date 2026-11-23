

 //this works perfect. allows users to select an element in a Revit model, then generates and visualizes rays cast from its surfaces
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RayCasting : IExternalCommand
    {
        private bool createVisualization = true; // Set this to false to disable DirectShape creation

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Part 1: Element Selection and Display Properties
            ElementId selectedId = uiDoc.Selection.PickObject(ObjectType.Element).ElementId;
            Element selectedElement = doc.GetElement(selectedId);

            TaskDialog.Show("Element Info", $"Element ID: {selectedId}\nElement Type: {selectedElement.GetType().Name}\nElement Name: {selectedElement.Name}");

            // Part 2: Display Geometry Information
            Options options = new Options();
            GeometryElement geomElem = selectedElement.get_Geometry(options);
            int numFaces = 0;
            int numEdges = 0;

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    numFaces += solid.Faces.Size;
                    numEdges += solid.Edges.Size;
                }
                else if (geomObj is GeometryInstance geomInstance)
                {
                    GeometryElement instanceGeom = geomInstance.GetInstanceGeometry();
                    foreach (GeometryObject instanceGeomObj in instanceGeom)
                    {
                        if (instanceGeomObj is Solid instanceSolid)
                        {
                            numFaces += instanceSolid.Faces.Size;
                            numEdges += instanceSolid.Edges.Size;
                        }
                    }
                }
            }

            TaskDialog.Show("Geometry Info", $"Number of Faces: {numFaces}\nNumber of Edges: {numEdges}");

            // Part 3: Create and Visualize Rays
            using (Transaction trans = new Transaction(doc, "Create Rays"))
            {
                trans.Start();

                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is Solid solid)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace planarFace = face as PlanarFace;
                            if (planarFace != null)
                            {
                                // ISO-WDR-based ray casting logic here
                                CreateRaysForFace(face, planarFace, doc);
                            }
                        }
                    }
                    else if (geomObj is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeom = geomInstance.GetInstanceGeometry();
                        foreach (GeometryObject instanceGeomObj in instanceGeom)
                        {
                            if (instanceGeomObj is Solid instanceSolid)
                            {
                                foreach (Face face in instanceSolid.Faces)
                                {
                                    PlanarFace planarFace = face as PlanarFace;
                                    if (planarFace != null)
                                    {
                                        // ISO-WDR-based ray casting logic here
                                        CreateRaysForFace(face, planarFace, doc);
                                    }
                                }
                            }
                        }
                    }
                }

                trans.Commit();
            }

            // Function to create rays for a single face
            void CreateRaysForFace(Face face, PlanarFace planarFace, Document doc)
            {
                BoundingBoxUV bbox = face.GetBoundingBox();
                UV min = bbox.Min;
                UV max = bbox.Max;

                // Calculate the dimensions of the face
                double faceWidth = max.U - min.U;
                double faceHeight = max.V - min.V;

                // Set a minimum grid size
                double minGridSize = 0.1; // You can adjust this value

                // Decide the number of grids based on the face dimensions and minimum grid size
                int numGridsX = Math.Max(1, (int)Math.Floor(faceWidth / minGridSize));
                int numGridsY = Math.Max(1, (int)Math.Floor(faceHeight / minGridSize));

                double gridWidth = faceWidth / numGridsX;
                double gridHeight = faceHeight / numGridsY;

                Debug.WriteLine($"Creating rays for face...");
                Debug.WriteLine($"Face Width: {faceWidth}, Face Height: {faceHeight}");
                Debug.WriteLine($"Grid Width: {gridWidth}, Grid Height: {gridHeight}");

                for (double u = min.U; u <= max.U; u += gridWidth)
                {
                    for (double v = min.V; v <= max.V; v += gridHeight)
                    {
                        double u_centroid = (u + u + gridWidth) / 2.0;
                        double v_centroid = (v + v + gridHeight) / 2.0;

                        // Check if u and v for the centroid are within the bounds of the face
                        if (u_centroid >= min.U && u_centroid <= max.U && v_centroid >= min.V && v_centroid <= max.V)
                        {
                            Debug.WriteLine($"Current u: {u}, Current v: {v}");

                            UV centroidUV = new UV(u_centroid, v_centroid);
                            XYZ centroid = face.Evaluate(centroidUV);

                            if (createVisualization) // Check the flag here
                            {
                                // Create DirectShape to visualize the ray
                                Line rayLine = Line.CreateBound(centroid, centroid + planarFace.FaceNormal.Normalize() * 10);  // Length of 10
                                DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                                rayShape.SetShape(new GeometryObject[] { rayLine });
                                rayShape.Name = "Ray";
                            }
                        }
                    }
                }
            }

            return Result.Succeeded;
        }

    }
}


