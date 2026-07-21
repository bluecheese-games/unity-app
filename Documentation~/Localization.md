# Localization Module

The localization system of the BlueCheese ecosystem: editable translation tables, runtime resolution,
full editor tooling (dedicated window, missing-key scanner), AI-assisted translation (Anthropic / Gemini)
and bidirectional sync with external sources (`.obd` files, extensible to the cloud).

> Namespaces: `BlueCheese.App` (runtime) and `BlueCheese.App.Editor` (tooling).

---

## Table of contents

- [Quick start](#quick-start)
- [Dependencies](#dependencies)
- [Core concepts](#core-concepts)
- [Runtime usage](#runtime-usage)
- [Editor tooling](#editor-tooling)
- [AI translation](#ai-translation)
- [External source sync (.obd)](#external-source-sync-obd)
- [Extension points](#extension-points)
- [File layout](#file-layout)
- [Screenshots to provide](#screenshots-to-provide)

---

## Quick start

1. **Create a table.** *Project ▸ Create ▸ Localization ▸ Translation Table* (or drop an `.obd` file in the
   project — a linked table is created automatically, see [.obd sync](#external-source-sync-obd)).
2. **Set supported languages.** **Tools ▸ Localization ▸ Localization Settings** → pick the default
   language and the supported languages. The asset lives in a `Resources` folder as `LocalizationSettings`.
3. **Add keys and translations.** **Tools ▸ Localization ▸ Translation Editor** (or double-click the table)
   → add a key at the bottom, fill translations, and *Validate*.
4. **Display a localized text.** Add a `LocalizedText` component next to a `TMP_Text`, then pick the key in
   the **Translation Key** field. Type a new name and use **＋ Create new key** to create it on the fly
   (using the current text as the default-language translation).
5. **Or translate from code:**

   ```csharp
   using BlueCheese.App;

   string ok = Translator.Translate("Common.Ok");          // implicit string → TranslationKey
   myLabel.text = Translator.Translate("Menu.Play");
   ```

6. **Switch language at runtime:**

   ```csharp
   ServiceLocator.Resolve<ILocalizationService>().SetCurrentLanguage(Language.French);
   // → every LocalizedText refreshes automatically (ChangeLanguageSignal).
   ```

7. *(Optional)* **Translate with AI.** Configure a provider + API key in **Tools ▸ Localization ▸ AI
   Translation Settings**, then use **✨ Translate with AI** in the detail panel, or **✨ Translate** on a
   multi-selection.

---

## Dependencies

| Dependency | Used for |
|---|---|
| **TextMesh Pro** (`TMP_Text`) | `LocalizedText` component, editor preview & tooling |
| **BlueCheese.Core** — DI (`ServiceContainer`, `ServiceLocator`) | Registering/resolving localization services |
| **BlueCheese.Core** — Signals (`SignalAPI`) | `ChangeLanguageSignal` (live text refresh) |
| **BlueCheese.Core** — Utils (`AssetBank`, `ProcessQueue`, `DateTimeExtensions`) | Table loading, progress queues, `TimeAgo()` |
| **BlueCheese.Core.Editor** (`EditorGUIHelper`, `EditorIcon`, `SearchKeyWindow`) | Searchable key fields, icons |
| **Newtonsoft.Json** (`com.unity.nuget.newtonsoft-json`) | Round-trip read/write of `.obd` files |
| **UnityWebRequest** | AI provider calls (editor only) |

Runtime services are registered by `DefaultServicesInstaller.RegisterDefaultServices()` on the
`UnityApp.Builder` (see the BlueCheese bootstrap convention).

---

## Core concepts

| Type | Role |
|---|---|
| `Language` (enum) | Supported languages (English, French, …, Malay). |
| `TranslationKey` (struct) | Serializable reference to a key: `key`, `pluralKey`, `parameters`. Immutable. |
| `TranslationTableAsset` (ScriptableObject) | A table: list of `TranslationItem` (key + per-language translations). *Create ▸ Localization ▸ Translation Table*. |
| `TranslationItem` | A key + its `Translation`s (per language) + status + dates. |
| `Translation` | A cell: `Language`, `Value`, `AITranslated`, `LastModified` (per-cell UTC ticks). |
| `TranslationStatus` | `Validated`, `Modified`, `ToRemove`. |
| `ITranslationService` / `TranslationService` | Resolves a `TranslationKey` → text (current language → default → raw key). |
| `ILocalizationService` / `LocalizationService` | Device / default / current language; persists the choice. |
| `Translator` (static) | Translation facade used by `TranslationKey` (resolves the service at runtime **and** in the editor). |
| `LocalizedText` (MonoBehaviour) | Applies a `TranslationKey` to a `TMP_Text`, refreshing on language change. |
| `LocalizationSettingsAsset` | Default language + supported languages (loaded from `Resources`). |

### How a translation resolves

`TranslationService.Translate(key)` tries, in order:
1. the **current language**,
2. the **default language**,
3. otherwise the **raw key** (never an empty string).

**Plurals** use `pluralKey`: `TranslationKey.Format(singular, plural)` picks the plural form when an integer
parameter `> 1` is present (`{0}` is substituted via `string.Format`).

---

## Runtime usage

### 1. The `LocalizedText` component

Add `LocalizedText` to a GameObject that has a `TMP_Text`, then assign the key in the inspector
(**Translation Key** field, with search/creation — see the [drawer](#the-translationkey-property-drawer)).

The text refreshes automatically:
- on `Start`,
- on every `ChangeLanguageSignal` (language change).

```csharp
// Dynamic parameter (replaces {0}) — updates the TMP immediately.
myLocalizedText.SetParameter(0, playerName.ToString());
```

### 2. Translating from code

`TranslationKey` has implicit conversions to/from `string`, so:

```csharp
using BlueCheese.App;

// Simplest form (implicit string → TranslationKey → Translate):
string label = Translator.Translate("Common.Ok");

// With parameters and a plural form:
var key = new TranslationKey(
    key: "Inventory.Apples",
    parameters: new[] { count.ToString() },
    pluralKey: "Inventory.Apples.Plural");
string text = key;            // implicit conversion → Translator.Translate(key)

// Check whether a translation exists:
if (Translator.HasTranslation("Some.Key")) { /* … */ }
```

### 3. Switching language

```csharp
// Resolved through the DI container (registered by RegisterDefaultServices()).
var localization = ServiceLocator.Resolve<ILocalizationService>();
localization.SetCurrentLanguage(Language.French);
// → publishes ChangeLanguageSignal; all LocalizedText refresh.
```

React to language changes elsewhere:

```csharp
SignalAPI.Subscribe<ChangeLanguageSignal>(signal =>
{
    Debug.Log($"Language: {signal.Language}");
}, this);
```

### 4. Settings (`LocalizationSettingsAsset`)

Create the asset (*Create ▸ Localization ▸ Settings*, or **Tools ▸ Localization ▸ Localization Settings**)
inside a `Resources` folder named `LocalizationSettings`. It defines the **default language** and the
**supported languages** (the device→default fallback relies on it).

---

## Editor tooling

All tools live under the **Tools ▸ Localization** menu:

| Menu | Window / action |
|---|---|
| **Translation Editor** | Main table editor (multi-tab). |
| **Localization Settings** | Selects/creates the settings asset. |
| **AI Translation Settings** | AI translation configuration. |
| **Missing Keys Scanner** | Finds `LocalizedText` whose key exists in no table. |

### The Translation Editor window

![Translation Editor](Images/translation-editor.png)

Open it from the menu above, by **double-clicking** a `TranslationTableAsset`, or via the *Open* button in
the inspector.

- **Multi-table tabs**: open several tables; the tab shows the asset name (the window title stays
  "Translation Editor"). Tabs survive domain reloads.
- **Empty state**: lists every table in the project (name, key count, date, "validated" progress) + a
  key/translation search.
- **Toolbar**: search (key or translation), **status** filter, count.
- **Language management**: removable pills + *Add Language* (missing supported languages / all missing).
- **List**: per row, a selection checkbox, a **status icon** (✔ validated / ✎ modified / 🗑 to remove), the
  **key**, the **default translation**, and a ✨ when the key contains AI text. The **Key / Default**
  columns are **resizable** (handle between them, width persisted in `EditorPrefs`).
- **Detail panel** (right, docked):
  ![Detail panel](Images/detail-panel.png)
  - renamable key (renaming offers to **propagate** across project references),
  - one editable field per language (✨ if AI),
  - **docked info** at the bottom: status, dates; actions **Find References**, **✨ Translate with AI**,
    **Validate** (yellow text if *Modified*, green check if *Validated*), **Move**, **Delete**.
- **Bulk action bar** (footer): on a multi-selection → **✨ Translate**, **Validate**, **Delete**, **Move to**.
- **Add-key row** pinned at the bottom: *New key* field + optional default translation.

### The `TranslationKey` property drawer

![Key drawer](Images/key-drawer.png)

Used everywhere a `TranslationKey` field is shown (including `LocalizedText`):

- **Searchable key selector** with a **None** option (empty key = no translation).
- When the search finds nothing → a **"＋ Create new key"** button in the dropdown: creates the key
  (choosing the table if several) using the **current TMP text** as the default translation.
- When the key is valid: a small **open button** on the right → opens the table in the Translation Editor
  with the key **preselected**; a **foldout** reveals **Plural Key** (with None to clear it) and
  **Parameters**.

### Missing Keys Scanner

Scans prefabs + scenes to surface `LocalizedText` whose key is missing from every table, with add / ignore
actions.

---

## AI translation

### Settings (`AITranslationSettings`)

![AI settings](Images/ai-settings.png)

Menu **Tools ▸ Localization ▸ AI Translation Settings**. The (shared/committed) asset holds:

- **Provider**: `None` (default), **Anthropic**, **Gemini**.
- **Model**: preset list per provider (searchable selector) **+ custom field**.
- **API key**: stored **per machine and per provider in `EditorPrefs`** — never committed.
- **"Test API" button**: minimal ping; for Anthropic it also reports the current rate-limit window
  (remaining requests/tokens).
- **Guidelines** (tone, formality, forbidden words…), **Project name / info**.
- **Include usage context**, **Enforce max chars**, **Glossary sample size**, **Alternatives count** (1–5).

The `IAITranslationProvider` abstraction makes it easy to add other backends (OpenAI, Ollama…).

### Translate a single key

The **✨ Translate with AI** button in the detail panel. The context sent to the model is **rich**: source
text, a **glossary** of validated translations, **usage context** (scene/prefab, the field's **hierarchy
path**, neighbouring texts under the same parent), an estimated **character budget**, guidelines and
project info.

- Results arrive with **Modified** status (for review).
- When multiple alternatives are requested, a **selector** replaces the field (the most likely one is
  applied by default); a **cross** resets it.
  ![AI alternatives](Images/ai-alternatives.png)
- **Validate** locks in the choice (selectors become plain fields) and validates.

### Bulk translate

![Bulk translation](Images/bulk-translate.png)

The **✨ Translate** button in the bulk bar translates the selected keys **sequentially** (one request per
key), filling **empty languages** with the most likely option. An **animated spinner** shows on the current
row (in place of the ✨) and the bar reads "✨ Translating x/y…". A single project scan provides the usage
context for every key.

---

## External source sync (.obd)

Links a table to an external source and keeps both in sync automatically.

### `.obd` format

A JSON array; **one object = one key**:
- `"ID"`: the key (e.g. `Common.Cancel`).
- Per language: a code field (`EN`, `FR`, `DE`, `BR`, `CN`, `TW`, `MY`…) + a `{CODE}_UpdateDate` in the
  `YY:MM:DD-HH:MM:SS` format.
- `"GD"`: reference column (ignored on import, preserved on write).

The **per-cell timestamp** drives conflict resolution ("most recent wins").

### Automatic behavior

![.obd sync](Images/obd-sync.png)

- **Detection**: as soon as an `.obd` enters/changes in the project (`AssetPostprocessor`), a **linked**
  `TranslationTableAsset` is created automatically next to the file (if missing), then filled.
- **obd → table**: on `.obd` (re)import.
- **table → obd**: on **save** of the linked table (`OnWillSaveAssets`).
- **Per-cell merge** (`TranslationSyncEngine`): union of keys/languages, the cell with the most recent
  timestamp wins, in both directions. Idempotent and convergent; an anti-loop guard
  (`TranslationSyncGuard`) prevents the reimport triggered by our own writes.
- **Write fidelity**: only cells that actually changed are rewritten (Newtonsoft); order / `GD` / `null`
  are preserved.

### Language mapping

`ObdLanguageMap` maps code ↔ `Language` (e.g. `BR`→Portuguese, `CN`→ChineseSimplified,
`TW`→ChineseTraditional, `IN`→Indonesian, `MY`→Malay…). Unmapped codes are ignored.

---

## Extension points

- **New AI provider**: implement `IAITranslationProvider`, add it to the `AITranslationProviderKind` enum,
  to the `AIModelCatalog` and to the `AIProviders.Create` switch.
- **New translation source (cloud, CSV, Google Sheets…)**: implement `ITranslationSource` (`Read`/`Write`
  of the `TranslationSnapshot` pivot model); the merge engine and the link are reused as-is. Add a trigger
  (webhook/poll) where relevant.

---

## File layout

```
Assets/unity-app/
├─ Runtime/Services/Localization/
│   ├─ Language.cs, LangUtilities.cs
│   ├─ TranslationKey.cs, Translator.cs, LocalizedText.cs
│   ├─ TranslationTable.cs, TranslationTableAsset.cs, TranslationTableCollection.cs
│   ├─ TranslationService.cs, EditorTranslationService.cs, ITranslationService.cs
│   ├─ LocalizationService.cs, ILocalizationService.cs, LocalizationSettingsAsset.cs
│   ├─ TranslationStatus.cs, ITranslationTableAsset.cs, TranslationAssetFinder.cs
├─ Editor/Services/Localization/
│   ├─ TranslationTableWindow.cs            (main editor)
│   ├─ TranslationKeyPropertyDrawer.cs, TranslationKeyReferencesWindow.cs, TranslationKeyReferenceFinder.cs
│   ├─ TranslationTableEditor.cs, TranslationTableCollectionEditor.cs, LocalizationSettingsEditor.cs
│   ├─ MissingLocalizationKeysWindow.cs, TMPContextMenu.cs
│   ├─ AITranslation.cs, AITranslationSettings.cs, AITranslationContextBuilder.cs
│   └─ Sync/
│       ├─ ITranslationSource.cs, TranslationSnapshot.cs
│       ├─ TranslationSyncEngine.cs, TranslationSyncGuard.cs, TranslationTableSaveProcessor.cs
│       └─ Obd/  (ObdFormat.cs, ObdTranslationSource.cs, ObdAssetPostprocessor.cs)
```

---

## Screenshots to provide

Drop images into **`Assets/unity-app/Documentation~/Images/`** (the `~` suffix makes Unity ignore this
folder — no asset-import clutter). Naming convention (kebab-case):

| Expected file | Content |
|---|---|
| `translation-editor.png` | Window overview (tabs + list + panel). |
| `detail-panel.png` | Key detail panel (languages + docked actions). |
| `key-drawer.png` | The property drawer on a `LocalizedText` (selector + foldout). |
| `key-drawer-create.png` | (optional) The dropdown with "＋ Create new key". |
| `missing-keys-scanner.png` | The Missing Keys Scanner window. |
| `ai-settings.png` | AI settings (provider, model, Test API). |
| `ai-alternatives.png` | Alternative selectors in the detail panel. |
| `bulk-translate.png` | Bulk bar + progress spinner on a row. |
| `obd-sync.png` | A table linked to an `.obd` (or the file + table side by side). |
| `localization-settings.png` | (optional) The `LocalizationSettings` inspector. |

The links in this document already point to these names: add the files and they will render.
