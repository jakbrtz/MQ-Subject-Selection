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
            Parser.Load();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (int i in new int[]{ 4, 4, 2, 4, 4, 2, 4, 4, 0, 4, 4, 1})
                plan.MaxSubjects.Add(i);

            foreach (Subject course in Parser.AllCourses())
                AddCriteriaToFLP(course);
        }

        readonly Plan plan = new Plan();
        Prerequisite currentDecision;
        Subject currentSubject;

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.SelectedCells.Count == 0) return;

            var selected = dataGridView1.SelectedCells[0].Value;
            if (selected == null) return;
            currentSubject = selected as Subject;

            Console.WriteLine();
            Console.WriteLine("Subject:        " + currentSubject.ID);

            PickNextDecision(plan, currentSubject);

            Console.WriteLine("Decision:       " + currentDecision??currentDecision.ToString());
            UpdateDecisionList();
            LoadPossibleTimes(currentSubject);

            Console.WriteLine("Prerequisites:  " + currentSubject.Prerequisites);
            Console.WriteLine();
        }

        void UpdateDecisionList()
        {
            LBXdecisions.Items.Clear();
            foreach (Prerequisite decision in plan.Decisions)
                LBXdecisions.Items.Add(decision);
            if (currentDecision == null)
                PickNextDecision(plan);
            LBXdecisions.SelectedItem = currentDecision;
            LoadCurrentDecision();
        }

        public void PickNextDecision(Plan plan, Subject selectedSubject = null) //TODO: suggest subjects in a more useful way
        {
            if (plan.Decisions.Count == 0)
                return;
            if (selectedSubject != null)
                currentDecision = plan.Decisions.Find(decision => decision.GetReasons().Contains(selectedSubject));
            if (currentDecision != null)
                return;
            currentDecision = plan.Decisions.First();
        }

        void LoadCurrentDecision()
        {
            Stopwatch timer4 = new Stopwatch();
            timer4.Start();
            FLPchoose.Controls.Clear();
            if (currentDecision != null)
                foreach (Criteria criteria in currentDecision.GetOptions())
                    AddCriteriaToFLP(criteria);
            timer4.Stop();
            Console.WriteLine("Adding buttons: " + timer4.ElapsedMilliseconds + "ms");
        }

        void UpdatePlanGUI()
        {
            dataGridView1.Rows.Clear();
            foreach (List<Subject> semester in plan.SubjectsInOrder)
                dataGridView1.Rows.Add(semester.ToArray());
            if (currentSubject != null)
                foreach (DataGridViewRow row in dataGridView1.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.Value is Subject && cell.Value == currentSubject)
                            dataGridView1.CurrentCell = cell;
            DataGridView1_CellClick(null, null);
        }

        void LoadPossibleTimes(Subject selected)
        {
            LBXtime.Items.Clear();
            foreach (int time in selected.GetPossibleTimes(plan))
                LBXtime.Items.Add(time);
        }

        private void LBXdecisions_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentDecision = LBXdecisions.SelectedItem as Prerequisite;
            LoadCurrentDecision();
            if (currentDecision == null) return;

            Console.Write("Reasons:        ");
            foreach (Subject reason in currentDecision.GetReasons())
                Console.Write(reason + " ");
            Console.WriteLine();
        }

        private void LBXtime_SelectedIndexChanged(object sender, EventArgs e)
        {
            int time = int.Parse(LBXtime.SelectedItem.ToString());
            Decider.MoveSubject(currentSubject, plan, time);
            UpdatePlanGUI();
        }

        void AddCriteriaToFLP(Criteria criteria)
        {
            if (!optionViews.TryGetValue(criteria, out OptionView optionView))
            {
                optionView = new OptionView(criteria);
                optionViews.Add(criteria, optionView);
                optionView.Click += OptionView_Click;
            }
            FLPchoose.Controls.Add(optionView);
        }

        readonly Dictionary<Criteria, OptionView> optionViews = new Dictionary<Criteria, OptionView>();

        private void OptionView_Click(object sender, EventArgs e)
        {
            while (!(sender is OptionView))
                sender = (sender as Control).Parent;

            Criteria selected = (sender as OptionView).Criteria;

            if (selected is Subject)
            {
                currentSubject = selected as Subject;
                Decider.AddSubject(currentSubject, plan);
                UpdatePlanGUI();
            }
            else if (selected is Prerequisite)
            {
                currentDecision = selected as Prerequisite;
                LoadCurrentDecision();
            }
        }
    }
}
