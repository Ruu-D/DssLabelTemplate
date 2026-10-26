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
