using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BimManagement
{
    public class CreatePushButtons
    {
        public static PushButtonData ButtonShareSheet()
        {
            RevitLibrary.ButtonData buttonData = new RevitLibrary.ButtonData()
            {
                AvailabilityClassName = string.Empty,
                Icon = Properties.Resources.Share,
                Name = "Enviar\nPlanos",
                Text = "Enviar\nPlanos",
                AssemblyName = Assembly.GetExecutingAssembly().Location,
                ClassName = "BimManagement.ShareSheetsApp",
                ShortDescription = "Prepara la documentacion para enviar",
                LongDescription = "Obtiene los planos del modelo para luego buscar esos nombres en los " +
                "archivos segun la revision que se indica, luego genera un modelo de revit para cada plano" +
                " y los pega en la carpeta de COMPARTIDOS con las misma estructura de de carpetas",

                Contextual = "https://www.casacontratistas.com/"
            };

            PushButtonData btnData = RevitLibrary.CreatePushButtons.PushButton(buttonData,
                Properties.Resources.ToolTip);

            return btnData;
        }
    }
}
