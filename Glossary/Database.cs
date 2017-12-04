using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;

namespace Glossary
{
    class Database
    {
        // path to data file
        private static String DataFilePath = "../../data/database.csv";

        // Levenshtein Distance Max value
        private static int LVD_MAX = 2;

        // Create a new DataTable.
        private DataSet dataSet;
        private DataTable dataTable;

        // Backreference to Backgroundworker to report progress
        private BackgroundWorker worker { get; set; }

        // progress value
        public volatile int progress = 0;

        // Constructor
        public Database(BackgroundWorker dbLoadWorker)
        {
            worker = dbLoadWorker;
        }

        public void InitDatabase()
        {
            InitDataTable();
            LoadDatabase();
        }

        private void InitDataTable()
        {
            // Instantiate the DataSet variable.
            dataSet = new DataSet("Glossary");

            // create DataTable
            dataTable = new DataTable("GlossaryTable");

            // Declare variables for DataColumn and DataRow objects.
            DataColumn column;

            // Create new DataColumn, set DataType, 
            // ColumnName and add to DataTable.    
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "id";
            column.ReadOnly = true;
            column.Unique = true;
            // Add the Column to the DataColumnCollection.
            dataTable.Columns.Add(column);

            // Create second column.
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "German";
            column.AutoIncrement = false;
            column.Caption = "German";
            column.ReadOnly = false;
            column.Unique = false;
            // Add the column to the table.
            dataTable.Columns.Add(column);

            // Create second column.
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "English";
            column.AutoIncrement = false;
            column.Caption = "English";
            column.ReadOnly = false;
            column.Unique = false;
            // Add the column to the table.
            dataTable.Columns.Add(column);

            // Make the ID column the primary key column.
            DataColumn[] PrimaryKeyColumns = new DataColumn[1];
            PrimaryKeyColumns[0] = dataTable.Columns["id"];
            dataTable.PrimaryKey = PrimaryKeyColumns;

            // Add the new DataTable to the DataSet.
            dataSet.Tables.Add(dataTable);
        }

        private void LoadDatabase()
        {
            int counter = 0;

            // open data file and read all lines 
            // we need to read all lines to be able to know the progress
            if (File.Exists(DataFilePath))
            {
                string[] lines = System.IO.File.ReadAllLines(DataFilePath);
                int cnt = lines.Count();

                // progress steps
                int ps = cnt / 100;

                foreach (string line in lines)
                {
                    String[] terms = line.Split(';');
                    String termDE = terms[0];
                    String termEN = terms[1];

                    // remove brackets and content in brackets
                    termDE = Regex.Replace(termDE, @" ?[\{\[\(].*?[\}\]\)]", string.Empty);
                    termDE = termDE.Trim();

                    // add data to database
                    DataRow row = dataTable.NewRow();
                    row["id"] = counter;
                    row["German"] = termDE;
                    row["English"] = termEN;
                    dataTable.Rows.Add(row);

                    counter++;
                    if ((counter/cnt) % ps == 0)
                    {
                        progress = 100 * counter / cnt;
                    }
                }
            }
            else
            {
                Console.WriteLine("File {0} does not exist!", DataFilePath);
            }
            progress = 100;
        }

        internal long getNumberOfEntries()
        {
            return dataTable.Rows.Count;
        }

        public DataTable Search(String searchTerm)
        {

            progress = 0;

            //var watch = System.Diagnostics.Stopwatch.StartNew();

            // query the database for an exact match
            EnumerableRowCollection<DataRow> resultList1 =
                from row in dataTable.AsEnumerable()
                where row.Field<String>("German") == searchTerm
                select row;

            progress = 10;

            //Console.WriteLine(watch.ElapsedMilliseconds.ToString("N"));
            //watch = System.Diagnostics.Stopwatch.StartNew();

            // query the database for match containing the searchTerm 
            // we could do this in on query but the the order would be mixed in the result
            // exact matches should be first though
            EnumerableRowCollection<DataRow> resultList2 =
                from row in dataTable.AsEnumerable()
                where row.Field<String>("German").Contains(searchTerm)
                select row;

            progress = 20;

            //Console.WriteLine(watch.ElapsedMilliseconds.ToString("N"));
            //watch = System.Diagnostics.Stopwatch.StartNew();

            // search using Levenshtein Distance
            // query the database
            EnumerableRowCollection<DataRow> fuzzyList =
                from row in dataTable.AsEnumerable()
                where Levenshtein.LevenshteinDistance(searchTerm, row.Field<String>("German").ToString()) <= LVD_MAX
                orderby Levenshtein.LevenshteinDistance(searchTerm, row.Field<String>("German").ToString()) ascending
                select row;

            progress = 30;

            //Console.WriteLine(watch.ElapsedMilliseconds.ToString("N"));
            //watch = System.Diagnostics.Stopwatch.StartNew();

            // union excludes entries already in the result
            IEnumerable<DataRow> union = resultList1.Union<DataRow>(resultList2.Union<DataRow>(fuzzyList));

            progress = 60;
            //Console.WriteLine(watch.ElapsedMilliseconds.ToString("N"));
            //watch = System.Diagnostics.Stopwatch.StartNew();

            DataTable dt = union.CopyToDataTable<DataRow>();

            progress = 100;
            //Console.WriteLine(watch.ElapsedMilliseconds.ToString("N"));

            return dt;
        }
    }
}

