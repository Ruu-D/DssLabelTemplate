# Changelog

## 1.1.4 rebuild — 2026-10-27 (same version number, Daan)

- **Delivery:** `dist\v1.1.4\DssLabelTemplate.exe`, 53,212,626 bytes, file version 1.1.4.0, SHA-256
  `37f8893b8129858dfecb1f061b110513aa6eac759af7ff504506bdecdb309142`. Built by `publish.cmd` after all 211 tests passed;
  the exe itself was not started. Rebuilt under the SAME number on Daan's request, so two different exes say 1.1.4: tell
  them apart by this checksum. The first 1.1.4 build (`58e7f644...`) was copied to
  `dist\_previous-builds\2026-10-27\v1.1.4\` first.
- **Tested by Daan on the Zebra ZD220 (2026-10-27): printing works perfectly with dithering OFF** in the printer driver
  (the first real test on the reprint printer; tested with the first 1.1.4 build).
- **`HideWindowsPrintDialog`** in `settings.json` ONLY (default false): the print button (and Ctrl+P) prints the shown
  label at once, without the Windows print dialog, on `PrinterName` when it is installed, otherwise Windows' default
  printer, with the driver's own settings. The page check and the square-label question still come when needed. The
  print button's tooltip then reads "Print this label (Ctrl+P)" (three languages). Explained in the packed README.
- 211 tests (was 210): the setting off by default, saved and loaded; the print button's text with and without it.

## 1.1.4 — 2026-10-26 (Daan)

- **Delivery:** `dist\v1.1.4\DssLabelTemplate.exe`, 53,212,137 bytes, file version 1.1.4.0, SHA-256
  `58e7f644e4380c6adeb5b272ce5904b1138d6a553867e5a992174589de8d9963`. Built by `publish.cmd` after all 210 tests passed;
  the exe itself was not started.

- **The turned preview and the turned print follow the operator's view:** `ShowLabelRotation` and `PrintLabelWithRotation`
  now turn the label by its ROTATION **plus `SheetRotationInViewer`** (`LabelRenderer.QuarterTurnsAsTheOperatorSees`).
  Daan's logic: ROTATION is how the label sits on the sheet; the operator sees the sheet turned by SheetRotationInViewer
  (standing at the unloading table, facing the screen and the reprint printer) and must stick the printed label on without
  turning it. Example: ROTATION 0 with SheetRotationInViewer -90 prints a quarter turn clockwise. In 1.1.3 the viewer turn
  was not added. Packed README updated (an existing README in the program folder is still never replaced).
- **`LabelDirection`** in `settings.json` ONLY: `"CCW"` (default, the earlier assumption) or `"CW"`: which way a positive
  ROTATION turns the label (for the turned preview and print). `SheetRotationInViewer` stays counter-clockwise. ROTATION 0
  = the label upright on the sheet, its bottom towards the Y = 0 edge (confirmed by Daan with his sample and
  SheetRotationInViewer -90). Explained in the packed README.
- 210 tests (was 194): printed square labels for ROTATION + viewer combinations (0 / -90, 0 / 270, 90 / -90, 180 / 90,
  1 / 90), the same with LabelDirection CW (90, 270, 180, 0 / -90, 90 / -90), upright without the setting even with a
  turned sheet map; LabelDirection loaded (CW / cw / CCW / unknown / missing) and saved, its helper not written. Rendered
  at a real 150% with SheetRotationInViewer -90.

## 1.1.3 — 2026-10-26 (Daan)

- **Delivery:** `dist\v1.1.3\DssLabelTemplate.exe`, 53,211,033 bytes, file version 1.1.3.0, SHA-256
  `5b5cf2641134720e02d9edabdefe44de642ebff1b59337d6647c1959ee5e1fb1`. Built by `publish.cmd` after all 194 tests passed;
  the exe itself was not started.

- **Preview height 60 / 40:** with a sheet map, the map (with the gap above and the margin below it) gets 40 % of the
  preview's height, the label with its info and problem lines 60 % (was about 70 / 30; `SheetMapShare` 0.40,
  `LabelPreview.MapHeight`).
- **`ShowLabelRotation`** in `settings.json` ONLY (default false): the preview shows the label turned by its ROTATION from
  the csv, counter-clockwise, as it will sit on the sheet. Only the preview.
- **`PrintLabelWithRotation`** in `settings.json` ONLY (default false): the print is turned by the ROTATION,
  counter-clockwise, around the middle of the label. Only for a square label; for another size the window asks whether
  to print upright (three languages, `Strings.RotationNeedsSquareLabelQuestion`).
- Both explained in the packed README (`program-folder\README.md`). An existing README in the program folder is never
  replaced, so a PC that already has one does not get these two sections.
- Rendered at a real 150% (60 x 60 template, ROTATION 0 / 90 / 180 / 270). 194 tests (was 183): the 40 % map, the two
  settings (off by default, saved and loaded), a printed square label turned to the right quarter for 0 / 90 / 180 / 270 /
  360 / 1, upright without the setting, the square check.

## 1.1.2 rebuild — 2026-10-26 (same version number, Daan)

- **Delivery:** `dist\v1.1.2\DssLabelTemplate.exe`, 53,210,028 bytes, file version 1.1.2.0, SHA-256
  `41d6038814d5ef9b7074f2b38441c93ae33659ee264a17f089f1e424df747a8a`. Built by `publish.cmd` after all 183 tests passed;
  the exe itself was not started. Rebuilt under the SAME number on Daan's request, so different exes say 1.1.2: tell
  them apart by this checksum. The first 1.1.2 build (`4ea7e7fb...`) was copied to `dist\_previous-builds\2026-10-26\v1.1.2\`
  first (publish.cmd empties `dist\v1.1.2`). An intermediate rebuild of the same evening (`7c3f1718...`, without the
  README) was replaced.
- **README.md in the program folder (Daan):** his `C:\DssLabelTemplate\README.MD` (the hidden settings of settings.json:
  `PrinterName`, `SheetRotationInViewer`, `SheetReferenceCorner`) is packed into the exe (`program-folder\README.md` in
  the repository) and written next to settings.json at the start when no README is there; an existing one (any case of
  the name) is never touched. Packed as a CLEANED copy: the original was saved with escaped Markdown (`\#`, `\*\*`, `\-`)
  and an empty line after every line; the text itself is unchanged.

