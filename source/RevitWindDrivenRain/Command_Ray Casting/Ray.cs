/*
 //this works perfect. perfect example of ray casting
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class WallToWallDistance : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            using (Transaction trans = new Transaction(doc, "Calculate Wall to Wall Distance"))
            {
                trans.Start();

                // Assume point is picked by the user where the ray will be cast
                XYZ pickedPoint = uiDoc.Selection.PickPoint("Pick a point to cast the ray");

                // Assume the ray direction is horizontally across the room (change as needed)
                XYZ rayDirection = new XYZ(1, 0, 0); // Along the X-axis

                double distance = GetWallToWallDistance(pickedPoint, rayDirection, doc);

                // Display the calculated distance using TaskDialog
                TaskDialog.Show("Distance Information", $"Wall to Wall Distance: {distance} units");

                trans.Commit();
            }

            return Result.Succeeded;
        }

        public double GetWallToWallDistance(XYZ startPoint, XYZ rayDirection, Document doc)
        {
            // Find a 3D view for ReferenceIntersector
            View3D view3D = new FilteredElementCollector(doc)
                                .OfClass(typeof(View3D))
                                .Cast<View3D>()
                                .FirstOrDefault(v => !v.IsTemplate);

            // Filter to only consider wall elements
            ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));
            ReferenceIntersector refIntersector = new ReferenceIntersector(wallFilter, FindReferenceTarget.Element, view3D);

            // Find the nearest wall in the direction of the ray
            ReferenceWithContext nearestWall1 = refIntersector.FindNearest(startPoint, rayDirection);
            if (nearestWall1 == null) return 0; // No wall found

            XYZ intersection1 = nearestWall1.GetReference().GlobalPoint;

            // Find the nearest wall in the opposite direction of the ray
            ReferenceWithContext nearestWall2 = refIntersector.FindNearest(startPoint, -rayDirection);
            if (nearestWall2 == null) return 0; // No wall found

            XYZ intersection2 = nearestWall2.GetReference().GlobalPoint;

            // Calculate the distance between the two walls
            double distance = intersection1.DistanceTo(intersection2);

            return distance;
        }
    }
}
*/


/*This code prompts the user to pick an element and a point in Autodesk Revit, 
then casts rays in multiple directions from the picked point to find intersecting 
elements, filtering out the initially picked element, and finally displays the directions and Element IDs of the 
intersecting elements in a dialog box.*/
/*
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            // Get the active view as a View3D object
            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;

            // Check if the active view is a 3D view
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            // Prompt the user to pick an element
            Reference pickedElementRef = sel.PickObject(ObjectType.Element, "Please pick an element.");
            Element pickedElement = doc.GetElement(pickedElementRef);
            TaskDialog.Show("Picked Element", $"You have picked an element with ID: {pickedElement.Id.ToString()}");

            // Prompt the user to pick a point
            XYZ pickedPoint = sel.PickPoint("Please pick a point to start the ray.");

            // Define multiple directions for the ray
            List<XYZ> directions = new List<XYZ>
    {
        new XYZ(1, 0, 0),
        new XYZ(0, 1, 0),
        new XYZ(0, 0, 1),
        new XYZ(-1, 0, 0),
        new XYZ(0, -1, 0),
        new XYZ(0, 0, -1)
    };

            // Create ReferenceIntersector
            ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);

            // Small offset value
            double offset = 0.001;

            // Create a dictionary to hold direction and SelectedElementId pairs
            Dictionary<XYZ, SelectedElementId> directionElementMap = new Dictionary<XYZ, SelectedElementId>();

            foreach (XYZ direction in directions)
            {
                // Offset the starting point slightly in the current direction
                XYZ offsetPoint = pickedPoint + direction * offset;

                // Find intersecting elements in the current direction
                ReferenceWithContext nearestRef = refIntersector.FindNearest(offsetPoint, direction);

                // Save the result for the current direction
                if (nearestRef != null)
                {
                    Element nearestElement = doc.GetElement(nearestRef.GetReference());
                    directionElementMap[direction] = nearestElement.Id;
                }
            }

            // Filter out the initially picked element
            SelectedElementId pickedElementId = pickedElement.Id;
            Dictionary<XYZ, SelectedElementId> filteredResults = new Dictionary<XYZ, SelectedElementId>();

            foreach (var pair in directionElementMap)
            {
                if (pair.Value != pickedElementId)
                {
                    filteredResults[pair.Key] = pair.Value;
                }
            }

            // Display the filtered results
            string dialogMessage = "Filtered Intersecting Elements:\n";
            foreach (var pair in filteredResults)
            {
                dialogMessage += $"Direction: {pair.Key}, Element ID: {pair.Value}\n";
            }

            TaskDialog.Show("Filtered Results", dialogMessage);



            return Result.Succeeded;
        }

    }
}
*/

