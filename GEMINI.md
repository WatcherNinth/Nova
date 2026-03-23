# Nova & NinthsLab Project Overview

This project consists of the **Nova Framework** (the base engine) and the **NinthsLab Project** (the primary workspace containing custom game logic, AI systems, and investigation mechanics).

## NinthsLab Core Systems (Primary Workspace)

The `Assets/NinthsLab` directory contains the main implementation of the game's unique mechanics.

### 1. Interrogation Logic Engine (`LogicEngine.LevelLogic`)
Located in `Assets/NinthsLab/Scripts/InterrorgationLevelScript/Logic/`. This is the orchestrator of the gameplay loop.
- **InterrorgationLevelManager**: The central controller that manages level loading and connects logic sub-managers.
- **PlayerMindMapManager**: Tracks the player's progress, including discovered nodes, unlocked entities, and used templates.
- **GamePhaseManager**: Manages the progression of investigation phases, handling locking/unlocking and completion criteria.
- **NodeLogicManager**: Executes the logic for "proving" nodes based on dependencies and handling mutual exclusivity (Mutex).
- **GameScopeManager**: Manages the "focus" of the investigation, tracking the dependency depth of the player's current line of questioning.

### 2. AI System (`AIEngine`)
Located in `Assets/NinthsLab/Scripts/AISystem/`. Integrates LLMs to analyze player input.
- **AIClient**: Handles raw network communication with AI APIs (e.g., Alibaba DashScope).
- **AIManager**: Parallelizes requests to multiple models (Referee and Discovery).
- **AIRefereeModel**: Determines if player input matches specific logic nodes/arguments.
- **AIDiscoveryModel**: Identifies if player input hints at new topics or clues not yet explicitly unlocked.

### 3. Custom Dialogue System (`DialogueSystem`)
Located in `Assets/NinthsLab/Scripts/InterrorgationLevelScript/Dialogue/`.
- **NovaScriptParser**: Parses custom scenario scripts containing text and embedded commands like `<| show('CharId', 'Variant') |>`.
- **DialogueRuntimeManager**: Manages the playback of dialogue batches and execution of accompanying visual commands.
- **StandardDialogueUI**: The frontend implementation featuring typewriter effects, avatar management, and log panels.

### 4. Node & Condition System (`LogicEngine`)
Located in `Assets/NinthsLab/Scripts/NodeDataSystem/`.
- **LevelGraphData**: The master data structure representing a level's entire logic tree, phases, and entities.
- **ConditionEvaluator**: A JSON-based logic gate system supporting complex `and`/`or`/`any_of` operations for node unlocking.
- **Validation System**: A robust self-checking framework (`IValidatable`) that ensures level JSONs are logically consistent before running.

### 5. Mid-Layer & Communication
Located in `Assets/NinthsLab/Scripts/InterrorgationLevelScript/MidLayer/`.
- **GameEventDispatcher / UIEventDispatcher**: Static event buses that decouple the deep logic layers from the UI presentation.
- **Game_UI_Coordinator**: Bridges high-level game logic events to the Dialogue and UI systems.

## Data & Assets
- **JSON Scenarios**: Levels are defined in complex JSON files parsed by `LevelGraphParser`.
- **Deduction Data**: ScriptableObject-based definitions for `Topics` and `Proofs` used in specialized deduction mini-games.
- **Inspiration System**: A hierarchical data structure (`InspirationDataType`) for the player's "brainstorming" UI.

## Building and Running
- Follow the standard Nova framework instructions (Unity 2020-2022, `Main.unity` scene).
- **Testing**: Use the specialized testers in `Assets/NinthsLab/Scripts/AISystem/` (e.g., `AIFullFlowDebug`, `AIRealIntegrationTester`) to simulate gameplay and AI responses without needing a full build.

## Development Conventions
- **Events First**: Use `GameEventDispatcher` for logic-to-UI communication. Avoid direct references between managers and UI components.
- **Validation**: When modifying level JSONs, use the `LevelTestManager` in the editor to run batch validations.
- **Logic Placement**: Game-wide mechanics belong in `NinthsLab`, while generic VN features (saving, settings) are handled by the `Nova` framework.
