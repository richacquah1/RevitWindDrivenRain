
//using Autodesk.Revit.UI;
//using System;
//using System.Windows;

//namespace RevitWindDrivenRain
//{
//    // Define an ExternalEvent and an IExternalEventHandler to apply user choices to the Revit project
//    public class ApplyChoiceHandler : IExternalEventHandler
//    {
//        public bool UserChoice { get; set; }

//        public void Execute(UIApplication app)
//        {
//            // Apply changes to the Revit project based on UserChoice
//            // This is where you could call CreateDefaultAnalysisDisplayStyle or any other Revit API method
//            TaskDialog.Show("User Choice", $"User chose: {UserChoice}");
//        }

//        public string GetName() => "Apply User Choice";
//    }

//    public partial class ChoiceWindow : Window
//    {
//        // Public property to access the user's choice
//        public bool UserChoice { get; private set; }

//        private ApplyChoiceHandler _handler = new ApplyChoiceHandler();
//        private ExternalEvent _externalEvent = null;

//        public ChoiceWindow()
//        {
//            InitializeComponent();
//            _externalEvent = ExternalEvent.Create(_handler);
//        }

//        private void TrueButton_Click(object sender, RoutedEventArgs e)
//        {
//            this.UserChoice = true;
//            this.Close(); // Close the window after setting the choice
//        }

//        private void FalseButton_Click(object sender, RoutedEventArgs e)
//        {
//            this.UserChoice = false;
//            this.Close(); // Close the window after setting the choice
//        }

//    }
//}


using Autodesk.Revit.DB;
using Autodesk.Revit.UI; // Required for ExternalEvent
using System;
using System.Windows; // Namespace for WPF types

namespace RevitWindDrivenRain
{
    public partial class VisualizationStyleSelector : Window
    {
        public Action<string> OnApplyStyleRequested;
        private Document _doc;
        private ExternalEvent _deleteStylesEvent; // Field to hold the ExternalEvent

        // Updated constructor that takes a Document and an ExternalEvent as arguments
        public VisualizationStyleSelector(Document doc, ExternalEvent deleteStylesEvent)
        {
            InitializeComponent();
            _doc = doc;
            _deleteStylesEvent = deleteStylesEvent; // Store the passed ExternalEvent
        }

        private void btnGradient_Click(object sender, RoutedEventArgs e)
        {
            SelectedStyle = "Gradient";
            OnApplyStyleRequested?.Invoke("Gradient");
            this.Close(); // Close the window after selection
        }

        private void btnSolidColor_Click(object sender, RoutedEventArgs e)
        {
            SelectedStyle = "SolidColor";
            OnApplyStyleRequested?.Invoke("SolidColor");
            this.Close(); // Close the window after selection
        }


        // This method is updated to use ExternalEvent for deletion
        private void btnDeleteStyles_Click(object sender, RoutedEventArgs e)
        {
            // Raise the external event to delete styles
            _deleteStylesEvent.Raise();
            MessageBox.Show("Request to delete all analysis display styles has been submitted.");
        }

        public string SelectedStyle { get; set; } = "SolidColor"; // Keeps a default style

        // Ensure you have InitializeComponent method call here if it's somehow missing from this snippet
    }
}


//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using RevitWindDrivenRain;
//using System;
//using System.Windows;
//using System.Collections.Generic;
//using System.Linq; // Add this line at the top of your code file


//namespace RevitWindDrivenRain
//{

//    public partial class ChoiceWindow : Window
//    {
//        private ApplyChoiceHandler _applyChoiceHandler;
//        private ExternalEvent _applyChoiceEvent;

//        public ChoiceWindow(ApplyChoiceHandler handler)
//        {
//            InitializeComponent();
//            _applyChoiceHandler = handler;
//            _applyChoiceEvent = ExternalEvent.Create(_applyChoiceHandler);
//        }

//        private void TrueButton_Click(object sender, RoutedEventArgs e)
//        {
//            UserSettings.UserChoice = true;
//            _applyChoiceEvent.Raise(); // Continue with raising the external event
//        }

//        private void FalseButton_Click(object sender, RoutedEventArgs e)
//        {
//            UserSettings.UserChoice = false;
//            _applyChoiceEvent.Raise();
//        }
//    }

//    public class ApplyChoiceHandler : IExternalEventHandler
//    {
//        public bool UserChoice { get; set; }

//        public void Execute(UIApplication app)
//        {
//            Document doc = app.ActiveUIDocument.Document;
//            try
//            {
//                TaskDialog.Show("Debug", "Entering Execute method of ApplyChoiceHandler.");

//                using (Transaction tx = new Transaction(doc, "Modify Document"))
//                {
//                    tx.Start();
//                    TaskDialog.Show("Debug", "Transaction started.");

//                    // Delete existing styles if needed
//                    //DeleteExistingStyles(doc);
//                    //TaskDialog.Show("Debug", "Existing styles deleted.");

//                    // Use wdrUtility to create and apply a new style based on UserChoice
//                    //AnalysisDisplayStyle newStyle = wdrUtility.CreateDefaultAnalysisDisplayStyle(doc, "My Custom Style", UserSettings.UserChoice);
//                    //TaskDialog.Show("Debug", "New AnalysisDisplayStyle created.");

//                    // Assuming FaceIntersectionPoints have been set prior to raising the external event
//                    var faceIntersectionPoints = UserSessionData.FaceIntersectionPoints;
//                    bool useSolidColorRange = UserSettings.UserChoice;

//                    // Now calling ApplyAVFToSelectedFacesandGrids as desired
//                    //wdrUtility.ApplyAVFToSelectedFacesandGrids(doc, faceIntersectionPoints, useSolidColorRange);
//                    wdrUtility.ApplyAVFToSelectedFacesandGrids(doc, faceIntersectionPoints, UserSettings.UserChoice);
//                    TaskDialog.Show("Debug", "ApplyAVFToSelectedFacesandGrids successfull.");

//                    tx.Commit();
//                    TaskDialog.Show("Debug", "Transaction committed.");
//                }
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"An error occurred in ApplyChoiceHandler.Execute: {ex.Message}\n{ex.StackTrace}");
//            }
//        }

//        private void DeleteExistingStyles(Document doc)
//        {
//            // No new Transaction is started here; we use the one from Execute
//            FilteredElementCollector collector = new FilteredElementCollector(doc);
//            var styles = collector.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
//            foreach (Element style in styles)
//            {
//                doc.Delete(style.Id);
//            }
//        }

//        public string GetName() => "Apply User Choice for Analysis Display Style";
//    }

//    public static class UserSessionData
//    {
//        public static Dictionary<Reference, List<XYZ>> FaceIntersectionPoints { get; set; } = new Dictionary<Reference, List<XYZ>>();
//        // Other session-specific data can be added here
//    }
//}

