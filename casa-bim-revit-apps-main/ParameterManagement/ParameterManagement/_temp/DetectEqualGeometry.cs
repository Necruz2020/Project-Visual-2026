using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Linq.Expressions;
using System.Diagnostics;

namespace ParameterManagement
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DetectEqualGeometryç : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get application and document objects
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            // Define a reference object to accept the pick result
            Reference pickedRef = null;

            // Pick first element base
            Selection sel = uiApp.ActiveUIDocument.Selection;
            pickedRef = sel.PickObject(ObjectType.Element, "Select base element");

            if (pickedRef == null) return Result.Cancelled;         

            // Now you have the ElementId of the selected linked element
            Element element = doc.GetElement(pickedRef);                       
            GeometryElement geometryElement = element.get_Geometry(new Options());      

            Solid solid = geometryElement.OfType<Solid>().FirstOrDefault(s => s.Volume != 0);

            if (solid == null)
            {
                var solidsFamilyInstance = geometryElement
                    .OfType<GeometryInstance>()
                    .SelectMany(g => g.GetInstanceGeometry().OfType<Solid>().Where(s => s.Volume > 0))
                    .ToList();

                solid = solidsFamilyInstance.Aggregate((x, y) => BooleanOperationsUtils
                .ExecuteBooleanOperation(x, y, BooleanOperationsType.Union));
            }

            if (solid == null) { return Result.Cancelled; }

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.WhereElementIsNotElementType();
            collector.WherePasses(new ElementIntersectsSolidFilter(solid)); // Apply intersection filter to find matches                     

            string idsAsString = string.Join(Environment.NewLine, collector.Select(el => el.Id.IntegerValue));
            TaskDialog.Show("Revit", idsAsString);
            return Result.Succeeded;
        }
    }
}

