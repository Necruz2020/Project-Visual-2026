#region Imported Namespaces

//.NET common used namespaces
//Revit.NET common used namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using RevitLibrary;

#endregion

namespace ParameterManagement
{
    //Main execute
    class CopyParametersTools
    {
        public static bool replaceValues { get; set; }

        public static void Execute()
        {
            // Select the base element
            Element baseElement = PickElement("Select Base Element");

            // Select the elements to copy
            IList<Element> elementsToCopy = PickElements("Select Elements");

            // Crear un objeto TaskDialog
            TaskDialog taskDialog = new TaskDialog("Confirmar reemplazo");

            // Establecer el contenido del diálogo
            taskDialog.MainContent = "¿Desea reemplazar los valores de los parámetros?";

            // Establecer botones
            taskDialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

            // Mostrar el diálogo y obtener el resultado
            TaskDialogResult result = taskDialog.Show();

            // Guardar el resultado en una variable
            replaceValues = (result == TaskDialogResult.Yes);

            // Get parameters from the base element
            IList<Parameter> baseParameters = baseElement.GetOrderedParameters();

            //Validation
            if (elementsToCopy == null || baseParameters == null) { return; }

            // Start the transaction
            using (Transaction trans = new Transaction(RevitTools.doc))
            {
                trans.Start("Copy Parameters");

                foreach (Element copyElement in elementsToCopy)
                {
                    // Get parameters from the element to copy
                    IList<Parameter> copyParameters = copyElement.GetOrderedParameters();

                    // Copy parameters from the base element to the element to copy
                    CopyParameters(baseParameters, copyParameters);
                }

                trans.Commit();
            }
        }

        public static void ExecuteOfLink()
        {
            // Select the base element
            Reference pickedRef = RevitTools.sel
                .PickObject(ObjectType.LinkedElement, "Selecciona el elemento Linkeado");

            if (pickedRef == null)
                return;

            ElementId linkedElementId = pickedRef.LinkedElementId;
            RevitLinkInstance linkInstance = RevitTools.doc.GetElement(pickedRef) as RevitLinkInstance;
            Document linkedDoc = linkInstance.GetLinkDocument();
            Element baseElement = linkedDoc.GetElement(linkedElementId);

            if (linkInstance == null)
                return;           

            //Element baseElement = PickElement("Select Base Element");

            // Select the elements to copy
            IList<Element> elementsToCopy = PickElements("Select Elements");

            // Get parameters from the base element
            IList<Parameter> baseParameters = baseElement.GetOrderedParameters();

            //Validation
            if (elementsToCopy == null || baseParameters == null) { return; }

            // Start the transaction
            using (Transaction trans = new Transaction(RevitTools.doc))
            {
                trans.Start("Copy Parameters");

                foreach (Element copyElement in elementsToCopy)
                {
                    // Get parameters from the element to copy
                    IList<Parameter> copyParameters = copyElement.GetOrderedParameters();

                    // Copy parameters from the base element to the element to copy
                    CopyParameters(baseParameters, copyParameters);
                }

                trans.Commit();
            }
        }


        // Helper method to pick a single element
        private static Element PickElement(string prompt)
        {
            try
            {
                Reference pickedRef = RevitTools.sel.PickObject(ObjectType.Element, prompt);
                return RevitTools.doc.GetElement(pickedRef);
            }
            catch { return null; }
        }


        // Helper method to pick multiple elements
        private static IList<Element> PickElements(string prompt)
        {
            try
            {
                IList<Reference> pickedRefs = RevitTools.sel.PickObjects(ObjectType.Element, prompt);
                return pickedRefs.Select(refElem => RevitTools.doc.GetElement(refElem)).ToList();
            }
            catch { return null; }
        }


        // Helper method to copy parameters from the source to destination
        public static void CopyParameters(IList<Parameter> sourceParameters, IList<Parameter> destinationParameters)
        {           
            foreach (Parameter sourceParameter in sourceParameters)
            {
                string paramId = sourceParameter.Definition.Name;
                Parameter destinationParameter = destinationParameters.FirstOrDefault(p => p.Definition.Name == paramId);

                if (destinationParameter == null) continue;
     
                try
                {
                    CopyParameterValues(sourceParameter, destinationParameter);
                }
                catch (Exception) { }
            }
        }

        // Helper method to copy values from the source parameter to the destination parameter
        private static void CopyParameterValues(Parameter sourceParameter, Parameter destinationParameter)
        {
            if (destinationParameter.HasValue && !replaceValues) return;

            if (!sourceParameter.HasValue && !destinationParameter.HasValue) return;

            switch (sourceParameter.StorageType)
            {
                case StorageType.None:
                    break;
                case StorageType.Integer:
                    destinationParameter.Set(sourceParameter.AsInteger());
                    break;
                case StorageType.Double:
                    destinationParameter.Set(sourceParameter.AsDouble());
                    break;
                case StorageType.String:
                    string value = sourceParameter.AsString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        destinationParameter.Set(value);
                    }
                    break;
                case StorageType.ElementId:
                    destinationParameter.Set(sourceParameter.AsElementId());
                    break;
            }
        }
    }    
}