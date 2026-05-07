# Main ERP Report Inventory for LC and Project Extracts

Scope: reports related to LC, project extracts, contractor/project accounting, extract summaries, and VAT/project invoices.

## LC Reports

| Report | File | Source form/function | Type |
| --- | --- | --- | --- |
| LC list/details Arabic | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_LC.rpt` | `FrmLC_Report` | Filtered analytical/list report |
| LC list/details English | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_LC_E.rpt` | `FrmLC_Report` | Filtered analytical/list report |
| LC printable detail | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_LC_Details2.rpt` | `FrmLC.print_report` | Printable LC document/details |
| LC detail older variants | `rpt_LC_Details.rpt`, `rpt_LC_Details2.rpt` | LC forms/search | Detail report variants |
| LC alarms | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\LCAlarms.rpt` | Alarm/report forms | Alert report |

`FrmLC_Report` builds a query over `TblLC`, `BanksData`, `currency`, `LCTypes`, `TblCountriesData`, `TblCustemers`, `TblBoxesData`; filters include LC type, bank, country, vendor, and dates.

`FrmLC.print_report` filters by current `LCNO`, joins project data, and chooses Arabic/English report file based on interface language.

## Project Extract Printable and Summary Reports

| Report | File | Source form/function | Type |
| --- | --- | --- | --- |
| Project bills/extracts | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_ProjectsBills2.rpt` | `frmProjectsReports` | Extract report |
| Project bills/extracts variant | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_ProjectsBills22.rpt` | `frmProjectsReports` | Extract report variant |
| Implemented project bills | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\projectsbillImplemented.rpt` | `frmProjectsReports.ShowReportsProjectsbillImplemented` | Execution/implemented quantities |
| Implemented project bills variant | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\projectsbillImplemented2.rpt` | `frmProjectsReports` | Execution report variant |
| Project status Arabic | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReStusProjectsReport.rpt` | `frmProjectsReports.ShowReportsProject` | Project status |
| Project status English | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReStusProjectsReportE.rpt` | `frmProjectsReports.ShowReportsProject` | Project status |
| Project master Arabic | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_Projects.rpt` | Project report forms | Project master |
| Project master English | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\rpt_Projects_E.rpt` | Project report forms | Project master |
| Project by customer | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ProjectsByCustomers.rpt` | Project report forms | Analytical |

The extract report query in `frmProjectsReports` reads `project_billl`, `projects`, `TblCustemers`, and `TblBranchesData`, with calculated project totals such as project bill values, advance payment totals, and performance/retention values.

## Project Payments and Contractor Accounting Reports

| Report | File | Source form/function | Type |
| --- | --- | --- | --- |
| Project payments | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReportPaymentProjects.rpt` | `frmProjectsReports.ShowReportsPaymentsProj` | Payments/receipts |
| Project employee transactions Arabic | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReEmpTransProjectsReport.rpt` | `frmProjectsReports.ShowReportsEmpTransProject` | Project labor/operation |
| Project employee transactions English | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReEmpTransProjectsReportE.rpt` | `frmProjectsReports.ShowReportsEmpTransProject` | Project labor/operation |
| Employee in project Arabic | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReEmpInProject.rpt` | Project report forms | Project labor |
| Employee in project English | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\ReEmpInProjectE.rpt` | Project report forms | Project labor |
| Project process 1/2 | `RepProcessofProject1.rpt`, `RepProcessofProject2.rpt` and English variants | Project report forms | Progress/process |
| Project equipment/material/expense process | `RepProcesspanofprojectEqu.rpt`, `RepProcesspanofprojectMatr.rpt`, `RepProcesspanofprojectExpen.rpt` | Project report forms | Cost/process analytics |
| Project accounting GL | `F:\Source Code\SatriahMain\Reports\GL _with_projects*.rpt` | GL/report forms | Ledger with project dimension |

## VAT / E-Invoice Related

| Report | File | Type |
| --- | --- | --- |
| Project VAT | `F:\Source Code\SatriahMain\Reports\REPORTS NEW\VAT\VatProjects.rpt` | VAT/project invoice report |
| VAT customer bills | `VATBillCustomer.rpt`, `VATBillCustomerE.rpt` | VAT invoices |
| General VAT reports | `RepVAT*.rpt`, `RepDocVAT*.rpt`, `RepItemsVAT*.rpt` | VAT analytics |

## Additional Project Report Locations

Potentially related construction/project reports exist under:

- `F:\Source Code\SatriahMain\Reports\construction\DetailedProject.rpt`
- `F:\Source Code\SatriahMain\Reports\construction\projectsrevenue*.rpt`
- `F:\Source Code\SatriahMain\Reports\emp\EmpProjects.rpt`
- `F:\Source Code\SatriahMain\Reports\rpt_Projects2.rpt`

These should be reviewed later when the Project Extracts reporting backlog is prioritized.

## Migration Guidance

- Do not copy Crystal reports into `Areas\MainErp` blindly.
- First create report inventory endpoints and parameter contracts.
- Classify each report as printable document, operational list, VAT/e-invoice report, or analytical report.
- Prefer rebuilding read models and report queries after schema mapping; only preserve Crystal layout when it is legally/business required.
