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
            foreach (int i in new int[]{ 4, 4, 2, 4, 4, 2, 4, 4, 0, 2, 1})
                plan.MaxSubjects.Add(i);
            Subject degree = new Subject("COURSES", "S1D S2D S3D", 
                "240cp including " +
                "FOSE3000 and " + 
                "(20cp from FOSE1005 or FOSE1015 or FOSE1025) and " +
                "(10cp from ASTR3810 or BIOL3420 or BIOL3610 or BIOL3640 or COGS3999 or COMP3850 or ENVS3390 or GEOP3800 or GEOS3080 or HLTH3050 or MATH3919 or MOLS3002 or MOLS3003 or PHYS3810 or STAT3199) and " +
                "(COMP1000 and COMP1350 and COMP2250 and " +
                "(10cp from COMP1010 or COMP1150 or COMP1300 or COMP1750) and " +
                "(10cp from ACCG1000 or BIOL1110 or BIOL1310 or CHEM1001 or COGS1000 or ENVS1017 or ENVS1018 or GEOP1010 or GEOP1030 or GEOS1110 or GEOS1120 or MATH1010 or MATH1015 or PHSY1010) and " +
                "(30cp from COMP units at 2000 level) and " +
                "(40cp from COMP3000 or COMP3010 or COMP3100 or COMP3120 or COMP3130 or COMP3150 or COMP3151 or COMP3160 or COMP3170 or COMP3180 or COMP3210 or COMP3220 or COMP3250 or COMP3300 or COMP3310 or COMP3320 or COMP3760 or COMP3770 or COMP3860 or COMP3870 or COMP3900))"
                , "");
            Decider.AddSubject(degree, plan);
            UpdateDecisionList();
            UpdatePlanGUI();
        }

        Plan plan = new Plan();
        Prerequisite currentDecision;
        Subject currentSubject;

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
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
            if (LBXchoose.SelectedItem is Subject)
            {
                currentSubject = LBXchoose.SelectedItem as Subject;
                Decider.AddSubject(currentSubject, plan);
                Prerequisite nextDecision = plan.Decisions.Find(decision => decision.IsSubset(currentDecision));
                UpdatePlanGUI();
                if (nextDecision != null)
                {
                    currentDecision = nextDecision;
                    UpdateDecisionList();
                }
            }
            else if (LBXchoose.SelectedItem is Prerequisite)
            {
                currentDecision = LBXchoose.SelectedItem as Prerequisite;
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
