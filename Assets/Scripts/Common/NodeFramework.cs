using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class Node
{
    public string name;

    [Header("UI")]
    public Button button;

    [Tooltip("Background shown when this node's children are visible")]
    public GameObject background;

    [Header("Hierarchy")]
    public List<Node> children = new List<Node>();

    [Header("Actions (Leaf Nodes Only)")]
    public UnityEvent onSelected;

    public bool HasChildren
    {
        get { return children != null && children.Count > 0; }
    }
}

public class NodeFramework : MonoBehaviour
{
    [Header("Root Nodes (Always Visible)")]
    public List<Node> rootNodes = new List<Node>();

    [Header("Root Background (Optional)")]
    public GameObject rootBackground;

    // Stack used only for child navigation (never roots)
    private Stack<Node> childStack = new Stack<Node>();

    // Stores layer-1 children of currently active root
    private List<Node> activeRootChildren = null;

    // ---------------- UNITY ----------------

    void Awake()
    {
        InitializeButtons(rootNodes);

        // Root buttons are always visible
        foreach (var root in rootNodes)
        {
            if (root.button != null)
                root.button.gameObject.SetActive(true);
        }

        // Hide everything else
        HideAllChildren();
        ShowBackground(rootBackground);
    }

    // ---------------- INITIALIZATION ----------------

    private void InitializeButtons(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.button != null)
            {
                node.button.onClick.RemoveAllListeners();
                node.button.onClick.AddListener(() => OnNodeClicked(node));
            }

            if (node.HasChildren)
                InitializeButtons(node.children);
        }
    }

    // ---------------- CLICK HANDLING ----------------

    private void OnNodeClicked(Node node)
    {
        if (node == null)
            return;

        // Invoke actions FIRST (if any are wired)
        if (node.onSelected != null && node.onSelected.GetPersistentEventCount() > 0)
        {
            node.onSelected.Invoke();
        }

        // ROOT CLICK
        if (rootNodes.Contains(node))
        {
            childStack.Clear();
            activeRootChildren = node.children;

            if (node.HasChildren)
            {
                RenderChildren(activeRootChildren);
                ShowBackground(node.background);
            }

            return;
        }

        // CHILD CLICK
        if (node.HasChildren)
        {
            childStack.Push(node);
            RenderChildren(node.children);
            ShowBackground(node.background);
        }
    }


    public void GoBack()
    {
        // Case 1: already at root only
        if (childStack.Count == 0 && activeRootChildren == null)
            return;

        // Case 2: coming back from layer 2 or deeper
        if (childStack.Count > 0)
        {
            childStack.Pop();

            if (childStack.Count > 0)
            {
                RenderChildren(childStack.Peek().children);
                ShowBackground(childStack.Peek().background);
            }
            else
            {
                // Back to layer 1
                RenderChildren(activeRootChildren);
                ShowBackground(GetActiveRootBackground());
            }

            return;
        }

        // Case 3: back from layer 1 to root only
        activeRootChildren = null;
        RenderChildren(null);
        ShowBackground(rootBackground);
    }

    // ---------------- RENDERING ----------------

    private void RenderChildren(List<Node> nodesToShow)
    {
        // Always reset child visibility from scratch
        HideAllChildren();

        if (nodesToShow == null)
            return;

        foreach (var node in nodesToShow)
        {
            if (node.button != null)
                node.button.gameObject.SetActive(true);
        }
    }

    private void HideAllChildren()
    {
        HideChildrenRecursive(rootNodes);
    }

    private void HideChildrenRecursive(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (!rootNodes.Contains(node) && node.button != null)
                node.button.gameObject.SetActive(false);

            if (node.HasChildren)
                HideChildrenRecursive(node.children);
        }
    }

    // ---------------- BACKGROUND HANDLING ----------------

    private void ShowBackground(GameObject bg)
    {
        HideAllBackgrounds();

        if (bg != null)
            bg.SetActive(true);
    }

    private void HideAllBackgrounds()
    {
        HideBackgroundRecursive(rootNodes);
    }

    private void HideBackgroundRecursive(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.background != null)
                node.background.SetActive(false);

            if (node.HasChildren)
                HideBackgroundRecursive(node.children);
        }
    }

    private GameObject GetActiveRootBackground()
    {
        if (activeRootChildren == null)
            return rootBackground;

        foreach (var root in rootNodes)
        {
            if (root.children == activeRootChildren)
                return root.background;
        }

        return rootBackground;
    }
}
