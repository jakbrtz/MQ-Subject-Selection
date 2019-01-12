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
            Subject degree = new Subject("COURSES", "0", "S1D S2D S3D",
                "(STAT150 OR STAT170 OR STAT171) (STAT270 OR STAT271) (STAT272 OR STAT273) (STAT278 OR STAT279) STAT399 STAT375 (6CP STAT302 STAT306 STAT328 STAT373 STAT378 STAT395) " +
                "COMP115 COMP125 ISYS114 (STAT170 OR STAT171) COMP225 COMP257 ISYS224 (STAT270 OR STAT271) COMP336 ISYS358 STAT302 (3CP COMP332 COMP348 STAT375)"
                , "");
            Decider.AddSubject(degree, plan);
            UpdateScreen();
        }

        Plan plan = new Plan();
        Prerequisit currentDecision;
        Subject currentSubject;

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var selected = dataGridView1.SelectedCells[0].Value;
            if (selected == null) return;
            currentSubject = selected as Subject;
            Prerequisit decision = currentSubject.Prerequisits.GetRemainingDecision(plan); //TODO: prune
            currentDecision = decision;
            LoadCurrentDecision();
            LoadPossibleTimes(currentSubject);
        }

        void UpdateScreen()
        {
            UpdateDecisionList();
            LoadCurrentDecision();
            UpdatePlanGUI();
        }

        void UpdateDecisionList()
        {
            LBXdecisions.Items.Clear();
            foreach (Prerequisit decision in plan.Decisions)
                LBXdecisions.Items.Add(decision);
        }

        void LoadCurrentDecision()
        {
            LBXchoose.Items.Clear();
            if (currentDecision != null)
                foreach (Criteria criteria in currentDecision.GetRemainingOptions(plan))
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
        }

        void LoadPossibleTimes(Subject selected)
        {
            LBXtime.Items.Clear();
            foreach (int time in selected.GetPossibleTimes(4))
                LBXtime.Items.Add(time);
        }

        private void LBXdecisions_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentDecision = LBXdecisions.SelectedItem as Prerequisit;
            LoadCurrentDecision();
        }

        private void LBXchoose_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<Subject> reasons = currentDecision.GetReasons();
            if (LBXchoose.SelectedItem is Subject)
            {
                Decider.AddSubject(LBXchoose.SelectedItem as Subject, plan);

                //TODO: recognise the decision is the same one, even though options have changed
                //      this can be done by having each prerequisit remember why it is a prerequisit
                //if (!plan.Decisions.Contains(currentDecision))
                currentDecision = null;
                UpdateScreen();
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
            plan.ForceTime(currentSubject, time);
            UpdatePlanGUI();
            DataGridView1_CellClick(null, null);
        }
    }
}