/*
This code casts rays from a picked face in Revit, highlights intersecting elements, 
and displays their IDs, excluding the picked element.*/
/*
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWindDrivenRain
{
    public class FaceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return true;
        }

        public bool AllowReference(Reference reference, XYZ point)
        {
            return reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_SURFACE;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            // Get the active view as a View3D object
            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;

            // Check if the active view is a 3D view
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            // Prompt the user to pick a face
            Reference pickedFaceRef = sel.PickObject(ObjectType.Face, new FaceSelectionFilter(), "Please pick a face.");
            Element pickedElement = doc.GetElement(pickedFaceRef);
            Face pickedFace = pickedElement.GetGeometryObjectFromReference(pickedFaceRef) as Face;

            // Get the centroid of the picked face to be used as the starting point
            XYZ pickedPoint = pickedFace.Evaluate(pickedFace.GetBoundingBox().Min + 0.5 * (pickedFace.GetBoundingBox().Max - pickedFace.GetBoundingBox().Min));

            // Small offset value
            double offset = 0.001;

            // Define multiple directions for the ray
            List<XYZ> directions = new List<XYZ>
    {
        new XYZ(1, 0, 0),
        new XYZ(0, 1, 0),
        new XYZ(0, 0, 1),
        new XYZ(-1, 0, 0),
        new XYZ(0, -1, 0),
        new XYZ(0, 0, -1)
    };

            // Create ReferenceIntersector
            ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);

            // Create a dictionary to hold direction and SelectedElementId pairs
            Dictionary<XYZ, SelectedElementId> directionElementMap = new Dictionary<XYZ, SelectedElementId>();

            foreach (XYZ direction in directions)
            {
                // Offset the starting point slightly in the current direction
                XYZ offsetPoint = pickedPoint + direction * offset;

                // Find intersecting elements in the current direction
                ReferenceWithContext nearestRef = refIntersector.FindNearest(offsetPoint, direction);

                // Save the result for the current direction
                if (nearestRef != null)
                {
                    Element nearestElement = doc.GetElement(nearestRef.GetReference());
                    directionElementMap[direction] = nearestElement.Id;
                }
            }

            // Create a list to hold the ElementIds of the filtered results
            List<SelectedElementId> highlightedElements = new List<SelectedElementId>();

            // Filter out the initially picked element
            SelectedElementId pickedElementId = pickedElement.Id;
            Dictionary<XYZ, SelectedElementId> filteredResults = new Dictionary<XYZ, SelectedElementId>();

            foreach (var pair in directionElementMap)
            {
                if (pair.Value != pickedElementId)
                {
                    filteredResults[pair.Key] = pair.Value;
                    highlightedElements.Add(pair.Value);
                }
            }

            // Highlight the filtered intersecting elements in the Revit interface
            sel.SetElementIds(highlightedElements);

            // Display the filtered results
            string dialogMessage;

            if (filteredResults.Count == 0)
            {
                dialogMessage = "No intersecting elements found in any direction, aside from the initially picked element.";
            }
            else
            {
                dialogMessage = "Filtered Intersecting Elements:\n";
                foreach (var pair in filteredResults)
                {
                    dialogMessage += $"Direction: {pair.Key}, Element ID: {pair.Value}\n";
                }
            }

            TaskDialog.Show("Filtered Results", dialogMessage);

            return Result.Succeeded;
        }

    }
}
*/
/*
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            // Get the active view as a View3D object
            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;

            // Check if the active view is a 3D view
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            // Prompt the user to pick an element
            Reference pickedElementRef = sel.PickObject(ObjectType.Element, "Please pick an element.");
            Element pickedElement = doc.GetElement(pickedElementRef);

            // Prepare for geometry options
            Options geomOptions = new Options();

            // Get geometry of the selected element
            GeometryElement geomElem;

            if (pickedElement is FamilyInstance)
            {
                FamilyInstance familyInstance = pickedElement as FamilyInstance;
                geomElem = familyInstance.Symbol.get_Geometry(geomOptions);
                // Apply the family instance's transform to the geometry, if needed
            }
            else
            {
                geomElem = pickedElement.get_Geometry(geomOptions);
            }


            // Initialize list to hold highlighted ElementIds
            List<SelectedElementId> highlightedElements = new List<SelectedElementId>();

            // Loop through all faces of the element
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        // Get the centroid of the face to be used as the starting point
                        XYZ pickedPoint = face.Evaluate(face.GetBoundingBox().Min + 0.5 * (face.GetBoundingBox().Max - face.GetBoundingBox().Min));

                        // Small offset value
                        double offset = 0.001;

                        // Define multiple directions for the ray
                        List<XYZ> directions = new List<XYZ>
                        {
                            new XYZ(1, 0, 0),
                            new XYZ(0, 1, 0),
                            new XYZ(0, 0, 1),
                            new XYZ(-1, 0, 0),
                            new XYZ(0, -1, 0),
                            new XYZ(0, 0, -1)
                        };

                        // Create ReferenceIntersector
                        ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);

                        // Create a dictionary to hold direction and SelectedElementId pairs
                        Dictionary<XYZ, SelectedElementId> directionElementMap = new Dictionary<XYZ, SelectedElementId>();

                        foreach (XYZ direction in directions)
                        {
                            // Offset the starting point slightly in the current direction
                            XYZ offsetPoint = pickedPoint + direction * offset;

                            // Find intersecting elements in the current direction
                            ReferenceWithContext nearestRef = refIntersector.FindNearest(offsetPoint, direction);

                            // Save the result for the current direction
                            if (nearestRef != null)
                            {
                                Element nearestElement = doc.GetElement(nearestRef.GetReference());
                                directionElementMap[direction] = nearestElement.Id;
                            }
                        }

                        // Filter out the initially picked element
                        SelectedElementId pickedElementId = pickedElement.Id;

                        foreach (var pair in directionElementMap)
                        {
                            if (pair.Value != pickedElementId)
                            {
                                highlightedElements.Add(pair.Value);
                            }
                        }
                    }
                }
            }

            // Highlight the filtered intersecting elements in the Revit interface
            sel.SetElementIds(highlightedElements);

            // Display the filtered results
            string dialogMessage;

            if (highlightedElements.Count == 0)
            {
                dialogMessage = "No intersecting elements found in any direction, aside from the initially picked element.";
            }
            else
            {
                dialogMessage = $"Filtered Intersecting Elements: {string.Join(", ", highlightedElements)}";
            }

            TaskDialog.Show("Filtered Results", dialogMessage);

            return Result.Succeeded;
        }
    }
}
*/


