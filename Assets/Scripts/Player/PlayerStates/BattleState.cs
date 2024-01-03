using UnityEngine;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;
using System;
using BattleSystem.ScriptableObjects.Skills;

public class BattleState : IState
{
    private int currentTurnIndex;
    public List<Character> allCharacters;
    public List<Character> turnOrder;

    private bool isPlayerTurn;
    private PlayerController playerController;

    private PlayerInput playerInput;
    private bool isWaitingForPlayerInput;

    public BattleState(PlayerController _playerController, List<Character> party, List<Character> enemies, bool ambush)
    {
        playerController = _playerController;
        // Initialize the turn order with the player's party and the enemies
        turnOrder = new List<Character>(party);
        turnOrder.AddRange(enemies);
        // copy the turn order to allCharacters
        allCharacters = new List<Character>(turnOrder);

        // If it's an ambush, the player's party goes first
        // Otherwise, the enemies go first
        currentTurnIndex = ambush ? 0 : party.Count;

        foreach (var character in turnOrder)
        {
            character.Init();
            character.HealthManager.OnRevive.AddListener(OnCharacterRevived);
        }
    }

    public void OnEnter()
    {
        isWaitingForPlayerInput = false;
        AffinityLog.Load();

        // TODO: entered the battle state, play animations and music
        Debug.Log("Entered battle state!");
        // log turn order by getting each character's display name
        string turnOrderString = "";
        foreach (var character in turnOrder)
        {
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

        playerController.OnSkillAndTargetSelected.AddListener(OnSkillAndTargetSelected);


        // TODO: awful debug code, remove this later
        playerInput.Debug.KillAllEnemies.performed += _ =>
        {
            foreach (var character in turnOrder)
            {
                if (!character.IsPlayerCharacter)
                {
                    character.HealthManager.Die();
                }
            }
        };
        playerInput.Debug.Skill1.performed += _ =>
        {
            if (isPlayerTurn)
            {
                OnSkillAndTargetSelected(playerController.playerCharacter.AvailableSkills[0], turnOrder[currentTurnIndex + 1]);
            }
        };
        playerInput.Debug.Skill2.performed += _ =>
        {
            if (isPlayerTurn)
            {
                OnSkillAndTargetSelected(playerController.playerCharacter.AvailableSkills[1], turnOrder[currentTurnIndex + 1]);
            }
        };
        playerInput.Debug.Skill3.performed += _ =>
        {
            if (isPlayerTurn)
            {
                OnSkillAndTargetSelected(playerController.playerCharacter.AvailableSkills[2], turnOrder[currentTurnIndex + 1]);
            }
        };

    }

    private void OnSkillAndTargetSelected(BaseSkill skill, Character target)
    {
        if (isWaitingForPlayerInput)
        {
            // Perform the selected skill on the selected target
            skill.Use(playerController.playerCharacter, target);
            isWaitingForPlayerInput = false;
            NextTurn();
        }
    }

    public void Tick()
    {
        // Remove dead characters from the turn order
        turnOrder.RemoveAll(character => !character.HealthManager.isAlive);

        // If there are no more player characters, it's a defeat
        if (!turnOrder.Exists(character => character.IsPlayerCharacter))
        {
            Debug.Log("Defeat!");
            // TODO: switch to defeat state
            return;
        }
        // If there are no more enemy characters, it's a victory
        else if (!turnOrder.Exists(character => !character.IsPlayerCharacter))
        {
            Debug.Log("Victory!");
            // TODO: switch to victory state
            return;
        }

        isPlayerTurn = turnOrder[currentTurnIndex].IsPlayerCharacter;
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
        // Remove all stat modifiers from all characters outside of battle
        foreach (var character in allCharacters)
        {
            character.HealthManager.RemoveAllStatModifiers();
        }
    }

    private void NextTurn()
    {
        // Increment the turn index, wrapping back to the start if it reaches the end of the list
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
    }

    private void OnCharacterRevived(Character character)
    {
        turnOrder.Add(character);
    }
}