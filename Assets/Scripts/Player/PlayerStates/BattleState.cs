using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatDetection;
using BeatDetection.DataStructures;
using Player.UI;
using ScriptableObjects.Characters;
using ScriptableObjects.Characters.Health;
using ScriptableObjects.Skills;
using ScriptableObjects.Util.DataTypes;
using ScriptableObjects.Util.DataTypes.Inventory;
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
        public List<Character> allCharacters;
        private Canvas battleCanvas;
        private Dictionary<Character, GameObject> characterGameObjects;

        private readonly int currentArena;

        private Character currentPlayerCharacter;
        private int currentTurnIndex;
        public List<Character> deadCharacters;
        private List<Transform> enemyPositions;

        private bool isPlayerTurn;
        private bool isScrolling;
        private readonly Character playerOneCharacter;

        private readonly PlayerController playerController;

        private PlayerInput playerInput;
        private List<Transform> playerPositions;
        private bool playerStartedTurn;

        // Targeting system
        private PlayerBattleState playerTurnState;

        private readonly float scrollDelay = 0.2f;

        private BaseSkill selectedSkill;
        private int selectedTargetIndex;
        private List<GameObject> selectedTargets;
        private List<Transform> shuffledEnemyPositions;
        private SkillListUI skillList;
        public List<Character> turnOrder;
        private InventoryItem selectedItem;

        public BattleState(PlayerController _playerController, List<Character> party, List<Character> enemies,
            bool isAmbush = false, int arena = 1)
        {
            currentArena = arena;
            playerController = _playerController;
            playerOneCharacter = Object.Instantiate(playerController.playerCharacter);

            // Initialize the turn order with the player's party and the enemies
            turnOrder = new List<Character>();
            allCharacters = new List<Character>();
            deadCharacters = new List<Character>();

            // Usage
            InitializeCharacters(party);
            InitializeCharacters(enemies);

            // If it's an ambush, the player's party goes first
            // Otherwise, the enemies go first
            currentTurnIndex = isAmbush ? 0 : party.Count;
            currentPlayerCharacter = turnOrder[currentTurnIndex];
            SetupEventListeners();
        }

        public void OnEnter()
        {
            // get the state driven camera and set the inBattle parameter to true
            playerController.stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, true);

            // get canvas by tag and set it active
            battleCanvas = GameObject.FindWithTag("BattleCanvas").GetComponent<Canvas>();
            battleCanvas.enabled = true;

            // Set up UI buttons with skills
            skillList = battleCanvas.GetComponentInChildren<SkillListUI>();
            skillList.PopulateList(currentPlayerCharacter);
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

        public void Tick()
        {
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

            currentPlayerCharacter = turnOrder[currentTurnIndex];
            isPlayerTurn = currentPlayerCharacter.IsPlayerCharacter;
            if (isPlayerTurn)
            {
                if (!playerStartedTurn)
                {
                    // Player has entered their turn
                    Debug.Log($"It's {currentPlayerCharacter.DisplayName}'s turn!");
                    playerStartedTurn = true;
                    playerTurnState = PlayerBattleState.SelectingAction;
                    currentPlayerCharacter.HealthManager.OnTurnStart();

                    // Skip this turn if we are still guarding
                    if (currentPlayerCharacter.HealthManager.isGuarding)
                    {
                        Debug.Log($"{currentPlayerCharacter.DisplayName} is still guarding!");
                        NextTurn();
                    }
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
            foreach (Character character in allCharacters)
            {
                HealthManager healthManager = character.HealthManager;
                var currentStats = healthManager.GetCurrentStats();
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
            foreach (GameObject c in characterGameObjects.Values) Object.Destroy(c);
            // clear all battle values
            characterGameObjects.Clear();
            allCharacters.Clear();
            deadCharacters.Clear();
            turnOrder.Clear();
            playerInput.Disable();
            playerInput.Dispose();
            battleCanvas.enabled = false;
            playerController.stateDrivenCamera.m_AnimatedTarget.SetBool(inBattle, false);

            SaveManager.SaveInventory(playerController.playerInventory.inventoryItems);

            AffinityLog.Save();
            SaveManager.SaveToFile();
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

            foreach (Character character in allCharacters)
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
                    GameObject characterGameObject = characterGameObjects.FirstOrDefault(x => x.Key == character).Value;
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

        private void InitializeCharacters(IEnumerable<Character> characters)
        {
            foreach (Character character in characters)
            {
                Character characterInstance = Object.Instantiate(character);
                turnOrder.Add(characterInstance);
                allCharacters.Add(characterInstance);
            }
        }

        private void OnCharacterDeath(string uuid, HealthManager killer)
        {
            // add character to dead characters
            Character deadCharacter = turnOrder.FirstOrDefault(c => c.UUID == uuid);
            if (deadCharacter != null)
            {
                GameObject deadCharacterObject = GameObject.Find(uuid);
                if (deadCharacterObject != null && !deadCharacter.IsPlayerCharacter)
                {
                    Object.Destroy(deadCharacterObject);
                    characterGameObjects.Remove(deadCharacter);
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
            characterGameObjects = new Dictionary<Character, GameObject>();
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
            shuffledEnemyPositions = Shuffle(enemyPositions);

            for (int i = 0; i < turnOrder.Count; i++)
            {
                Character character = turnOrder[i];
                SpawnCharacter(character, playerPositions, shuffledEnemyPositions, i);
            }
        }

        private void SpawnCharacter(Character characterToSpawn, List<Transform> _playerPositions,
            List<Transform> _enemyPositions, int index)
        {
            Transform spawnMarker = characterToSpawn.IsPlayerCharacter
                ? _playerPositions[index]
                :
                // Use modulo to wrap around if there are more enemies than positions
                _enemyPositions[index % enemyPositions.Count];
            GameObject characterGameObject =
                Object.Instantiate(characterToSpawn.prefab, spawnMarker.position, spawnMarker.rotation);
            // associate this character with a GUID
            characterGameObject.name = characterToSpawn.UUID;
            characterGameObjects.Add(characterToSpawn, characterGameObject);
        }

        private List<Transform> Shuffle(List<Transform> toShuffle)
        {
            List<Transform> shuffled = new(toShuffle);
            Random rng = new();
            int n = shuffled.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]);
            }

            return shuffled;
        }

        private void PlayerUseSkill(BeatResult result = BeatResult.Good)
        {
            if (result != BeatResult.Missed && result != BeatResult.Mashed)
                for (int index = 0; index < selectedTargets.Count; index++)
                {
                    GameObject selectedTarget = selectedTargets[index];
                    Character targetCharacter = characterGameObjects.FirstOrDefault(x => x.Value == selectedTarget).Key;
                    // only use cost on first target
                    selectedSkill.Use(playerOneCharacter, targetCharacter, index > 0);
                }
            else
                Debug.Log("Missed the attack!");

            playerController.UseSelectedSkill(result, selectedItem);

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

                if (selectedSkill == playerOneCharacter.weapon.skill)
                {
                    // go back to selecting action if we are selecting the attack skill
                    playerTurnState = PlayerBattleState.SelectingAction;
                }
                else
                {
                    playerTurnState = PlayerBattleState.SelectingSkill;
                    skillList.PopulateList(currentPlayerCharacter);
                    skillList.Show();
                }

                selectedSkill = null;
            }
            else if (isPlayerTurn && playerTurnState == PlayerBattleState.SelectingSkill && playerStartedTurn)
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
                SelectSkill(playerOneCharacter.weapon.skill);
            }
            else if (actionType == BattleActionType.Skill)
            {
                UpdateHealthUIs();
                skillList.PopulateList(currentPlayerCharacter);
                skillList.Show();
                playerTurnState = PlayerBattleState.SelectingSkill;
            }
            else if (actionType == BattleActionType.Item)
            {
                var playerInventory = playerController.playerInventory;

                // TODO: replace this with an actual item selection menu

                if (playerInventory.inventoryItems.Count < 1)
                {
                    GoBack();
                    return;
                }

                var item = playerInventory.inventoryItems.First();
                if (item.Key == null) return;

                int count = playerInventory.inventoryItems[item.Key];
                if (count < 1)
                {
                    GoBack();
                    return;
                }

                playerController.playerInventory.UseItem(playerController, item.Key);
                selectedItem = item.Key;
            }
            else if (actionType == BattleActionType.Defend)
            {
                currentPlayerCharacter.HealthManager.StartGuarding(2);
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
            if ((isRevivalSkill && !isAlive) || (!isRevivalSkill && isAlive))
            {
                return (selectedSkill.CanTargetAllies && character.IsPlayerCharacter) ||
                       (selectedSkill.CanTargetEnemies && !character.IsPlayerCharacter);
            }

            return false;
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
                selectedTargets.AddRange(characterGameObjects.Where(c => IsTargetable(c.Key)).Select(c => c.Value));
            }
            else
            {
                // if we are only targeting one character, we insert it at 0 and don't have any other list items
                GameObject target = characterGameObjects.FirstOrDefault(c => IsTargetable(c.Key)).Value;
                selectedTargets.Insert(0, target);
            }
            
            UpdateHealthUIs();
            playerTurnState = PlayerBattleState.Targeting;
            playerInput.UI.Submit.Enable();
        }

        private void UpdateHealthUIs()
        {
            foreach (KeyValuePair<Character, GameObject> target in characterGameObjects)
            {
                CharacterHealthUI characterHealthUI = target.Value.GetComponentInChildren<CharacterHealthUI>();
                if (!characterHealthUI) continue;

                if (selectedTargets.Contains(target.Value))
                    characterHealthUI.ShowHealth(target.Key.HealthManager.CurrentHP);
                else
                    characterHealthUI.HideHealth();
            }
        }

        private void TargetSelected(InputAction.CallbackContext ctx)
        {
            if (!isPlayerTurn || playerTurnState != PlayerBattleState.Targeting || !playerStartedTurn) return;

            if (selectedSkill.costsHP)
            {
                if (currentPlayerCharacter.HealthManager.CurrentHP < selectedSkill.cost)
                {
                    Debug.Log("Not enough HP!");
                    GoBack();
                    return;
                }
            }
            else if (currentPlayerCharacter.HealthManager.CurrentSP < selectedSkill.cost)
            {
                Debug.Log("Not enough SP!");
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
                characterGameObjects.Count < 1 || isScrolling || selectedSkill.TargetsAll)
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
            Dictionary<Character, GameObject> targetableObjects = new();

            // Check if the selected skill is a revival skill
            bool isRevivalSkill = selectedSkill.skillType == SkillType.Revive;

            foreach (KeyValuePair<Character, GameObject> characterGameObject in characterGameObjects)
            {
                // If the skill is a revival skill, we can target dead characters
                if (isRevivalSkill && deadCharacters.Contains(characterGameObject.Key))
                {
                    targetableObjects.Add(characterGameObject.Key, characterGameObject.Value);
                }
                // If the skill is not a revival skill, we can only target alive characters
                else if (!isRevivalSkill && allCharacters.Contains(characterGameObject.Key))
                {
                    if ((!selectedSkill.CanTargetEnemies && !characterGameObject.Key.IsPlayerCharacter) ||
                        (!selectedSkill.CanTargetAllies && characterGameObject.Key.IsPlayerCharacter))
                    {
                        continue;
                    }

                    targetableObjects.Add(characterGameObject.Key, characterGameObject.Value);
                }
            }

            return targetableObjects;
        }

        private async void Scroll(Vector2 direction)
        {
            Dictionary<Character, GameObject> targetableObjects = GetTargetableObjects();

            while (isScrolling)
            {
                int increment = direction.x < 0 || direction.y > 0 ? -1 : 1;

                selectedTargetIndex += increment;
                while (selectedTargetIndex >= 0 && selectedTargetIndex < targetableObjects.Count &&
                       targetableObjects.Values.ElementAt(selectedTargetIndex) == null)
                    selectedTargetIndex += increment;

                if (selectedTargetIndex < 0 || selectedTargetIndex >= targetableObjects.Count)
                    selectedTargetIndex = increment == 1 ? 0 : targetableObjects.Count - 1;

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

        private void NextTurn()
        {
            // Increment the turn index, wrapping back to the start if it reaches the end of the list
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            playerStartedTurn = false;
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

            // SpawnCharacter(character, ArenaManager.Instance.PlayerPositions[currentArena].Positions, ArenaManager.Instance.EnemyPositions[currentArena].Positions, turnOrder.Count - 1);
        }
    }
}