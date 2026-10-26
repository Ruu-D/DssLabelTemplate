# Usage

The main application, **DssLabelTemplate.exe**, can be used to:

- Edit label templates
- Visualize labels
- Print labels

# Remarks - Settings

There are several hidden parameters available in the **settings.json** file:

### PrinterName

```json
"PrinterName": "CutePDF Writer"
```

Can be used to force a specific printer.

- If a printer name is specified, that printer will always be used.
- If left blank (`""`), the default Windows printer will be used.

### SheetRotationInViewer

```json
"SheetRotationInViewer": -90
```

Used to adjust the operator's view of the CNC-machined sheet.

This can be useful when the computer display is rotated and the displayed sheet orientation differs from the actual printer orientation.

### SheetReferenceCorner

```json
"SheetReferenceCorner": "BottomLeft"
```

Accepted values:

| English | Dutch |
|----------|----------|
| `BottomLeft` | `Linksonder` |
| `TopLeft` | `Linksboven` |
| `BottomRight` | `Rechtsonder` |
| `TopRight` | `Rechtsboven` |

> **Important:** This parameter must match the sheet reference corner configured in the DUIVESTEIN automatic printer system.

### ShowLabelRotation

```json
"ShowLabelRotation": true
```

Only for the preview: if `true`, the label preview is turned by the rotation value from the CSV (in the direction of `LabelDirection`) plus `SheetRotationInViewer`, as the label sits on the sheet map (= as the operator sees it). If `false` (default), the label is shown upright.

### PrintLabelWithRotation

```json
"PrintLabelWithRotation": true
```

If `true`, the print is turned by the rotation value from the CSV (in the direction of `LabelDirection`) plus `SheetRotationInViewer`, around the middle of the label. The operator then takes the label off the printer and places it onto the sheet without turning it. Example: rotation 0 in the CSV with `"SheetRotationInViewer": -90` prints the label a quarter turn clockwise. If `false` (default), the label is printed upright.

This assumes the printer is placed so that the operator reads a normal (unturned) label upright, looking the same way as at the screen.

> **Important:** This only works if the label length and width are the same value (a square label). For any other label size, the program asks whether to print the label upright instead.

### LabelDirection

```json
"LabelDirection": "CCW"
```

Which way a positive rotation value from the CSV turns the label on the sheet: `CCW` (counter-clockwise, default) or `CW` (clockwise). Used by `ShowLabelRotation` and `PrintLabelWithRotation`.

A label with rotation 0 sits upright on the sheet: the bottom of the label points to the Y = 0 edge of the sheet. Check the direction on the machine with a label at rotation 90 or 270. `SheetRotationInViewer` itself always turns counter-clockwise.