- **Zero-point dot on the sheet map:** a round navy dot with a thin white ring on the corner the label X / Y are measured
  from, the same look as TsDssConverter's corner schematic. Drawn last (a label in that corner does not hide it); it turns
  with `SheetRotationInViewer`.
- **The reference corner is a setting:** `SheetReferenceCorner` in `settings.json` ONLY (not in the window), DSSClient's
  own names `Linksonder` (default) / `Linksboven` / `Rechtsonder` / `Rechtsboven`; the English names (`TopRight`,
  `Top-right`, …, any case) are understood too and written back with DSSClient's name; anything else becomes
  `Linksonder`. It must be the same corner as TsDssConverter's zero-point drop-down (= DSSClient's "Oriëntatie"):
  TsDssConverter converts TopSolid's bottom-left positions to that corner, so before this the sheet map showed the labels
  mirrored for any corner other than Linksonder. The map converts every position back (`LabelPreview.ToBottomLeft`) and
  puts the dot on that corner. Rendered at a real 150% with a csv converted to Rechtsboven: the same map as Linksonder
  with the original csv, only the dot moved.
- 183 tests (was 165): every corner gives the same spot for the sample part (X 399 / Y 540 from the bottom left), the dot's
  corner, the setting saved / loaded / understood / repaired; the README written byte for byte, an existing one untouched.

## 1.1.2 — 2026-10-26 (after Daan's test of 1.1.1)

