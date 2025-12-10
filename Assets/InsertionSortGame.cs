using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInsertionSortGame : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform cardPanel;
    public Button nextButton;
    public Button autoButton;
    public Button resetButton;
    public TextMeshProUGUI statusText;

    [Header("Optional Input")]
    public TMP_InputField inputField;   // type numbers here (optional)

    [Header("Sort Direction Buttons")]
    public Button sortAscButton;        // Ascending button
    public Button sortDescButton;       // Descending button

    [Header("Card Prefab")]
    public CardVisual cardPrefab;       // drag CardPrefab (from Project) here
    public int numCards = 5;

    [Header("Layout (row position)")]
    public float rowCenterX = 0f;
    public float rowCenterY = -50f;

    [Header("Animation")]
    public float cardSpacing = 140f;
    public float moveUpAmount = 40f;
    public float moveDuration = 0.25f;

    // runtime data
    private List<CardVisual> cards = new List<CardVisual>();
    private int[] values;
    private int i = 1;
    private bool isSorting = false;
    private Coroutine autoRoutine;

    // current sort direction (true = ascending, false = descending)
    private bool ascending = true;

    void Start()
    {
        // main controls
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (autoButton != null)
            autoButton.onClick.AddListener(OnAutoClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        // direction buttons
        if (sortAscButton != null)
            sortAscButton.onClick.AddListener(SetAscending);

        if (sortDescButton != null)
            sortDescButton.onClick.AddListener(SetDescending);

        // start with cards ready
        ResetCards();
    }

    // =====================================================
    // RESET CARDS (also used by UseInputArray)
    // =====================================================
    void ResetCards()
    {
        // Kill ALL running coroutines on this script (Step, AutoSort, etc.)
        StopAllCoroutines();
        autoRoutine = null;
        isSorting = false;
        i = 1;

        // Try to read values from input field
        int[] inputValues = TryReadInputNumbers();

        if (inputValues != null)
        {
            values = inputValues;
            numCards = values.Length;
        }
        else
        {
            // generate random values if no valid input
            numCards = Mathf.Max(1, numCards);
            values = new int[numCards];
            for (int k = 0; k < numCards; k++)
                values[k] = Random.Range(1, 14);   // 1–13
        }

        BuildCardsFromValues();

        // show the current direction in the status
        if (statusText != null)
            statusText.text = ascending
                ? "Status: Ready (Ascending)"
                : "Status: Ready (Descending)";
    }

    // =====================================================
    // Called by "Use Input" button (optional)
    // =====================================================
    public void UseInputArray()
    {
        // it will prefer input-field values
        ResetCards();
    }

    // =====================================================
    // sort direction setters (restart sort)
    // =====================================================
    void SetAscending()
    {
        ascending = true;
        ResetCards(); // restart with Ascending
    }

    void SetDescending()
    {
        ascending = false;
        ResetCards(); // restart with Descending
    }

    // =====================================================
    // Read numbers from inputField → int[]
    // Example input: "6 7 1 0 2 1"
    // =====================================================
    int[] TryReadInputNumbers()
    {
        if (inputField == null) return null;
        if (string.IsNullOrWhiteSpace(inputField.text)) return null;

        string[] tokens = inputField.text.Split(
            new char[] { ' ', ',', ';', '\t' },
            System.StringSplitOptions.RemoveEmptyEntries);

        List<int> list = new List<int>();

        foreach (string t in tokens)
        {
            if (int.TryParse(t, out int v))
            {
                // clamp to 1–13 (since you have 13 card sprites)
                v = Mathf.Clamp(v, 1, 13);
                list.Add(v);
            }
        }

        return (list.Count > 0) ? list.ToArray() : null;
    }

    // =====================================================
    // Build the row of cards from "values"
    // =====================================================
    private void BuildCardsFromValues()
    {
        // delete old cards
        foreach (Transform child in cardPanel)
            Destroy(child.gameObject);

        cards.Clear();

        for (int k = 0; k < values.Length; k++)
        {
            CardVisual card = Instantiate(cardPrefab, cardPanel, false);

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            float x = rowCenterX + (k - (values.Length - 1) / 2f) * cardSpacing;
            rt.anchoredPosition = new Vector2(x, rowCenterY);

            card.Setup(values[k]);
            cards.Add(card);
        }

        i = 1;
    }

    // =====================================================
    // Button Handlers
    // =====================================================
    void OnNextClicked()
    {
        if (isSorting) return;
        if (values == null || values.Length <= 1) return;

        // false = manual step
        StartCoroutine(Step(false));
    }

    void OnAutoClicked()
    {
        if (isSorting) return;
        if (values == null || values.Length <= 1) return;
        if (autoRoutine != null) return;

        autoRoutine = StartCoroutine(AutoSort());
    }

    void OnResetClicked()
    {
        ResetCards();
    }

    // =====================================================
    // Sorting
    // =====================================================
    IEnumerator AutoSort()
    {
        isSorting = true;

        while (i < values.Length)
        {
            // true = part of auto-sort
            yield return Step(true);
            yield return new WaitForSeconds(0.3f);
        }

        isSorting = false;
        autoRoutine = null;
    }

    IEnumerator Step(bool calledFromAuto)
    {
        if (values == null || cards == null)
            yield break;

        if (i >= values.Length || i >= cards.Count)
        {
            if (statusText != null)
                statusText.text = "Sorting complete!";
            yield break;
        }

        isSorting = true;

        // Safety: card might have been destroyed somehow
        CardVisual keyCard = cards[i];
        if (keyCard == null)
        {
            Debug.LogWarning("Key card is null; aborting step.");
            isSorting = false;
            yield break;
        }

        RectTransform keyRT = keyCard.GetComponent<RectTransform>();
        if (keyRT == null)
        {
            Debug.LogWarning("Key RectTransform is null; aborting step.");
            isSorting = false;
            yield break;
        }

        int keyValue = values[i];

        // Lift key card
        yield return MoveVertical(keyRT, moveUpAmount);

        int j = i - 1;

        // Compare based on ascending / descending
        while (j >= 0 &&
               ((ascending && values[j] > keyValue) ||
                (!ascending && values[j] < keyValue)))
        {
            values[j + 1] = values[j];

            CardVisual movingCard = cards[j];
            cards[j + 1] = movingCard;

            if (movingCard != null)
            {
                RectTransform movingRT = movingCard.GetComponent<RectTransform>();
                if (movingRT != null)
                    yield return MoveToIndex(movingRT, j + 1);
            }

            j--;
        }

        // insert key card
        values[j + 1] = keyValue;
        cards[j + 1] = keyCard;

        yield return MoveToIndex(keyRT, j + 1);

        // Drop key card back down
        yield return MoveVertical(keyRT, -moveUpAmount);

        i++;
        if (statusText != null)
        {
            statusText.text = (i < values.Length)
                ? $"Step i = {i}"
                : "Sorting complete!";
        }

        if (!calledFromAuto)
            isSorting = false;   // AutoSort will clear it when done
    }

    // =====================================================
    // Animation Helpers
    // =====================================================
    IEnumerator MoveVertical(RectTransform rt, float deltaY)
    {
        if (rt == null) yield break;

        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + new Vector2(0f, deltaY);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            if (rt == null) yield break;
            rt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
    }

    IEnumerator MoveToIndex(RectTransform rt, int index)
    {
        if (rt == null) yield break;

        Vector2 start = rt.anchoredPosition;
        float targetX = rowCenterX + (index - (values.Length - 1) / 2f) * cardSpacing;
        Vector2 end = new Vector2(targetX, start.y);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            if (rt == null) yield break;
            rt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
    }
}
