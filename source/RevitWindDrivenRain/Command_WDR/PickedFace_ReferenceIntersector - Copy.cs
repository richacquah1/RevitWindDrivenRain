using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RevitWindDrivenRain
{
    public static class WDRSimulationUtilities
    {
        public static double CalculateWindSpeedatHeightofFirstIntersection(double heightatPoint, double v_ref, double h_ref, double alpha)
        {
            return v_ref * Math.Pow((heightatPoint / h_ref), alpha);
        }
    }

    public class CumulativeData
    {
        public int CumulativeIntersectionCount { get; set; }
        public double CumulativeSumIntersectionValues { get; set; }
        public double CumulativeAverageCount { get; set; }
        public double CumulativeWindSpeed { get; set; }
        public double CumulativeWDR { get; set; }
    }

    public class GridCellInfo
    {
        public string GridCellId { get; set; }
        public XYZ GridCellCenter { get; set; }
        public int GridCellIntersectionCount { get; set; }
        public double GridCellSumIntersectionValues { get; set; }
        public double GridCellAverageCount { get; set; }
        public double GridCellHeight { get; set; }
        public double GridCellWindSpeed { get; set; }
        public double GridCellWDR { get; set; }
        public Reference FaceReference { get; set; }

        public CumulativeData CumulativeData { get; set; } = new CumulativeData();
    }

    [Transaction(TransactionMode.Manual)]
    public class SimpleSimulationTestCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Run the simple simulation test
                RunSimpleSimulationTest();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void InitializeCumulativeData(Dictionary<string, GridCellInfo> gridCells)
        {
            foreach (var gridCell in gridCells.Values)
            {
                gridCell.CumulativeData = new CumulativeData();
            }
        }

        private void UpdateCumulativeData(Dictionary<string, GridCellInfo> gridCells)
        {
            foreach (var gridCell in gridCells.Values)
            {
                gridCell.CumulativeData.CumulativeIntersectionCount += gridCell.GridCellIntersectionCount;
                gridCell.CumulativeData.CumulativeSumIntersectionValues += gridCell.GridCellSumIntersectionValues;
                gridCell.CumulativeData.CumulativeAverageCount += gridCell.GridCellAverageCount;
                gridCell.CumulativeData.CumulativeWindSpeed += gridCell.GridCellWindSpeed;
                gridCell.CumulativeData.CumulativeWDR += gridCell.GridCellWDR;
            }
        }

        private void RunSimpleSimulationTest()
        {
            // Create a simple gridCells dictionary for testing
            var gridCells = new Dictionary<string, GridCellInfo>
            {
                { "GridCell_1", new GridCellInfo { GridCellId = "GridCell_1", GridCellIntersectionCount = 2, GridCellSumIntersectionValues = 4.0, GridCellAverageCount = 2.0, GridCellWindSpeed = 3.5, GridCellWDR = 5.0 } },
                { "GridCell_2", new GridCellInfo { GridCellId = "GridCell_2", GridCellIntersectionCount = 3, GridCellSumIntersectionValues = 6.0, GridCellAverageCount = 3.0, GridCellWindSpeed = 4.0, GridCellWDR = 6.0 } }
            };

            // Initialize cumulative data
            InitializeCumulativeData(gridCells);

            // Simulate a few runs
            for (int i = 0; i < 3; i++)
            {
                // Print current state
                Debug.WriteLine($"Simulation {i + 1}");
                foreach (var gridCell in gridCells.Values)
                {
                    Debug.WriteLine($"Before update - {gridCell.GridCellId}: CumulativeIntersectionCount = {gridCell.CumulativeData.CumulativeIntersectionCount}, GridCellIntersectionCount = {gridCell.GridCellIntersectionCount}");
                }

                // Update cumulative data
                UpdateCumulativeData(gridCells);

                // Print updated state
                foreach (var gridCell in gridCells.Values)
                {
                    Debug.WriteLine($"After update - {gridCell.GridCellId}: CumulativeIntersectionCount = {gridCell.CumulativeData.CumulativeIntersectionCount}");
                }

                // Simulate resetting current counts for the next run
                foreach (var gridCell in gridCells.Values)
                {
                    gridCell.GridCellIntersectionCount = 0;
                    gridCell.GridCellSumIntersectionValues = 0;
                    gridCell.GridCellAverageCount = 0;
                    gridCell.GridCellWindSpeed = 0;
                    gridCell.GridCellWDR = 0;
                }
            }
        }
    }
}
