//using System;
//using System.IO;
//using System.Text;
//using System.Collections.Generic;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;

//namespace RevitWindDrivenRain
//{
//    public static class Logger
//    {
//        private static readonly string BaseLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RevitLogs");

//        static Logger()
//        {
//            Directory.CreateDirectory(BaseLogPath);
//        }

//        public static string GetBaseLogPath()
//        {
//            return BaseLogPath;
//        }

//        public static void Log(string message, string fileName)
//        {
//            try
//            {
//                string logFilePath = Path.Combine(BaseLogPath, fileName);
//                using (StreamWriter sw = new StreamWriter(logFilePath, true)) // 'true' to append to the file
//                {
//                    sw.WriteLine($"{DateTime.Now}: {message}");
//                }
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"Failed to write to log file: {ex.Message}");
//            }
//        }

//        public static void ClearLog(string fileName)
//        {
//            try
//            {
//                string logFilePath = Path.Combine(BaseLogPath, fileName);
//                using (StreamWriter sw = new StreamWriter(logFilePath, false)) // 'false' to overwrite the file
//                {
//                    sw.Write(string.Empty);
//                }
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"Failed to clear log file: {ex.Message}");
//            }
//        }
//    }

//}

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;

//namespace RevitWindDrivenRain
//{
//    public static class Logger
//    {
//        // Set the base log path to the Desktop instead of the Documents folder
//        private static string baseLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RevitLogs");

//        public static string GetBaseLogPath()
//        {
//            if (!Directory.Exists(baseLogPath))
//            {
//                Directory.CreateDirectory(baseLogPath);
//            }
//            return baseLogPath;
//        }

//        public static void ClearLog(string fileName)
//        {
//            string logFilePath = Path.Combine(GetBaseLogPath(), fileName);
//            if (File.Exists(logFilePath))
//            {
//                File.Delete(logFilePath);
//            }
//        }

//        public static void Log(string message, string fileName)
//        {
//            string logFilePath = Path.Combine(GetBaseLogPath(), fileName);
//            using (StreamWriter writer = new StreamWriter(logFilePath, true))
//            {
//                writer.WriteLine(message);
//            }
//        }

//        public static void LogFaceIntersectionMap(Document doc, Dictionary<Reference, List<IntersectionInfo>> faceIntersectionMap, string fileName)
//        {
//            StringBuilder logBuilder = new StringBuilder();
//            foreach (var kvp in faceIntersectionMap)
//            {
//                logBuilder.AppendLine($"Face: {kvp.Key.ConvertToStableRepresentation(doc)}");
//                foreach (var intersection in kvp.Value)
//                {
//                    logBuilder.AppendLine($"RayId: {intersection.RayId}, IntersectionPoint: {intersection.IntersectionPoint}, IntersectionValue: {intersection.IntersectionValue}, GridCellId: {intersection.GridCellId}, Distance: {intersection.Distance}");
//                }
//                logBuilder.AppendLine("-----");
//            }
//            Log(logBuilder.ToString(), fileName);
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitWindDrivenRain
{
    public static class Logger
    {
        // Set the base log path to the Desktop instead of the Documents folder
        private static string baseLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RevitLogs");

        public static string GetBaseLogPath()
        {
            if (!Directory.Exists(baseLogPath))
            {
                Directory.CreateDirectory(baseLogPath);
            }
            return baseLogPath;
        }

        public static void ClearLog(string fileName)
        {
            string logFilePath = Path.Combine(GetBaseLogPath(), fileName);
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }

        public static void Log(string message, string fileName)
        {
            string logFilePath = Path.Combine(GetBaseLogPath(), fileName);
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(message);
            }
        }

        public static void LogFaceIntersectionMap(Document doc, Dictionary<Reference, List<IntersectionInfo>> faceIntersectionMap, string fileName)
        {
            StringBuilder logBuilder = new StringBuilder();
            foreach (var kvp in faceIntersectionMap)
            {
                logBuilder.AppendLine($"Face: {kvp.Key.ConvertToStableRepresentation(doc)}");
                foreach (var intersection in kvp.Value)
                {
                    logBuilder.AppendLine($"RayId: {intersection.RayId}, IntersectionPoint: {intersection.IntersectionPoint}, IntersectionValue: {intersection.IntersectionValue}, GridCellId: {intersection.GridCellId}, Distance: {intersection.Distance}");
                }
                logBuilder.AppendLine("-----");
            }
            Log(logBuilder.ToString(), fileName);
        }
    }
}
