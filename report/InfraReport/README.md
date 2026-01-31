# InfraReport

This folder contains a standalone LaTeX report for YummyZoom infrastructure/deployment.

## Files
- `InfraReport.tex`: main LaTeX file (standalone)

## Build
From this directory:

```bash
cd /mnt/e/source/repos/CA/YummyZoom/report/InfraReport
pdflatex InfraReport.tex
```

(If you use a LaTeX toolchain that requires multiple passes, run `pdflatex` twice.)

## Figures
The report embeds images from:
- `../res/diagrams/c4_container_view.png`
- `../res/diagrams/azure_deployment_view.png`
