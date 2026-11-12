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
using OfficeOpenXml;
using System.Data.SqlClient;
using System.Diagnostics;
using Autodesk.Revit.Creation;
using System.Security.Cryptography;

#endregion
namespace ParameterManagement
{
    /// <summary>
    /// Clase de herramientas de vista
    /// </summary>
    class ViewTools
    {
        public static string pathFile { get; set; }       
    }

    class CreateFillParameterTools
    {
        private static List<BuiltInCategory> builtInCategories;
        private static CategorySet categorySet;

        public class ExcelParameter
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool SetParameter { get; set; }
            public object Value { get; set; }
        }

        public static List<string> existingParameters { get; set; }
        public static List<ExcelParameter> excelParameters { get; set; }
        public static ICollection<Element> allElements { get; set; }

        public static void Execute()
        {
            // Ruta al archivo Excel
            //string filePath = "C:\\Users\\Usuario\\Desktop\\Set de Propiedades_PQT 4.xlsx";

            // Llama a la función para leer el archivo Excel
            excelParameters = ReadExcelFile(ViewTools.pathFile);

            // Parametros que existen en el modelo
            existingParameters = GetParameters();

            // Start the transaction
            using (Transaction trans = new Transaction(RevitTools.doc))
            {
                try
                {
                    trans.Start("Add Parameters From Excel");

                    SetCategories(); // Asegúrate de que las categorías estén configuradas correctamente

                    foreach (ExcelParameter parameter in excelParameters)
                    {
                        AddParameter(parameter, existingParameters);
                    }

                    // Asignar valores a parametros
                    SetParameter(excelParameters);
                    TaskDialog.Show("CASA", "Se ha completado con exito");
                }
                catch (Exception ex)
                {
                    // Manejar excepciones específicas según sea necesario
                    Debug.WriteLine("Error during transaction: " + ex.Message);
                }
                finally
                {
                    trans.Commit();
                }
            }
        }

        static void AddParameter(ExcelParameter excelParameter, List<string> existingParameters)
        {
            bool inLOI = excelParameter.Name.Contains("LOI")
                || excelParameter.Name.Contains("04_0");

            BuiltInParameterGroup group = inLOI ? BuiltInParameterGroup.INVALID
                : BuiltInParameterGroup.PG_DATA;

            if (existingParameters.Contains(excelParameter.Name))
            {
                ModifyExistingParameters(excelParameter, group);
                return;
            }

            string fileShared = RevitTools.app.SharedParametersFilename;
            string tempFileShared = Path.GetTempFileName() + ".txt";
            using (File.Create(tempFileShared)) { }

            ParameterType parameterType = ParameterType.Text;

            switch (excelParameter.Type.ToLower())
            {
                case "m":
                    parameterType = ParameterType.Length;
                    break;
                case "m2":
                    parameterType = ParameterType.Area;
                    break;
                case "m3":
                    parameterType = ParameterType.Volume;
                    break;
                case "kg.":
                    parameterType = ParameterType.Weight;
                    break;
                // Agrega casos para otros tipos de parámetros si es necesario
                default:
                    // Manejar el tipo desconocido según sea necesario
                    Debug.WriteLine("Tipo de parámetro desconocido: " + excelParameter.Type);
                    break;
            }

            RevitTools.app.SharedParametersFilename = tempFileShared;
            ExternalDefinitionCreationOptions options =
                new ExternalDefinitionCreationOptions(excelParameter.Name, parameterType);

            ExternalDefinition definition = RevitTools.app.OpenSharedParameterFile()
                .Groups.Create("CASA").Definitions
                .Create(options) as ExternalDefinition;

            RevitTools.app.SharedParametersFilename = fileShared;
            File.Delete(tempFileShared);

            Binding binding = RevitTools.app.Create.NewInstanceBinding(categorySet);

            BindingMap bindingMap = RevitTools.doc.ParameterBindings;
            bindingMap.Insert(definition, binding, group);
        }

