using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static IronPython.Modules._ast;

namespace RevitWindDrivenRain
{
    public partial class WDR_User_Interface : System.Windows.Controls.UserControl
    {
        private UIDocument uiDoc;
        private ExternalCommandData commandData;

        private List<Face> selectedFaces = new List<Face>();
        public double WindDirection { get; private set; }
        public double WindSpeed { get; private set; }
        public double RainfallIntensity { get; private set; }

        public bool SelectAllFaces { get; private set; }

        public event EventHandler RunSimulationClicked;

        public WDR_User_Interface(UIDocument uiDoc, ExternalCommandData commandData)
        {
            InitializeComponent();
            this.uiDoc = uiDoc;
            this.commandData = commandData;
            RunSimulationButton.Click += OnRunSimulationButton_Click;
            SelectAllFacesCheckbox.Checked += OnSelectAllFacesChecked;
            SelectAllFacesCheckbox.Unchecked += OnSelectAllFacesUnchecked;
            SelectFaces.Click += SelectFaces_Click;
        }

        private void OnSelectAllFacesChecked(object sender, RoutedEventArgs e)
        {
            if (this.uiDoc == null)
            {
                TaskDialog.Show("Error", "UIDocument is not set.");
                return;
            }

            Document doc = this.uiDoc.Document;
            Autodesk.Revit.DB.View currentView = doc.ActiveView;

            if (currentView == null)
            {
                TaskDialog.Show("Error", "No active view found.");
                return;
            }

            List<Face> faces = new List<Face>();
            Options geomOptions = new Options { View = currentView };

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Select All Faces");

                FilteredElementCollector collector = new FilteredElementCollector(doc, currentView.Id)
                    .WhereElementIsNotElementType();

                foreach (Element elem in collector)
                {
                    GeometryElement geomElem = elem.get_Geometry(geomOptions);
                    if (geomElem != null)
                    {
                        ProcessGeometry(geomElem, faces);
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Faces Found", $"Number of faces found: {faces.Count}");
        }

        private void ProcessGeometry(GeometryElement geomElement, List<Face> faces)
        {
            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        faces.Add(face);
                    }
                }
                else if (geomObj is GeometryInstance geomInstance)
                {
                    // Recursively process the geometry instance
                    GeometryElement instanceGeometry = geomInstance.GetInstanceGeometry();
                    ProcessGeometry(instanceGeometry, faces);
                }
            }
        }



        private void OnSelectAllFacesUnchecked(object sender, RoutedEventArgs e)
        {
            SelectAllFaces = false;
            SelectFaces.IsEnabled = true;

            ClearSelectedFaces(); // Call a method to clear selected faces
        }

        private void ClearSelectedFaces()
        {
            if (this.uiDoc == null)
            {
                TaskDialog.Show("Error", "UIDocument is not set.");
                return;
            }

            Document doc = this.uiDoc.Document;

            using (Transaction tx = new Transaction(doc, "Clear Selected Faces"))
            {
                tx.Start();

                // Clear the selected faces list
                selectedFaces.Clear();

                tx.Commit();
            }
        }

        private void SelectFaces_Click(object sender, RoutedEventArgs e)
        {
            if (this.uiDoc == null)
            {
                TaskDialog.Show("Error", "UIDocument is not set.");
                return;
            }

            try
            {
                // Logic to allow the user to manually select faces in the model
                IList<Reference> selectedReferences = this.uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces");

                // Process the selected faces
                foreach (var reference in selectedReferences)
                {
                    Face face = this.uiDoc.Document.GetElement(reference.ElementId).GetGeometryObjectFromReference(reference) as Face;
                    if (face != null)
                    {
                        // Perform operations with the selected face
                        // For example, add it to a list or process it directly
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Handle the case where the user cancels the face selection
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "An error occurred: " + ex.Message);
            }
        }


        private void OnSetSectionBoxButton_Click(object sender, RoutedEventArgs e)

        {
            try
            {
                Document doc = this.uiDoc.Document;
                Autodesk.Revit.DB.View activeView = doc.ActiveView;

                // Ensure the active view is a 3D view
                if (activeView is View3D view3D)
                {
                    using (Transaction tx = new Transaction(doc, "Set Section Box"))
                    {
                        tx.Start();

                        // Enable the section box if it's not already enabled
                        if (!view3D.IsSectionBoxActive)
                        {
                            view3D.IsSectionBoxActive = true;
                        }

                        // Optionally, you can set an initial size and position for the section box here
                        // Example: view3D.SetSectionBox(new BoundingBoxXYZ { ... });

                        tx.Commit();
                    }

                    System.Windows.MessageBox.Show("Please adjust the section box in the active 3D view to encompass the area of interest.");
                }
                else
                {
                    System.Windows.MessageBox.Show("Please switch to a 3D view to set the section box.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error occurred: " + ex.Message);
            }
        }


        private void OnRunSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
            {
                try
                {
                    Document doc = this.uiDoc.Document;

                    // Create SimulationSetup with all required parameters
                    SimulationSetup setup = new SimulationSetup(this.uiDoc, this.commandData, WindSpeed, WindDirection, RainfallIntensity);
                    setup.Initialize(this.commandData);

                    // GetCurrentViewSectionBoxDimensions needs to be defined in SimulationSetup and called here
                    var (buildingLength, buildingWidth, buildingHeight, buildingLocation) = setup.GetCurrentViewSectionBoxDimensions(doc);

                    var planeDimensions = setup.CalculatePlaneDimensions(buildingLength, buildingWidth);
                    double planeLength = planeDimensions.totalLength;
                    double planeWidth = planeDimensions.totalWidth;

                    setup.CreateAndVisualizeSimulationPlane(doc, buildingLocation, buildingHeight, buildingLength, buildingWidth);

                }
                catch (InvalidOperationException ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter valid inputs.");
            }
        }




        private bool IsSectionBoxSet()
        {
            Document doc = this.uiDoc.Document;
            Autodesk.Revit.DB.View activeView = doc.ActiveView;

            // Ensure the active view is a 3D view
            if (activeView is View3D view3D)
            {
                return view3D.IsSectionBoxActive && view3D.GetSectionBox() != null;
            }
            return false;
        }


        private bool ValidateInputs()
        {
            if (double.TryParse(WindSpeedTextBox.Text, out double windSpeed) &&
                double.TryParse(RainfallIntensityTextBox.Text, out double rainfallIntensity) &&
                IsWindDirectionValid(WindDirectionTextBox.Text, out double windDirection))
            {
                WindSpeed = windSpeed;
                RainfallIntensity = rainfallIntensity;
                WindDirection = windDirection;
                return true;
            }
            return false;
        }

        private bool IsWindDirectionValid(string windDirectionInput, out double windDirection)
        {
            windDirection = 0;
            if (double.TryParse(windDirectionInput, out double direction))
            {
                // Array of valid wind direction angles
                double[] validDirections = new double[] { 0, 22.5, 45, 67.5, 90, 112.5, 135, 157.5, 180, 202.5, 225, 247.5, 270, 292.5, 315, 337.5, 360 };
                if (validDirections.Contains(direction))
                {
                    windDirection = direction;
                    return true;
                }
            }
            return false;
        }


    }
}
