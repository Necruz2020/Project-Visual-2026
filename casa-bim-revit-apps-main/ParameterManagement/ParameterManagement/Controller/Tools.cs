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
    /// <summary>
    /// Herramientas para obtner elementos
    /// </summary>
    class RevitTools
    {
        public static ExternalCommandData commandData { get; set; }
        public static UIDocument uidoc { get; internal set; }
        public static Application app { get; set; }
        public static Document doc { get; set; }
        public static Selection sel { get; set; }
    }
}