        static void ModifyExistingParameters(ExcelParameter excelParameter, BuiltInParameterGroup group)
        {
            //si existe agregarle las categorias necesarias
            // Obtiene el parámetro existente
            DefinitionBindingMapIterator it = RevitTools.doc.ParameterBindings.ForwardIterator();
            while (it.MoveNext())
            {
                Definition definition = it.Key as Definition;
                if (definition != null && definition.Name == excelParameter.Name)
                {
                    // Elimina el enlace existente
                    RevitTools.doc.ParameterBindings.Remove(definition);
                    Binding binding = RevitTools.app.Create.NewInstanceBinding(categorySet);
                    RevitTools.doc.ParameterBindings.Insert(definition, binding, group);
                    break;
                }              
            }
        }


        static void SetParameter(List<ExcelParameter> excelParameters)
        {
            List<ExcelParameter> excelParametersSet = excelParameters
                .Where(p => p.SetParameter == true)
                .ToList();

            foreach (Element element in allElements)
            {
                foreach (ExcelParameter excelParameter in excelParametersSet)
                {
                    Parameter parameterElement = element.LookupParameter(excelParameter.Name);

                    if (parameterElement == null) Debug.WriteLine(excelParameter.Name);
                    if (parameterElement == null) continue;

                    try
                    {
                        switch (excelParameter.Type.ToLower())
                        {
                            case "texto":
                                parameterElement.Set(excelParameter.Value.ToString());
                                break;
                            case "valor":
                                parameterElement.Set(excelParameter.Value.ToString());
                                break;
                            case "url*":
                                parameterElement.Set(excelParameter.Value.ToString());
                                break;
                        }
                    }
                    catch { }
                }
            }
        }

        static ICollection<Element> GetAllElements()
        {
            FilteredElementCollector collector = new FilteredElementCollector(RevitTools.doc);
            SetCategories();  // Asegúrate de que las categorías estén configuradas correctamente

            ElementMulticategoryFilter multicategoryFilter =
                new ElementMulticategoryFilter(builtInCategories);

            return collector
                .WhereElementIsNotElementType()
                .WherePasses(multicategoryFilter)
                .ToElements();
        }

        static List<string> GetParameters()
        {
            List<string> parameterNames = new List<string>();

            allElements = GetAllElements();

            foreach (Element element in allElements)
            {
                IList<Parameter> parameters = element.GetOrderedParameters();

                foreach (Parameter parameter in parameters)
                {
                    string paramName = parameter.Definition.Name;
                    if (!parameterNames.Contains(paramName))
                    {
                        parameterNames.Add(paramName);
                    }
                }
            }

            return parameterNames;
        }

        static void SetCategories()
        {
            if (categorySet == null) categorySet = new CategorySet();

            builtInCategories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_Rebar,
                BuiltInCategory.OST_Parts,
                BuiltInCategory.OST_Materials,
                BuiltInCategory.OST_GenericModel
            };

            foreach (BuiltInCategory builtInCategory in builtInCategories)
            {
                Category category = RevitTools.doc.Settings.Categories.get_Item(builtInCategory);

                if (category != null)
                {
                    categorySet.Insert(category);
                }
            }
        }

        static List<ExcelParameter> ReadExcelFile(string filePath)
        {
            List<ExcelParameter> excelParameters = new List<ExcelParameter>();

            FileInfo excelFile = new FileInfo(filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage package = new ExcelPackage(excelFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 6; row <= rowCount; row++)
                {
                    string name = worksheet.Cells[row, 2].Text.Trim();
                    string type = worksheet.Cells[row, 3].Text.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
                    {
                        continue;
                    }

                    string setText = worksheet.Cells[row, 5].Text.Trim();
                    bool setParameter = string.Equals(setText, "SI", StringComparison.OrdinalIgnoreCase);

                    ExcelParameter parameter = new ExcelParameter
                    {
                        Name = name,
                        Type = type,
                        Value = worksheet.Cells[row, 4].Value,
                        SetParameter = setParameter
                    };

                    excelParameters.Add(parameter);
                }
            }

            return excelParameters;
        }
    }
}


