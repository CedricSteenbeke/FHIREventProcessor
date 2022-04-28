# FHIR Post Processor sample code
This sample code is NOT meant for production, it is purely for testing purposes.

# Scenario
When a new DiagnosticReport (DR) is created we wish to trigger an external algortihm that does not handle FHIR bundles.
We need to fetch the DR and retreive all associated Observations. Once this is done we want to map the different Observations for specific values to the format defined by the Algorithm.

# notes on markers
name - unit - range
*WCC - 10^9/l - 5.0-16.0
BMI - # - 15-30
*PLT - 10^9/l - 150-450
MCV - fL - 0.0-100.0
MPV - fL - 0.0-11.0
*BASA - 10^9/l - 0.0-0.1
Age - years - 0-120
LYMA - 10^9/l - 0.0-5.0
SBP - mmHg - 80-200
DBP - mmHg - 40-100
Proteinuria - category - 0-5