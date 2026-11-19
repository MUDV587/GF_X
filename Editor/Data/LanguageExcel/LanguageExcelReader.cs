using OfficeOpenXml;
using OfficeOpenXml.Drawing.Controls;
using System;
using System.Collections.Generic;
using System.IO;

namespace UGF.EditorTools
{
    /// <summary>
    /// 语言 Excel 只读解析器。纯文件解析，不依赖任何设置/路径，
    /// 供 GameData 导出管线与 Localization 工具共用。
    /// </summary>
    public static class LanguageExcelReader
    {
        public const string EXCEL_I18N_TAG = "i18n";//把Excel表备注行标识为i18n的列扫描到多语言Excel
        private const int LockColumn = 1;
        private const int KeyColumn = 2;
        private const int ValueColumn = 3;

        /// <summary>加载语言 Excel 的全部文本（首个工作表，第 1 列锁定勾选框，第 2 列 Key，第 3 列 Value）。</summary>
        public static void LoadLanguageExcelTexts(string languageExcelFile, ref List<LocalizationText> localizationTexts)
        {
            localizationTexts.Clear();
            if (!File.Exists(languageExcelFile))
            {
                return;
            }

            try
            {
                using FileStream excelStream = new FileStream(languageExcelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var excelPackage = new ExcelPackage(excelStream);
                if (excelPackage.Workbook.Worksheets.Count <= 0)
                {
                    return;
                }

                var excelSheet = excelPackage.Workbook.Worksheets[0];
                if (excelSheet.Dimension == null)
                {
                    return;
                }

                for (var rowIndex = excelSheet.Dimension.Start.Row; rowIndex <= excelSheet.Dimension.End.Row; rowIndex++)
                {
                    var key = excelSheet.GetValue<string>(rowIndex, KeyColumn);
                    var value = excelSheet.GetValue<string>(rowIndex, ValueColumn);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    var langText = new LocalizationText
                    {
                        Key = key,
                        Value = value
                    };
                    var cell = excelSheet.Cells[rowIndex, LockColumn];
                    var checkBox = excelSheet.Drawings[cell.Address] as ExcelControlCheckBox;
                    langText.Locked = checkBox != null && checkBox.Checked == eCheckState.Checked;
                    localizationTexts.Add(langText);
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"读取语言本地化文本列表失败:{exception.Message}");
            }
        }
    }
}
