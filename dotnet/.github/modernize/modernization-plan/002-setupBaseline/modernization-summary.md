# 002-setupBaseline

- finalStatus: failed
- successCriteriaStatus: passBuild=false, passUnitTests=false
- summary: Inspected the original application at baseline commit 495ebc0263443dcd488cca69d86dace0132336e5 and identified SQL Server LocalDB with integrated authentication. The baseline agent's session prohibited writing the frozen baseline artifacts; no test-case bundle was created and the baseline build and tests were not run.
- failureReason: Baseline agent could not write output files in its session.
