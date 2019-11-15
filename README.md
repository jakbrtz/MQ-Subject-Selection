# MQ-Subject-Selection

*Work in Progress*

This application is supposed to help students create a study plan by recommending subjects and making sure all prerequisites are met.

At the moment, it gets information from a database found at [http://reports.handbook.mq.edu.au/internal/index_2020.php](http://reports.handbook.mq.edu.au/internal/index_2020.php). I had to modify some of the prerequisites so they will get parsed correctly.

To set up a course structure, type its prerequisites into the **Form1_Load** method.

I am making this application as an exercise to learn about Graph Theory and Optimization.

# Important Files:

## Plan.cs
A class that keeps track of decisions and which subjects have been selected and in what order. It contains a method called **Order()** which finds an appropriate order to put the selected subjects in.

## Subject.cs
This contains the classes **Subject** and **Prerequisite**, as well as their superclass **Criteria**. The purpose of the superclass is to allow prerequisites to be made of subjects and other prerequisites, for example *ABST1000 and (ABST2020 or ABST2060 or ABST2035)*.

## Decider.cs
Static class that looks at all the decisions that the user must make and determines whether any of them are predetermined.
