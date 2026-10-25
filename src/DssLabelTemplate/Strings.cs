namespace DssLabelTemplate;

/// <summary>
/// ALL texts of the user interface, in Dutch, French and English, in one place so they are easy to change.
/// The three versions of a text always sit next to each other: nl, fr, en. The language is chosen by
/// <see cref="Localizer"/>. Never write a text that the user can see anywhere else.
/// </summary>
internal static class Strings
{
    private static string T(string dutch, string french, string english) => Localizer.T(dutch, french, english);

    // ---- Names that are the same in every language ----
    public const string AppName = "DssLabelTemplate";
    public const string DeveloperCredit = "Dev.: Daan Verhoost";
    public const string CompanyName = "ROGIERS NV/SA";
    public const string CompanyUrl = "https://www.rogiers.be/";
    public const string CreditSeparator = " | ";  // between the credit and the app version, top right
    public const string UserGuideUrl = "https://github.com/Ruu-D/DssLabelTemplate/"; // opened by the user-guide icon: the GitHub page of this tool

    /// <summary>The line below the program name in the banner. English in every language, like the banner of TsDssConverter.</summary>
    public const string BannerSubtitle = "Visualize and manually edit label files for a Duivestein system";

    /// <summary>The two arrows next to the preview title (pictures, the same in every language).</summary>
    public const string PreviousArrow = "◀";
    public const string NextArrow = "▶";

    /// <summary>At the top right of the window, after the credit, e.g. "App version: 0.1.0".</summary>
    public static string AppVersion(string version) => T(
        $"App-versie: {version}",
        $"Version de l'application : {version}",
        $"App version: {version}");

    /// <summary>The tooltip of the user-guide icon right after the app version.</summary>
    public static string UserGuideTooltip => T(
        "Gebruikershandleiding (opent de website)",
        "Guide d'utilisation (ouvre le site web)",
        "User guide (opens the website)");

    // ---- Language box: its title has all three languages, so it can be found in any language ----
    public const string LanguageTitle = "Taal / Langue / Language";
    public const string LanguageDutch = "Dutch - Nederlands";
    public const string LanguageFrench = "French - Français";
    public const string LanguageEnglish = "English - Engels";

    // ---- Row 1: the two files ----
    public static string FileCaption => T("Labelbestanden", "Fichiers d'étiquettes", "Label files");

    /// <summary>Below the caption: the two kinds of label files of Duivestein (DSS Software Interface, "Label").</summary>
    public static string FileDescription => T(
        "Labeltemplate + label-csv: het template (xml) beschrijft de opmaak van één label, met variabelen zoals $LENGTH$; de " +
        "label-csv bevat per label één regel met de plaats op de plaat en de gegevens die bij het afdrukken worden ingevuld.\n" +
        "Label-xml: één bestand per plaat met opmaak én gegevens van alle labels samen (één <PrintLabel> per label): geen csv nodig.",
        "Modèle + CSV d'étiquettes : le modèle (xml) décrit la mise en page d'une étiquette, avec des variables comme $LENGTH$ ; " +
        "le csv contient une ligne par étiquette avec la position sur le panneau et les données remplies à l'impression.\n" +
        "XML d'étiquettes : un fichier par panneau avec la mise en page et les données de toutes les étiquettes (un <PrintLabel> par étiquette) : pas de csv.",
        "Label template + label CSV: the template (XML) describes the layout of one label, with variables like $LENGTH$; the " +
        "label CSV has one line per label with its place on the sheet and the data that is filled in when printing.\n" +
        "Label XML: one file per sheet with the layout and the data of all its labels together (one <PrintLabel> per label): no CSV needed.");

    /// <summary>In front of the two path boxes.</summary>
    public static string XmlFileCaption => T("Template of label-xml:", "Modèle ou XML d'étiquettes :", "Template or label XML:");
    public static string CsvFileCaption => T("Label-csv:", "CSV d'étiquettes :", "Label CSV:");

    /// <summary>The grey text in the empty csv box: when a csv is useful, and when it is not.</summary>
    public static string CsvPlaceholder => T(
        "Bij een template: de gegevens, één regel per label",
        "Avec un modèle : les données, une ligne par étiquette",
        "With a template: the data, one line per label");

    public static string CsvNotNeededPlaceholder => T(
        "Niet nodig: de label-xml bevat de gegevens al",
        "Inutile : le XML d'étiquettes contient déjà les données",
        "Not needed: the label XML already holds the data");

    public static string Browse => T("Bladeren…", "Parcourir…", "Browse…");