- **The sheet map can be turned:** a new setting `SheetRotationInViewer` in `settings.json` ONLY (not in the window,
  Daan's request): 0, 90, 180, 270 or -90, -180, -270 degrees, counter-clockwise (a negative value = clockwise). The
  label PC sits on a console that can be turned, so the operator may see the sheet on the machine from another side than
  along its length. Only the sheet map under the label turns (the sheet and all its labels); the label itself stays upright.
  Any other value (45, 360, …) becomes 0. Not in the file yet = 0, and the program writes it the next time it saves its
  settings (e.g. when it is closed).
- **More height for the editor / csv table and the preview** (Daan): the description of the two kinds of label files
  is no longer a text above the path boxes but the hover text of a "?" icon (`media\howto.png`) before each path
  caption; tapping or clicking the icon shows it too (touch screen). The text is cut into lines of at most 80
  characters and stays up 32 seconds. The divider line between the status line and the two columns is gone.
- **Big files open about 3 x faster** (Daan: the window froze for almost 6 seconds). The cause, measured: the csv table
  sized its columns by measuring every cell of every line when it was shown (about 1 s per 1000 lines). Now every column
  is sized once, from its title and its longest text. 1000 lines: 1.3 → 0.4 s; 5000 lines (1.4 MB): about 6 → 1.7 s.
- **A gray "loading" layer** while files of 200 KB or more are opened: see-through gray over the whole window (also the
  title bar) with a white box "Bezig met laden…" / "Chargement…" / "Loading…" and the wait cursor. Clicks and keys
  during that time are caught by the layer and ignored, instead of being carried out afterwards. Small files: no layer.
- **The buttons below the preview:** the counter `2 / 3` against the left edge of the preview, the arrows in the middle,
  the print button against its right edge; the counter in bold. The three buttons have the same width, wider than before: they grow with
  the preview column, from 72 up to 120 px at 100% (100 in the standard window); the height is unchanged.
- 165 tests (sheet-map geometry, the setting, the "?" icons and their text, big files, the gray layer, the button row).
  Rendered at 0°, 90° and -90° with Daan's sample (sheet 2850 x 2100), and the whole window at a real 150%.
- **Delivery:** `dist\v1.1.2\DssLabelTemplate.exe`, 53,208,020 bytes, file version 1.1.2.0, SHA-256
  `4ea7e7fb80a0628ea75e45dc787dd407c95ed4db93f6eb4dda37a667fc58f86c`. Built by `publish.cmd` after all 165 tests passed;
  it contains `media\Print.png` as it was in the working copy (changed by Daan). The exe itself was not started.

## 1.1.1 — 2026-10-25 (after Daan's test of 1.1.0)

- **The window's place is remembered:** position, size (stored at 100%, so it fits a monitor with another scaling) and
  maximised, in `settings.json` (`WindowLeft`, `WindowTop`, `WindowWidth`, `WindowHeight`, `WindowMaximized`). A place
  that is on no screen any more gives the standard window in the middle of the screen.
- **The column share is no longer lost:** in 1.1.0 a share chosen in a maximised window was squeezed to fit the standard
  window at the next start and that squeezed value was then saved (opening the last files saves the settings). Now only
  dragging changes the remembered share; a window that is too small uses a squeezed share without saving it, and the
  chosen share comes back as soon as there is room.
- **A label CSV is shown as its table first** (opening a pair or choosing a csv); closing the csv shows the text again.
- **The bold title "Labelbestanden" above the description is gone** (the description stays).
- 143 tests. The window place checked at a real 150%.
- **Delivery:** `dist\v1.1.1\DssLabelTemplate.exe`, 53,202,577 bytes, file version 1.1.1.0, SHA-256
  `840500501d6c80e9f98f27a4006a903742b52589634e2e137e0fb1372ba62905`. Started once with `--data` on an empty folder and
  closed normally: it wrote the examples and saved its window place in settings.json.

## 1.1.0 — 2026-10-25 (step 6: printing; waiting for Daan's check)

- **Print button** (Daan: a reprint station for damaged labels, a Zebra ZD220 via USB, direct thermal, peel-off, next to
  the unloading belt): right of the ▶ arrow, the same size as the arrows, with the printer icon `media\Print.png`
  (Daan's picture, built into the exe, `AppIcons.Print`). Also Ctrl+P. It opens the Windows print dialog with the printer
  of the last time (`settings.json` → `PrinterName`) and "current page" = the label in the preview; "pages" are the label
  numbers, so all labels or a range can be printed too. One label per page, 1 to 1 in mm from the top left corner of the
  paper, drawn by the same `LabelRenderer` as the preview (`LabelPrinter.cs`). The page size comes from the printer driver
  (set up for the label on the reprint PC); when it differs more than 1 mm from the label size of the window, the
  program asks before printing. The button is shown as soon as there is a label (also just one).
- **Real QR codes** with **QRCoder** (NuGet, MIT, approved by Daan; pinned to 1.6.0 because 1.8.1 was only 12 days old):
  in the preview and on paper (`QrCode.cs`, error correction M, no white border: the template places the code).
- **Printer dots:** on the printer, barcode bars and QR squares are a whole number of printer dots and start on a dot
  (a 203 dpi printer has 7.99 dots per mm), so the codes stay scannable. Barcodes are still always Code 128 B.
- **Banner:** "Visualize, edit and print label files for DUIVESTEIN" (was "Visualize and manually edit label files for a
  Duivestein system"); `media\header.png` written again.
- **The vertical line between the editor and the preview can be dragged** (Daan) to make one column wider; double-click =
  two equal halves again. Each column keeps at least 330 px (at 100%). The share is remembered (`EditorColumnShare` in
  `settings.json`, 0.1 – 0.9, default 0.5). The line is now a 41 px wide handle (`ColumnSplitter`) with the 1 px line in the
  middle, thicker and darker under the mouse.
- **After Daan's first test (same day):** the counter, the arrows and the print button moved BELOW the preview, centred,
  and are twice as big (72 x 56 at 100%, arrow signs and icon twice as big too); the editor and the vertical line run
  down to the bottom of that row. The label size moved from below the path boxes to their old place, right of the
  preview title; the status line now has the whole width.
- **After Daan's second test (same evening, touch screen in mind):** the `Sluiten` / Close button is gone (the window's X
  still asks about unsaved changes). The blue Save button moved above the editor, left of `Template (xml)`, renamed
  `Template opslaan` / `Enregistrer le modèle` / `Save template`, and is only shown while there are unsaved changes. The
  footer row is gone: the preview's bottom is in line with the editor's again, and the big counter / arrows / print row
  moved down into the freed place (centred below the preview, the ROGIERS logo at the left). The editor column now keeps
  at least the width of its three title buttons when the line is dragged.
- **After Daan's test with a real TsDssConverter label CSV:** X and Y below the label showed "?". Cause: the
  HUPPY-PINCKAERS templates used the column names of Duivestein's example (`$XPOS$`, `$YPOS$`, `$LENGTH$`, `$WIDTH$`),
  TsDssConverter's csv calls them `X`, `Y`, `SHEETLENGTH`, `SHEETWIDTH`. The two sample templates now use those names (and
  `$PANELLENGTH$` / `$PANELWIDTH$` for the dimensions, as in Daan's own copy); `DAAN_ROGIERS-P2026.09_001.csv`
  (TsDssConverter's expected output) is added as their example data. The preview now names, in red below the label,
  every variable of the template that is not a column of the csv (`LabelItem.MissingVariables`), instead of a silent "?".
  Daan's own copy in `C:\DssLabelTemplate\labels` is not touched (he edits it).
- **CSV table for a touch screen:** font 4 points bigger (13 pt), rows 14 px (at 100%) higher than their text.
- 138 tests. Rendered at a real 150% in Dutch and French (the only scaling of Daan's monitors at that moment).
- **Delivery (customer test version):** `dist\v1.1.0\DssLabelTemplate.exe`, 53,201,994 bytes, file version 1.1.0.0, SHA-256
  `ce52bf29e37890e8d491044b0e59488b5506f3129229932d01d995c79660760e`. Started once with `--data` on an empty folder: it
  kept running and wrote settings.json, `labels\` (4 xml) and `CSV examples\` (2 csv). Not yet tried on the Zebra ZD220.

## 1.0.0 — 2026-10-25 (first release)

- Version 0.5.0, checked by Daan, with a grey editor background `#E8E8E8` (`Theme.XmlBackground`; `#A0A0A0` was too dark to read the tags, a test checks the contrast now).
- The user-guide icon opens this tool's own GitHub page, https://github.com/Ruu-D/DssLabelTemplate/ (was TsDssConverter's).
- README: the banner of the window at the top (`media\header.png`, written by `--write-icon media` from the same code as
  the window) and Daan's screenshot `media\Main-UI.png`.
- Pushed to GitHub by Daan.
- Delivery (rebuilt with the GitHub link, on Daan's request under the same number): `dist\v1.0.0\DssLabelTemplate.exe`,
  53,119,368 bytes, file version 1.0.0.0, SHA-256 `766202f7c658e9baac88984d0dd9ac8ce22da91331331542e1a04d18bbd1dc4b`.
  The build before it (`5df2ec24…`) still opened TsDssConverter's page. (The very first publish under 1.0.0 still had
  version 0.5.0.0 inside: the PC clock had gone back, so the old generated version file in `obj\` looked newer than the
  csproj. Clean `obj\` + `bin\` builds fixed it.)

## 0.5.0 — 2026-10-25 (step 5)

- The source code moved to `C:\Dev\DssLabelTemplate` (Daan).
- **Program folder `C:\DssLabelTemplate`** (Daan): `settings.json` is there (was `%APPDATA%\DssLabelTemplate`) and is
  written at the first start, with two example folders: `labels\` (the xml files of `samples\pdf-manual`) and
  `CSV examples\` (its csv). The examples are built into the exe and written only when their folder does not exist yet
  (`AppDataFolder.cs`). `--data <folder>` still points everything elsewhere.
- **Tab order** (Daan): Tab only goes through template path → csv path → label length → label width; entering a field
  (Tab or click) selects all its text, so typing replaces it.
- **Two separate file lines** (Daan: no more second dialog asking for the other file): `Template of label-xml:` and
  `Label-csv:`, each with its own Browse. The csv can be chosen before or after the template; another template keeps the
  csv; a label-xml closes it and greys out the csv line; an empty csv box + Enter = the template without data. The Browse
  dialogs start in the example folders. Section title `Labelbestanden`, description and status texts adapted.
- **Editor background grey** (Daan): `Theme.XmlBackground` (first `#F3F3F3`), also inside the rounded frame.
- 117 tests.
- First delivery build (before the grey background): `dist\v0.5.0\DssLabelTemplate.exe`, 53,119,197 bytes, SHA-256
  `4fd129236462fbed57f404a2eb44cba69f06e84790bc02a57292926f54413da6`. Started once with `--data` on an empty folder: it
  wrote settings.json, `labels\` (4 xml) and `CSV examples\` (1 csv).

## 0.4.0 — 2026-10-25 (step 4)

- **XML editor with colours** (Daan): the plain text box is replaced by Scintilla (NuGet `Scintilla5.NET` 7.1.1, MIT, the
  editor of Notepad++), approved by Daan. Tags, attribute names, values and comments in palette colours, line numbers,
  a brand-blue box around every `$VARIABLE$`; opening a file cannot be undone with Ctrl+Z (`XmlEditor.cs`).
- The exe stays ONE file: the two native Scintilla DLLs are built into it and written once to
  `%LOCALAPPDATA%\DssLabelTemplate\scintilla\<hash>\` at start (`ScintillaFiles.cs`); the loose copies of the package are kept
  out of the publish folder. Checked by starting the published exe.
- **View of the loaded csv** (Daan): with a label-csv, two buttons above the left column, `Template (xml)` / `Label-csv`,
  switch between the editor and a read-only table of the csv (`#` + its columns). The table and the arrows follow each
  other: the arrows select the label's row, clicking a row shows that label.
- The column titles next to buttons are always one line ("…" when too long): the French csv title wrapped at first.
- `RoundedFrame` (shared by `TextBoxFrame` and the new `ContentFrame`); `Theme.Xml*` colours and `StyleToggleButton`.
- 101 tests.
- Delivery: `dist\v0.4.0\DssLabelTemplate.exe`, 53,115,797 bytes, SHA-256
  `d086c1fd1a8fb151b476b81eb81388e0aee6dd702415eeebd81cbb3cb09f5ac8`.

## 0.3.0 — 2026-10-24 (step 3)

- **Label size fields** (Daan): `Labelformaat (mm): [100] × [100]` on the status line, against the right edge in line with
  Browse. Length = horizontal (X), width = vertical (Y), 1 – 1000 mm, dot or comma; saved in settings.json
  (`LabelLength`, `LabelWidth`, default 100 × 100; an impossible value in the file is back to 100). An invalid entry turns
  red and the last valid size stays.
- The preview draws the paper at that size (before: 100 × 100, growing with the objects). Objects that stick out are still
  drawn on the pale blue around it, and a red line says how many objects are (partly) outside the label.
- 85 tests.
- Delivery: `dist\v0.3.0\DssLabelTemplate.exe`, 51,795,676 bytes, SHA-256
  `4483732c8db1a30a0528775c770b61f0d7fd3a3c4e2b36e5ca8614e85a34f4ff`.

## 0.2.0 — 2026-10-24 (step 2)

Daan's 6 requests after checking 0.1.0, based on Duivestein's "DSS Software Interface v1.7" (2024-11-08):
- **Own logo** (Claude's design): a white label with a QR-code-like pattern and a peeled corner, drawn in code
  (`AppLogo.cs`). It replaces TsDssConverter's icon in the banner and in `media\app.ico` (16 – 256 px, written with
  `--write-icon media`; also `icon-256.png`, `icon-512.png`). `header@2x.png` and `app-icon.svg` of TsDssConverter removed.
- **Banner subtitle**: "Visualize and manually edit label files for a Duivestein system".
- **The examples of the PDF as test data** in `samples/pdf-manual/`: label-csv + template (p. 26, the template's typo
  `Width="$WIDTH$""` corrected) and the label-xml (p. 27).
- **Only label files can be opened**: Browse offers `*.csv` (label-csv; then the template is asked) and `*.xml` (label-xml,
  or a template; then its label-csv is asked, Cancel = without data). The same for a typed path and the command line.
  The last xml + csv are opened again at start (`LastCsvFile` in settings.json).
- **Description of the two kinds** below "Labeltemplate (bestand)", and a status line below the path box.
- **Arrows** ◀ ▶ with "2 / 3" next to "Voorbeeld van het label", against the right edge of the preview, when there is more
  than one label (a label-xml of a sheet, or a template with a csv of several lines — the merged labels have several IDs too).
- The preview draws the labels: Text, Line, Rectangle, Image (placeholder when the picture is not on this PC), Barcode
  (drawn as Code 128 B), QRCode (a QR-looking pattern), Arrow (8 directions, 8 fills), with an info line (ID, X, Y,
  rotation, sheet size), a red line for objects that cannot be drawn (with their line number) and a small sheet map.
  Assumptions where the manual is silent: see CLAUDE.md §7 (label 100 x 100 mm, counter-clockwise rotation, …).
- Disabled look for `RoundedButton`. 74 tests.
- Delivery: `dist\v0.2.0\DssLabelTemplate.exe`, 51,793,947 bytes, SHA-256
  `c1c8a937165f1b7b8539196135ac387862cf35278f0360dd306ce2b669c4e5b5`.

## 0.1.0 — 2026-10-24 (step 1)

- New project, started from the look of TsDssConverter 1.1.0's settings window (banner, top row with language, credit,
  version and user-guide icon, footer with logo, `Theme`, `FluentControls`, 940 x 780 window). The conversion, tray and CLI
  code of TsDssConverter were not taken over.
- Row 1: template file (path box, Enter opens, Browse). Row 2: text editor (Consolas, no word wrap) and a preview panel
  (placeholder until the template format is known).
- Open / Save (Ctrl+O / Ctrl+S) with the encoding and line endings of the file kept; unsaved-changes question; `*` in the
  title; the last file is opened again at start.
- Banner drawn in code with the name `DssLabelTemplate` (the picture of TsDssConverter has its name in it).
- Settings (`Language`, `LastFile`) in `%APPDATA%\DssLabelTemplate\settings.json`.
- `publish.cmd`: one self-contained `DssLabelTemplate.exe`. 30 tests.
