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
using System.Collections.ObjectModel;

namespace ParameterManagement
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DetectIntersectedElementsFromLinks : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get application and document objects
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            // Define a reference object to accept the pick result
            IList<Reference> pickedRefs = null;

            // Pick first element base
            Selection sel = uiApp.ActiveUIDocument.Selection;
            pickedRefs = sel.PickObjects(ObjectType.LinkedElement, "Select base element");

            if (pickedRefs == null) return Result.Cancelled;

            //List for conflict element
            StringBuilder moreOneElement = new StringBuilder();
            moreOneElement.AppendLine("Los siguientes elementos se superponen con mas de un elemento:");

            foreach (Reference pickedRef in pickedRefs)
            {
                ElementId linkedElementId = pickedRef.LinkedElementId;

                // Get the RevitLinkInstance from the linked element
                RevitLinkInstance linkInstance = doc.GetElement(pickedRef) as RevitLinkInstance;

                //Get Transform
                Transform transform = linkInstance.GetTotalTransform();

                if (linkInstance == null) return Result.Failed;

                // Get the document of the linked model
                Document linkedDoc = linkInstance.GetLinkDocument();

                // Get the element from the linked document
                Element linkedElement = linkedDoc.GetElement(linkedElementId);

                // Now you have the linked element and can access its geometry
                GeometryElement geometryElement = linkedElement?.get_Geometry(new Options());

                Solid solid = geometryElement.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);

                if (solid == null)
                {
                    var solidsFamilyInstance = geometryElement
                        .OfType<GeometryInstance>()
                        .SelectMany(g => g.GetInstanceGeometry().OfType<Solid>().Where(s => s.Volume > 0))
                        .ToList();

                    solid = solidsFamilyInstance.Aggregate((x, y) => BooleanOperationsUtils
                    .ExecuteBooleanOperation(x, y, BooleanOperationsType.Union));
                }

                if (solid == null) return Result.Cancelled;

                //Move to solid
                solid = SolidUtils.CreateTransformed(solid, transform);

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.WhereElementIsNotElementType();
                collector.WherePasses(new ElementIntersectsSolidFilter(solid));

                if (collector.Count() > 1)
                {
                    // Get IDs of the elements
                    List<int> elementIds = collector.Select(el => el.Id.IntegerValue).ToList();

                    // Append the IDs to the StringBuilder
                    foreach (int elementId in elementIds)
                    {
                        moreOneElement.AppendLine(elementId.ToString());
                    }
                }
                else
                {
                    ElementCategoryFilter partsCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Parts);
                    FamilyInstance foundElement = collector.ToElements().FirstOrDefault() as FamilyInstance;
                    if (foundElement != null)
                    {
                        IList<ElementId> parts = foundElement
                        .GetDependentElements(partsCategoryFilter);

                        foreach (ElementId elementId in parts)
                        {
                            // Verificar la intersección con el sólido
                            bool intersects = false;

                            Element part = doc.GetElement(elementId);                            
                            GeometryElement partGeometry = part.get_Geometry(new Options());
                            Solid partSolid = geometryElement.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);
                            if (partSolid != null) { continue; }
                            TaskDialog.Show("volumne",partSolid.Volume.ToString());
                            intersects = AreSolidsIntersecting(partSolid, solid);
                            TaskDialog.Show("intersect", intersects.ToString());
                            if (intersects)
                            {
                                TaskDialog.Show("Parte que intersecta", elementId.IntegerValue.ToString());
                            }
                        }

                        TaskDialog.Show("revit", parts.Count().ToString());
                    }

                }
            }


            TaskDialog.Show("Revit", moreOneElement.ToString());

            return Result.Succeeded;
        }

        // Método para verificar la intersección de dos sólidos
        private bool AreSolidsIntersecting(Solid solid1, Solid solid2)
        {
            foreach (Face face1 in solid1.Faces)
            {
                foreach (Face face2 in solid2.Faces)
                {
                    // Verificar si las caras se intersectan
                    FaceIntersectionFaceResult result = face1.Intersect(face2);

                    // Verificar si hay una intersección
                    if (result == FaceIntersectionFaceResult.Intersecting)
                    {
                        // Las caras se intersectan
                        return true;
                    }
                }
            }

            // No hay intersección detectada
            return false;
        }
    }
}

