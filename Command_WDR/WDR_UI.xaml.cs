using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace RevitWindDrivenRain
{
    public partial class ComprehensiveInputForm : Window
    {
        public event EventHandler<bool?> RequestCloseDialog;

        public string FeedbackMessage
        {
            get { return (string)GetValue(FeedbackMessageProperty); }
            set { SetValue(FeedbackMessageProperty, value); }
        }

        public static readonly DependencyProperty FeedbackMessageProperty =
            DependencyProperty.Register("FeedbackMessage", typeof(string), typeof(ComprehensiveInputForm), new PropertyMetadata(""));

        private readonly double defaultHeight;
        private readonly double defaultRotationAngle;
        private readonly double defaultWindSpeed;
        private readonly double defaultRaindropRadius;
        private readonly double defaultRainfallIntensity;
        private readonly double defaultSimulationTimeInMinutes;
        private readonly double defaultGridsizeInMillimeters;
        private Document _doc;
        private ExternalEvent _deleteStylesEvent;

        public double Height { get; private set; }
        public double[] RotationAngles { get; private set; }
        public double[] WindSpeeds { get; private set; }
        public double[] RainfallIntensityMmPerHr { get; private set; }

        public double RaindropRadiusM { get; private set; }

        public double SimulationTimeInMinutes { get; private set; }
        public double SpacingInMillimeters { get; private set; }

        public bool GenerateOriginalRays => generateOriginalRaysCheckBox.IsChecked == true;
        public bool GenerateAdditionalRays => generateAdditionalRaysCheckBox.IsChecked == true;
        public string AVFApproach
        {
            get
            {
                if (intersectionBasedRadioButton.IsChecked == true) return "Ray-Intersection-Based";
                else if (gridBasedRadioButton.IsChecked == true) return "ISO-WDR-Based";
                else return string.Empty;
            }
        }

        public Action<string> OnApplyStyleRequested;
        public string SelectedStyle { get; set; } = "SolidColor";

        public ComprehensiveInputForm(double defaultHeight, double defaultRotationAngle, double defaultWindSpeed, double defaultRaindropRadius, double defaultRainfallIntensity, double defaultSimulationTimeInMinutes, double defaultGridsizeInMillimeters, Document doc, ExternalEvent deleteStylesEvent)
        {
            this.defaultHeight = defaultHeight;
            this.defaultRotationAngle = defaultRotationAngle;
            this.defaultWindSpeed = defaultWindSpeed;
            this.defaultRaindropRadius = defaultRaindropRadius;
            this.defaultRainfallIntensity = defaultRainfallIntensity;
            this.defaultSimulationTimeInMinutes = defaultSimulationTimeInMinutes;
            this.defaultGridsizeInMillimeters = defaultGridsizeInMillimeters;
            _doc = doc;
            _deleteStylesEvent = deleteStylesEvent;

            InitializeComponent();
            InitializeComponents();
            SetDefaultValues(defaultHeight, defaultRotationAngle, defaultWindSpeed, defaultRaindropRadius, defaultRainfallIntensity, defaultSimulationTimeInMinutes, defaultGridsizeInMillimeters);
            RegisterTextChangedEvents();
        }

        private void InitializeComponents()
        {
            okButton.Click += OkButton_Click;
            cancelButton.Click += CancelButton_Click;
            intersectionBasedRadioButton.Checked += RadioButton_CheckedChanged;
            gridBasedRadioButton.Checked += RadioButton_CheckedChanged;
            radioGradient.Checked += RadioButton_Checked;
            radioSolidColor.Checked += RadioButton_Checked;
        }

        private void SetDefaultValues(double height, double rotationAngle, double windSpeed, double raindropRadius, double rainfallIntensity, double simulationTimeInMinutes, double spacingInMillimeters)
        {
            heightTextBox.Text = height.ToString(CultureInfo.InvariantCulture);
            rotationAngleTextBox.Text = rotationAngle.ToString(CultureInfo.InvariantCulture);
            windSpeedTextBox.Text = windSpeed.ToString(CultureInfo.InvariantCulture);
            raindropRadiusTextBox.Text = raindropRadius.ToString(CultureInfo.InvariantCulture);
            rainfallIntensityTextBox.Text = rainfallIntensity.ToString(CultureInfo.InvariantCulture);
            simulationTimeInMinutesTextBox.Text = simulationTimeInMinutes.ToString(CultureInfo.InvariantCulture);
            raySpacingTextBox.Text = spacingInMillimeters.ToString(CultureInfo.InvariantCulture);
        }

        private void RegisterTextChangedEvents()
        {
            heightTextBox.TextChanged += (s, e) => ValidateSingleInput(heightTextBox, defaultHeight, "Height");
            rotationAngleTextBox.TextChanged += (s, e) => ValidateMultipleInputs(rotationAngleTextBox, defaultRotationAngle, "Rotation Angle");
            windSpeedTextBox.TextChanged += (s, e) => ValidateMultipleInputs(windSpeedTextBox, defaultWindSpeed, "Wind Speed");
            raindropRadiusTextBox.TextChanged += (s, e) => ValidateSingleInput(raindropRadiusTextBox, defaultRaindropRadius, "Raindrop Radius");
            rainfallIntensityTextBox.TextChanged += (s, e) => ValidateMultipleInputs(rainfallIntensityTextBox, defaultRainfallIntensity, "Rainfall Intensity");
        }

        private void ValidateSingleInput(System.Windows.Controls.TextBox textBox, double defaultValue, string fieldName)
        {
            if (!double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                feedbackLabel.Content = $"{fieldName} is invalid. Reverting to default.";
                textBox.Text = defaultValue.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                feedbackLabel.Content = "";
            }
        }

        private void ValidateMultipleInputs(System.Windows.Controls.TextBox textBox, double defaultValue, string fieldName)
        {
            var values = textBox.Text.Split(',')
                .Select(valueStr => double.TryParse(valueStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : (double?)null)
                .ToArray();

            if (values.Any(value => value == null))
            {
                feedbackLabel.Content = $"{fieldName} contains invalid entries.";
            }
            else
            {
                feedbackLabel.Content = "";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;
            StringBuilder errorMessageBuilder = new StringBuilder();

            if (!double.TryParse(heightTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedHeight))
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid height.");
            }

            var rotationAngles = rotationAngleTextBox.Text.Split(',')
                .Select(angleStr => double.TryParse(angleStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double angle) ? angle : (double?)null)
                .ToArray();

            var windSpeeds = windSpeedTextBox.Text.Split(',')
                .Select(speedStr => double.TryParse(speedStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double speed) ? speed : (double?)null)
                .ToArray();

            var rainfallIntensities = rainfallIntensityTextBox.Text.Split(',')
                .Select(intensityStr => double.TryParse(intensityStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double intensity) ? intensity : (double?)null)
                .ToArray();

            if (rotationAngles.Any(angle => angle == null))
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid rotation angles.");
            }

            if (windSpeeds.Any(speed => speed == null))
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid wind speeds.");
            }

            if (rainfallIntensities.Any(intensity => intensity == null))
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid rainfall intensities.");
            }

            if (rotationAngles.Length != windSpeeds.Length || windSpeeds.Length != rainfallIntensities.Length)
            {
                isValid = false;
                errorMessageBuilder.AppendLine("The number of rotation angles, wind speeds, and rainfall intensities must be the same.");
            }

            if (!double.TryParse(raindropRadiusTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedRaindropRadius))
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid raindrop radius.");
            }

            if (!double.TryParse(simulationTimeInMinutesTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedSimulationTime) || parsedSimulationTime < 0)
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid simulation time. Value must be non-negative.");
            }

            if (!double.TryParse(raySpacingTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedSpacingInMillimeters) || parsedSpacingInMillimeters <= 0)
            {
                isValid = false;
                errorMessageBuilder.AppendLine("Invalid ray spacing. Value must be positive.");
            }

            if (!isValid)
            {
                feedbackLabel.Content = errorMessageBuilder.ToString();
            }
            else
            {
                Height = parsedHeight;
                RotationAngles = rotationAngles.Cast<double?>().Select(x => x.Value).ToArray();
                WindSpeeds = windSpeeds.Cast<double?>().Select(x => x.Value).ToArray();
                RainfallIntensityMmPerHr = rainfallIntensities.Cast<double?>().Select(x => x.Value).ToArray();
                RaindropRadiusM = parsedRaindropRadius;
                SimulationTimeInMinutes = parsedSimulationTime;
                SpacingInMillimeters = parsedSpacingInMillimeters;

                feedbackLabel.Content = string.Empty;
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void FocusOnInvalidControl(System.Windows.Controls.TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }

        private void RadioButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            if (radioButton != null && radioButton.IsChecked == true)
            {
                if (radioButton == intersectionBasedRadioButton)
                {
                    generateOriginalRaysCheckBox.IsEnabled = true;
                    generateAdditionalRaysCheckBox.IsEnabled = true;
                }
                else if (radioButton == gridBasedRadioButton)
                {
                    generateOriginalRaysCheckBox.IsEnabled = true;
                    generateAdditionalRaysCheckBox.IsEnabled = true;
                }
            }
        }

        private void ValidateInput(System.Windows.Controls.TextBox textBox, double defaultValue, string fieldName)
        {
            if (!double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                feedbackLabel.Content = $"{fieldName} is invalid. Reverting to default.";
                textBox.Text = defaultValue.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                feedbackLabel.Content = "";
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.IsChecked == true)
            {
                SelectedStyle = radioButton.Content.ToString().Contains("Gradient") ? "Gradient" : "SolidColor";
                OnApplyStyleRequested?.Invoke(SelectedStyle);
            }
        }

        private void btnDeleteStyles_Click(object sender, RoutedEventArgs e)
        {
            _deleteStylesEvent.Raise();
            MessageBox.Show("Request to delete all analysis display styles has been submitted.");
        }
    }
}
