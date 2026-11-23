// Referenece https://www.youtube.com/watch?v=lYjHDkpbsas 

#region Namespaces
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;


#endregion

namespace RevitWindDrivenRain
{
    class App : IExternalApplication
    {

        const string TAB_NAME = "RevitWindDrivenRain";
        const string PANEL4_NAME = "Weathering Dose";//"Results and Visualization"
        const string PANEL6_NAME = "Code Testing";
        // private object button;

        public Result OnStartup(UIControlledApplication a)
        {
            //get the ribbon tab
            try
            {
                a.CreateRibbonTab(TAB_NAME);
            }
            catch (Exception) { } // tab already exist


           

            //START OF PANEL 4
            //get or create PANELS
            RibbonPanel panel4 = null;
            List<RibbonPanel> panels4 = a.GetRibbonPanels(TAB_NAME);
            foreach (RibbonPanel pnl4 in panels4)
            {
                if (pnl4.Name == PANEL4_NAME)
                {
                    panel4 = pnl4;
                    break;
                }
            }

            //couldnt find the panel, create it
            if (panel4 == null)
            {
                panel4 = a.CreateRibbonPanel(TAB_NAME, PANEL4_NAME);
            }

            //create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            PushButtonData panel4_Pushbtn1 = new PushButtonData("MyButton", "Wind Driven Rain", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.PickedFaceRayProjectionCode_new")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };
            PushButton add_panel4_Pushbtn1 = panel4.AddItem(panel4_Pushbtn1) as PushButton;

            //STACKED BUTTONS
            //PushButtonData panel4_Stackbtn1 = new PushButtonData("MyStackButton1", "Floor-Ceiling Distance", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.CheckElementType")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/icon50X50.png"))
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel4_Stackbtn2 = new PushButtonData("MyStackButton2", "My Stack Ribbon Button 2", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.PickedFaceRayProjectionCode_new")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/icon30X30.png")),
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel4_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.ApplyAVFToAllFaces")//ApplyAVFToAllFace
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/icon30X30.png")),
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            //};



            //panel4.AddStackedItems(panel4_Stackbtn1, panel4_Stackbtn2, panel4_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc

            //END OF PANEL 4



            ////START OF PANEL 6
            ////get or create PANELS
            RibbonPanel panel6 = null;
            List<RibbonPanel> panels6 = a.GetRibbonPanels(TAB_NAME);
            foreach (RibbonPanel pnl6 in panels6)
            {
                if (pnl6.Name == PANEL6_NAME)
                {
                    panel6 = pnl6;
                    break;
                }
            }

            //couldnt find the panel, create it
            if (panel6 == null)
            {
                panel6 = a.CreateRibbonPanel(TAB_NAME, PANEL6_NAME);
            }


            //create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            PushButtonData panel6_Pushbtn1 = new PushButtonData("MyTestButton1", "GetFaceRefFromElements", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.CreateMaterials")// GetFaceRefFromElements
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/LCC30X30.png"))
            };
            PushButton add_panel6_Pushbtn1 = panel6.AddItem(panel6_Pushbtn1) as PushButton;

            PushButtonData panel6_Pushbtn2 = new PushButtonData("MyTestButton2", "AVFtoSelectedElement", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.AVFtoSelectedElement")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/Results30x30.png"))
            };
            PushButton add_panel6_Pushbtn2 = panel6.AddItem(panel6_Pushbtn2) as PushButton;


            ////-------------------------------TEST-----------------------------------
            //// Create PullDown Button
            PushButtonData panel6_PushDropDownbtn1 = new PushButtonData("PullTestButton1", "CheckElementType", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.CheckElementType")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn2 = new PushButtonData("MyTestDropDownButton2", "test3DPreview", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.test3DPreviewXAML")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn3 = new PushButtonData("MyTestDropDownButton3", "SelectFaceAddAVF", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.ApplyAVFToFace")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn4 = new PushButtonData("MyTest_DropDownButton4", "AddButtonName", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.RayCastingFaceIntersection")//CreateDummyGeometry,RayCastingFaceIntersection, GeometryUtility, SolidIntersect
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };


            PushButtonData panel6_PushDropDownbtn5 = new PushButtonData("TestButton5", "My Ribbon Button", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.RayCasting")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn6 = new PushButtonData("MyDropDownButton6", "ListContactFacesCommand", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.RayIntersectCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn7 = new PushButtonData("MyDropDownButton7", "SolidIntersectNEW", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.ColorPickedFacesCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn8 = new PushButtonData("MyDropDownButton8", "SolidIntersect", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.RayCastingFaceIntersection")//CreateDummyGeometry,RayCastingFaceIntersection, GeometryUtility, SolidIntersect
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn9 = new PushButtonData("InteractiveMap", "InteractiveMap", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.ShowWebViewCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn10 = new PushButtonData("MyDropDownButton10", "ShowMapViewCommand", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.ShowInteractiveMapCommand") //RevitWindDrivenRain.Command_AddParameters.SetProjectLocationUpdate.InteractiveMapWindow.ShowInteractiveMapCommand
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn11 = new PushButtonData("MyDropDownButton11", "RaindropAVFApplication", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.RaindropAVFApplication")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn12 = new PushButtonData("MyDropDownButton12", "ElementLoopSelector", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.ElementLoopSelector")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn13 = new PushButtonData("MyDropDownButton13", "FindAndHighlightElementsWithoutMaterials", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.FindAndHighlightElementsWithoutMaterials")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn14 = new PushButtonData("MyDropDownButton14", "SetMaterialClassCommand", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.SetMaterialClassCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn15 = new PushButtonData("MyDropDownButton15", "AVFapplyColorToFace ", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.CreateAnalysisDisplayTypeRanges")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            PushButtonData panel6_PushDropDownbtn16 = new PushButtonData("MyDropDownButton16", "CreateElement", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.CreateElement")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/IFC30X30.png"))
            };

            SplitButtonData panel6_PushSplitbtn = new SplitButtonData("splitButton1", "Split");
            SplitButton panel6_PushDropDownbtns = panel6.AddItem(panel6_PushSplitbtn) as SplitButton;

            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn1);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn2);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn3);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn4);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn5);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn6);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn7);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn8);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn9);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn10);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn11);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn12);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn13);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn14);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn15);
            panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn16);

            ////-------------------------------END_TEST-----------------------------------


            //STACKED BUTTONS
            /*
            PushButtonData panel3_Stackbtn1 = new PushButtonData("MyStackButton1", "My Stack Ribbon Button 1", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/icon50X50.png"))
                //LargeImage = panel1_Stackbtn1_icon
            };

            PushButtonData panel3_Stackbtn2 = new PushButtonData("MyStackButton2", "My Stack Ribbon Button 2", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/icon30X30.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };

            PushButtonData panel3_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWindDrivenRain.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWindDrivenRain;component/Resources/icon30X30.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };
            panel3.AddStackedItems(panel3_Stackbtn1, panel3_Stackbtn2, panel3_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc
            */
            //END OF PANEL 6


            // ENDING LINE FOR APPLICATION INTERFACE
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }


    }
}
