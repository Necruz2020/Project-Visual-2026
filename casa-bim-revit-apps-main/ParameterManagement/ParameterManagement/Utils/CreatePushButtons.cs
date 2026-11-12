using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManagement
{
    public class CreatePushButtons
    {
        public static PushButtonData ButtonCopyParameter()
        {            
            RevitLibrary.ButtonData buttonData = new RevitLibrary.ButtonData()
            {
                AvailabilityClassName = string.Empty,
                Icon = Properties.Resources.CopyParameter_Medium,
                Name = "Copy\nParameters",
                Text = "Copy\nParameters",
                AssemblyName = Assembly.GetExecutingAssembly().Location,
                ClassName = "ParameterManagement.CopyParametersApp",
                ShortDescription = "Copy all parameters",
                LongDescription = "Copy all parameters taking an element as a base",
                Contextual = "https://www.casacontratistas.com/"
            };

            PushButtonData btnData = RevitLibrary.CreatePushButtons.PushButton(buttonData,
                Properties.Resources.ToolTip);

            return btnData;
        }

        public static PushButtonData ButtonCreateAndFillParameters()
        {
            RevitLibrary.ButtonData buttonData = new RevitLibrary.ButtonData()
            {
                AvailabilityClassName = string.Empty,
                Icon = Properties.Resources.CreateParameter_Small,
                Name = "Create\nParameters",
                Text = "Create\nParameters",
                AssemblyName = Assembly.GetExecutingAssembly().Location,
                ClassName = "ParameterManagement.CreateFillParameterApp",
                ShortDescription = "Create new parameters and fill their values",
                LongDescription = "From the excel file create project parameters" +
                " and then you can assign the values for those parameters",
                Contextual = "https://www.casacontratistas.com/"
            };

            PushButtonData btnData = RevitLibrary.CreatePushButtons.PushButton(buttonData,
                Properties.Resources.ToolTip);

            return btnData;
        }
    }
}
