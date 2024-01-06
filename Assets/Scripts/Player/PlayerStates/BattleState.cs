using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleSystem;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Skills;
using BattleSystem.UI;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Player.PlayerStates
{
    public enum PlayerBattleState
    {
        Waiting,
        Targeting,
        SelectingSkill,
        Attacking
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
        private Dictionary<UUIDCharacterInstance, GameObject> characterGameObjects;
        private List<Transform> enemyPositions;
        private List<Transform> playerPositions;
        private List<GameObject> selectedTargets;
        private int selectedTargetIndex;
    
        private float scrollDelay = 0.2f;
    
        private int currentArena;
        private static readonly int cam = Animator.StringToHash("arenaCam");
        private static readonly int inBattle = Animator.StringToHash("inBattle");
        private List<Transform> shuffledEnemyPositions;
        private bool isScrolling;
        private Character currentPlayerCharacter;
        private BaseSkill selectedSkill;
        private Canvas battleCanvas;
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
            if (stateDrivenCamera != null) stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, true);
        
            // get canvas by tag and set it active
            battleCanvas = GameObject.FindWithTag("BattleCanvas").GetComponent<Canvas>();
            battleCanvas.enabled = true;
        
            // Set up UI buttons with skills
            // TODO: move this to a separate script
            var buttons = battleCanvas.GetComponentsInChildren<Button>();
            var texts = battleCanvas.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                var text = texts[i];
                var skill = playerCharacter.Character.AvailableSkills[i];
                button.onClick.AddListener(() => SelectSkill(skill));
                text.text = skill.name;
            }
        
        }

        private void SetupEventListeners()
        {
            playerController.SelectSkillEvent.AddListener(SelectSkill);
            playerController.PlayerUsedSkillEvent.AddListener(PlayerUseSkill);
        
            foreach (var uUIDCharacter in allCharacters)
            {
                uUIDCharacter.Character.HealthManager.OnRevive.AddListener(OnCharacterRevived);
                uUIDCharacter.Character.HealthManager.OnDeath.AddListener(OnCharacterDeath);
                uUIDCharacter.Character.HealthManager.OnDamage.AddListener((healthManager, damage) =>
                {
                    Debug.Log($"{uUIDCharacter.Character.DisplayName} took {damage} damage. ({healthManager.CurrentHP} HP left)");
                    UpdateHealthUIs();
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
                characterGameObjects.Remove(deadCharacter);

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
            characterGameObjects = new();
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
            characterGameObjects.Add(characterInstance, characterGameObject);
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
            playerTurnState = PlayerBattleState.Waiting;

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
            selectedTargets = new List<GameObject> { characterGameObjects.Values.FirstOrDefault() };
            selectedTargetIndex = 0;
            
            playerInput.UI.Select.started += StartScrolling;
            playerInput.UI.Select.canceled += StopScrolling;
            playerInput.UI.Submit.performed += TargetSelected;
            playerInput.UI.Cancel.performed += GoBack;

        }

        private void PlayerUseSkill()
        {
            foreach (GameObject selectedTarget in selectedTargets)
            {
                var targetCharacter = characterGameObjects.FirstOrDefault(x => x.Value == selectedTarget).Key;
                selectedSkill.Use(playerCharacter, targetCharacter);
            }
            isWaitingForPlayerInput = false;
            playerTurnState = PlayerBattleState.Waiting;
            selectedTargets.Clear();
            UpdateHealthUIs();
            NextTurn();
        }

        private void GoBack(InputAction.CallbackContext obj)
        {
            if (isPlayerTurn && playerTurnState == PlayerBattleState.Targeting && isWaitingForPlayerInput)
            {
                selectedTargets.Clear();
                UpdateHealthUIs();
                selectedSkill = null;
                playerTurnState = PlayerBattleState.SelectingSkill;
            }
        }

        private void SelectSkill(BaseSkill skill)
        {
            if (playerTurnState == PlayerBattleState.SelectingSkill)
            {
                selectedSkill = skill;
                selectedTargets.Clear();
                if (selectedSkill.TargetsAll)
                {
                    var targetList = new List<GameObject>();
                    if (selectedSkill.CanTargetEnemies)
                    {
                        // add all enemies to list of targets
                        targetList.AddRange(characterGameObjects.Where(c => !c.Key.Character.IsPlayerCharacter).Select(c => c.Value));
                    }
                    if (selectedSkill.CanTargetAllies)
                    {
                        // add all allies to list of targets
                        targetList.AddRange(characterGameObjects.Where(c => c.Key.Character.IsPlayerCharacter).Select(c => c.Value));
                    }

                    selectedTargets.AddRange(targetList);
                }
            
                // set selected target to the first enemy in the list if the current selected skill targets enemies
                else if (selectedSkill.CanTargetEnemies)
                {
                    selectedTargets.Insert(0, characterGameObjects.FirstOrDefault(c => !c.Key.Character.IsPlayerCharacter).Value);
                }
                else
                {
                    // otherwise set it to the player character
                    selectedTargets.Insert(0, characterGameObjects[playerCharacter]);
                }

                UpdateHealthUIs();
                
                playerTurnState = PlayerBattleState.Targeting;
            
            }
        }

        private void UpdateHealthUIs()
        {
            foreach (var target in characterGameObjects)
            {
                var characterHealthUI = target.Value.GetComponentInChildren<CharacterHealthUI>();
                if (characterHealthUI == null) continue;
                
                if (selectedTargets.Contains(target.Value))
                {
                    characterHealthUI.ShowHealth(target.Key.Character.HealthManager.CurrentHP);
                }
                else
                {
                    characterHealthUI.HideHealth();
                }
            }
        }

        private void TargetSelected(InputAction.CallbackContext ctx)
        {
            if (isPlayerTurn && playerTurnState == PlayerBattleState.Targeting && isWaitingForPlayerInput)
            {
                // TODO: PlayerUseSkill should be called from an event elsewhere once we have completed our animation and QTE/minigame
                PlayerUseSkill();
            }

        }

        private void StartScrolling(InputAction.CallbackContext ctx)
        {
            if (playerTurnState != PlayerBattleState.Targeting || !isWaitingForPlayerInput || characterGameObjects.Count < 1 || isScrolling || selectedSkill.TargetsAll)
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

        private Dictionary<UUIDCharacterInstance, GameObject> GetTargetableObjects()
        {
            var targetableObjects = new Dictionary<UUIDCharacterInstance, GameObject>(characterGameObjects);
            if (!selectedSkill.CanTargetEnemies)
            {
                foreach (var characterGameObject in characterGameObjects)
                {
                    if (!characterGameObject.Key.Character.IsPlayerCharacter)
                    {
                        targetableObjects.Remove(characterGameObject.Key);
                    }
                }
            }
            if (!selectedSkill.CanTargetAllies)
            {
                foreach (var characterGameObject in characterGameObjects)
                {
                    if (characterGameObject.Key.Character.IsPlayerCharacter)
                    {
                        targetableObjects.Remove(characterGameObject.Key);
                    }
                }
            }

            return targetableObjects;
        }
    
        private async void Scroll(Vector2 direction)
        {
            var targetableObjects = GetTargetableObjects();
        
            while (isScrolling)
            {
                int increment = (direction.x < 0 || direction.y > 0) ? -1 : 1;

                selectedTargetIndex += increment;
                while (selectedTargetIndex >= 0 && selectedTargetIndex < targetableObjects.Count && targetableObjects.Values.ElementAt(selectedTargetIndex) == null)
                {
                    selectedTargetIndex += increment;
                }

                if (selectedTargetIndex < 0 || selectedTargetIndex >= targetableObjects.Count)
                    selectedTargetIndex = (increment == 1) ? 0 : targetableObjects.Count - 1;

                selectedTargets[0] = targetableObjects.Values.ElementAt(selectedTargetIndex);
                Debug.Log("Selected target: " + selectedTargets[0].name);
                UpdateHealthUIs();
            
                // Wait for a short delay before scrolling again
                await Task.Delay((int)(scrollDelay * 1000));
            }
        }

        private void StopScrolling(InputAction.CallbackContext ctx)
        {
            isScrolling = false;
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
                    // Player has entered their turn
                    Debug.Log("It's the player's turn!");
                    isWaitingForPlayerInput = true;
                    playerTurnState = PlayerBattleState.SelectingSkill;
                    UpdateHealthUIs();
                }

                if (playerTurnState == PlayerBattleState.Targeting)
                {
                    if (selectedTargets != null)
                    {
                        foreach (var target in selectedTargets)
                        {
                            Debug.DrawLine(Camera.main.transform.position, target.transform.position, Color.red);
                        }
                    
                    }

                    if (battleCanvas != null)
                        battleCanvas.enabled = false;
                }
                else if (playerTurnState == PlayerBattleState.SelectingSkill)
                {
                    // TODO: logic for selecting a skill
                    if (battleCanvas != null)
                        battleCanvas.enabled = true;
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
                foreach (GameObject c in characterGameObjects.Values)
                {
                    Object.Destroy(c);
                }
                characterGameObjects.Clear();
            }
            allCharacters.Clear();
            deadCharacters.Clear();
            turnOrder.Clear();
            playerInput.Disable();
            playerInput.Dispose();
            playerController.PlayerUsedSkillEvent.RemoveListener(PlayerUseSkill);
            battleCanvas.enabled = false;
            stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, false);
        }

        private void NextTurn()
        {
            // Increment the turn index, wrapping back to the start if it reaches the end of the list
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        }

        private void OnCharacterRevived(string uuid)
        {
            // add the character back to the turn order
            var character = deadCharacters.FirstOrDefault(c => c.UUID == uuid);
            if (character == null)
            {
                Debug.LogError("Character not found");
                return;
            }
            deadCharacters.Remove(character);
            turnOrder.Add(character);

            SpawnCharacter(character, ArenaManager.Instance.PlayerPositions[currentArena].Positions, ArenaManager.Instance.EnemyPositions[currentArena].Positions, turnOrder.Count - 1);
        }
    }
}