using System;
using System.Collections.Generic;
using System.Text;

namespace SRXDScoreMod; 

// Used to log play data in a tabular format
internal class StringTable {
    public enum Alignment {
        Left,
        Right
    }

    private string[] header;
    private string[,] data;
    private Alignment[] alignments;
    private int[] columnWidths;
    private bool[] mergedWithNext;
    private int columns;
    private int rows;

    public StringTable(int columns, int rows) {
        this.columns = columns;
        this.rows = rows;
        header = new string[columns];
        data = new string[columns, rows];
        alignments = new Alignment[columns];
        columnWidths = new int[columns];
        mergedWithNext = new bool[columns];

        for (int i = 0; i < columns; i++) {
            header[i] = string.Empty;
                
            for (int j = 0; j < rows; j++)
                data[i, j] = string.Empty;
        }
    }

    public void SetHeader(params string[] values) {
        int count = Math.Min(columns, values.Length);

        for (int i = 0; i < count; i++)
            header[i] = values[i];

        for (int i = 0; i < columns - 1; i++)
            mergedWithNext[i] = string.IsNullOrWhiteSpace(header[i + 1]);
    }

    public void SetRow(int row, params string[] values) {
        int count = Math.Min(columns, values.Length);

        for (int i = 0; i < count; i++) {
            string value = values[i];
                
            data[i, row] = value;

            if (value.Length > columnWidths[i])
                columnWidths[i] = value.Length;
        }
    }

    public void SetDataAlignment(params Alignment[] alignments) {
        int count = Math.Min(columns, alignments.Length);

        for (int i = 0; i < count; i++)
            this.alignments[i] = alignments[i];
    }

    public void ClearData() {
        for (int i = 0; i < columns; i++) {
            for (int j = 0; j < rows; j++)
                data[i, j] = string.Empty;

            columnWidths[i] = 0;
        }
    }

    public IEnumerable<string> GetRows() {
        var builder = new StringBuilder();

        for (int i = 0; i < columns - 1; i++) {
            string cell = header[i];
            int width = columnWidths[i];

            if (mergedWithNext[i]) {
                width += columnWidths[i + 1] + 1;
                    
                if (cell.Length > width) {
                    if (alignments[i] == Alignment.Right)
                        columnWidths[i] += cell.Length - width;
                    else
                        columnWidths[i + 1] += cell.Length - width;
                }

                i++;
            }
            else if (cell.Length > width) {
                width = cell.Length;
                columnWidths[i] = width;
            }
                
            builder.Append(cell.PadRight(width));
            builder.Append(" | ");
        }

        string last = header[columns - 1];

        if (last.Length > columnWidths[columns - 1])
            columnWidths[columns - 1] = last.Length;
            
        builder.Append(last);

        yield return builder.ToString();

        builder.Clear();

        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < columns - 1; j++) {
                string cell = data[j, i];
                    
                if (alignments[j] == Alignment.Right)
                    builder.Append(cell.PadLeft(columnWidths[j]));
                else
                    builder.Append(cell.PadRight(columnWidths[j]));

                if (mergedWithNext[j])
                    builder.Append(' ');
                else
                    builder.Append(" | ");
            }

            last = data[columns - 1, i];
                
            if (alignments[columns - 1] == Alignment.Right)
                builder.Append(last.PadLeft(columnWidths[columns - 1]));
            else
                builder.Append(last);

            yield return builder.ToString();

            builder.Clear();
        }
    }
}