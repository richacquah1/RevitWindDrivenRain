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
    [Transaction(TransactionMode.Manual)]
    public class DisplayAnalysis : IExternalCommand
    {
        const string _prompt = "Please pick faces to apply color";

        // Section: Selection Filter
        class BimElementFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
            {
                return null != e.Category && e.Category.HasMaterialQuantities;
            }

            public bool AllowReference(Reference r, XYZ p)
            {
                return true;
            }
        }

        // Section: Analysis Display Style
        void SetAnalysisDisplayStyle(Document doc)
        {
            AnalysisDisplayStyle analysisDisplayStyle;

            const string styleName = "Revit Webcam Display Style";

            // Check if the display style already exists
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> elements = collector.OfClass(typeof(AnalysisDisplayStyle))
                .Where(x => x.Name.Equals(styleName))
                .Cast<Element>()
                .ToList();

            if (elements.Count > 0)
            {
                // Use the existing display style
                analysisDisplayStyle = elements[0] as AnalysisDisplayStyle;
            }
            else
            {
                // Create a new display style
                AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings =
                    new AnalysisDisplayColoredSurfaceSettings();
                coloredSurfaceSettings.ShowGridLines = false;

                AnalysisDisplayColorSettings colorSettings =
                    new AnalysisDisplayColorSettings();
                colorSettings.MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255); // Blue
                colorSettings.MinColor = new Autodesk.Revit.DB.Color(255, 255, 255); // White

                AnalysisDisplayLegendSettings legendSettings =
                    new AnalysisDisplayLegendSettings();
                legendSettings.NumberOfSteps = 10;
                legendSettings.Rounding = 0.05;
                legendSettings.ShowDataDescription = false;
                legendSettings.ShowLegend = true;

                // Start a transaction to create the analysis display style
                Transaction transaction = new Transaction(doc);
                transaction.Start("Create AnalysisDisplayStyle");

                try
                {
                    analysisDisplayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                        doc, styleName, coloredSurfaceSettings, colorSettings, legendSettings);

                    // Assign the display style to the active view
                    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

                    transaction.Commit();
                }
                catch
                {
                    transaction.RollBack();
                    throw;
                }
            }

            // Create a new SpatialFieldManager if it does not exist for the view
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
            }
        }



        // Section: External Command Execution
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                View view = doc.ActiveView;

                // Prompt the user to select faces to display the webcam image
                IList<Reference> references = uidoc.Selection.PickObjects(ObjectType.Face, new BimElementFilter(), _prompt)
                    .Select(r => r.ElementId)
                    .Distinct()
                    .Select(id => new Reference(doc.GetElement(id)))
                    .ToList();

                if (references.Count == 0)
                {
                    return Result.Cancelled;
                }

                // Set the analysis display style for the active view
                SetAnalysisDisplayStyle(doc);

                // Start the surface color mapping event handler
                SurfaceColorMappingHandler.Start(view, references);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

    }
}
#endif