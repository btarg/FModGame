using UnityEngine;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Skills;
using Cinemachine;
using System;
using System.Linq;
using UnityEditor;

public class BattleState : IState
{
    [Header("Turn Order")]
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

    private int currentArena;

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

        foreach (var uUIDCharacter in allCharacters)
        {
            uUIDCharacter.Character.HealthManager.OnRevive.AddListener(OnCharacterRevived);
            uUIDCharacter.Character.HealthManager.OnDeath.AddListener(OnCharacterDeath);
        }
        stateDrivenCamera = Camera.main.gameObject.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineStateDrivenCamera;
        stateDrivenCamera.m_AnimatedTarget.SetBool("inBattle", true);
    }

    private void InitializeCharacters(IEnumerable<Character> characters)
    {
        foreach (var character in characters)
        {
            character.Init();
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
        stateDrivenCamera.m_AnimatedTarget.SetInteger("arenaCam", arenaCam);
    }

    public void SpawnCharacters(int arena)
    {
        // Ensure the arena number is valid
        if (arena < 0 || arena >= ArenaManager.Instance.PlayerPositions.Count || arena >= ArenaManager.Instance.EnemyPositions.Count)
        {
            Debug.LogError("Invalid arena number");
            return;
        }

        // Get the spawn positions for this arena
        List<Transform> playerPositions = ArenaManager.Instance.PlayerPositions[arena].Positions;
        List<Transform> enemyPositions = ArenaManager.Instance.EnemyPositions[arena].Positions;
        Shuffle(enemyPositions);

        for (int i = 0; i < turnOrder.Count; i++)
        {
            UUIDCharacterInstance character = turnOrder[i];
            SpawnCharacter(character, playerPositions, enemyPositions, i);

        }
    }

    private void SpawnCharacter(UUIDCharacterInstance characterInstance, List<Transform> playerPositions, List<Transform> enemyPositions, int index)
    {
        Transform spawnMarker;
        if (characterInstance.Character.IsPlayerCharacter)
        {
            spawnMarker = playerPositions[index];
        }
        else
        {
            // Use modulo to wrap around if there are more enemies than positions
            spawnMarker = enemyPositions[index % enemyPositions.Count];
        }
        GameObject instance = GameObject.Instantiate(characterInstance.Character.prefab, spawnMarker.position, spawnMarker.rotation);
        // associate this character with a GUID
        instance.name = characterInstance.UUID;
    }

    private void Shuffle(List<Transform> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Transform value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void OnEnter()
    {
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

        playerController.OnSkillAndTargetSelected.AddListener(PlayerUseSkill);


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

        isPlayerTurn = turnOrder[currentTurnIndex].Character.IsPlayerCharacter;
        if (isPlayerTurn)
        {
            if (!isWaitingForPlayerInput)
            {
                Debug.Log("It's the player's turn!");
                isWaitingForPlayerInput = true;
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