The examples of "DSS Software Interface v1.7" (Duivestein, 2024-11-08), typed over from the PDF. Used by the tests.

label-example.csv    "Label-csv with extra data columns" (p. 26), tab-separated, 3 labels.
label-template.xml   "Label template example with multiple print objects" (p. 26), the template for label-example.csv.
                     ONE change: the PDF has a typo on line 2, Width="$WIDTH$"" (two quotes at the end), which is not
                     valid xml; here it is Width="$WIDTH$".
label-example.xml    "Label-xml example" (p. 27): one sheet with 2 labels, layout and data in one file.
                     The barcode values are printed over two lines in the PDF; the text layer of the PDF says
                     13579-houtproplus24680 and 13579-houtproplus53210 (no dash before the last digits).

The pictures they refer to (C:\DSS\DUIVESTEIN.png, C:\DSS\DUIVESTEIN-LOGO.bmp) are on Duivestein's label PC, not here:
the preview draws a placeholder with the file name.

HUPPY-PINCKAERS-60x60.xml, HUPPY-PINCKAERS-100x100.xml
                     Daan's own label templates (60 x 60 mm and 100 x 100 mm labels), not from the PDF.

Every *.xml and *.csv in this folder is also built into DssLabelTemplate.exe and written to C:\DssLabelTemplate\labels and
C:\DssLabelTemplate\CSV examples at the first start (only when that folder does not exist yet).
