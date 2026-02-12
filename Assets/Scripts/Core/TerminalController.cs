using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerminalController : MonoBehaviour
{
    [Header("Refs UI")]
    public TMP_InputField input;
    public TMP_Text historyText;
    public ScrollRect scrollRect;

    [Header("Logic")]
    public CommandProcessor cmdProcessor;

    [Header("Options")]
    public int maxLines = 200;

    private readonly List<string> lines = new();

    // Autocomplete state
    private List<string> suggestions = new();
    private int suggestionIndex = -1;

    // "Anchor" = ce que l'utilisateur a tapé avant de cycler avec Tab
    private string lastPrefix = "";
    private int lastTokenIndex = -1;

    // Historique des commandes tapées
    private readonly List<string> commandHistory = new();
    private int historyIndex = -1;
    private string draftBuffer = "";

    [Header("Open/Close")]
    public GameObject terminalRoot;          // Panel/Canvas du terminal
    public KeyCode toggleKey = KeyCode.F1;   // touche d'ouverture/fermeture
    public bool startOpen = false;

    [Header("Typing Slow Motion")]
    public bool slowMoWhileTyping = true;
    [Range(0.01f, 1f)] public float timeScaleWhileTyping = 0.2f;
    public float fixedDeltaBase = 0.02f;

    private bool isSlowMoActive = false;


    void Start()
    {
        if (input != null)
            input.onSubmit.AddListener(_ => SubmitFromInput());

        if (terminalRoot != null)
            terminalRoot.SetActive(startOpen);

        if (!startOpen)
            StopTypingSlowMo();
        else
            FocusInput();

    }

    void Update()
    {
        // Toggle terminal UNIQUEMENT si on n'est PAS en train d'écrire
        if (Input.GetKeyDown(toggleKey))
        {
            // Si le terminal est ouvert ET que l'input est focus → on ignore
            if (terminalRoot != null &&
                terminalRoot.activeSelf &&
                input != null &&
                input.isFocused)
            {
                // Sécurité : ne pas fermer pendant la saisie
            }
            else
            {
                ToggleTerminal();
                return;
            }
        }


        // Si terminal fermé -> on ne fait rien + temps normal
        if (terminalRoot != null && !terminalRoot.activeSelf)
        {
            StopTypingSlowMo();
            return;
        }


        if (input == null || !input.isFocused)
            return;

        // SlowMo uniquement quand on tape (focus + texte non vide)
        if (slowMoWhileTyping && input.isFocused && !string.IsNullOrWhiteSpace(input.text))
            StartTypingSlowMo();
        else
            StopTypingSlowMo();


        // Reset autocomplete dès qu'on tape autre chose que TAB
        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab))
            ResetAutocomplete();

        // Entrée -> submit
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitFromInput();
            ResetAutocomplete();
            return;
        }

        // Tab -> autocomplete (cycle)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Autocomplete();
            return;
        }

        // Historique commandes : Up/Down
        if (Input.GetKeyDown(KeyCode.UpArrow))
            HistoryUp();

        if (Input.GetKeyDown(KeyCode.DownArrow))
            HistoryDown();
    }

    public void ToggleTerminal()
    {
        if (terminalRoot == null) return;

        bool willOpen = !terminalRoot.activeSelf;
        terminalRoot.SetActive(willOpen);

        if (willOpen)
        {
            FocusInput();
        }
        else
        {
            StopTypingSlowMo();
            ResetAutocomplete();
        }
    }

    void StartTypingSlowMo()
    {
        if (!slowMoWhileTyping) return;
        if (isSlowMoActive) return;

        isSlowMoActive = true;
        Time.timeScale = timeScaleWhileTyping;
        Time.fixedDeltaTime = fixedDeltaBase * Time.timeScale;
    }

    void StopTypingSlowMo()
    {
        if (!isSlowMoActive) return;

        isSlowMoActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = fixedDeltaBase;
    }


    public void SubmitFromInput()
    {
        if (cmdProcessor == null || input == null) return;

        var line = input.text;
        if (string.IsNullOrWhiteSpace(line))
            return;

        AddLine($"> {line}");

        // Historique des commandes
        commandHistory.Add(line);
        historyIndex = -1;
        draftBuffer = "";

        // Exécution
        string result = cmdProcessor.ExecuteLine(line);

        if (result == "__CLEAR__")
        {
            ClearHistory();
        }
        else if (!string.IsNullOrWhiteSpace(result))
        {
            AddLine(result);
        }

        input.text = "";
        input.caretPosition = 0;
        FocusInput();
        StopTypingSlowMo(); // après envoi, on revient au temps normal


        // autoscroll
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    int TargetPriority(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 100;

        s = s.ToLowerInvariant();

        if (s == "self") return 0;
        if (s == "selected") return 1;
        if (s == "nearest") return 2;
        if (s == "view") return 3;

        return 10; // les noms d’objets après
    }


    void Autocomplete()
    {
        if (cmdProcessor == null || input == null) return;

        string raw = input.text ?? "";

        // tokens, en gardant la possibilité d'ajouter un token vide si espace final
        var parts = raw.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).ToList();

        // tokenIndex = token à compléter
        int tokenIndex = (parts.Count == 0) ? 0 : parts.Count - 1;

        // si l'utilisateur a un espace à la fin, on complète le token suivant (vide)
        if (raw.EndsWith(" "))
        {
            tokenIndex = parts.Count;
            parts.Add("");
        }

        // token courant dans l'input (peut être une suggestion déjà remplie)
        string currentToken = (tokenIndex >= 0 && tokenIndex < parts.Count) ? parts[tokenIndex] : "";

        // Détecter si on est en train de cycler : si le token courant est déjà une suggestion,
        // on garde le prefix "anchor" (lastPrefix) pour ne pas reconstruire la liste et bloquer le cycle.
        bool cycling = (tokenIndex == lastTokenIndex &&
                        suggestionIndex != -1 &&
                        suggestions.Count > 0 &&
                        suggestions.Any(s => string.Equals(s, currentToken, System.StringComparison.OrdinalIgnoreCase)));

        string prefix = cycling ? lastPrefix : currentToken;

        bool isSpawn = parts.Count > 0 && string.Equals(parts[0], "spawn", System.StringComparison.OrdinalIgnoreCase);

        // Pool selon contexte + token
        IEnumerable<string> pool;
        if (!isSpawn)
        {
            // <action> <target>
            pool = (tokenIndex == 0) ? cmdProcessor.GetCommandKeywords()
                                    : cmdProcessor.GetTargetNames();
        }
        else
        {
            // spawn <id> <target>
            if (tokenIndex == 0) pool = cmdProcessor.GetCommandKeywords();
            else if (tokenIndex == 1) pool = cmdProcessor.GetSpawnIds();
            else pool = cmdProcessor.GetTargetNames();
        }

        // Rebuild suggestions si contexte changé (sauf si on cycle)
        if (!cycling &&
            (tokenIndex != lastTokenIndex ||
             !string.Equals(prefix, lastPrefix, System.StringComparison.OrdinalIgnoreCase) ||
             suggestions.Count == 0))
        {
            lastTokenIndex = tokenIndex;
            lastPrefix = prefix;

            bool isTargetPool = (!isSpawn && tokenIndex >= 1) || (isSpawn && tokenIndex >= 2);


            suggestions = pool
                .Where(s => !string.IsNullOrWhiteSpace(s) &&
                            s.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => isTargetPool ? TargetPriority(s) : 0)
                .ThenBy(s => s)
                .ToList();


            suggestionIndex = -1;
        }

        if (suggestions.Count == 0)
            return;

        // Cycle Tab
        suggestionIndex = (suggestionIndex + 1) % suggestions.Count;
        string chosen = suggestions[suggestionIndex];

        while (parts.Count <= tokenIndex) parts.Add("");
        parts[tokenIndex] = chosen;

        string rebuilt = string.Join(" ", parts);

        // Confort: après compléter action ou spawn id, ajouter espace
        if (!rebuilt.EndsWith(" "))
        {
            if (tokenIndex == 0 || (isSpawn && tokenIndex == 1))
                rebuilt += " ";
        }

        input.text = rebuilt;
        input.caretPosition = input.text.Length;
        FocusInput();
    }

    void ResetAutocomplete()
    {
        suggestions.Clear();
        suggestionIndex = -1;
        lastPrefix = "";
        lastTokenIndex = -1; // IMPORTANT : force rebuild
    }

    void HistoryUp()
    {
        if (commandHistory.Count == 0) return;

        if (historyIndex == -1)
            draftBuffer = input.text;

        historyIndex = Mathf.Clamp(historyIndex + 1, 0, commandHistory.Count - 1);

        int idx = commandHistory.Count - 1 - historyIndex;
        input.text = commandHistory[idx];
        input.caretPosition = input.text.Length;
        FocusInput();

        ResetAutocomplete();
    }

    void HistoryDown()
    {
        if (commandHistory.Count == 0) return;
        if (historyIndex == -1) return;

        historyIndex--;

        if (historyIndex < 0)
        {
            historyIndex = -1;
            input.text = draftBuffer;
        }
        else
        {
            int idx = commandHistory.Count - 1 - historyIndex;
            input.text = commandHistory[idx];
        }

        input.caretPosition = input.text.Length;
        FocusInput();

        ResetAutocomplete();
    }

    public void AddLine(string s)
    {
        lines.Add(s);

        if (lines.Count > maxLines)
            lines.RemoveAt(0);

        if (historyText != null)
            historyText.text = string.Join("\n", lines);

        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    public void ClearHistory()
    {
        lines.Clear();
        if (historyText != null)
            historyText.text = "";
    }

    void FocusInput()
    {
        if (input == null) return;
        input.ActivateInputField();
        input.Select();
    }

    void OnDisable()
    {
        // sécurité
        StopTypingSlowMo();
    }
}
