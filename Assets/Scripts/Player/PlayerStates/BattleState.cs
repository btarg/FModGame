using UnityEngine;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;

public class BattleState : IState
{
    private int currentTurnIndex;
    public List<Character> turnOrder;

    private bool isPlayerTurn;
    private PlayerController playerController;

    private PlayerInput playerInput;

    public BattleState(PlayerController _playerController, List<Character> party, List<Character> enemies, bool ambush)
    {
        playerController = _playerController;
        // Initialize the turn order with the player's party and the enemies
        turnOrder = new List<Character>(party);
        turnOrder.AddRange(enemies);

        // If it's an ambush, the player's party goes first
        // Otherwise, the enemies go first
        currentTurnIndex = ambush ? 0 : party.Count;

        foreach (var character in turnOrder)
        {
            character.Init();
        }
    }

    public void OnEnter()
    {
        // TODO: entered the battle state, play animations and music
        Debug.Log("Entered battle state!");
        // log turn order by getting each character's display name
        string turnOrderString = "";
        foreach (var character in turnOrder)
        {
            turnOrderString += character.DisplayName + ", ";
        }
        Debug.Log("Turn order: " + turnOrderString);


        playerInput = new PlayerInput();
        playerInput.Debug.Enable();
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
    }

    public void Tick()
    {
        // Remove dead characters from the turn order
        turnOrder.RemoveAll(character => !character.HealthManager.isAlive);

        // If there are no more player characters, it's a defeat
        if (!turnOrder.Exists(character => character.IsPlayerCharacter))
        {
            Debug.Log("Defeat!");
            return;
        }
        // If there are no more enemy characters, it's a victory
        else if (!turnOrder.Exists(character => !character.IsPlayerCharacter))
        {
            Debug.Log("Victory!");
            return;
        }

        isPlayerTurn = turnOrder[currentTurnIndex].IsPlayerCharacter;
        if (isPlayerTurn)
        {
            // TODO: Handle player's turn
        }
        else
        {
            // TODO: Handle enemy's turn
        }
        NextTurn(); // Move to the next turn after handling the current turn
    }

    public void OnExit()
    {
        playerController.EnterExplorationState();
    }

    private void NextTurn()
    {
        // Increment the turn index, wrapping back to the start if it reaches the end of the list
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
    }
}