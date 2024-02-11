using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatDetection;
using BeatDetection.DataStructures;
using Player.UI;
using ScriptableObjects.Characters;
using ScriptableObjects.Characters.AiStates;
using ScriptableObjects.Characters.Health;
using ScriptableObjects.Skills;
using ScriptableObjects.Util.DataTypes;
using ScriptableObjects.Util.DataTypes.Inventory;
using ScriptableObjects.Util.DataTypes.Stats;
using ScriptableObjects.Util.SaveLoad;
using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Player.PlayerStates
{
    // Select Action -> Select Skill -> Select Target -> Attack
    public enum PlayerBattleState
    {
        Waiting,
        SelectingAction,
        SelectingSkill,
        SelectingItem,
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
        private static readonly int cam = Animator.StringToHash("arenaCam");
        private static readonly int inBattle = Animator.StringToHash("inBattle");
        private static readonly int onHitAnimation = Animator.StringToHash("Hit");
        private Canvas battleCanvas;

        private readonly int currentArena;

        private Dictionary<Character, GameObject> characterDictionary = new();

        private Character currentCharacter;
        private int currentTurnIndex;
        public List<Character> deadCharacters;
        private List<Transform> enemyPositions;

        private bool isPlayerTurn;
        private bool isScrolling;

        private Character playerOneCharacter;
        private readonly PlayerController playerController;
        private List<Character> party => playerController.party;

        private List<Character> enemies;
        private bool isAmbush;
        public List<Character> turnOrder;

        private PlayerInput playerInput;
        private List<Transform> playerPositions;

        private bool playerStartedTurn;
        private bool enemyStartedTurn;
        private bool isWaitingForTurn;

        private PlayerBattleState playerTurnState;
        private SkillListUI skillList;

        private readonly float scrollDelay = 0.2f;

        private BaseSkill selectedSkill;
        private int selectedTargetIndex;
        private List<GameObject> selectedTargets;

        private InventoryItem selectedItem;

        private bool earlyExit = false;

        public BattleState(PlayerController _playerController, List<Character> _enemies,
            bool _isAmbush = false, int arena = 1)
        {
            currentArena = arena;
            playerController = _playerController;
            enemies = _enemies;
            isAmbush = _isAmbush;
        }

        public void OnEnter()
        {
            // Initialize the turn order with the player's party and the enemies
            turnOrder = new List<Character>();
            characterDictionary = new Dictionary<Character, GameObject>();
            deadCharacters = new List<Character>();

            // Initialize the characters
            InitializeCharacters(party);
            InitializeCharacters(enemies, false);

            // if all the players from the party in the turn order are dead then exit battle
            if (!turnOrder.Exists(c => c.IsPlayerCharacter))
            {
                Debug.Log("No players are alive!");
                earlyExit = true;
                playerController.EnterExplorationState();
                return;
            }

            // If it's an ambush, the player's party goes first
            // Otherwise, the enemies go first
            currentTurnIndex = isAmbush ? 0 : party.Count;
            currentCharacter = turnOrder[currentTurnIndex];
            SetupEventListeners();


            // get the state driven camera and set the inBattle parameter to true
            playerController.stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, true);

            // get canvas by tag and set it active
            battleCanvas = GameObject.FindWithTag("BattleCanvas").GetComponent<Canvas>();
            battleCanvas.enabled = true;

            // Set up UI buttons with skills
            skillList = battleCanvas.GetComponentInChildren<SkillListUI>();
            skillList.PopulateList(currentCharacter);
            skillList.Hide();


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
            enemyStartedTurn = false;
            isWaitingForTurn = false;

            AffinityLog.Load();

            // TODO: entered the battle state, play animations and music
            Debug.Log("Entered battle state!");
            // log turn order by getting each character's display name
            string turnOrderString = "";
            foreach (Character character in turnOrder)
            {
                turnOrderString += character.DisplayName + ", ";
                // log the character's strengths and weaknesses
                foreach (KeyValuePair<ElementType, StrengthType> strength in
                         AffinityLog.GetStrengthsEncountered(character.characterID))
                    Debug.Log($"{character.characterID} is strong against {strength.Key} {strength.Value}");

                foreach (ElementType weakness in AffinityLog.GetWeaknessesEncountered(character.characterID))
                    Debug.Log($"{character.characterID} is weak to {weakness}");

                Debug.Log($"{playerOneCharacter} is level {playerOneCharacter.Stats.currentLevel}");
            }

            Debug.Log("Turn order: " + turnOrderString);

            playerInput = new PlayerInput();
            playerInput.Battle.Enable();
            playerInput.UI.Enable();

            selectedTargets = new();
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

        public void Tick()
        {
            if (earlyExit) return;
            if (currentCharacter == turnOrder[currentTurnIndex])
                currentCharacter.CharacterStateMachine.Tick();

            if (isWaitingForTurn) return;

            // If there are no more player characters, it's a defeat
            if (!turnOrder.Exists(c => c.IsPlayerCharacter))
            {
                Debug.Log("Defeat!");
                playerController.EnterExplorationState();
                // TODO: switch to defeat state
                return;
            }
            // If there are no more enemy characters, it's a victory

            if (!turnOrder.Exists(c => !c.IsPlayerCharacter))
            {
                Debug.Log("Victory!");
                playerController.EnterExplorationState();
                return;
            }

            currentCharacter = turnOrder[currentTurnIndex];
            isPlayerTurn = currentCharacter.IsPlayerCharacter;
            if (!currentCharacter.HealthManager.isAlive)
            {
                Debug.Log($"{currentCharacter.DisplayName} is dead!");
                NextTurn();
                return;
            }

            // Skip this turn if we are still guarding
            if (currentCharacter.HealthManager.isGuarding)
            {
                Debug.Log($"{currentCharacter.DisplayName} is still guarding!");
                NextTurn();
                return;
            }

            if (isPlayerTurn)
            {
                if (!playerStartedTurn)
                {
                    // Player has entered their turn
                    Debug.Log($"It's {currentCharacter.DisplayName}'s turn!");
                    playerTurnState = PlayerBattleState.SelectingAction;

                    currentCharacter.HealthManager.OnTurnStart();

                    playerStartedTurn = true;
                }
            }
            else
            {
                if (!enemyStartedTurn)
                {
                    enemyStartedTurn = true;
                    // add a listener to the AI state machine to go to the next turn when we finish thinking
                    currentCharacter.NextTurnEvent.AddListener(NextTurn);
                    currentCharacter.CharacterStateMachine.SetState(new AIThinkingState(currentCharacter, turnOrder));
                }
            }

            isWaitingForTurn = true;
        }

        public void OnExit()
        {
            foreach (Character character in characterDictionary.Keys)
            {
                HealthManager healthManager = character.HealthManager;
                RawCharacterStats currentStats = healthManager.GetCurrentStats();
                Debug.Log(healthManager.isAlive
                    ? $"{character.DisplayName} survived with {currentStats.HP} HP and {currentStats.SP} SP"
                    : $"{character.DisplayName} is dead");

                // Remove battle state listeners
                healthManager.OnRevive.RemoveAllListeners();
                healthManager.OnDeath.RemoveAllListeners();

                if (character.IsPlayerCharacter)
                {
                    // Save the character's stats
                    SaveManager.SaveStats(character.characterID, currentStats);
                }
                else if (!character.HealthManager.isAlive)
                {
                    // give the whole party XP for killed enemies
                    foreach (Character partyMember in playerController.party)
                    {
                        GivePlayersXP(partyMember, character);
                    }
                }

                // Remove all stat modifiers from all characters outside of battle
                healthManager.RemoveAllStatModifiers();
            }

            // Remove all characters from the scene
            foreach (GameObject c in characterDictionary.Values) Object.Destroy(c);
            // clear all battle values
            characterDictionary.Clear();
            deadCharacters.Clear();
            turnOrder.Clear();

            if (!earlyExit)
            {
                playerInput.Disable();
                playerInput.Dispose();
                battleCanvas.enabled = false;

                SaveManager.SaveInventory(playerController.playerInventory.inventoryItems);
                AffinityLog.Save();
                SaveManager.SaveToFile();
            }

            playerController.stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, false);
        }

        private void GivePlayersXP(Character partyMember, Character characterKilled)
        {
            partyMember.Stats.GainXP(characterKilled.Stats.XPDroppedOnDeath, (stats, leveledUp) =>
            {
                // log stats and if we leveled up
                Debug.Log($"{partyMember.DisplayName} gained {stats.XPDroppedOnDeath} XP.");
                if (leveledUp)
                    Debug.Log($"{partyMember.DisplayName} leveled up! New level: {stats.currentLevel}");
            });
        }

        private void SetupEventListeners()
        {
            playerController.SelectSkillEvent.AddListener(SelectSkill);
            playerController.SelectItemEvent.AddListener(item =>
            {
                selectedItem = item;
                SelectSkill(item.Skill);
                Debug.Log($"Selected item: {selectedItem.displayName}");
            });

            foreach (Character character in characterDictionary.Keys)
            {
                character.HealthManager.OnRevive.AddListener(OnCharacterRevived);
                character.HealthManager.OnDeath.AddListener(OnCharacterDeath);
                character.HealthManager.OnHealed.AddListener((target, healer, amount) =>
                {
                    Debug.Log(
                        $"{healer.DisplayName} healed {target.DisplayName} for {amount} HP. ({target.HealthManager.CurrentHP} HP left)");
                    UpdateHealthUIs();
                });
                character.HealthManager.OnDamage.AddListener((healthManager, elementType, damage) =>
                {
                    Debug.Log(
                        $"{character.DisplayName} took {damage} {elementType} damage. ({healthManager.CurrentHP} HP left)");
                    UpdateHealthUIs();

                    // animate damage
                    GameObject characterGameObject = characterDictionary[character];
                    if (characterGameObject == null) return;
                    Animator animator = characterGameObject.GetComponentInChildren<Animator>();
                    if (animator != null) animator.SetTrigger(onHitAnimation);
                });
                character.HealthManager.OnDamageEvaded.AddListener(() =>
                {
                    Debug.Log($"{character.DisplayName} evaded the attack!");
                });
                character.HealthManager.OnWeaknessEncountered.AddListener(elementType =>
                {
                    Debug.Log($"{character.DisplayName} is weak to {elementType}!");
                    if (!AffinityLog.GetWeaknessesEncountered(character.characterID).Contains(elementType))
                        AffinityLog.LogWeakness(character.characterID, elementType);
                });
                character.HealthManager.OnStrengthEncountered.AddListener((elementType, strengthType) =>
                {
                    Debug.Log($"{character.DisplayName} is strong against {strengthType}!");
                    if (!AffinityLog.GetStrengthsEncountered(character.characterID).ContainsKey(elementType))
                        AffinityLog.LogStrength(character.characterID, elementType, strengthType);
                });
            }
        }

        private void InitializeCharacters(IEnumerable<Character> characters, bool loadFromSave = true)
        {
            foreach (Character character in characters)
            {
                if (character == null) continue;

                Character characterInstance = Object.Instantiate(character);
                characterInstance.UUID = Guid.NewGuid().ToString();
                characterInstance.InitCharacter(loadFromSave);

                if (characterInstance.HealthManager.isAlive)
                    turnOrder.Add(characterInstance);
                characterDictionary.Add(characterInstance, null);

                if (characterInstance.characterID == playerController.playerCharacter.characterID)
                {
                    playerOneCharacter = characterInstance;
                }

                character.CharacterStateMachine.SetState(new CharacterIdleState());
            }
        }

        private void OnCharacterDeath(string uuid, HealthManager killer)
        {
            // add character to dead characters
            Character deadCharacter = turnOrder.FirstOrDefault(c => c.UUID == uuid);
            if (deadCharacter != null)
            {
                GameObject deadCharacterObject = characterDictionary[deadCharacter];
                if (deadCharacterObject != null && !deadCharacter.IsPlayerCharacter)
                {
                    Object.Destroy(deadCharacterObject);
                    characterDictionary[deadCharacter] = null;
                }

                deadCharacters.Add(deadCharacter);
                turnOrder.Remove(deadCharacter);

                deadCharacter.HealthManager.OnRevive.RemoveListener(OnCharacterDeath);
                Debug.Log($"{deadCharacter.DisplayName} has died! UUID: {deadCharacter.UUID}");
            }
            else
            {
                Debug.LogError("Dead Character not found");
            }
        }

        public void SwitchCameraState(int arenaCam)
        {
            playerController.stateDrivenCamera.m_AnimatedTarget.SetInteger(cam, arenaCam);
        }

        public void SpawnCharacters(int arena)
        {
            // Ensure the arena number is valid
            if (arena < 0 || arena >= ArenaManager.Instance.PlayerPositions.Count ||
                arena >= ArenaManager.Instance.EnemyPositions.Count)
            {
                Debug.LogError("Invalid arena number");
                return;
            }

            // Get the spawn positions for this arena
            playerPositions = ArenaManager.Instance.PlayerPositions[arena].Positions;
            enemyPositions = ArenaManager.Instance.EnemyPositions[arena].Positions;

            // Spawn characters
            for (int i = 0; i < characterDictionary.Keys.Count; i++)
            {
                Character character = characterDictionary.Keys.ElementAt(i);
                List<Transform> positions = character.IsPlayerCharacter ? playerPositions : enemyPositions;
                SpawnCharacter(character, positions, i);
            }
        }

        private void SpawnCharacter(Character characterToSpawn, List<Transform> positions, int index)
        {
            // Use the modulo operator to ensure the index is within the bounds of the list
            index %= positions.Count;

            Transform spawnMarker = positions[index];

            GameObject characterGameObject =
                Object.Instantiate(characterToSpawn.prefab, spawnMarker.position, spawnMarker.rotation);

            // Set the name to be the DisplayName plus a new UUID
            characterGameObject.name = characterToSpawn.UUID;

            // Add the GameObject to the dictionary with the corresponding Character instance as the key
            characterDictionary[characterToSpawn] = characterGameObject;
        }

        private void PlayerUseSkill(BeatResult result = BeatResult.Good)
        {
            if (result != BeatResult.Missed && result != BeatResult.Mashed)
            {
                int index = 0;
                foreach (var selectedTarget in selectedTargets)
                {
                    var targets = GetTargetableObjects();
                    Character targetCharacter = targets.FirstOrDefault(x => x.Value == selectedTarget).Key;

                    // only use cost on first target
                    selectedSkill.Use(playerOneCharacter, targetCharacter, index > 0);
                    index++;
                }
            }
            else
                Debug.Log("Missed the attack!");

            playerController.UseSelectedSkill(result, selectedItem);

            // if we have used an item, reset the selected item
            selectedItem = null;

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

                // go back to selecting action if we are selecting the attack skill
                if (selectedSkill == playerOneCharacter.weapon.Skill)
                {
                    playerTurnState = PlayerBattleState.SelectingAction;
                    skillList.Hide();
                }
                else
                {
                    if (selectedItem != null)
                    {
                        skillList.PopulateList(playerController.playerInventory);
                        playerTurnState = PlayerBattleState.SelectingItem;
                    }
                    else
                    {
                        skillList.PopulateList(currentCharacter);
                        playerTurnState = PlayerBattleState.SelectingSkill;
                    }

                    skillList.Show();
                }

                selectedSkill = null;
                selectedItem = null;
            }
            else if (isPlayerTurn && playerTurnState == PlayerBattleState.SelectingSkill ||
                     playerTurnState == PlayerBattleState.SelectingItem && playerStartedTurn)
            {
                playerTurnState = PlayerBattleState.SelectingAction;
                skillList.Hide();
            }
        }

        private void SelectAction(BattleActionType actionType)
        {
            if (playerTurnState != PlayerBattleState.SelectingAction) return;
            // disable the submit button
            playerInput.UI.Submit.Disable();

            if (actionType == BattleActionType.Attack)
            {
                SelectSkill(playerOneCharacter.weapon.Skill);
            }
            else if (actionType == BattleActionType.Skill)
            {
                UpdateHealthUIs();
                skillList.PopulateList(currentCharacter);
                skillList.Show();
                playerTurnState = PlayerBattleState.SelectingSkill;
            }
            else if (actionType == BattleActionType.Item)
            {
                UpdateHealthUIs();
                skillList.PopulateList(playerController.playerInventory);

                if (playerController.playerInventory.IsEmpty())
                {
                    Debug.Log("No items to use!");
                    GoBack();
                    return;
                }

                skillList.Show();
                playerTurnState = PlayerBattleState.SelectingItem;
            }
            else if (actionType == BattleActionType.Defend)
            {
                currentCharacter.HealthManager.StartGuarding(1);
                playerTurnState = PlayerBattleState.Waiting;
                NextTurn();
            }
        }

        private bool IsTargetable(Character character)
        {
            // Check if the selected skill is a revival skill
            bool isRevivalSkill = selectedSkill.skillType == SkillType.Revive;

            // If the selected skill is a revival skill, the character should be dead to be targetable
            // If it's not, the character should be alive to be targetable
            bool isAlive = character.HealthManager.isAlive;
            if ((isRevivalSkill && isAlive) || (!isRevivalSkill && !isAlive)) return false;

            // Check if the character is an ally and if the skill can target allies
            return character.IsPlayerCharacter && selectedSkill.CanTargetAllies ||
                   // Check if the character is an enemy and if the skill can target enemies
                   !character.IsPlayerCharacter && selectedSkill.CanTargetEnemies;
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

            var targetableObjects = GetTargetableObjects().Values.ToList();
            if (skill.TargetsAll)
            {
                selectedTargets.AddRange(targetableObjects);
            }
            else if (targetableObjects.Count > 0)
            {
                selectedTargets.Add(targetableObjects[0]);
            }

            UpdateHealthUIs();
            playerTurnState = PlayerBattleState.Targeting;
            playerInput.UI.Submit.Enable();
        }

        private void UpdateHealthUIs()
        {
            foreach (KeyValuePair<Character, GameObject> pair in characterDictionary)
            {
                Character character = pair.Key;
                GameObject target = pair.Value;

                CharacterHealthUI characterHealthUI = target.GetComponentInChildren<CharacterHealthUI>();
                if (!characterHealthUI) continue;

                if (selectedTargets.Contains(target))
                {
                    characterHealthUI.ShowHealth(character.HealthManager.CurrentHP);
                }
                else
                {
                    characterHealthUI.HideHealth();
                }
            }
        }

        private void TargetSelected(InputAction.CallbackContext ctx)
        {
            if (!isPlayerTurn || playerTurnState != PlayerBattleState.Targeting || !playerStartedTurn) return;

            if (selectedSkill.costsHP)
            {
                if (currentCharacter.HealthManager.CurrentHP < selectedSkill.cost)
                {
                    Debug.Log("Not enough HP!");
                    GoBack();
                    return;
                }
            }
            else if (currentCharacter.HealthManager.CurrentSP < selectedSkill.cost)
            {
                Debug.Log("Not enough SP!");
                GoBack();
                return;
            }

            // Cannot use revival skills if there are no dead party members
            if (selectedSkill.skillType == SkillType.Revive && !deadCharacters.Intersect(playerController.party).Any())
            {
                Debug.Log("No dead characters to revive!");
                GoBack();
                return;
            }

            playerTurnState = PlayerBattleState.Attacking;

            void UseSkillAction()
            {
                playerController.simpleQTE.StartQTE(4, PlayerUseSkill);
            }

            MyAudioManager.Instance.beatScheduler.RunOnNextBeat(UseSkillAction);
        }

        private void StartScrolling(InputAction.CallbackContext ctx)
        {
            if (playerTurnState != PlayerBattleState.Targeting || !playerStartedTurn ||
                characterDictionary.Count < 1 || isScrolling || selectedSkill.TargetsAll)
                return;

            isScrolling = true;
            Vector2 direction = ctx.ReadValue<Vector2>();

            if (direction.magnitude > 0.5f)
            {
                if (Math.Abs(direction.x - direction.y) > 0.5f)
                {
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                        direction.y = 0;
                    else
                        direction.x = 0;
                }

                Scroll(direction);
            }
        }

        private Dictionary<Character, GameObject> GetTargetableObjects()
        {
            return characterDictionary.Where(pair =>
                pair.Value != null && IsTargetable(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private async void Scroll(Vector2 direction)
        {
            Dictionary<Character, GameObject> targetableObjects = GetTargetableObjects();

            while (isScrolling)
            {
                int increment = direction.x < 0 || direction.y > 0 ? -1 : 1;

                // If the direction is up or down, adjust the increment to move vertically
                if (direction.y != 0)
                {
                    increment *= enemyPositions.Count;
                }

                selectedTargetIndex += increment;
                while (selectedTargetIndex >= 0 && selectedTargetIndex < targetableObjects.Count &&
                       targetableObjects.Values.ElementAt(selectedTargetIndex) == null)
                    selectedTargetIndex += increment;

                if (selectedTargetIndex < 0 || selectedTargetIndex >= targetableObjects.Count)
                    selectedTargetIndex = increment == 1 ? 0 : targetableObjects.Count - 1;

                if (selectedTargetIndex >= 0 && selectedTargetIndex < targetableObjects.Values.Count)
                {
                    selectedTargets[0] = targetableObjects.Values.ElementAt(selectedTargetIndex);
                }

                // Debug.Log("Selected target: " + selectedTargets[0].name);
                UpdateHealthUIs();

                // Wait for a short delay before scrolling again
                await Task.Delay((int)(scrollDelay * 1000));
            }
        }

        private void StopScrolling(InputAction.CallbackContext ctx)
        {
            isScrolling = false;
        }

        private void NextTurn()
        {
            currentCharacter.CharacterStateMachine.SetState(new CharacterIdleState());

            // Increment the turn index, wrapping back to the start if it reaches the end of the list
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            playerStartedTurn = false;
            enemyStartedTurn = false;
            isWaitingForTurn = false;

            selectedTargets.Clear();
        }

        private void OnCharacterRevived(string uuid, HealthManager reviver)
        {
            // add the character back to the turn order
            Character character = deadCharacters.FirstOrDefault(c => c.UUID == uuid);
            if (character == null)
            {
                Debug.LogError("Character not found");
                return;
            }

            deadCharacters.Remove(character);
            turnOrder.Add(character);

            Debug.Log($"{character.DisplayName} has been revived by {reviver}!");
        }
    }
}