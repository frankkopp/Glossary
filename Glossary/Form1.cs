using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Glossary
{
    public partial class mainWindowForm : Form
    {

        private BackgroundWorker DbLoadWorker = new BackgroundWorker();
        private BackgroundWorker SearchWorker = new BackgroundWorker();

        private Database db;

        private DataTable DTable = new DataTable();
        private BindingSource SBind = new BindingSource();
        private Stopwatch watch;

        private Timer timer;

        /// <summary>
        /// Constructor for main window
        /// </summary>
        public mainWindowForm()
        {
            InitializeComponent();
            StartInitializingDatabaseMemoryStatus();
            InitProgressbarUpdater();
            InitDbLoadWorker();
            InitSearchWorker();
            searchTerm.Focus();

            // Bind the DataGridView to the BindingSource
            // and load the data from the database.
            resultGrid.DataSource = SBind;

            // create database - needs to ne initialized later
            db = new Database(DbLoadWorker); // parameter worker to report progress
        }

        /// <summary>
        /// Updates the status label for used memory every 200ms
        /// </summary>
        private void StartInitializingDatabaseMemoryStatus()
        {
            long memory = GC.GetTotalMemory(true)/1000000;
            statusMemoryLabel.Text = memory.ToString("N0") + " MB";

            var timer = new Timer { Interval = 200 };
            timer.Tick += (o, args) =>
            {
                memory = GC.GetTotalMemory(true)/1000000;
                statusMemoryLabel.Text = memory.ToString("N0") + " MB";
            };
            timer.Start();
        }

        /// <summary>
        /// Initializes the Backgroundworker 
        /// </summary>
        private void InitDbLoadWorker()
        {
            // initialize the search worker
            SearchWorker.DoWork += new DoWorkEventHandler(SearchWorker_DoWork);
            SearchWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SearchWorker_RunWorkerCompleted);
            SearchWorker.ProgressChanged += new ProgressChangedEventHandler(SearchWorker_ProgressChanged);
            SearchWorker.WorkerReportsProgress = true;
            SearchWorker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Initializes the Backgroundworker 
        /// </summary>
        private void InitSearchWorker()
        {
            // initialize the db load worker
            DbLoadWorker.DoWork += new DoWorkEventHandler(DbLoadWorker_DoWork);
            DbLoadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DbLoadWorker_RunWorkerCompleted);
            DbLoadWorker.ProgressChanged += new ProgressChangedEventHandler(DbLoad_ProgressChanged);
            DbLoadWorker.WorkerReportsProgress = true;
            DbLoadWorker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// runs at Shown event of mainWindowForm to load database
        /// </summary>
        private void StartInitializingDatabase(object sender, EventArgs e)
        {
            InitDatabaseTask();
        }

        /// <summary>
        /// loads database as a parallel thread (Task)
        /// </summary>
        private void InitDatabaseTask()
        {
            if (!DbLoadWorker.IsBusy) //Check if the worker is already in progress
            {
                statusLabel.Text = "Loading database...";
                searchTerm.Enabled = false;
                goButton.Enabled = false;
                this.Cursor = Cursors.WaitCursor; // hourglass cursor
                watch = System.Diagnostics.Stopwatch.StartNew();
                object[] arrObjects = new object[] {}; //Declare the array of objects
                DbLoadWorker.RunWorkerAsync(arrObjects); //Call the background worker
            }
        }

        /// <summary>
        /// creates database  which in turn loads data 
        /// </summary>
        private void DbLoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            db.InitDatabase();
        }

        /// <summary>
        /// updates the progress bar for running tasks
        /// </summary>
        private void InitProgressbarUpdater()
        {
            timer = new Timer { Interval = 200 };
            timer.Tick += (o, args) =>
            {
                statusProgressBar.Value = db.progress;
            };
            timer.Start();
        }

        private void DbLoadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            watch.Stop();
            statusLabel.Text = "Loaded " + db.getNumberOfEntries().ToString("N0") + " Entries in " + watch.ElapsedMilliseconds.ToString("N0") + "ms" + ". Ready!";
            timer.Stop();
            searchTerm.Enabled = true;
            goButton.Enabled = true;
            this.Cursor = Cursors.Default;
            searchTerm.Focus();
            searchTerm.SelectAll();
        }

        private void DbLoad_ProgressChanged(object sender, ProgressChangedEventArgs e) { } // => statusProgressBar.Value = e.ProgressPercentage;

        /// <summary>
        /// is called when the GO button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoButton_Click(object sender, EventArgs e)
        {
            if (!SearchWorker.IsBusy)//Check if the worker is already in progress
            {
                searchTerm.Enabled = false;
                goButton.Enabled = false;
                this.Cursor = Cursors.WaitCursor; // hourglass cursor
                Application.DoEvents();
                watch = System.Diagnostics.Stopwatch.StartNew();
                statusProgressBar.Value = 0;
                statusLabel.Text = "Search database..."; // set status in Statusbar
                object[] arrObjects = new object[] { searchTerm.Text }; //Declare the array of objects
                SearchWorker.RunWorkerAsync(arrObjects); //Call the background worker
            }
        }

        /// <summary>
        /// background task to execute a search in the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void SearchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] arrObjects = (object[])e.Argument;
            String term = (String)arrObjects[0];
            e.Result = db.Search(term);
        }

        /// <summary>
        /// Callback when search Task is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void SearchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null) //Check if the worker has been canceled or if an error occurred
            {
                DataTable resultTable = (DataTable)e.Result; //Get the result from the background thread
                resultTable.Columns.Remove("id"); // remove id column as we do not want to show it 
                SBind.DataSource = resultTable; // shows result table in the UI by binding it to the UI control
            }
            else if (e.Cancelled)
            {
                statusLabel.Text = "User Canceled";
            }
            else
            {
                statusLabel.Text = "An error has occurred";
            }
            watch.Stop();
            statusLabel.Text = "Ready! (" + watch.ElapsedMilliseconds.ToString("N0") + "ms)";
            this.Cursor = Cursors.Default;
            searchTerm.Enabled = true;
            goButton.Enabled = true;
            searchTerm.Focus();
        }

        protected void SearchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        /// <summary>
        /// is called when the search field is entered to clear the previous text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchTerm_Enter(object sender, EventArgs e)
        {
            searchTerm.SelectAll();
        }


    }
}
