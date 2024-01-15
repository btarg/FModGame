using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleSystem;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Skills;
using BeatDetection;
using BeatDetection.DataStructures;
using Cinemachine;
using Player.SaveLoad;
using Player.UI;
using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Player.PlayerStates
{
    // Select Action -> Select Skill -> Select Target -> Attack
    public enum PlayerBattleState
    {
        Waiting,
        SelectingAction,
        SelectingSkill,
        Targeting,
        Attacking
    }
    
    public enum BattleActionType
    {
        Attack,
        Skill,
        Item,
        Defend
    }

    public class BattleState : IState
    {
        private int currentTurnIndex;
        public List<UUIDCharacterInstance> allCharacters;
        public List<UUIDCharacterInstance> turnOrder;
        public List<UUIDCharacterInstance> deadCharacters;

        private bool isPlayerTurn;

        private PlayerInput playerInput;
        private bool playerStartedTurn;
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
        private SkillListUI skillList;
        private static readonly int onHitAnimation = Animator.StringToHash("Hit");

        public BattleState(PlayerController _playerController, List<Character> _party, List<Character> _enemies, bool isAmbush = false, int _arena = 1)
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
            currentTurnIndex = isAmbush ? 0 : _party.Count;

            SetupEventListeners();

            stateDrivenCamera = Camera.main.gameObject.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineStateDrivenCamera;
            if (stateDrivenCamera != null) stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, true);
        
            // get canvas by tag and set it active
            battleCanvas = GameObject.FindWithTag("BattleCanvas").GetComponent<Canvas>();
            battleCanvas.enabled = true;
        
            // Set up UI buttons with skills
            skillList = battleCanvas.GetComponentInChildren<SkillListUI>();
            skillList.PopulateList(playerCharacter);
            skillList.Hide();

        }

        private void SetupEventListeners()
        {
            playerController.SelectSkillEvent.AddListener(SelectSkill);
            
            foreach (var uUIDCharacter in allCharacters)
            {
                uUIDCharacter.Character.HealthManager.OnRevive.AddListener(OnCharacterRevived);
                uUIDCharacter.Character.HealthManager.OnDeath.AddListener(OnCharacterDeath);
                uUIDCharacter.Character.HealthManager.OnDamage.AddListener((healthManager, elementType, damage) =>
                {
                    Debug.Log($"{uUIDCharacter.Character.DisplayName} took {damage} {elementType} damage. ({healthManager.CurrentHP} HP left)");
                    UpdateHealthUIs();
                    
                    // animate damage
                    GameObject characterGameObject = characterGameObjects.FirstOrDefault(x => x.Key == uUIDCharacter).Value;
                    if (characterGameObject == null) return;
                    Animator animator = characterGameObject.GetComponentInChildren<Animator>();
                    if (animator != null) animator.SetTrigger(onHitAnimation);
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
                    Object.Destroy(deadCharacterObject);
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

            playerStartedTurn = false;
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
            playerInput.Battle.Enable();
            playerInput.UI.Enable();

            selectedTargets = new List<GameObject> { characterGameObjects.Values.FirstOrDefault() };
            selectedTargetIndex = 0;
            
            playerInput.UI.Select.started += StartScrolling;
            playerInput.UI.Select.canceled += StopScrolling;
            playerInput.UI.Submit.performed += TargetSelected;
            playerInput.UI.Cancel.performed += _ => GoBack();

            playerInput.Battle.Attack.performed += _ => SelectAction(BattleActionType.Attack);
            playerInput.Battle.Skill.performed += _ => SelectAction(BattleActionType.Skill);
            playerInput.Battle.Item.performed += _ => SelectAction(BattleActionType.Item);
            playerInput.Battle.Guard.performed += _ => SelectAction(BattleActionType.Defend);
        }

        private void PlayerUseSkill(BeatResult result = BeatResult.Good)
        {
            if (result != BeatResult.Missed && result != BeatResult.Mashed)
            {
                foreach (GameObject selectedTarget in selectedTargets)
                {
                    var targetCharacter = characterGameObjects.FirstOrDefault(x => x.Value == selectedTarget).Key;
                    selectedSkill.Use(playerCharacter, targetCharacter);
                }
            }
            else
            {
                Debug.Log("Missed the attack!");
            }
            playerController.UseSelectedSkill(result);

            playerStartedTurn = false;
            playerTurnState = PlayerBattleState.Waiting;
            selectedTargets.Clear();
            UpdateHealthUIs();
            NextTurn();
        }

        private void GoBack()
        {
            if (isPlayerTurn && playerTurnState == PlayerBattleState.Targeting && playerStartedTurn)
            {
                selectedTargets.Clear();
                UpdateHealthUIs();

                if (selectedSkill == playerCharacter.Character.attackSkill)
                {
                    // go back to selecting action if we are selecting the attack skill
                    playerTurnState = PlayerBattleState.SelectingAction;
                }
                else
                {
                    playerTurnState = PlayerBattleState.SelectingSkill;
                    skillList.PopulateList(playerCharacter);
                    skillList.Show();
                }
                selectedSkill = null;
                
            } else if (isPlayerTurn && playerTurnState == PlayerBattleState.SelectingSkill && playerStartedTurn)
            {
                playerTurnState = PlayerBattleState.SelectingAction;
                skillList.Hide();
            }
        }

        private void SelectAction(BattleActionType actionType)
        {
            if (playerTurnState != PlayerBattleState.SelectingAction) return;

            if (actionType == BattleActionType.Attack)
            {
                SelectSkill(playerCharacter.Character.attackSkill);
            }
            else if (actionType == BattleActionType.Skill)
            {
                UpdateHealthUIs();
                skillList.PopulateList(playerCharacter);
                skillList.Show();
                playerTurnState = PlayerBattleState.SelectingSkill;
            }
            else if (actionType == BattleActionType.Item)
            {
                // TODO: implement items
                // get the first item in the inventory and use it
                var inventoryItems = playerController.playerInventory.inventoryItems;
                if (inventoryItems.Count > 0)
                {
                    var item = inventoryItems.First();
                    playerController.playerInventory.UseItem(playerController, item.Key);
                }
                else
                {
                    Debug.Log("No items in inventory!");
                }
            }
            else if (actionType == BattleActionType.Defend)
            {
                playerCharacter.Character.HealthManager.StartGuarding(1);
                playerTurnState = PlayerBattleState.Waiting;
                NextTurn();
            }
        }
        
        private void SelectSkill(BaseSkill skill)
        {
            if (skill == null)
            {
                Debug.LogError("Skill is null");
                return;
            }
            
            skillList.Hide();
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

        private void UpdateHealthUIs()
        {
            foreach (var target in characterGameObjects)
            {
                var characterHealthUI = target.Value.GetComponentInChildren<CharacterHealthUI>();
                if (!characterHealthUI) continue;
                
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
            if (isPlayerTurn && playerTurnState == PlayerBattleState.Targeting && playerStartedTurn)
            {
                if (selectedSkill.costsHP)
                {
                    if (playerCharacter.Character.HealthManager.CurrentHP < selectedSkill.cost)
                    {
                        Debug.Log("Not enough HP!");
                        GoBack();
                        return;
                    }
                }
                else if (playerCharacter.Character.HealthManager.CurrentSP < selectedSkill.cost)
                {
                    Debug.Log("Not enough SP!");
                    GoBack();
                    return;
                }
                
                playerTurnState = PlayerBattleState.Attacking;
                void UseSkillAction() => playerController.simpleQTE.StartQTE(4, PlayerUseSkill);
                MyAudioManager.Instance.beatScheduler.RunOnNextBeat(UseSkillAction);
            }
        }

        private void StartScrolling(InputAction.CallbackContext ctx)
        {
            if (playerTurnState != PlayerBattleState.Targeting || !playerStartedTurn || characterGameObjects.Count < 1 || isScrolling || selectedSkill.TargetsAll)
                return;
        
            isScrolling = true;
            Vector2 direction = ctx.ReadValue<Vector2>();
    
            if (direction.magnitude > 0.5f)
            {
                if (Math.Abs(direction.x - direction.y) > 0.5f)
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
            Debug.Log(playerTurnState);
            
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
                if (!playerStartedTurn)
                {
                    // Player has entered their turn
                    Debug.Log("It's the player's turn!");
                    playerStartedTurn = true;
                    playerTurnState = PlayerBattleState.SelectingAction;
                    currentPlayerCharacter.HealthManager.OnTurnStart();
                }

                // TODO: this is incredibly fucking stupid
                // disable or enable the submit button depending on state to prevent selecting a skill immediately attacking
                if (playerTurnState == PlayerBattleState.Targeting)
                {
                    playerInput.UI.Submit.Enable();
                    playerInput.UI.Select.Enable();
                }
                else
                {
                    playerInput.UI.Submit.Disable();
                    playerInput.UI.Select.Disable();
                }
            }
            else
            {
                // TODO: Handle enemy's turn
                Debug.Log("It's the enemy's turn!");
                // TODO: wait for enemy AI input
                currentPlayerCharacter.HealthManager.OnTurnStart();
                // for now we move to next turn straight away
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
                // Remove this character's game object
                foreach (GameObject c in characterGameObjects.Values)
                {
                    Object.Destroy(c);
                }

                if (characterInstance.Character.IsPlayerCharacter)
                {
                    // save the player characters' stats for out of battle
                    playerController.playerCharacter = characterInstance.Character;
                }
            }
            characterGameObjects.Clear();
            allCharacters.Clear();
            deadCharacters.Clear();
            turnOrder.Clear();
            playerInput.Disable();
            playerInput.Dispose();
            battleCanvas.enabled = false;
            stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, false);
        }

        private void NextTurn()
        {
            // Increment the turn index, wrapping back to the start if it reaches the end of the list
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            playerStartedTurn = false;
            selectedTargets.Clear();
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