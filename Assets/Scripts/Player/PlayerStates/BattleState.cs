using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Skills;
using Cinemachine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BattleSystem;
using Player;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public enum PlayerBattleState
{
    Targeting,
    SelectingSkill
}

public class BattleState : IState
{
    private int currentTurnIndex;
    public List<UUIDCharacterInstance> allCharacters;
    public List<UUIDCharacterInstance> turnOrder;
    public List<UUIDCharacterInstance> deadCharacters;

    private bool isPlayerTurn;

    private PlayerInput playerInput;
    private bool isWaitingForPlayerInput;
    CinemachineStateDrivenCamera stateDrivenCamera;

    PlayerController playerController;
    UUIDCharacterInstance playerCharacter;

    // Targeting system
    private PlayerBattleState playerTurnState;
    private List<GameObject> enemyGameObjects;
    private List<Transform> enemyPositions;
    private List<Transform> playerPositions;
    private GameObject selectedTarget;
    private int selectedTargetIndex;

    private float scrollTimer = 0f;
    private float scrollDelay = 0.2f;
    
    private int currentArena;
    private static readonly int cam = Animator.StringToHash("arenaCam");
    private static readonly int inBattle = Animator.StringToHash("inBattle");
    private List<Transform> shuffledEnemyPositions;
    private bool isScrolling;
    private Character currentPlayerCharacter;


