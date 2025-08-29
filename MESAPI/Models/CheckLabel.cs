using LabelManager2;
using System;
using System.Linq;

namespace MES.Models
{
    /// <summary>
    /// List input parameter
    /// </summary>
    public class CheckLabel
    {
        public static object GetInfo (string path_folder)
        {
            object labelInfo = new
            {
                FormVariables = "",
                FreeVariables = "",
                Formulas = "",
                Barcodes = "",
                Texts = ""
            };
            try
            {
                ApplicationClass lbl = new ApplicationClass();
                Document doc = lbl.ActiveDocument; ;
                lbl.Documents.Open(path_folder, false);
                doc = lbl.ActiveDocument;
                var formVariables = Enumerable.Range(1, doc.Variables.FormVariables.Count)
                    .Select(i => doc.Variables.FormVariables.Item(i))
                    .Select(v => new
                    {
                        Name = v.Name,
                        Value = v.Value
                    })
                    .ToList();

                var freeVariables = Enumerable.Range(1, doc.Variables.FreeVariables.Count)
                    .Select(i => doc.Variables.FreeVariables.Item(i))
                    .Select(v => new
                    {
                        Name = v.Name,
                        Value = v.Value
                    })
                    .ToList();

                var formulas = Enumerable.Range(1, doc.Variables.Formulas.Count)
                    .Select(i => doc.Variables.Formulas.Item(i))
                    .Select(v => new
                    {
                        Name = v.Name,
                        Expression = v.Expression,
                        Value = v.Value
                    })
                    .ToList();

                var barcodes = Enumerable.Range(1, doc.DocObjects.Barcodes.Count)
                    .Select(i => doc.DocObjects.Barcodes.Item(i))
                    .Select(o => new
                    {
                        Name = o.Name,
                        Left = o.Left,
                        Top = o.Top,
                        VariableName = o.VariableName,
                        Symbology = o.Symbology.ToString(),
                        Value = o.Value,
                        Printable = o.Printable
                    })
                    .ToList();

                var texts = Enumerable.Range(1, doc.DocObjects.Texts.Count)
                    .Select(i => doc.DocObjects.Texts.Item(i))
                    .Select(o => new
                    {
                        Name = o.Name,
                        Left = o.Left,
                        Top = o.Top,
                        VariableName = o.VariableName,
                        Value = o.Value,
                        Data1 = o.Font.Name
                    })
                    .ToList();
                labelInfo = new
                {
                    FormVariables = formVariables,
                    FreeVariables = freeVariables,
                    Formulas = formulas,
                    Barcodes = barcodes,
                    Texts = texts
                };
                lbl.Quit();
                return labelInfo;
            }
            catch (Exception ex)
            {
                labelInfo = new
                {
                    FormVariables = "Exception: " + ex.Message,
                    FreeVariables = "",
                    Formulas = "",
                    Barcodes = "",
                    Texts = ""
                };
                return labelInfo;
            }
        }
    }
}