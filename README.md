# MQ-Subject-Selection

*Work in Progress*

This application is supposed to help students create a study plan by suggesting subjects and making sure all Prerequisits are met.
At the moment, it gets subject data from a local .txt file, and it only knows about these Macquarie University subjects:
 * COMP
 * DMTH
 * ISYS
 * MAS
 * MATH
 * STAT
To set up a major, type its prerequisits into the **Form1_Load** method.
I am making this application as an excersize to learn about Graph Theory and Optimization.

# These are the most important files:

## Plan.cs
A class that keeps track of decisions and which subjects have been selected and in what order. It contains a method called **Order()** which finds an appropriate order to put the selected subjects in.

## Subject.cs
This contains the classes **Subject** and **Prerequisit**, as well as their superclass **Criteria**. The purpose of the superclass is to allow prerequisits to be made of subjects and other prerequisits, for example *MATH235 AND (MATH232 OR MATH236)*.

## Decider.cs
Static class that looks at all the decisions that the user must make and determines whether any of them are predetermined.