    public BattleState(PlayerController _playerController, List<Character> _party, List<Character> _enemies, bool _ambush, int _arena)
    {
        currentArena = _arena;
        playerController = _playerController;
        playerCharacter = new UUIDCharacterInstance(playerController.playerCharacter);

        // Initialize the turn order with the player's party and the enemies
        turnOrder = new List<UUIDCharacterInstance>();
        allCharacters = new();
        deadCharacters = new();

        // Usage
        InitializeCharacters(_party);
        InitializeCharacters(_enemies);

        // If it's an ambush, the player's party goes first
        // Otherwise, the enemies go first
        currentTurnIndex = _ambush ? 0 : _party.Count;

        SetupEventListeners();

        stateDrivenCamera = Camera.main.gameObject.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineStateDrivenCamera;
        stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, true);
    }

    private void SetupEventListeners()
    {
        foreach (var uUIDCharacter in allCharacters)
        {
            uUIDCharacter.Character.HealthManager.OnRevive.AddListener(OnCharacterRevived);
            uUIDCharacter.Character.HealthManager.OnDeath.AddListener(OnCharacterDeath);
            uUIDCharacter.Character.HealthManager.OnDamage.AddListener((healthManager, damage) =>
            {
                Debug.Log($"{uUIDCharacter.Character.DisplayName} took {damage} damage. ({healthManager.CurrentHP} HP left)");
            });
            uUIDCharacter.Character.HealthManager.OnDamageEvaded.AddListener(() =>
            {
                Debug.Log($"{uUIDCharacter.Character.DisplayName} evaded the attack!");
            });
            uUIDCharacter.Character.HealthManager.OnWeaknessEncountered.AddListener((elementType) =>
            {
                Debug.Log($"{uUIDCharacter.Character.DisplayName} is weak to {elementType}!");
                if (!AffinityLog.GetWeaknessesEncountered(uUIDCharacter.Character.name).Contains(elementType))
                {
                    AffinityLog.LogWeakness(uUIDCharacter.Character.name, elementType);
                }
            });
            uUIDCharacter.Character.HealthManager.OnStrengthEncountered.AddListener((elementType, strengthType) =>
            {
                Debug.Log($"{uUIDCharacter.Character.DisplayName} is strong against {strengthType}!");
                if (!AffinityLog.GetStrengthsEncountered(uUIDCharacter.Character.name).ContainsKey(elementType))
                {
                    AffinityLog.LogStrength(uUIDCharacter.Character.name, elementType, strengthType);
                }
            });
        }
    }

    private void InitializeCharacters(IEnumerable<Character> characters)
    {
        foreach (var character in characters)
        {
            var characterInstance = new UUIDCharacterInstance(character);
            turnOrder.Add(characterInstance);
            allCharacters.Add(characterInstance);
        }
    }

    private void OnCharacterDeath(string uuid)
    {
        // add character to dead characters
        var deadCharacter = turnOrder.FirstOrDefault(c => c.UUID == uuid);
        if (deadCharacter != null)
        {
            deadCharacters.Add(deadCharacter);
            GameObject deadCharacterObject = GameObject.Find(uuid);
            if (deadCharacterObject != null)
            {
                GameObject.Destroy(deadCharacterObject);
            }
            turnOrder.Remove(deadCharacter);

            deadCharacter.Character.HealthManager.OnRevive.RemoveListener(OnCharacterDeath);
            Debug.Log($"{deadCharacter.Character.DisplayName} has died! UUID: {deadCharacter.UUID}");
        }
        else
        {
            Debug.LogError("Dead Character not found");
        }

    }

    public void SwitchCameraState(int arenaCam)
    {
        // TODO: use hashes instead of strings
        stateDrivenCamera.m_AnimatedTarget.SetInteger(cam, arenaCam);
    }

    public void SpawnCharacters(int arena)
    {
        enemyGameObjects = new List<GameObject>();
        // Ensure the arena number is valid
        if (arena < 0 || arena >= ArenaManager.Instance.PlayerPositions.Count || arena >= ArenaManager.Instance.EnemyPositions.Count)
        {
            Debug.LogError("Invalid arena number");
            return;
        }

        // Get the spawn positions for this arena
        playerPositions = ArenaManager.Instance.PlayerPositions[arena].Positions;
        enemyPositions = ArenaManager.Instance.EnemyPositions[arena].Positions;
        shuffledEnemyPositions = Shuffle(enemyPositions);

        for (int i = 0; i < turnOrder.Count; i++)
        {
            UUIDCharacterInstance character = turnOrder[i];
            SpawnCharacter(character, playerPositions, shuffledEnemyPositions, i);
        }
    }

    private void SpawnCharacter(UUIDCharacterInstance characterInstance, List<Transform> playerPositions, List<Transform> enemyPositions, int index)
    {
        Transform spawnMarker = characterInstance.Character.IsPlayerCharacter ? playerPositions[index] :
            // Use modulo to wrap around if there are more enemies than positions
            enemyPositions[index % enemyPositions.Count];
        GameObject characterGameObject = Object.Instantiate(characterInstance.Character.prefab, spawnMarker.position, spawnMarker.rotation);
        // associate this character with a GUID
        characterGameObject.name = characterInstance.UUID;
        if (!characterInstance.Character.IsPlayerCharacter)
        {
            enemyGameObjects.Add(characterGameObject);
        }
    }

    private List<Transform> Shuffle(List<Transform> toShuffle)
    {
        List<Transform> shuffled = new(toShuffle);
        System.Random rng = new();
        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]);
        }

        return shuffled;
    }

    public void OnEnter()
    {
        // default to first state
        playerTurnState = PlayerBattleState.Targeting;

        // First arena is 1
        if (currentArena < 1)
        {
            Debug.LogError("Invalid arena number");
            return;
        }
        SwitchCameraState(currentArena);
        SpawnCharacters(currentArena - 1);

        isWaitingForPlayerInput = false;
        AffinityLog.Load();

        // TODO: entered the battle state, play animations and music
        Debug.Log("Entered battle state!");
        // log turn order by getting each character's display name
        string turnOrderString = "";
        foreach (var characterInstance in turnOrder)
        {
            var character = characterInstance.Character;
            turnOrderString += character.DisplayName + ", ";
            // log the character's strengths and weaknesses
            foreach (var strength in AffinityLog.GetStrengthsEncountered(character.name))
            {
                Debug.Log($"{character.name} is strong against {strength.Key} {strength.Value}");
            }
            foreach (var weakness in AffinityLog.GetWeaknessesEncountered(character.name))
            {
                Debug.Log($"{character.name} is weak to {weakness}");
            }
        }
        Debug.Log("Turn order: " + turnOrderString);

        playerInput = new PlayerInput();
        playerInput.Debug.Enable();
        playerInput.UI.Enable();

        playerController.PlayerUsedSkillEvent.AddListener(PlayerUseSkill);

        selectedTarget = enemyGameObjects[0];
        selectedTargetIndex = 0;

        // TODO: awful debug code, remove this later
        playerInput.Debug.Skill1.performed += _ =>
        {
            if (isPlayerTurn)
            {
                UUIDCharacterInstance target = turnOrder.FirstOrDefault(c => !c.Character.IsPlayerCharacter);
                if (target != null)
                {
                    PlayerUseSkill(playerCharacter.Character.AvailableSkills[0], target);
                }
            }
        };
        playerInput.UI.Select.started += StartScrolling;
        playerInput.UI.Select.canceled += StopScrolling;

    }
    private void StartScrolling(InputAction.CallbackContext ctx)
    {
        if (playerTurnState != PlayerBattleState.Targeting || !isWaitingForPlayerInput || enemyGameObjects.Count < 1 || isScrolling)
            return;

        isScrolling = true;
        Vector2 direction = ctx.ReadValue<Vector2>();
    
        if (direction.magnitude > 0f)
        {
            if (Math.Abs(direction.x - direction.y) > 0f)
            {
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    direction.y = 0;
                }
                else
                {
                    direction.x = 0;
                }
            }
            Scroll(direction);
        }
    }

    private async void Scroll(Vector2 direction)
    {
        while (isScrolling)
        {
            int increment = (direction.x < 0 || direction.y > 0) ? -1 : 1;

            selectedTargetIndex += increment;
            while (selectedTargetIndex >= 0 && selectedTargetIndex < enemyGameObjects.Count && enemyGameObjects[selectedTargetIndex] == null)
            {
                selectedTargetIndex += increment;
            }

            if (selectedTargetIndex < 0 || selectedTargetIndex >= enemyGameObjects.Count)
                selectedTargetIndex = (increment == 1) ? 0 : enemyGameObjects.Count - 1;

            selectedTarget = enemyGameObjects[selectedTargetIndex];

            Debug.Log("Selected target: " + selectedTarget.name);
            // TODO: move the target indicator to the selected target

            // Wait for a short delay before scrolling again
            await Task.Delay((int)(scrollDelay * 1000));
        }
    }

    private void StopScrolling(InputAction.CallbackContext ctx)
    {
        isScrolling = false;
    }

    
    private void PlayerUseSkill(BaseSkill skill, UUIDCharacterInstance target)
    {
        if (isWaitingForPlayerInput)
        {
            // Perform the selected skill on the selected target
            skill.Use(playerCharacter, target);
            isWaitingForPlayerInput = false;
            NextTurn();
        }
    }

    public void Tick()
    {
        // If there are no more player characters, it's a defeat
        if (!turnOrder.Exists(c => c.Character.IsPlayerCharacter))
        {
            Debug.Log("Defeat!");
            // TODO: switch to defeat state
            return;
        }
        // If there are no more enemy characters, it's a victory
        else if (!turnOrder.Exists(c => !c.Character.IsPlayerCharacter))
        {
            Debug.Log("Victory!");
            // TODO: switch to victory state
            return;
        }

        currentPlayerCharacter = turnOrder[currentTurnIndex].Character;
        isPlayerTurn = currentPlayerCharacter.IsPlayerCharacter;
        if (isPlayerTurn)
        {
            if (!isWaitingForPlayerInput)
            {
                Debug.Log("It's the player's turn!");
                isWaitingForPlayerInput = true;
            }

            if (playerTurnState == PlayerBattleState.Targeting)
            {
                Debug.DrawLine(Camera.main.transform.position, selectedTarget.transform.position, Color.red);
            }
            else if (playerTurnState == PlayerBattleState.SelectingSkill)
            {
                
                // TODO: logic for selecting a skill
            }

        }
        else
        {
            // TODO: Handle enemy's turn
            Debug.Log("It's the enemy's turn!");
            // TODO: wait for enemy AI input
            NextTurn();
        }
    }
    
    public void OnExit()
    {
        foreach (var characterInstance in allCharacters)
        {
            var healthManager = characterInstance.Character.HealthManager;
            // Remove all stat modifiers from all characters outside of battle
            healthManager.RemoveAllStatModifiers();
            // Remove battle state listeners
            healthManager.OnRevive.RemoveListener(OnCharacterRevived);
            healthManager.OnDeath.RemoveListener(OnCharacterDeath);
        }
    }

    private void NextTurn()
    {
        // Increment the turn index, wrapping back to the start if it reaches the end of the list
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
    }

    private void OnCharacterRevived(string uuid)
    {
        // add the character back to the turn order
        var Character = deadCharacters.FirstOrDefault(c => c.UUID == uuid);
        if (Character == null)
        {
            Debug.LogError("Character not found");
            return;
        }
        deadCharacters.Remove(Character);
        turnOrder.Add(Character);

        SpawnCharacter(Character, ArenaManager.Instance.PlayerPositions[currentArena].Positions, ArenaManager.Instance.EnemyPositions[currentArena].Positions, turnOrder.Count - 1);
    }
}