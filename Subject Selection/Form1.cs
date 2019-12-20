﻿using System;
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
        Content currentContent;

        private void Form1_Load(object sender, EventArgs e)
        {
            Parser.LoadData();

            foreach (Subject subject in Parser.AllSubjects())
                CreateOptionView(subject);

            for (int year = 1; year <= 4; year++)
            {
                plan.MaxSubjects.Add(new Time { year = year, session = Session.S1 }, 4);
                plan.MaxSubjects.Add(new Time { year = year, session = Session.FY1 }, 1);
                plan.MaxSubjects.Add(new Time { year = year, session = Session.WV }, 1);
                plan.MaxSubjects.Add(new Time { year = year, session = Session.S2 }, 4);
                plan.MaxSubjects.Add(new Time { year = year, session = Session.FY2 }, 1);
                plan.MaxSubjects.Add(new Time { year = year, session = Session.S3 }, 2);
            }
            foreach (Time time in plan.MaxSubjects.Keys)
                plan.SubjectsInOrder.Add(time, new List<Subject>());

            foreach (Course course in Parser.AllCourses().Where(course => !course.HasBeenBanned(plan,0)).OrderBy(course => course.ID))
                AddOptionToFLP(course);

            UpdatePlanTable();
        }

        private void LBXdecisions_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentDecision = LBXdecisions.SelectedItem as Decision;

            if (currentDecision != null)
            {
                Console.WriteLine("Current Decision:    " + currentDecision.ToString());
                Console.WriteLine("Reasons:             " + string.Join(", ", currentDecision.GetReasons()));
            }
            else
                Console.WriteLine("Current Decision:    null");

            DisplayCurrentDecision();
        }

        private void DGVplanTable_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Make sure that something has been selected
            if (DGVplanTable.SelectedCells.Count == 0) return;

            // Detect what has been selected
            var selected = DGVplanTable.SelectedCells[0].Value;
            if (selected == null) return;
            currentContent = selected as Subject;

            Console.WriteLine("Subject:             " + currentContent.ID);
            Console.WriteLine("Prerequisites:       " + currentContent.Prerequisites);
            Console.WriteLine("Corequisites:        " + currentContent.Corequisites);

            // Show the user a list of times that the subject can be slotted into
            LBXtime.Items.Clear();
            foreach (Time time in (currentContent as Subject).GetPossibleTimes(plan.MaxSubjects))
                LBXtime.Items.Add(time);

            // Show the user a decision according to what subject has been selected

            // Check if there are any decisions left
            if (plan.Decisions.Count > 0)
            {
                // Check if the current decision is still a thing
                if (currentDecision != null)
                    currentDecision = plan.Decisions.Find(decision => currentDecision.Covers(decision));
                // Check if the currently selected subject has a prerequisite that needs deciding
                if (currentDecision == null || sender != null)
                    currentDecision = plan.Decisions.Find(decision => !decision.IsElective() && decision.GetReasons().Contains(currentContent));
                // Check if there are any decisions about courses
                if (currentDecision == null)
                    currentDecision = plan.Decisions.Find(decision => decision.Options.Any(option => option is Course));
                // Pick the first one
                if (currentDecision == null)
                    currentDecision = plan.Decisions.First();
            }
            else
                currentDecision = null;

            LBXdecisions.SelectedItem = currentDecision;
        }

        private int firstOption = 0;
        readonly int maxOptionsPerPage = 80;
        void DisplayCurrentDecision()
        {
            Stopwatch timerButtons = new Stopwatch();
            timerButtons.Start();
            FLPchoose.SuspendLayout();
            FLPchoose.Controls.Clear();
            if (currentDecision != null)
            {
                if (firstOption > currentDecision.Options.Count)
                    firstOption = 0;
                foreach (Option option in currentDecision.Options.Skip(firstOption).Take(maxOptionsPerPage))
                    AddOptionToFLP(option);
                firstOption += maxOptionsPerPage;
                if (maxOptionsPerPage < currentDecision.Options.Count)
                    AddNextButton();
            }
            FLPchoose.ResumeLayout();
            timerButtons.Stop();
            Console.WriteLine("Adding buttons:      " + timerButtons.ElapsedMilliseconds + "ms");
        }

        readonly Dictionary<Option, OptionView> optionViews = new Dictionary<Option, OptionView>();

        void AddOptionToFLP(Option option)
        {
            if (!optionViews.TryGetValue(option, out OptionView optionView))
                optionView = CreateOptionView(option);
            FLPchoose.Controls.Add(optionView);
        }

        OptionView CreateOptionView(Option option)
        {
            OptionView optionView = new OptionView(option);
            optionView.Click += OptionView_Click;
            optionViews.Add(option, optionView);
            return optionView;
        }

        private void OptionView_Click(object sender, EventArgs e)
        {
            // Get to the OptionView parent control
            while (!(sender is OptionView))
                sender = (sender as Control).Parent;
            // Find the option associated with that control
            Option selected = (sender as OptionView).Option;

            if (selected is Content)
            {
                // Add the subject to the plan
                currentContent = selected as Content;
                Decider.AddContent(currentContent, plan);
                UpdatePlanTable();
            }
            else if (selected is Decision)
            {
                // Display the prerequisite as the current decision
                currentDecision = selected as Decision;
                DisplayCurrentDecision();
                // TODO: process the decision (in case it is an AND selection)
            }

            RefreshDecisionList();
        }

        void AddNextButton()
        {
            Button nextPageButton = new Button
            {
                Text = "Next"
            };
            nextPageButton.Click += NextPageButton_Click;
            FLPchoose.Controls.Add(nextPageButton);
        }

        private void NextPageButton_Click(object sender, EventArgs e)
        {
            DisplayCurrentDecision();
        }

        void RefreshDecisionList()
        {
            LBXdecisions.Items.Clear();
            foreach (Decision decision in plan.Decisions)
                LBXdecisions.Items.Add(decision);
            LBXdecisions.SelectedItem = currentDecision;
        }

        private void LBXtime_SelectedIndexChanged(object sender, EventArgs e)
        {
            Time? time = LBXtime.SelectedItem as Time?;
            if (!time.HasValue) return;
            Decider.MoveSubject(currentContent as Subject, plan, time.Value);
            UpdatePlanTable();
            RefreshDecisionList();
        }

        private void BTNremove_Click(object sender, EventArgs e)
        {
            if (currentContent == null)
                return;
            Decider.RemoveContent(currentContent, plan);
            currentContent = null;
            UpdatePlanTable();
            RefreshDecisionList();
        }

        void UpdatePlanTable()
        {
            // Refresh Columns
            DGVplanTable.Columns.Clear();
            foreach (Time time in plan.IterateTimes())
                if (time.session == Session.S1 || time.session == Session.S2 || plan.SubjectsInOrder[time].Any())
                    DGVplanTable.Columns.Add(time.ToString(), time.ToString());
            // Refresh rows
            DGVplanTable.Rows.Clear();
            for (int i = 0; i < plan.MaxSubjects.Values.Max(); i++)
                DGVplanTable.Rows.Add();
            // Populate cells
            foreach (KeyValuePair<Time, List<Subject>> kvp in plan.SubjectsInOrder)
                for (int j = 0; j < kvp.Value.Count; j++)
                    DGVplanTable.Rows[j].Cells[kvp.Key.ToString()].Value = kvp.Value[j];
            // Select current subject
            if (currentContent != null)
                foreach (DataGridViewRow row in DGVplanTable.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.Value is Content && cell.Value == currentContent)
                            DGVplanTable.CurrentCell = cell;
            // Label course
            groupBox2.Text = string.Join(", ", plan.SelectedCourses.Select(course => course.Name));
            // Select current subject
            DGVplanTable_CellClick(null, null);
        }
    }
}
