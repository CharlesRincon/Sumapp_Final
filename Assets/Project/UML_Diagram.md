# Diagrama UML del Proyecto

Este archivo contiene la representación visual de las relaciones entre las clases principales utilizando sintaxis de **Mermaid**.

```mermaid
classDiagram
    class GameManager {
        <<Singleton>>
        +GameState CurrentState
        +List~PlayerSessionData~ Players
        +AdvanceTurn()
        +ResolveTile()
    }

    class PlayerSessionData {
        <<NetworkBehaviour>>
        +String Nick
        +Int WaterAmount
        +Int MoneyAmount
        +Int MinigameClickCount
        +RPC_RequestMove()
    }

    class WeatherMinigameManager {
        <<NetworkBehaviour>>
        +RemainingTime float
        +CurrentCardIndex int
        +RPC_SubmitAnswer()
    }

    class WeatherUIController {
        +UpdateScoreUI()
        +UpdateCardUI()
        +OnAnswerResult()
    }

    class WeatherCardDefinition {
        <<ScriptableObject>>
        +Sprite Illustration
        +String Description
        +Bool IsElNino
    }

    class TriviaUIController {
        +ShowQuestion()
        +OnAnswerClicked()
    }

    class TriviaDatabase {
        <<ScriptableObject>>
        +List~TriviaQuestion~ Questions
        +GetRandom()
    }

    GameManager "1" -- "*" PlayerSessionData : manages
    WeatherMinigameManager "1" -- "*" WeatherCardDefinition : uses
    WeatherUIController "1" ..> WeatherMinigameManager : observes
    WeatherUIController "1" ..> WeatherCardDefinition : displays
    TriviaUIController "1" ..> TriviaDatabase : fetches
    PlayerSessionData "1" -- "1" GameManager : reports to
```

## Instrucciones de Visualización
Puedes visualizar este diagrama directamente en:
1. **GitHub/GitLab:** Renderizan automáticamente los bloques de código `mermaid`.
2. **VS Code:** Con la extensión "Markdown Preview Mermaid Support".
3. **Mermaid Live Editor:** Copia el código en [mermaid.live](https://mermaid.live/).
