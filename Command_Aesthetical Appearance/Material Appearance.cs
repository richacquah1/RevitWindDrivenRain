using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;

namespace RevitWindDrivenRain
{
    [Transaction(TransactionMode.Manual)]
    public class MaterialAgingCommand : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            try
            {
                // Get the active Revit document
                Document doc = commandData.Application.ActiveUIDocument.Document;

                // Display a form to input age using a slider
                AgeSelectionForm ageForm = new AgeSelectionForm();
                if (ageForm.ShowDialog() == DialogResult.OK)
                {
                    double selectedAge = ageForm.SelectedAge;

                    // Select an element (for example, a wall)
                    Reference pickedElement = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element);

                    using (Transaction tx = new Transaction(doc, "Material Aging"))
                    {
                        if (tx.Start() == TransactionStatus.Started)
                        {
                            // Get the selected element
                            Element element = doc.GetElement(pickedElement.ElementId);

                            // Get the material of the element
                            ElementId materialId = element.GetMaterialIds(false).FirstOrDefault();
                            if (materialId != null)
                            {
                                Material material = doc.GetElement(materialId) as Material;

                                // Calculate the degradation factor based on selected age
                                double degradationFactor = selectedAge / 10.0; // Assuming a range of 0-10 years

                                if (material != null)
                                {
                                    // Modify the material properties to simulate aging
                                    Color color = material.Color;
                                    Color agedColor = new Color((byte)(color.Red * (1 - degradationFactor)), color.Green, color.Blue);
                                    material.Color = agedColor;
                                }
                            }

                            // Commit the transaction
                            tx.Commit();
                        }
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }


public class AgeSelectionForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.TrackBar ageSlider;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button okButton;

        public double SelectedAge { get; private set; }

        public AgeSelectionForm()
        {
            InitializeComponent();
        }

        private void ageSlider_Scroll(object sender, EventArgs e)
        {
            label1.Text = $"Select Material Age: {ageSlider.Value} years";
        }

        private void InitializeComponent()
        {
            this.ageSlider = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.ageSlider)).BeginInit();
            this.SuspendLayout();

            // Age Slider
            ageSlider.Location = new System.Drawing.Point(12, 40);
            ageSlider.Maximum = 10;
            ageSlider.Name = "ageSlider";
            ageSlider.Size = new System.Drawing.Size(200, 45);
            ageSlider.Value = 5;
            ageSlider.TickFrequency = 1;
            ageSlider.Scroll += new System.EventHandler(this.ageSlider_Scroll); // Add this event handler

            // Label
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 10);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(123, 17);
            label1.Text = "Select Material Age: 5 years"; // Initial label text

            // OK Button
            okButton.Location = new System.Drawing.Point(90, 100);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 25);
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += new System.EventHandler(this.okButton_Click);

            // Add controls to the form
            Controls.Add(ageSlider);
            Controls.Add(label1);
            Controls.Add(okButton);

            // Set form properties
            this.ClientSize = new System.Drawing.Size(224, 140);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select Material Age";

        }



        private void okButton_Click(object sender, EventArgs e)
        {
            SelectedAge = ageSlider.Value;
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }

}