// this works but needs to be optimized
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            // Get the active view as a View3D object
            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;

            // Check if the active view is a 3D view
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            // Prompt the user to pick an element (this can also be an instance)
            Reference pickedElementRef = sel.PickObject(ObjectType.Element, "Please pick an element.");
            Element pickedElement = doc.GetElement(pickedElementRef);

            // Prepare for geometry options
            Options geomOptions = new Options();

            // Get the geometry of the selected element (or its symbol if it's an instance)
            GeometryElement geomElem = (pickedElement is FamilyInstance) ?
                ((FamilyInstance)pickedElement).Symbol.get_Geometry(geomOptions) :
                pickedElement.get_Geometry(geomOptions);

            // Initialize list to hold highlighted ElementIds
            List<ElementId> highlightedElements = new List<ElementId>();

            // Loop through all faces of the element
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        // Get the centroid of the face to be used as the starting point
                        XYZ pickedPoint = face.Evaluate(face.GetBoundingBox().Min + 0.5 * (face.GetBoundingBox().Max - face.GetBoundingBox().Min));

                        // Small offset value
                        double offset = 0.001;

                        // Define multiple directions for the ray
                        List<XYZ> directions = new List<XYZ>
                        {
                            new XYZ(1, 0, 0),
                            new XYZ(0, 1, 0),
                            new XYZ(0, 0, 1),
                            new XYZ(-1, 0, 0),
                            new XYZ(0, -1, 0),
                            new XYZ(0, 0, -1)
                        };

                        // Create ReferenceIntersector
                        ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);

                        // Create a dictionary to hold direction and SelectedElementId pairs
                        Dictionary<XYZ, ElementId> directionElementMap = new Dictionary<XYZ, ElementId>();

                        foreach (XYZ direction in directions)
                        {
                            // Offset the starting point slightly in the current direction
                            XYZ offsetPoint = pickedPoint + direction * offset;

                            // Find intersecting elements in the current direction
                            ReferenceWithContext nearestRef = refIntersector.FindNearest(offsetPoint, direction);

                            // Save the result for the current direction if it's a solid
                            // Save the result for the current direction if it's a solid
                            if (nearestRef != null)
                            {
                                Element nearestElement = doc.GetElement(nearestRef.GetReference());
                                GeometryElement geomElemNearest;

                                if (nearestElement is FamilyInstance)
                                {
                                    FamilyInstance familyInstance = nearestElement as FamilyInstance;
                                    geomElemNearest = familyInstance.Symbol.get_Geometry(new Options());
                                }
                                else
                                {
                                    geomElemNearest = nearestElement.get_Geometry(new Options());
                                }

                                if (geomElemNearest != null)  // Add this null check
                                {
                                    bool isSolid = false;
                                    foreach (GeometryObject geomObjNearest in geomElemNearest)
                                    {
                                        if (geomObjNearest is Solid solidNearest && solidNearest.Volume > 0)
                                        {
                                            isSolid = true;
                                            break;
                                        }
                                    }

                                    if (isSolid)
                                    {
                                        directionElementMap[direction] = nearestElement.Id;
                                    }
                                }
                            }

                        }

                        // Filter out the initially picked element
                        ElementId pickedElementId = pickedElement.Id;

                        foreach (var pair in directionElementMap)
                        {
                            if (pair.Value != pickedElementId)
                            {
                                highlightedElements.Add(pair.Value);
                            }
                        }
                    }
                }
            }

            // Highlight the filtered intersecting elements in the Revit interface
            sel.SetElementIds(highlightedElements);

            // Display the filtered results
            string dialogMessage;

            if (highlightedElements.Count == 0)
            {
                dialogMessage = "No intersecting solid elements found in any direction, aside from the initially picked element.";
            }
            else
            {
                dialogMessage = $"Filtered Intersecting Solid Elements: {string.Join(", ", highlightedElements)}";
            }

            TaskDialog.Show("Filtered Results", dialogMessage);

            return Result.Succeeded;
        }
    }
}

