All of the data from comes from [the university website](http://reports.handbook.mq.edu.au/internal/index_2020.php).

The data was converted to plain-text files by copying all the text into notepad. This got rid of the formatting and attempted to keep the tables by using *tab* characters between cells. 
The titles at the top of these files were deleted.

The excel table was saved to a comma-separated vector (csv) file.

The resulting files are translated from the code in **Parser.cs**

There are some mistakes in these files:

* These degrees use the word "essential" when they should say "elective":
    * C000095
    * C000006
    * C000131
* These minors use the word "elective" when they should say "option set":
	* T000382
* These subjects' prerequisites need extra extra/less brackets to get parsed correctly:
    * AFIN3052
	* AHIS3250
	* BIOL2410
	* COGS2000
    * COMP2800
	* EDST4040
	* HLTH3100
    * MGMT3904
* These subjects' prerequisites are not written in the same pattern as the other prerequisites:
    * BIOL3450
	* CHIN1320
	* GEOS2126
    * LAWS5060
	* MECH4002
	* PICT3001
	* PICT3020
	* PSYU3332
    * SPHL2212
    * SPHL3311
* These subjects' prerequisites are just weird and need to be written better:
    * BIOL2220
    * PSYU3349
    * PSYU3351
* These subjects' prerequisites contain a typo
	* AFIN2070
	* ECHE2310
	* ECHE2320
	* ECHE3150
	* ECHE3330
	* ENGL2031
	* HLTH3050
	* MATH1015
	* PICT3011
	* PSYU3336
	* STAT1371
* These subjects' prerequisites refer to admission to invalid degrees, so those references were removed
	* ACST (anything that requires admission)
	* COMP1010
	* EDST2000
	* EDST2100
	* EDST2110
	* ENGG4104
* These subjects' corequisites are not written in the same pattern as the other corequisites:
	* GEOS2111
	* MMCC3150
	* MMCC3160
* Sometimes there are a pair of subjects with a NCCW relation that only goes one way? I'm not sure if this has a special meaning of if they're mistakes.
    * ACST2052 -> AFIN2053
    * AFIN3029 -> ACST3006
    * BIOL1620 -> BIOL1610
    * BUSA3015 -> ECON3061
    * CHIN2210 -> CHIN3010
    * COMP1150 -> MMCC1011
    * ECHE2310 -> EDST2120
    * ECHE4350 -> EDST3160
    * ECHP3300 -> ECHP3270
    * EDST2140 -> ECHE2320
    * EDST3140 -> EDTE3870
    * FOSE1005 -> MATH1000
    * FREN1320 -> FREN3020
    * FREN2220 -> FREN3020
    * ITAL1210 -> ITAL2010
    * JPNS1210 -> JPNS2010
    * JPNS1310 -> JPNS2210
    * JPNS3010 -> JPNS2210
    * LAWS2300 -> ACCG2051
    * LING3300 -> SPHL3300
    * MEDI2005 -> HLTH2301
    * MEDI2100 -> ANAT1002
    * MEDI2200 -> BIOL2110
    * MEDI2200 -> BMOL2401
    * MEDI2300 -> BIOL2230
    * MGMT2018 -> MGMT3902
    * MGMT3051 -> MGMT3903
    * MGRK1210 -> MGRK2010
    * MGRK2220 -> MGRK3020
    * MQBS3000 -> PACE3099
    * PHYS1210 -> PHYS1010
    * PHYS1210 -> PHYS1020
    * PLSH1210 -> PLSH2010
    * PLSH1220 -> PLSH2020
    * PLSH1310 -> PLSH3010
    * PLSH2210 -> PLSH3010
    * RUSS1210 -> RUSS2010
    * SLAS1210 -> SLAS2010
    * SLAS1310 -> SLAS3010
    * SLAS2210 -> SLAS3010
    * STAT2170 -> BIOL2610
    * STAT2170 -> PSYU2248
    * STAT2371 -> BIOL2610
    * STAT2371 -> PSYU2248
    * TELE3001 -> STAT3494
* These degrees need a tab in their notes:
	* C000132
	* C000195
* These degrees were missing a CP number:
	* C000195
	* C000008
* There was no degree with these awards so I created ones that could not be completed:
	* BEd(ECE)
* Most of the double-degrees that say they need a minor from Bachelor of Arts don't give a list of what they are, so there is an extra resource called arts_minor
	* Then the correct degrees (including C000004) had to have their list of Art minors removed to avoid the minors being copied twice
* These courses are inconsistent on how they explain to do ARTS2000 or a minor from Arts:
	* D000053
	* D000010
	* D000041
* This major had a specific requirement that forced it to be written differently:
	* N000113
* This course cannot be completed as a single degree, so it got removed but its relevant double-degrees are still in the file:
	* C000196
* ECON1031 has a dash in its list of NCCWs, but I'm not going to do anything about that because it's referring to the old unit codes
* These courses are impossible to satisfy due to issues with NCCWs:
	* D000002: ACST2001 & ACST2002
	* D000008: ACST1052 & STAT1250
	* D000070: STAT1170 & (STAT1250 STAT1371)
	* D000030: STAT1170 & STAT1250
	* D000029: LAWS2300 & ACCG2051
	* D000048: LAWS2300 & ACCG2051
	* D000003: EDST3140 & (EDTE4220 EDTE4240 EDTE4260 EDTE4280 EDTE4300 EDTE4320 EDTE4340 EDTE4400)
	* D000023: EDST3140 & (EDTE4220 EDTE4240 EDTE4260 EDTE4280 EDTE4300 EDTE4320 EDTE4340 EDTE4400)
	* D000019: EDST3140 & (EDTE4300 EDTE4340)
	* C000012: EDST2140 & ECHE2320
	* D000063: STAT1170 & STAT1250
	* D000088: STAT1250 & STAT1371
	* D000127: STAT1371 & N000055
	* D000004: STAT1250 & ECON2041 & (30cp ACST2001 ACST2002 AFIN2070 ECON2041 STAT2371 STAT2372)
* Courses including these degrees aren't in the program since the database is missing subjects:
	* Bachelor of Environment
	* Bachelor of Psychology
	* Bachelor of Cognitive and Brain Sciences
	* Anything with Master
* Why do some prerequisites say " at 1000 level or above"? All subjects are 1000 level or above.