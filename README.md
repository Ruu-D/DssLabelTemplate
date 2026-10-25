# DssLabelTemplate

A small Windows tool to **visualize and manually edit label files for a Duivestein system**.
Sister tool of **TsDssConverter** (which writes the label CSV files for the Duivestein warehouse); it uses the same look.

- **One single `.exe`**, no installer, no .NET installation needed, no Excel/Office.
- Opens the two kinds of Duivestein label files (DSS Software Interface v1.7):
  - a **label CSV + label template**: the CSV has one line per label (its place on the sheet and its data); the template
    (XML) describes the layout of one label with variables like `$LENGTH$` that are filled in from each CSV line;
  - a **label XML**: one file per sheet with the layout and the data of all its labels.
- Two file lines: the template or label XML (shown in the editor) and, for a template, the label CSV with the data.
- Left: the XML in a coloured editor with line numbers (Scintilla, the editor of Notepad++), or the data of the label CSV
  as a table. Right: a preview of one label, updated while typing, with arrows to go through all labels, where the label
  sits on the sheet, and a small map of the sheet.
- Dutch, French and English interface. Files are saved back in their own encoding and line endings.
- Installed by copying the exe to `C:\DssLabelTemplate`. That folder also holds `settings.json` and, from the first start,
  the example files in `labels\` and `CSV examples\`.

## Status

| Part | State |
|---|---|
| Window, file selection, text editor, open / save | Done (0.1.0, checked) |
| Own logo and icon; both kinds of label files; label preview with arrows | Done (0.2.0) |
| Label size fields (length × width in mm, default 100 × 100); objects outside the label are flagged | Done (0.3.0) |
| Coloured XML editor with line numbers; read-only view of the label CSV, linked to the preview | Done (0.4.0) |
| Program folder `C:\DssLabelTemplate` with examples; separate file lines for template and CSV; Tab order | Done (0.5.0, checked) |
| **First release** | **1.0.0** |

Third-party component: [Scintilla.NET](https://github.com/desjarlais/Scintilla.NET) (`Scintilla5.NET`, MIT licence).

The preview follows Duivestein's manual. The label size is not in the label files, so it is set in the window. Where the
manual is silent, the preview assumes (to be checked on a printed label): rotation counter-clockwise, text positioned by
its top-left corner. Barcodes and QR codes are drawn to show place and size; they are not meant to be scanned.

## Open questions

1. The real label size (mm) and a printed label to compare the preview with.
2. Real templates, to be copied from a working Duivestein system later.
3. Own user-guide page.

## Building

```
dotnet test DssLabelTemplate.slnx
cmd /c ".\publish.cmd"        # tests, then dist\v<version>\DssLabelTemplate.exe
```

Developer: Daan Verhoost — ROGIERS NV/SA.