/*
 //works perfect used FindNearest Method
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            // Get the active view as a View3D object
            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;

            // Check if the active view is a 3D view
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            // Prompt the user to pick an element (this can also be an instance)
            Reference pickedElementRef = sel.PickObject(ObjectType.Element, "Please pick an element.");
            Element pickedElement = doc.GetElement(pickedElementRef);

            // Prepare for geometry options
            Options geomOptions = new Options();

            // Get the geometry of the selected element (or its symbol if it's an instance)
            GeometryElement geomElem = (pickedElement is FamilyInstance) ?
                ((FamilyInstance)pickedElement).Symbol.get_Geometry(geomOptions) :
                pickedElement.get_Geometry(geomOptions);

            // Initialize list to hold highlighted ElementIds
            List<SelectedElementId> highlightedElements = new List<SelectedElementId>();

            // Loop through all faces of the element
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        PlanarFace planarFace = face as PlanarFace; // <-- Add this line

                        // Get the centroid of the face to be used as the starting point
                        XYZ pickedPoint = face.Evaluate(face.GetBoundingBox().Min + 0.5 * (face.GetBoundingBox().Max - face.GetBoundingBox().Min));

                        // Small offset value
                        double offset = 0.001;

                        // Define multiple directions for the ray
                        List<XYZ> directions = new List<XYZ>
                        {
                            new XYZ(1, 0, 0),
                            new XYZ(0, 1, 0),
                            new XYZ(0, 0, 1),
                            new XYZ(-1, 0, 0),
                            new XYZ(0, -1, 0),
                            new XYZ(0, 0, -1)
                        };

                        // Prompt the user with a checkbox to visualize rays
                        bool visualizeRays = PromptUserForVisualization();
                        if (visualizeRays)
                        {
                            using (Transaction tx = new Transaction(doc))
                            {
                                tx.Start("Visualize Rays");

                                // Visualization logic for rays
                                //VisualizeRays(directions, pickedPoint, doc, uiDoc);
                                //VisualizeRays(face,planarFace, doc);
                                VisualizeRays(pickedPoint, directions, doc);

                                tx.Commit();
                            }

                        }

                        // Create ReferenceIntersector
                        ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);

                        // Create a dictionary to hold direction and SelectedElementId pairs
                        Dictionary<XYZ, SelectedElementId> directionElementMap = new Dictionary<XYZ, SelectedElementId>();

                        foreach (XYZ direction in directions)
                        {
                            // Offset the starting point slightly in the current direction
                            XYZ offsetPoint = pickedPoint + direction * offset;

                            // Find intersecting elements in the current direction
                            //ReferenceWithContext nearestRef = refIntersector.FindNearest(offsetPoint, direction);
                             ReferenceWithContext nearestRef = refIntersector.FindNearest(offsetPoint, direction);

                            // Save the result for the current direction if it's a solid
                            if (nearestRef != null)
                            {
                                Element nearestElement = doc.GetElement(nearestRef.GetReference());
                                GeometryElement geomElemNearest;

                                if (nearestElement is FamilyInstance)
                                {
                                    FamilyInstance familyInstance = nearestElement as FamilyInstance;
                                    geomElemNearest = familyInstance.Symbol.get_Geometry(new Options());
                                }
                                else
                                {
                                    geomElemNearest = nearestElement.get_Geometry(new Options());
                                }

                                if (geomElemNearest != null)  // Add this null check
                                {
                                    bool isSolid = false;
                                    foreach (GeometryObject geomObjNearest in geomElemNearest)
                                    {
                                        if (geomObjNearest is Solid solidNearest && solidNearest.Volume > 0)
                                        {
                                            isSolid = true;
                                            break;
                                        }
                                    }

                                    if (isSolid)
                                    {
                                        directionElementMap[direction] = nearestElement.Id;
                                    }
                                }
                            }

                        }

                        // Filter out the initially picked element
                        SelectedElementId pickedElementId = pickedElement.Id;

                        foreach (var pair in directionElementMap)
                        {
                            if (pair.Value != pickedElementId)
                            {
                                highlightedElements.Add(pair.Value);
                            }

                        }
                    }
                }

                // Highlight the filtered intersecting elements in the Revit interface
                sel.SetElementIds(highlightedElements);

                // Display the filtered results
                string dialogMessage;

                if (highlightedElements.Count == 0)
                {
                    dialogMessage = "No intersecting solid elements found in any direction, aside from the initially picked element.";
                }
                else
                {
                    dialogMessage = $"Filtered Intersecting Solid Elements: {string.Join(", ", highlightedElements)}";
                }

                TaskDialog.Show("Filtered Results", dialogMessage);
            }


            return Result.Succeeded;
        }




        private bool PromptUserForVisualization()
        {
            using (System.Windows.Forms.Form prompt = new System.Windows.Forms.Form())
            {
                prompt.Width = 250;
                prompt.Height = 150;
                prompt.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                prompt.Text = "Visualization Options";
                prompt.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                System.Windows.Forms.CheckBox visualizeCheckbox = new System.Windows.Forms.CheckBox
                {
                    Text = "Visualize Rays",
                    AutoSize = true,
                    Checked = false,
                    Location = new System.Drawing.Point(10, 10)
                };
                prompt.Controls.Add(visualizeCheckbox);

                System.Windows.Forms.Button okButton = new System.Windows.Forms.Button
                {
                    Text = "OK",
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Location = new System.Drawing.Point(10, 40)
                };
                prompt.Controls.Add(okButton);
                prompt.AcceptButton = okButton;

                System.Windows.Forms.DialogResult dialogResult = prompt.ShowDialog();
                return dialogResult == System.Windows.Forms.DialogResult.OK && visualizeCheckbox.Checked;
            }
        }


        //void VisualizeRays(Face face, PlanarFace planarFace, Document doc)
        //{
        //    BoundingBoxUV bbox = face.GetBoundingBox();
        //    UV min = bbox.Min;
        //    UV max = bbox.Max;

        //    double faceWidth = max.U - min.U;
        //    double faceHeight = max.V - min.V;

        //    double minGridSize = 1.5; // You can adjust this value

        //    int numGridsX = Math.Max(1, (int)Math.Floor(faceWidth / minGridSize));
        //    int numGridsY = Math.Max(1, (int)Math.Floor(faceHeight / minGridSize));

        //    double gridWidth = faceWidth / numGridsX;
        //    double gridHeight = faceHeight / numGridsY;

        //    for (double u = min.U; u <= max.U; u += gridWidth)
        //    {
        //        for (double v = min.V; v <= max.V; v += gridHeight)
        //        {
        //            double u_centroid = (u + u + gridWidth) / 2.0;
        //            double v_centroid = (v + v + gridHeight) / 2.0;

        //            if (u_centroid >= min.U && u_centroid <= max.U && v_centroid >= min.V && v_centroid <= max.V)
        //            {
        //                UV centroidUV = new UV(u_centroid, v_centroid);
        //                XYZ centroid = face.Evaluate(centroidUV);

        //                Line rayLine = Line.CreateBound(centroid, centroid + planarFace.FaceNormal.Normalize() * 10); // Length of 10 units

        //                // Create DirectShape to visualize the ray
        //                DirectShape rayShape = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
        //                rayShape.SetShape(new GeometryObject[] { rayLine });
        //                rayShape.Name = "Ray";
        //            }
        //        }
        //    }
        //}


        void VisualizeRays(XYZ pickedPoint, List<XYZ> directions, Document doc)
        {
            foreach (XYZ direction in directions)
            {
                // Create a Line object for each ray, based on the picked point and direction
                Line rayLine = Line.CreateBound(pickedPoint, pickedPoint + direction * 10); // Length of 10 units

                // Create DirectShape to visualize the ray
                DirectShape rayShape = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new GeometryObject[] { rayLine });
                rayShape.Name = "Ray";
            }
        }


    }
}
*/

