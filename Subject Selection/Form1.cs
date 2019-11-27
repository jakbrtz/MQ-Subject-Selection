using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Subject_Selection
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        readonly Plan plan = new Plan();
        Decision currentDecision;
        Subject currentSubject;

        private void Form1_Load(object sender, EventArgs e)
        {
            Parser.LoadData();

            foreach (int i in new int[]{ 4, 4, 2, 4, 4, 2, 4, 4, 0, 4, 4, 1})
                plan.MaxSubjects.Add(i);

            foreach (Subject course in Parser.AllCourses())
                AddCriteriaToFLP(course);
        }

        private void LBXdecisions_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentDecision = LBXdecisions.SelectedItem as Decision;

            Console.WriteLine("Current Decision:    " + currentDecision.ToString());
            Console.Write("Reasons:             ");
            foreach (Subject reason in currentDecision.GetReasons())
                Console.Write(reason + " ");
            Console.WriteLine();

            DisplayCurrentDecision();
        }

        private void DGVplanTable_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Make sure that something has been selected
            if (DGVplanTable.SelectedCells.Count == 0) return;

            // Detect what has been selected
            var selected = DGVplanTable.SelectedCells[0].Value;
            if (selected == null) return;
            currentSubject = selected as Subject;

            Console.WriteLine("Subject:             " + currentSubject.ID);
            Console.WriteLine("Prerequisites:       " + currentSubject.Prerequisites);

            // Show the user a decision according to what subject has been selected
            PickNextDecision(plan);

            // Show the user a list of times that the subject can be slotted into
            LBXtime.Items.Clear();
            foreach (int time in currentSubject.GetPossibleTimes(plan))
                LBXtime.Items.Add(time);
        }

        public void PickNextDecision(Plan plan) //TODO: suggest subjects in a more useful way
        {
            // Check if there are any decisions left
            if (plan.Decisions.Count == 0)
                return;
            // Check if the currently selected subject has a prerequisite that needs deciding
            if (currentSubject != null)
                currentDecision = plan.Decisions.Find(decision => decision.GetReasons().Contains(currentSubject));
            if (currentDecision != null)
                return;
            // Check if there are any decisions about courses
            currentDecision = plan.Decisions.Find(decision => decision.GetSubjects().Any(subject => !subject.IsSubject));
            if (currentDecision != null)
                return;
            // Pick the first one
            currentDecision = plan.Decisions.First();
        }

        void DisplayCurrentDecision()
        {
            Stopwatch timerButtons = new Stopwatch();
            timerButtons.Start();
            bool suspendedLayout = false;
            if (FLPchoose.Controls.Count > 100)
            {
                suspendedLayout = true;
                FLPchoose.SuspendLayout();
            }
            FLPchoose.Controls.Clear();
            if (currentDecision != null)
            {
                if (!suspendedLayout && currentDecision.GetOptions().Count > 100)
                {
                    suspendedLayout = true;
                    FLPchoose.SuspendLayout();
                }
                foreach (Criteria criteria in currentDecision.GetOptions())
                    AddCriteriaToFLP(criteria);
            }
            if (suspendedLayout)
                FLPchoose.ResumeLayout();
            timerButtons.Stop();
            Console.WriteLine("Adding buttons:      " + timerButtons.ElapsedMilliseconds + "ms");
        }

        readonly Dictionary<Criteria, OptionView> optionViews = new Dictionary<Criteria, OptionView>();

        void AddCriteriaToFLP(Criteria criteria)
        {
            if (!optionViews.TryGetValue(criteria, out OptionView optionView))
            {
                optionView = new OptionView(criteria);
                optionView.Click += OptionView_Click;
                optionViews.Add(criteria, optionView);
            }
            FLPchoose.Controls.Add(optionView);
        }

        private void OptionView_Click(object sender, EventArgs e)
        {
            // Get to the OptionView parent control
            while (!(sender is OptionView))
                sender = (sender as Control).Parent;
            // Find the criteria associated with that control
            Criteria selected = (sender as OptionView).Criteria;

            if (selected is Subject)
            {
                // Add the subject to the plan
                currentSubject = selected as Subject;
                Decider.AddSubject(currentSubject, plan);
                UpdatePlanTable();
            }
            else if (selected is Decision)
            {
                // Display the prerequisite as the current decision
                currentDecision = selected as Decision;
                DisplayCurrentDecision();
                // TODO: process the decision (in case it is an AND selection)
            }

            // Refresh the list of decisions that need to be made
            LBXdecisions.Items.Clear();
            foreach (Decision decision in plan.Decisions)
                LBXdecisions.Items.Add(decision);
            LBXdecisions.SelectedItem = currentDecision;
        }

        private void LBXtime_SelectedIndexChanged(object sender, EventArgs e)
        {
            int time = int.Parse(LBXtime.SelectedItem.ToString());
            Decider.MoveSubject(currentSubject, plan, time);
            UpdatePlanTable();
        }

        void UpdatePlanTable()
        {
            DGVplanTable.Rows.Clear();
            foreach (List<Subject> semester in plan.SubjectsInOrder)
                DGVplanTable.Rows.Add(semester.ToArray());
            if (currentSubject != null)
                foreach (DataGridViewRow row in DGVplanTable.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.Value is Subject && cell.Value == currentSubject)
                            DGVplanTable.CurrentCell = cell;
            DGVplanTable_CellClick(null, null);
        }
    }
}
