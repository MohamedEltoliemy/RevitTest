using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace TASK1
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreateFloorFromLines : IExternalCommand
    {
        // function to check the shape is closed or not
        private bool IsClosedShape(List<Line> lines)
        {
            // list is empty 
            if (lines == null || lines.Count == 0)
                return false;
            // if not go to check 
            for (int i = 0; i < lines.Count; i++)
            {
                // hold the first line in list
                var currentLine = lines[i];
                // hold its end point 
                var currentEndPoint = currentLine.GetEndPoint(1);
                bool hasConnection = false;

                for (int j = 0; j < lines.Count; j++)
                {
                    if (i != j)
                    {
                        // hold another line
                        var nextLine = lines[j];
                        // hold its start point 
                        var nextStartPoint = nextLine.GetEndPoint(0);

                        if (currentEndPoint.IsAlmostEqualTo(nextStartPoint, 0.0001))
                        {
                            hasConnection = true;
                            break;
                        }
                    }
                }

                if (!hasConnection)
                {
                    TaskDialog.Show("Warning", $"Line {i + 1} is not connected to any other line at its endpoint");
                    return false;
                }
            }
            // check start point and end point of the shape in general
            var firstPoint = lines[0].GetEndPoint(0);
            var lastPoint = lines[lines.Count - 1].GetEndPoint(1);

            if (!firstPoint.IsAlmostEqualTo(lastPoint, 0.0001))
            {
                TaskDialog.Show("Warning", "Shape is not closed: End point does not connect to start point");
                return false;
            }

            return true;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;

                List<Line> lines = new List<Line>
                {
                    Line.CreateBound(new XYZ(0, 0, 0), new XYZ(79, 0, 0)),         // Line1
                    Line.CreateBound(new XYZ(44, 25, 0), new XYZ(13, 25, 0)),      // Line2
                    Line.CreateBound(new XYZ(13, 40, 0), new XYZ(-8, 40, 0)),      // Line3
                    Line.CreateBound(new XYZ(55, 34, 0), new XYZ(55, 10, 0)),      // Line4
                    Line.CreateBound(new XYZ(79, 34, 0), new XYZ(55, 34, 0)),      // Line5
                    Line.CreateBound(new XYZ(0, 20, 0), new XYZ(0, 0, 0)),         // Line6
                    Line.CreateBound(new XYZ(55, 10, 0), new XYZ(44, 12, 0)),      // Line7
                    Line.CreateBound(new XYZ(-8, 40, 0), new XYZ(-8, 20, 0)),      // Line8
                    Line.CreateBound(new XYZ(79, 0, 0), new XYZ(79, 34, 0)),       // Line9
                    Line.CreateBound(new XYZ(44, 12, 0), new XYZ(44, 25, 0)),      // Line10
                    Line.CreateBound(new XYZ(-8, 20, 0), new XYZ(0, 20, 0)),       // Line11
                    Line.CreateBound(new XYZ(13, 25, 0), new XYZ(13, 40, 0))       // Line12
                };

                if (!IsClosedShape(lines))
                {
                    return Result.Failed;
                }

                Level level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .WhereElementIsNotElementType()
                    .Cast<Level>()
                    .FirstOrDefault();

                if (level == null)
                {
                    TaskDialog.Show("Error", "No level found in the project");
                    return Result.Failed;
                }

                CurveArray curveArray = new CurveArray();
                foreach (Line line in lines)
                {
                    curveArray.Append(line);
                }

                FloorType floorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault();

                if (floorType == null)
                {
                    TaskDialog.Show("Error", "No default floor type found");
                    return Result.Failed;
                }

                using (Transaction transaction = new Transaction(doc, "Create Floor"))
                {
                    transaction.Start();
                    try
                    {
                        Floor floor = doc.Create.NewFloor(curveArray, floorType, level, false);
                        if (floor != null)
                        {
                            transaction.Commit();
                            TaskDialog.Show("Success", "Floor created successfully!");
                            return Result.Succeeded;
                        }
                        else
                        {
                            transaction.RollBack();
                            TaskDialog.Show("Error", "Failed to create floor");
                            return Result.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        TaskDialog.Show("Error", "Error while creating floor: " + ex.Message);
                        return Result.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Unexpected error: " + ex.Message);
                return Result.Failed;
            }
        }
    }
}