/*
// IDENTIFY INTERSECTING FACES 
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            Reference pickedElementRef = sel.PickObject(ObjectType.Element, "Please pick an element.");
            Element pickedElement = doc.GetElement(pickedElementRef);

            Options geomOptions = new Options();
            GeometryElement geomElem = (pickedElement is FamilyInstance) ?
                ((FamilyInstance)pickedElement).Symbol.get_Geometry(geomOptions) :
                pickedElement.get_Geometry(geomOptions);

            //List<SelectedElementId> highlightedElements = new List<SelectedElementId>();
            HashSet<SelectedElementId> highlightedElements = new HashSet<SelectedElementId>();

            // Initialize a HashSet to keep track of unique intersecting face IDs
            HashSet<int> uniqueFaceIDs = new HashSet<int>();

            bool visualizeRays = PromptUserForVisualization();

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        PlanarFace planarFace = face as PlanarFace;

                        XYZ pickedPoint = face.Evaluate(face.GetBoundingBox().Min + 0.5 * (face.GetBoundingBox().Max - face.GetBoundingBox().Min));

                        double offset = 0.001;

                        List<XYZ> directions = new List<XYZ>
                        {
                            new XYZ(1, 0, 0),
                            new XYZ(0, 1, 0),
                            new XYZ(0, 0, 1),
                            new XYZ(-1, 0, 0),
                            new XYZ(0, -1, 0),
                            new XYZ(0, 0, -1)
                        };


                        if (visualizeRays)
                        {
                            using (Transaction tx = new Transaction(doc))
                            {
                                tx.Start("Visualize Rays");
                                VisualizeRays(pickedPoint, directions, doc);
                                tx.Commit();
                            }
                        }

                        ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);
                        Dictionary<XYZ, SelectedElementId> directionElementMap = new Dictionary<XYZ, SelectedElementId>();

                        foreach (XYZ direction in directions)
                        {
                            XYZ offsetPoint = pickedPoint + direction * offset;
                            ReferenceWithContext nearestRef = FindIntersectingElementsUsingFind(refIntersector, offsetPoint, direction);

                            Dictionary<Face, SelectedElementId> intersectingFaces = FindIntersectingFaces(offsetPoint, direction, view3D, doc);

                            foreach (var intersectingFace in intersectingFaces.Keys) // Changed variable name here
                            {
                                // Use GetHashCode() or any unique identifier for the Face object
                                int uniqueFaceID = intersectingFace.GetHashCode();
                                uniqueFaceIDs.Add(uniqueFaceID);
                            }

                            if (nearestRef != null)
                            {
                                Element nearestElement = doc.GetElement(nearestRef.GetReference());
                                GeometryElement geomElemNearest;
                                if (nearestElement is FamilyInstance)
                                {
                                    FamilyInstance familyInstance = nearestElement as FamilyInstance;
                                    geomElemNearest = familyInstance.Symbol.get_Geometry(new Options());
                                }
                                else
                                {
                                    geomElemNearest = nearestElement.get_Geometry(new Options());
                                }

                                if (geomElemNearest != null)
                                {
                                    bool isSolid = false;
                                    foreach (GeometryObject geomObjNearest in geomElemNearest)
                                    {
                                        if (geomObjNearest is Solid solidNearest && solidNearest.Volume > 0)
                                        {
                                            isSolid = true;
                                            break;
                                        }
                                    }

                                    if (isSolid)
                                    {
                                        directionElementMap[direction] = nearestElement.Id;
                                    }
                                }
                            }
                        }

                        SelectedElementId pickedElementId = pickedElement.Id;

                        foreach (var pair in directionElementMap)
                        {
                            if (pair.Value != pickedElementId)
                            {
                                highlightedElements.Add(pair.Value);  // Will only add if the SelectedElementId is not already in the set
                            }
                        }
                    }
                }

                //sel.SetElementIds(highlightedElements);
                // Highlight the filtered intersecting elements in the Revit interface
                sel.SetElementIds(new List<SelectedElementId>(highlightedElements));

                string dialogMessage;
                if (highlightedElements.Count == 0)
                {
                    dialogMessage = "No intersecting solid elements found in any direction, aside from the initially picked element.";
                }
                else
                {
                    dialogMessage = $"Filtered Intersecting Solid Elements: {string.Join(", ", highlightedElements)}";
                }

                TaskDialog.Show("Filtered Results", dialogMessage);

                // Display unique intersecting face IDs
                string uniqueFaceIDsString = string.Join(", ", uniqueFaceIDs);
                TaskDialog.Show("Unique Intersecting Faces", $"Unique Intersecting Face IDs: {uniqueFaceIDsString}");

            }

            return Result.Succeeded;
        }

        private ReferenceWithContext FindIntersectingElementsUsingFind(ReferenceIntersector refIntersector, XYZ offsetPoint, XYZ direction)
        {
            IList<ReferenceWithContext> references = refIntersector.Find(offsetPoint, direction);

            if (references.Count > 0)
            {
                return references[0];
            }

            return null;
        }

        private bool PromptUserForVisualization()
        {
            using (System.Windows.Forms.Form prompt = new System.Windows.Forms.Form())
            {
                prompt.Width = 250;
                prompt.Height = 150;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Visualization Options";
                prompt.StartPosition = FormStartPosition.CenterScreen;

                CheckBox visualizeCheckbox = new CheckBox
                {
                    Text = "Visualize Rays",
                    AutoSize = true,
                    Checked = false,
                    Location = new System.Drawing.Point(10, 10)
                };
                prompt.Controls.Add(visualizeCheckbox);

                Button okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(10, 40)
                };
                prompt.Controls.Add(okButton);
                prompt.AcceptButton = okButton;

                DialogResult dialogResult = prompt.ShowDialog();
                return dialogResult == DialogResult.OK && visualizeCheckbox.Checked;
            }
        }

        void VisualizeRays(XYZ pickedPoint, List<XYZ> directions, Document doc)
        {
            foreach (XYZ direction in directions)
            {
                Line rayLine = Line.CreateBound(pickedPoint, pickedPoint + direction * 10);

                DirectShape rayShape = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new GeometryObject[] { rayLine });
                rayShape.Name = "Ray";
            }
        }

        private Dictionary<Face, SelectedElementId> FindIntersectingFaces(XYZ offsetPoint, XYZ direction, View3D view3D, Document doc)
        {
            Dictionary<Face, SelectedElementId> intersectingFaces = new Dictionary<Face, SelectedElementId>();

            ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);
            IList<ReferenceWithContext> intersects = refIntersector.Find(offsetPoint, direction);

            foreach (ReferenceWithContext refWithContext in intersects)
            {
                Reference reference = refWithContext.GetReference();
                Element element = doc.GetElement(reference);
                GeometryObject geoObject = element.GetGeometryObjectFromReference(reference);

                if (geoObject is Face face)
                {
                    intersectingFaces[face] = element.Id;
                }
            }

            return intersectingFaces;
        }


    }
}
*/

