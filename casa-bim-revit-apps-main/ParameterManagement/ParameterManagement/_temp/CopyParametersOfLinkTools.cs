using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManagement
{
    public class CopyParametersOfLinkTools
    {
        public static void Execute()
        {
            //IList<Reference> pickedRefs = RevitTools.sel
            //    .PickObjects(ObjectType.LinkedElement, "Selecciona los elementos a analizar");

            Reference pickedRef = RevitTools.sel
                .PickObject(ObjectType.LinkedElement, "Selecciona el elemento Linkeado");


            if (pickedRef == null)
                return;

            StringBuilder message = new StringBuilder();
            message.AppendLine("Los siguientes elementos no se ha copiado los atributos:");
            int startLength = message.Length;

            // Start the transaction
            using (Transaction trans = new Transaction(RevitTools.doc))
            {
                trans.Start("Copy Parameters");

                ElementId linkedElementId = pickedRef.LinkedElementId;

                RevitLinkInstance linkInstance = RevitTools.doc.GetElement(pickedRef) as RevitLinkInstance;

                if (linkInstance == null)
                    return;

                Document linkedDoc = linkInstance.GetLinkDocument();



                //ProcessLinkedElement(linkInstance, linkedElementId, linkedDoc, message);

                //foreach (Reference pickedRef in pickedRef)
                //{
                //    ElementId linkedElementId = pickedRef.LinkedElementId;

                //    RevitLinkInstance linkInstance = RevitTools.doc.GetElement(pickedRef) as RevitLinkInstance;

                //    if (linkInstance == null)
                //        return;

                //    Document linkedDoc = linkInstance.GetLinkDocument();

                //    ProcessLinkedElement(linkInstance, linkedElementId, linkedDoc, message);
                //}

                trans.Commit();
            }

            if (message.Length == startLength)
            {
                message.Clear();
                message.AppendLine("La Aplicación se ejecutó con exito");
            }

            TaskDialog.Show("CASA", message.ToString());
        }

        private static void ProcessLinkedElement(RevitLinkInstance linkInstance, ElementId linkedElementId,
            Document linkedDoc, StringBuilder moreOneElement)
        {
            Transform transform = linkInstance.GetTotalTransform();

            Element linkedElement = linkedDoc.GetElement(linkedElementId);
            GeometryElement geometryElement = linkedElement?.get_Geometry(new Options());

            Solid solid = GetSolidFromGeometry(geometryElement, transform);

            if (solid != null)
            {
                FilteredElementCollector collector = GetElementCollectorForSolid(solid, RevitTools.doc);

                if (collector.Count() == 1)
                {
                    //Copy Parameters
                    IList<Parameter> sourceParameters = linkedElement.GetOrderedParameters();
                    IList<Parameter> destinationParameters = collector.FirstOrDefault().GetOrderedParameters();
                    CopyParametersTools.CopyParameters(sourceParameters, destinationParameters);
                }
                else
                {
                    AppendElementIdsToStringBuilder(collector, moreOneElement);
                }
            }
        }

        private static Solid GetSolidFromGeometry(GeometryElement geometryElement, Transform transform)
        {
            Solid solid = geometryElement?.OfType<Solid>().FirstOrDefault(s => s.Volume > 0);

            if (solid == null)
            {
                var solidsFamilyInstance = geometryElement
                    .OfType<GeometryInstance>()
                    .SelectMany(g => g.GetInstanceGeometry().OfType<Solid>().Where(s => s.Volume > 0))
                    .ToList();

                solid = solidsFamilyInstance.Aggregate((x, y) => BooleanOperationsUtils
                .ExecuteBooleanOperation(x, y, BooleanOperationsType.Union));
            }

            return solid != null ? SolidUtils.CreateTransformed(solid, transform) : null;
        }

        private static FilteredElementCollector GetElementCollectorForSolid(Solid solid, Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.WhereElementIsNotElementType();
            collector.WherePasses(new ElementIntersectsSolidFilter(solid));

            return collector;
        }

        private static void AppendElementIdsToStringBuilder(FilteredElementCollector collector, StringBuilder stringBuilder)
        {
            List<int> elementIds = collector.Select(el => el.Id.IntegerValue).ToList();

            foreach (int elementId in elementIds)
            {
                stringBuilder.AppendLine(elementId.ToString());
            }
        }
    }
}