    /// <summary>The file type of the upper Browse button and of saving a new text ("text|pattern", an OpenFileDialog filter).</summary>
    public static string XmlFileFilter => T(
        "Labeltemplate of label-xml (*.xml)|*.xml",
        "Modèle ou XML d'étiquettes (*.xml)|*.xml",
        "Label template or label XML (*.xml)|*.xml");

    public static string CsvFilter => T("Label-csv (*.csv)|*.csv", "CSV d'étiquettes (*.csv)|*.csv", "Label CSV (*.csv)|*.csv");

    // ---- The status line below the files ----
    public static string StatusNothingOpen => T("Nog geen bestand geopend.", "Aucun fichier ouvert.", "No file opened yet.");

    public static string StatusCsvWithoutTemplate => T(
        "Label-csv geopend: kies nu het template (xml) erbij.",
        "CSV d'étiquettes ouvert : choisissez maintenant le modèle (xml).",
        "Label CSV opened: now choose its template (XML).");

    public static string StatusLabelXml(int labels) => T(
        $"Label-xml met {labels} label(s).",
        $"XML d'étiquettes avec {labels} étiquette(s).",
        $"Label XML with {labels} label(s).");

    public static string StatusTemplateWithCsv(int labels) => T(
        $"Labeltemplate met de gegevens van de label-csv: {labels} label(s).",
        $"Modèle d'étiquette avec les données du csv : {labels} étiquette(s).",
        $"Label template with the data of the label CSV: {labels} label(s).");

    public static string StatusTemplateWithoutCsv => T(
        "Labeltemplate zonder label-csv: de variabelen ($…$) blijven leeg.",
        "Modèle d'étiquette sans csv : les variables ($…$) restent vides.",
        "Label template without a label CSV: the variables ($…$) stay empty.");

    public static string StatusXmlError => T(
        "Het bestand is (nog) geen geldige xml: zie het voorbeeld.",
        "Le fichier n'est pas (encore) un xml valide : voir l'aperçu.",
        "The file is not valid XML (yet): see the preview.");

    // ---- The label size, right of the status line ----
    public static string LabelSizeCaption => T("Labelformaat (mm):", "Format d'étiquette (mm) :", "Label size (mm):");
    public const string LabelSizeTimes = "×";

    public static string LabelLengthTooltip => T(
        "Lengte van het label: horizontaal, langs X (1 – 1000 mm)",
        "Longueur de l'étiquette : horizontale, le long de X (1 – 1000 mm)",
        "Length of the label: horizontal, along X (1 – 1000 mm)");

    public static string LabelWidthTooltip => T(
        "Breedte van het label: verticaal, langs Y (1 – 1000 mm)",
        "Largeur de l'étiquette : verticale, le long de Y (1 – 1000 mm)",
        "Width of the label: vertical, along Y (1 – 1000 mm)");

    // ---- Row 2: the two columns ----
    public static string EditorCaption => T("Tekst van het template", "Texte du modèle", "Template text");
    public static string CsvCaption => T("Gegevens (alleen lezen)", "Données (lecture seule)", "Data (read-only)");

    /// <summary>The two buttons above the left column (only with a label-csv): the template text or the csv data.</summary>
    public static string XmlViewButton => T("Template (xml)", "Modèle (xml)", "Template (XML)");
    public static string CsvViewButton => T("Label-csv", "CSV d'étiquettes", "Label CSV");

    /// <summary>The first column of the csv table: the number of the label, as in "2 / 3" above the preview.</summary>
    public const string CsvNumberColumn = "#";
    public static string PreviewCaption => T("Voorbeeld van het label", "Aperçu de l'étiquette", "Label preview");
    public static string PreviousLabel => T("Vorig label", "Étiquette précédente", "Previous label");
    public static string NextLabel => T("Volgend label", "Étiquette suivante", "Next label");

    public static string PreviewNoFile => T(
        "Kies hierboven een labeltemplate (met zijn label-csv) of een label-xml.",
        "Choisissez ci-dessus un modèle d'étiquette (avec son csv) ou un xml d'étiquettes.",
        "Choose a label template (with its label CSV) or a label XML above.");

    public static string PreviewXmlError(string reason) => T(
        $"De xml kan niet worden gelezen:\n{reason}",
        $"Le xml ne peut pas être lu :\n{reason}",
        $"The XML cannot be read:\n{reason}");

    public static string PreviewNotALabelFile => T(
        "Dit is geen labelbestand: de buitenste node moet <DSSPrintJob> zijn.",
        "Ce n'est pas un fichier d'étiquettes : le nœud racine doit être <DSSPrintJob>.",
        "This is not a label file: the outer node must be <DSSPrintJob>.");

