# FHIR Post Processor sample code
This sample code is **NOT meant for production**, it is purely for testing purposes.

# Scenario
When a new DiagnosticReport (DR) is created we wish to trigger an external algortihm that does not handle FHIR bundles.
We need to fetch the DR and retreive all associated Observations. Once this is done we want to map the different Observations for specific values to the format defined by the Algorithm.

# Diagram
![Solution Diagram](/Samples/Images/SolutionArhitecture.drawio.png)

# Notes on markers
Please note that the Loinc codes used ar **NOT OFFICIAL**, this is just a **sample** to showcase how the mapping could be done.

---
| name | unit | range | Loinc used |
|------|------|-------|------------|
| WCC | 10^9/l | 5.0-16.0 | 000-0 |
| BMI | # | 15-30 | X000-1 |
| PLT | 10^9/l | 150-450 | 777-3 |
| MCV | fL | 0.0-100.0 | 787-2 |
| MPV | fL | 0.0-11.0 | X000-2 |
| BASA | 10^9/l | 0.0-0.1 | 704-7 |
| Age | years | 0-120 |  | 
| LYMA | 10^9/l | 0.0-5.0 | X000-3 |
| SBP | mmHg | 80-200 | X000-4 |
| DBP | mmHg | 40-100 | X000-5 |
| Proteinuria | category | 0-5 | X000-6 | 
---

# Power app
The sample Power App has three screens:
- home
- new patient
- Observations

The home screen allows you to search for a existing patient and add observations for this patient. You can also navigate to the create a new patient screen.
![Patient Search Screen](/PowerPlatform/SimplePatientSearch.png)

On the create patient screen you can submit a minimal set of parameters for the specific patient. This is done to mimimize the amount of PII we store.
![Patient creation Screen](/PowerPlatform/addNewPatient.png)

The final screen is where we can submit the Observations and kickstart the analysis process if using the HTTP triggered Function.
![Patient creation Screen](/PowerPlatform/SubmitAndAnalyseParameters.png)