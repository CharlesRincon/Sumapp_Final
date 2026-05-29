# Project Guidelines

## Code Style

- Use C# 9.0 with .NET Standard 2.1
- Follow SOLID principles: single responsibility, dependency inversion, interface segregation
- Namespace convention: `Networking.Services`, `Networking.Managers`, `Networking.Models`, `Networking.Events`, `Networking.UI`
- Exemplar files: [FusionNetworkService.cs](Assets/Project/Scripts/Networking/Services/FusionNetworkService.cs), [GameManager.cs](Assets/Project/Scripts/Networking/Managers/GameManager.cs)

## Architecture

Event-driven service layer with Photon Fusion 2.x networking:

- Services handle network callbacks and raise events
- Managers consume events and modify networked state
- Networked properties auto-sync via IL Weaver
- Host authority for critical state changes
  Use existing docs instead of duplicating implementation details:
- [README.md](Assets/Project/Scripts/Networking/README.md) for system overview
- [ARCHITECTURE.md](Assets/Project/Scripts/Networking/ARCHITECTURE.md) for dependency graphs and design rationale
- [SETUP_GUIDE.md](Assets/Project/Scripts/Networking/SETUP_GUIDE.md) for integration workflow
- [MINIGAME_SETUP.md](Assets/Project/Scripts/Networking/MINIGAME_SETUP.md) for scene setup specifics

## Build and Test

- Use Unity Editor 6000.3.11f1 (version-sensitive project)
- No CLI build/test pipeline is defined; use Unity Editor workflows
- Solution: Sumapp_Final.sln with 7 projects
- Validate networking by running 2+ editor instances (Host + Client) in the same room
- Main scenes: LobbyScene, Minigame

## Conventions

- Events: Centralized in NetworkEventDefinitions.asset, accessed via `NetworkEventDefinitions.Instance.OnXxxEvent`
- Networked state: Always in PlayerSessionData or Managers, flagged `[Networked]`
- Host logic: Gate with `if (Object.HasStateAuthority)`
- Scene names: Hardcoded strings "LobbyScene", "Minigame"
- Singletons: `GameManager.Instance`, `FusionNetworkService.LocalRunner`
- Asset locations: Resources/Events/, Resources/Characters/, Resources/Prefabs/, Assets/Project/Art/
- Avoid common pitfalls: - Do not spawn or mutate network-critical state before NetworkRunner is initialized - Do not update host-authoritative state from non-authority clients - Do not remove `[Networked]` from synchronized properties - Do not rename scenes without updating hardcoded references - Do not leave event references unassigned (prefer centralized definitions)
  See [QUICK_REFERENCE.md](Assets/Project/Scripts/Networking/QUICK_REFERENCE.md) for common tasks and debug snippets.