/*
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections.Generic;
using Autodesk.Revit.DB.Analysis;

namespace RevitWindDrivenRain
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RayIntersectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection sel = uiDoc.Selection;

            View3D view3D = doc.GetElement(doc.ActiveView.Id) as View3D;
            if (view3D == null)
            {
                TaskDialog.Show("Error", "The active view must be a 3D view.");
                return Result.Failed;
            }

            Reference pickedElementRef = sel.PickObject(ObjectType.Element, "Please pick an element.");
            Element pickedElement = doc.GetElement(pickedElementRef);

            Options geomOptions = new Options();
            GeometryElement geomElem = (pickedElement is FamilyInstance) ?
                ((FamilyInstance)pickedElement).Symbol.get_Geometry(geomOptions) :
                pickedElement.get_Geometry(geomOptions);

            //List<SelectedElementId> highlightedElements = new List<SelectedElementId>();
            HashSet<SelectedElementId> highlightedElements = new HashSet<SelectedElementId>();

            // Initialize a HashSet to keep track of unique intersecting face IDs
            HashSet<int> uniqueFaceIDs = new HashSet<int>();

            bool visualizeRays = PromptUserForVisualization();

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        PlanarFace planarFace = face as PlanarFace;

                        XYZ pickedPoint = face.Evaluate(face.GetBoundingBox().Min + 0.5 * (face.GetBoundingBox().Max - face.GetBoundingBox().Min));

                        double offset = 0.001;

                        List<XYZ> directions = new List<XYZ>
                        {
                            new XYZ(1, 0, 0),
                            new XYZ(0, 1, 0),
                            new XYZ(0, 0, 1),
                            new XYZ(-1, 0, 0),
                            new XYZ(0, -1, 0),
                            new XYZ(0, 0, -1)
                        };


                        if (visualizeRays)
                        {
                            using (Transaction tx = new Transaction(doc))
                            {
                                tx.Start("Visualize Rays");
                                VisualizeRays(pickedPoint, directions, doc);
                                tx.Commit();
                            }
                        }

                        ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);
                        Dictionary<XYZ, SelectedElementId> directionElementMap = new Dictionary<XYZ, SelectedElementId>();

                        foreach (XYZ direction in directions)
                        {
                            XYZ offsetPoint = pickedPoint + direction * offset;
                            ReferenceWithContext nearestRef = FindIntersectingElementsUsingFind(refIntersector, offsetPoint, direction);

                            Dictionary<Face, SelectedElementId> intersectingFaces = FindIntersectingFaces(offsetPoint, direction, view3D, doc);

                            foreach (var intersectingFace in intersectingFaces.Keys) // Changed variable name here
                            {
                                // Use GetHashCode() or any unique identifier for the Face object
                                int uniqueFaceID = intersectingFace.GetHashCode();
                                uniqueFaceIDs.Add(uniqueFaceID);
                            }

                            if (nearestRef != null)
                            {
                                Element nearestElement = doc.GetElement(nearestRef.GetReference());
                                GeometryElement geomElemNearest;
                                if (nearestElement is FamilyInstance)
                                {
                                    FamilyInstance familyInstance = nearestElement as FamilyInstance;
                                    geomElemNearest = familyInstance.Symbol.get_Geometry(new Options());
                                }
                                else
                                {
                                    geomElemNearest = nearestElement.get_Geometry(new Options());
                                }

                                if (geomElemNearest != null)
                                {
                                    bool isSolid = false;
                                    foreach (GeometryObject geomObjNearest in geomElemNearest)
                                    {
                                        if (geomObjNearest is Solid solidNearest && solidNearest.Volume > 0)
                                        {
                                            isSolid = true;
                                            break;
                                        }
                                    }

                                    if (isSolid)
                                    {
                                        directionElementMap[direction] = nearestElement.Id;
                                    }
                                }
                            }
                        }

                        SelectedElementId pickedElementId = pickedElement.Id;

                        foreach (var pair in directionElementMap)
                        {
                            if (pair.Value != pickedElementId)
                            {
                                highlightedElements.Add(pair.Value);  // Will only add if the SelectedElementId is not already in the set
                            }
                        }
                    }
                }

                //sel.SetElementIds(highlightedElements);
                // Highlight the filtered intersecting elements in the Revit interface
                sel.SetElementIds(new List<SelectedElementId>(highlightedElements));

                string dialogMessage;
                if (highlightedElements.Count == 0)
                {
                    dialogMessage = "No intersecting solid elements found in any direction, aside from the initially picked element.";
                }
                else
                {
                    dialogMessage = $"Filtered Intersecting Solid Elements: {string.Join(", ", highlightedElements)}";
                }

                TaskDialog.Show("Filtered Results", dialogMessage);

                // Display unique intersecting face IDs
                string uniqueFaceIDsString = string.Join(", ", uniqueFaceIDs);
                TaskDialog.Show("Unique Intersecting Faces", $"Unique Intersecting Face IDs: {uniqueFaceIDsString}");

            }

            return Result.Succeeded;
        }

        private ReferenceWithContext FindIntersectingElementsUsingFind(ReferenceIntersector refIntersector, XYZ offsetPoint, XYZ direction)
        {
            IList<ReferenceWithContext> references = refIntersector.Find(offsetPoint, direction);

            if (references.Count > 0)
            {
                return references[0];
            }

            return null;
        }

        private bool PromptUserForVisualization()
        {
            using (System.Windows.Forms.Form prompt = new System.Windows.Forms.Form())
            {
                prompt.Width = 250;
                prompt.Height = 150;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Visualization Options";
                prompt.StartPosition = FormStartPosition.CenterScreen;

                CheckBox visualizeCheckbox = new CheckBox
                {
                    Text = "Visualize Rays",
                    AutoSize = true,
                    Checked = false,
                    Location = new System.Drawing.Point(10, 10)
                };
                prompt.Controls.Add(visualizeCheckbox);

                Button okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(10, 40)
                };
                prompt.Controls.Add(okButton);
                prompt.AcceptButton = okButton;

                DialogResult dialogResult = prompt.ShowDialog();
                return dialogResult == DialogResult.OK && visualizeCheckbox.Checked;
            }
        }

        void VisualizeRays(XYZ pickedPoint, List<XYZ> directions, Document doc)
        {
            foreach (XYZ direction in directions)
            {
                Line rayLine = Line.CreateBound(pickedPoint, pickedPoint + direction * 10);

                DirectShape rayShape = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new GeometryObject[] { rayLine });
                rayShape.Name = "Ray";
            }
        }

        private Dictionary<Face, SelectedElementId> FindIntersectingFaces(XYZ offsetPoint, XYZ direction, View3D view3D, Document doc)
        {
            Dictionary<Face, SelectedElementId> intersectingFaces = new Dictionary<Face, SelectedElementId>();

            ReferenceIntersector refIntersector = new ReferenceIntersector(view3D);
            IList<ReferenceWithContext> intersects = refIntersector.Find(offsetPoint, direction);

            foreach (ReferenceWithContext refWithContext in intersects)
            {
                Reference reference = refWithContext.GetReference();
                Element element = doc.GetElement(reference);
                GeometryObject geoObject = element.GetGeometryObjectFromReference(reference);

                if (geoObject is Face face)
                {
                    intersectingFaces[face] = element.Id;
                }
            }

            return intersectingFaces;
        }

        private int GetOrCreateAnalysisResultSchema(SpatialFieldManager sfm, Document doc)
        {
            AnalysisResultSchema arSchema = null;
            int schemaIndex = -1;
            // Implement this method or replace it with the actual logic to get the schema by index
            Func<SpatialFieldManager, int, AnalysisResultSchema> YourMethodToGetSchemaByIndex = (a, b) => null;

            IList<int> registeredResultIndices = sfm.GetRegisteredResults();
            foreach (int index in registeredResultIndices)
            {
                AnalysisResultSchema tempSchema = YourMethodToGetSchemaByIndex(sfm, index);

                if (tempSchema != null && tempSchema.Name == "MyIntersectingFacesSchema")
                {
                    arSchema = tempSchema;
                    schemaIndex = index;
                    break;
                }
            }

            // If no existing schema, then create a new one
            if (arSchema == null)
            {
                arSchema = new AnalysisResultSchema("MyIntersectingFacesSchema", "Schema for coloring intersecting faces");
                AnalysisDisplayStyle analysisStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                    doc,
                    "FaceVisualizer",
                    new AnalysisDisplayColoredSurfaceSettings(),
                    new AnalysisDisplayColorSettings(),
                    new AnalysisDisplayLegendSettings()
                );
                arSchema.AnalysisDisplayStyleId = analysisStyle.Id;

                using (Transaction t = new Transaction(doc, "Register AnalysisResultSchema"))
                {
                    t.Start();
                    schemaIndex = sfm.RegisterResult(arSchema);
                    t.Commit();
                }
            }

            return schemaIndex;
        }

        //private void ApplyColorToIntersectingFace(Document doc, Face face, SpatialFieldManager sfm, int schemaIndex)
        //{
        //    IList<UV> pts = new List<UV>();
        //    IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>();
        //    CalculateFieldPointsAndValues(face, ref pts, ref valuesAtPoints);

        //    FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(pts);
        //    FieldValues vals = new FieldValues(valuesAtPoints);

        //    // Ensure the parameters align with what is expected by the API.
        //    // sfm.UpdateSpatialFieldPrimitive(face.Reference.SelectedElementId.IntegerValue, schemaIndex, pnts, vals);
        //    sfm.UpdateSpatialFieldPrimitive(face.Reference.SelectedElementId.IntegerValue, pnts, vals, schemaIndex);

        //}

        private void ApplyColorToIntersectingFace(Document doc, Face face, SpatialFieldManager sfm, int schemaIndex)
        {
            IList<UV> pts = new List<UV>();
            IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>();

            // Assuming CalculateFieldPointsAndValues function is available to get points and values
            CalculateFieldPointsAndValues(face, ref pts, ref valuesAtPoints);

            FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(pts);
            FieldValues vals = new FieldValues(valuesAtPoints);

            // Start a new transaction
            using (Transaction t = new Transaction(doc, "Apply Color To Intersecting Face"))
            {
                t.Start();

                sfm.UpdateSpatialFieldPrimitive(face.Reference.SelectedElementId.IntegerValue, pnts, vals, schemaIndex);

                t.Commit();
            }
        }


        private void CalculateFieldPointsAndValues(Face face, ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints)
        {
            BoundingBoxUV bb = face.GetBoundingBox();
            double uStep = (bb.Max.U - bb.Min.U) / 10;
            double vStep = (bb.Max.V - bb.Min.V) / 10;

            for (double u = bb.Min.U; u <= bb.Max.U; u += uStep)
            {
                for (double v = bb.Min.V; v <= bb.Max.V; v += vStep)
                {
                    UV uv = new UV(u, v);
                    pts.Add(uv);
                    ValueAtPoint valueAtPoint = CalculateValueAtPoint(uv, face);
                    valuesAtPoints.Add(valueAtPoint);
                }
            }
        }

        private ValueAtPoint CalculateValueAtPoint(UV uv, Face face)
        {
            XYZ point = face.Evaluate(uv);
            double distanceToOrigin = point.DistanceTo(XYZ.Zero);
            return new ValueAtPoint(new List<double> { distanceToOrigin });
        }




    }
}
*/