    public static string PreviewEmptyLabelFile => T(
        "Leeg labelbestand (geen <PrintLabel>): voor deze plaat worden geen labels geprint.",
        "Fichier d'étiquettes vide (aucun <PrintLabel>) : aucune étiquette n'est imprimée pour ce panneau.",
        "Empty label file (no <PrintLabel>): no labels are printed for this sheet.");

    public static string PreviewCsvWithoutRows => T(
        "De label-csv bevat geen labels (alleen een kopregel).",
        "Le csv d'étiquettes ne contient aucune étiquette (seulement l'en-tête).",
        "The label CSV has no labels (only a header line).");

    /// <summary>Below the label: where it sits on the sheet.</summary>
    public static string LabelInfo(string id, string x, string y, string rotation) => T(
        $"ID {id}  ·  X {x}  ·  Y {y}  ·  rotatie {rotation}°",
        $"ID {id}  ·  X {x}  ·  Y {y}  ·  rotation {rotation}°",
        $"ID {id}  ·  X {x}  ·  Y {y}  ·  rotation {rotation}°");

    public static string SheetInfo(string length, string width) => T(
        $"plaat {length} × {width} mm",
        $"panneau {length} × {width} mm",
        $"sheet {length} × {width} mm");

    /// <summary>A print object that could not be drawn (a number is missing or wrong), e.g. "&lt;Text&gt; on line 5".</summary>
    public static string ObjectNotDrawn(string objectName, int line) => T(
        $"<{objectName}> op regel {line}",
        $"<{objectName}> à la ligne {line}",
        $"<{objectName}> on line {line}");

    public static string ObjectsNotDrawn(int count, string first) => T(
        $"{count} object(en) niet getekend (ontbrekend of ongeldig getal), o.a. {first}",
        $"{count} objet(s) non dessiné(s) (nombre manquant ou invalide), p. ex. {first}",
        $"{count} object(s) not drawn (missing or invalid number), e.g. {first}");

    public static string ObjectsOutsideLabel(int count) => T(
        $"{count} object(en) vallen (deels) buiten het label",
        $"{count} objet(s) (en partie) hors de l'étiquette",
        $"{count} object(s) (partly) outside the label");

    // ---- Footer ----
    public static string Save => T("Opslaan", "Enregistrer", "Save");
    public static string Close => T("Sluiten", "Fermer", "Close");

    // ---- Questions and messages ----
    /// <summary>The name used for a text that was not saved to a file yet.</summary>
    public static string Untitled => T("(nieuw)", "(nouveau)", "(new)");

    public static string SaveChangesQuestion(string fileName) => T(
        $"De wijzigingen in {fileName} zijn niet opgeslagen. Nu opslaan?",
        $"Les modifications de {fileName} ne sont pas enregistrées. Enregistrer maintenant ?",
        $"The changes to {fileName} have not been saved. Save them now?");

    public static string CannotReadFile(string reason) => T(
        $"Het bestand kon niet worden gelezen: {reason}",
        $"Le fichier n'a pas pu être lu : {reason}",
        $"The file could not be read: {reason}");

    public static string CannotSaveFile(string reason) => T(
        $"Het bestand kon niet worden opgeslagen: {reason}",
        $"Le fichier n'a pas pu être enregistré : {reason}",
        $"The file could not be saved: {reason}");

    public static string EditorFilesFailed(string folder, string reason) => T(
        $"De bestanden van de teksteditor konden niet worden geschreven in {folder} ({reason}). Het programma stopt.",
        $"Les fichiers de l'éditeur de texte n'ont pas pu être écrits dans {folder} ({reason}). Le programme s'arrête.",
        $"The files of the text editor could not be written to {folder} ({reason}). The program stops.");

    public static string DataFolderFailed(string folder, string reason) => T(
        $"De map {folder} kon niet worden gemaakt ({reason}). Het programma stopt.",
        $"Le dossier {folder} n'a pas pu être créé ({reason}). Le programme s'arrête.",
        $"The folder {folder} could not be made ({reason}). The program stops.");

    public static string SettingsSaveFailed(string reason) => T(
        $"De instellingen konden niet worden opgeslagen: {reason}",
        $"Les paramètres n'ont pas pu être enregistrés : {reason}",
        $"The settings could not be saved: {reason}");

    public static string CannotOpenLink(string reason) => T(
        $"De website kon niet worden geopend: {reason}",
        $"Le site web n'a pas pu être ouvert : {reason}",
        $"The website could not be opened: {reason}");
}
