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
            SubjectReader.Load();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (int i in new int[]{ 4, 4, 1, 4, 4, 0, 4, 4, 0, 2, 1})
                plan.MaxSubjects.Add(i);
            Subject degree = new Subject("COURSES", "S1D S2D S3D", "10CP", "");
            Decider.AddSubject(degree, plan);
            UpdateDecisionList();
            UpdatePlanGUI();
        }

        Plan plan = new Plan();
        Prerequisit currentDecision;
        Subject currentSubject;

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var selected = dataGridView1.SelectedCells[0].Value;
            if (selected == null) return;
            currentSubject = selected as Subject;
            currentDecision = plan.Decisions.Find(decision => decision.GetReasons().Contains(currentSubject));
            UpdateDecisionList();
            LoadPossibleTimes(currentSubject);

            Console.WriteLine(currentSubject.Prerequisits);
        }

        void UpdateDecisionList()
        {
            LBXdecisions.Items.Clear();
            foreach (Prerequisit decision in plan.Decisions)
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
            currentDecision = LBXdecisions.SelectedItem as Prerequisit;
            LoadCurrentDecision();

            if (currentDecision == null) return;
            foreach (Subject reason in currentDecision.GetReasons())
                Console.Write(reason + " ");
            Console.WriteLine();
        }

        private void LBXchoose_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LBXchoose.SelectedItem is Subject)
            {
                currentSubject = LBXchoose.SelectedItem as Subject;
                Decider.AddSubject(currentSubject, plan);
                Prerequisit nextDecision = plan.Decisions.Find(decision => decision.IsSubset(currentDecision));
                UpdatePlanGUI();
                if (nextDecision != null)
                {
                    currentDecision = nextDecision;
                    UpdateDecisionList();
                }
            }
            else if (LBXchoose.SelectedItem is Prerequisit)
            {
                currentDecision = LBXchoose.SelectedItem as Prerequisit;
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
