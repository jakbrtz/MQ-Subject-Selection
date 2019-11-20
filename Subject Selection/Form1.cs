using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            Decider.AddSubject(Parser.GetCourse("C000011"), plan);

            UpdateDecisionList();
            UpdatePlanGUI();
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

            currentDecision = plan.Decisions.Find(decision => decision.GetReasons().Contains(currentSubject));

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
                currentDecision = plan.PickNextDecision();
            LBXdecisions.SelectedItem = currentDecision;
            LoadCurrentDecision();
        }

        void LoadCurrentDecision()
        {
            LBXchoose.Items.Clear();
            if (currentDecision != null)
                foreach (Criteria criteria in currentDecision.GetOptions())
                    LBXchoose.Items.Add(criteria);
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

        private void LBXchoose_SelectedIndexChanged(object sender, EventArgs e)
        {
            Criteria selected = LBXchoose.SelectedItem as Criteria;

            if (selected is Subject)
            {
                currentSubject = selected as Subject;
                Decider.AddSubject(currentSubject, plan);
                Prerequisite nextDecision = plan.Decisions.Find(decision => decision.IsSubset(currentDecision));
                UpdatePlanGUI();
                if (nextDecision != null)
                {
                    currentDecision = nextDecision;
                    UpdateDecisionList();
                }
            }
            else if (selected is Prerequisite)
            {
                currentDecision = selected as Prerequisite;
                LoadCurrentDecision();
            }
        }

        private void LBXtime_SelectedIndexChanged(object sender, EventArgs e)
        {
            int time = int.Parse(LBXtime.SelectedItem.ToString());
            Decider.MoveSubject(currentSubject, plan, time);
            UpdatePlanGUI();
        }
    }
}
