---
name: phase0
description: Install all required Unity packages for the PokerClient project (Newtonsoft JSON, DOTween, TextMeshPro Essentials). Run when starting the project or when invoked with /phase0.
---

# Phase 0: Package Installation

You are setting up the Unity package environment for the HijackPoker PokerClient project.
Read CLAUDE.md in the project root before doing anything else.

## Goal
Install all required packages so the project compiles cleanly. No code is written in this phase.

## Required Packages

### 1. Newtonsoft JSON
- **Package name**: `com.unity.nuget.newtonsoft-json`
- **How**: Window → Package Manager → + → Add package by name → enter name → Add
- **Verify**: `using Newtonsoft.Json;` compiles without error

### 2. DOTween
- **Source**: https://dotween.demigiant.com/download.php OR Unity Asset Store
- **Alternative via Package Manager**: Add via git URL if available, otherwise:
  - Download DOTween free from demigiant.com
  - Import the .unitypackage into the project
  - Run DOTween Setup (the dialog that appears after import)
- **Verify**: `using DG.Tweening;` compiles without error

### 3. TextMeshPro Essentials
- **How**: Window → TextMeshPro → Import TMP Essential Resources
- **When**: Only if TMP font assets are missing (may already be done)
- **Verify**: Can add a TextMeshProUGUI component to a GameObject without errors

## Steps via Unity MCP (if connected)

If `mcp__unity__*` tools are available, use them to:
1. Check if packages are already installed via `read_console` after compilation
2. Use `manage_script` or `batch_execute` to trigger package installation if supported
3. If MCP cannot install packages, instruct the user manually

## Manual Steps (if MCP not available)

Give the user these exact steps:
1. In Unity Editor: Window → Package Manager
2. Click the **+** button (top left) → "Add package by name"
3. Type: `com.unity.nuget.newtonsoft-json` → click Add
4. Wait for installation to complete
5. For DOTween: go to https://dotween.demigiant.com/download.php, download free version, drag .unitypackage into Unity, import all, click Setup DOTween
6. For TMP: Window → TextMeshPro → Import TMP Essential Resources → Import

## Acceptance Criteria
- [ ] Project compiles with zero errors after all packages installed
- [ ] `using Newtonsoft.Json;` works
- [ ] `using DG.Tweening;` works
- [ ] `using TMPro;` works
- [ ] DOTween Setup wizard completed (creates DOTween/Resources folder)

## Next Phase
After all packages are installed and project compiles cleanly, proceed to `/phase1`.
