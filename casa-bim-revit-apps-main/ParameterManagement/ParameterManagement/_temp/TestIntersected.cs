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
    public class TestIntersected : IExternalCommand
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
            pickedRefs = sel.PickObjects(ObjectType.Element, new PartSelectionFilter(), "Select base element");

            if (pickedRefs == null) return Result.Cancelled;

            //List for conflict element
            StringBuilder moreOneElement = new StringBuilder();
            moreOneElement.AppendLine("Los siguientes elementos se superponen con mas de un elemento:");


            foreach (Reference pickedRef in pickedRefs)
            {
                Element element = doc.GetElement(pickedRef);

                // Now you have the linked element and can access its geometry
                GeometryElement geometryElement = element?.get_Geometry(new Options());

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

                // Obtén todas las instancias de enlace en el documento actual
                FilteredElementCollector linkCollector = new FilteredElementCollector(doc);
                ICollection<Element> linkInstances = linkCollector.OfClass(typeof(RevitLinkInstance)).ToElements();

                foreach (RevitLinkInstance linkInstance in linkInstances)
                {
                    //Get Transform
                    Transform transform = linkInstance.GetTotalTransform();

                    //Move to solid
                    solid = SolidUtils.CreateTransformed(solid, transform);

                    if (linkInstance == null) return Result.Failed;

                    // Get the document of the linked model
                    Document linkedDoc = linkInstance.GetLinkDocument();

                    using (Transaction tx = new Transaction(doc, "Check Intersection"))
                    {
                        tx.Start();
                        //ElementId copiedSolidId = ElementTransformUtils
                        //.CopyElements(doc, new List<ElementId> { element.Id },
                        //linkInstance.GetLinkDocument(), Transform.Identity, new CopyPasteOptions()).First();

                        DirectShape newElement = DirectShape
                            .CreateElement(linkedDoc, new ElementId(BuiltInCategory.OST_Walls));                                               
                                                
                        // Now you have the linked element and can access its geometry
                        //Element soliElement = linkedDoc.GetElement(copiedSolidId);
                        GeometryElement geometryElementLink = newElement?.get_Geometry(new Options());

                        Solid solidLink = geometryElementLink.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);

                        FilteredElementCollector collector = new FilteredElementCollector(linkedDoc);
                        collector.WhereElementIsNotElementType();
                        collector.WherePasses(new ElementIntersectsSolidFilter(solidLink));

                        if (collector.Count() > 1)
                        {
                            // Get IDs of the elements
                            List<int> elementIds = collector.Select(el => el.Id.IntegerValue).ToList();

                            // Append the IDs to the StringBuilder
                            foreach (int id in elementIds)
                            {
                                moreOneElement.AppendLine(id.ToString());
                            }
                        }

                        List<Element> links = collector.ToElements().ToList();

                        tx.Commit();
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

        // Define a custom filter for selecting only parts
        public class PartSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                // Add logic here to determine if the element is a part
                // For example, you can check if the element category is Parts
                return elem.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_Parts;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                // Allow only element references
                return true;
            }
        }
    }
}
