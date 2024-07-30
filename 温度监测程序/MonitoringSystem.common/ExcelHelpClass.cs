using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace 温度监测程序.MonitoringSystem.common
{
    public class ExcelHelpClass
    {
        public string GetPath(string type)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                try
                {
                    saveFileDialog.FileName = DateTime.Now.ToString("yyyy-MM-dd");
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = saveFileDialog.FileName;
                        if (Path.GetExtension(filePath) != type)
                        {
                            filePath += type;
                        }
                        return filePath;
                    }
                    return null;
                }
                catch (Exception)
                {
                    MessageBox.Show("请选择正确的路径！");
                    return null;
                }
            }
        }

        public bool AppendData(IWorkbook readWorkbook, string path, List<List<string>> dataCount)
        {
            try
            {
                if (readWorkbook == null || dataCount.Count <= 0)
                {
                    return false;
                }
                ISheet sheet = readWorkbook.GetSheetAt(0);
                ICellStyle cellStyle = readWorkbook.CreateCellStyle();
                cellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                cellStyle.VerticalAlignment = VerticalAlignment.Center;
                for (int j = 0; j < dataCount.Count; j++)
                {
                    List<string> data = dataCount[j];
                    if (data.Count > 0)
                    {
                        IRow row = sheet.CreateRow(sheet.LastRowNum + 1);
                        for (int i = 0; i < data.Count; i++)
                        {
                            ICell cell = row.CreateCell(i);
                            cell.SetCellValue(data[i]);
                            cell.CellStyle = cellStyle;
                        }
                    }
                }
                using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    readWorkbook.Write(stream);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public IWorkbook GetReadWorkbook(string FilePath)
        {
            try
            {
                IWorkbook ReadWorkbook = null;
                string ExtensionName = Path.GetExtension(FilePath);
                FileStream FileStream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite);
                ReadWorkbook = (ExtensionName.Equals(".xls") ? ((IWorkbook)new HSSFWorkbook(FileStream)) : ((IWorkbook)((!ExtensionName.Equals(".xlsx")) ? null : new XSSFWorkbook((Stream)FileStream))));
                FileStream.Close();
                return ReadWorkbook;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IWorkbook GetWriteWorkbook(string FilePath)
        {
            IWorkbook WriteWorkbook = null;
            string ExtensionName = Path.GetExtension(FilePath);
            if (ExtensionName.Equals(".xls"))
            {
                return new HSSFWorkbook();
            }
            if (ExtensionName.Equals(".xlsx"))
            {
                return new XSSFWorkbook();
            }
            return null;
        }

        public List<ISheet> GetSheets(IWorkbook ReadWorkbook)
        {
            List<ISheet> Sheets = new List<ISheet>();
            int SheetCount = ReadWorkbook.NumberOfSheets;
            for (int i = 0; i < SheetCount; i++)
            {
                Sheets.Add(ReadWorkbook.GetSheetAt(i));
            }
            return Sheets;
        }

        public ISheet GetSingleSheet(string sheetName, IWorkbook ReadWorkbook)
        {
            int sheetIndex = ReadWorkbook.GetSheetIndex(sheetName);
            return ReadWorkbook.GetSheetAt(sheetIndex);
        }
    }
}
