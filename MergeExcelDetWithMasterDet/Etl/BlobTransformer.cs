﻿//using ExcelDataReader;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Caf.Projects.CafLogisticsSampleProcessing.Etl
{
    /// <summary>
    /// Transforms Excel files
    /// </summary>
    public class BlobTransformer
    {
        /// <summary>
        /// Merges two Excel files of similar format
        /// </summary>
        /// <param name="det">Excel file with new data</param>
        /// <param name="master">Excel file for new data to be copied to</param>
        /// <param name="headerRow">Row number of header</param>
        /// <param name="templateNameRow">Row number that contains the template name - used to assume files are the same format</param>
        /// <param name="templateNameCol">Column number that contains the template name - used to assume files are the same format</param>
        /// <returns>MemoryStream of the merged files</returns>
        /// <exception cref="ArgumentException">Thrown when one or more blobs have no data or the two files do not have the same template name</exception>
        public MemoryStream MergeBlobs(
            MemoryStream det, 
            MemoryStream master,
            int headerRow,
            int templateNameRow = 1,
            int templateNameCol = 1)
        {
            if(det.Length == 0 || master.Length == 0)
            {
                throw new ArgumentException("One or more input blobs do not have data");
            }

            MemoryStream resultStream = new MemoryStream();

            using (ExcelPackage detPackage = new ExcelPackage(det))
            using (ExcelPackage masterPackage = new ExcelPackage(master))
            {
                ExcelWorksheet detWS = detPackage.Workbook.Worksheets[1];
                ExcelWorksheet masterWS = masterPackage.Workbook.Worksheets[1];

                if(!VerifyTemplatesMatch(detWS, masterWS, 
                    templateNameRow, templateNameCol))
                {
                    throw new ArgumentException(
                        "DET file does not match Master file");
                }

                // References Master, only copies from DET if cell is empty or is a parameter (cell above header row)
                // This means values in Master cannot be changed.
                ExcelCellAddress end = masterWS.Dimension.End;
                
                for(int row = 1; row <= end.Row; row++)
                {
                    for (int col = 1; col <= end.Column; col++)
                    {
                        if(row < headerRow)
                        {
                            UpdateWithOverwrite(masterWS,  detWS, row, col);
                        }
                        else
                        {
                            UpdateWithoutOverwrite(masterWS, detWS, row, col);
                        }
                    }
                }

                masterPackage.SaveAs(resultStream);
            }

            return resultStream;
        }

        /// <summary>
        /// Copies cell value in DET to same cell in Master, overwrites any previous values
        /// </summary>
        /// <param name="master">Master file with value to overwrite</param>
        /// <param name="det">DET file with value to write to Master</param>
        /// <param name="row">Row number of cell with value</param>
        /// <param name="col">Column number of cell with value</param>
        /// <returns>true if value copied</returns>
        private bool UpdateWithOverwrite(
            ExcelWorksheet master,
            ExcelWorksheet det,
            int row,
            int col)
        {
            var detValue = det.Cells[row, col].Text;
            master.Cells[row, col].Value = detValue;

            return true;
        }

        /// <summary>
        /// Copies cell value in DET to same cell in Master, does not copy if Master has current value
        /// </summary>
        /// <param name="master">Master file to copy value into</param>
        /// <param name="det">DET file with value to write to Master</param>
        /// <param name="row">Row number of cell with value</param>
        /// <param name="col">Column number of cell with value</param>
        /// <returns>true if value copied, false if not</returns>
        private bool UpdateWithoutOverwrite(
            ExcelWorksheet master,
            ExcelWorksheet det,
            int row,
            int col)
        {
            bool valueReplaced = false;

            // Get value from Master, if blank, check DET for value
            var masterValue = master.Cells[row, col].Text;
            if (masterValue.Length == 0)
            {
                var detValue = det.Cells[row, col].Text;
                if (detValue.Length > 0)
                {
                    master.Cells[row, col].Value = detValue;

                    valueReplaced = true;
                }
            }

            return valueReplaced;
        }

        /// <summary>
        /// Compares the template names of the two Excel files
        /// </summary>
        /// <param name="det">Excel file with new data</param>
        /// <param name="master">Excel file for new data to be copied to</param>
        /// <param name="templateNameRow">Row number that contains the template name</param>
        /// <param name="templateNameCol">Column number that contains the template name</param>
        /// <returns>True if the template names match, false if not</returns>
        private bool VerifyTemplatesMatch(
            ExcelWorksheet det, 
            ExcelWorksheet master,
            int templateNameRow,
            int templateNameCol)
        {
            bool doColNumMatch = 
                det.Dimension.End.Column == master.Dimension.End.Column;

            bool doTemplateTypesMatch = 
                det.Cells[templateNameRow, templateNameCol].GetValue<string>() == 
                master.Cells[templateNameRow, templateNameCol].GetValue<string>();

            return doColNumMatch && doTemplateTypesMatch;
        }
    }
}
