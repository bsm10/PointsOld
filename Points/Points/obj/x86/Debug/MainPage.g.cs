﻿#pragma checksum "E:\Programming\MyProject\Points_win10\Points\Points\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "27B67BAD7F6CAFB2CBA94384A1978C21"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Points
{
    partial class MainPage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                {
                    this.canvas = (global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)(target);
                    #line 12 "..\..\..\MainPage.xaml"
                    ((global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)this.canvas).Draw += this.canvas_Draw;
                    #line 12 "..\..\..\MainPage.xaml"
                    ((global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)this.canvas).Tapped += this.canvas_Tapped;
                    #line 12 "..\..\..\MainPage.xaml"
                    ((global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)this.canvas).PointerMoved += this.canvas_PointerMoved;
                    #line default
                }
                break;
            case 2:
                {
                    this.txtCoordinate = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

