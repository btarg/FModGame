using BattleSystem.ScriptableObjects.Characters;
using UnityEngine;
using UnityEngine.UIElements;

namespace Player.UI
{
    public class SkillListUI : MonoBehaviour
    {
        private UIDocument listDocument;
        public VisualTreeAsset listElementTemplate;
        private TemplateContainer listElementTemplateInstance;
        
        private PlayerController playerController;
        private UUIDCharacterInstance currentPlayerCharacter;
        
        // Start is called before the first frame update
        void Start()
        {
            listDocument = GetComponent<UIDocument>();
            var player = GameObject.FindGameObjectWithTag("Player");
            playerController = player.GetComponent<PlayerController>();
        }
    
        public void PopulateList(UUIDCharacterInstance playerCharacter)
        {
            listDocument.rootVisualElement.Clear();
            currentPlayerCharacter = playerCharacter;

            foreach (var skill in playerCharacter.Character.AvailableSkills)
            {
                var instance = listElementTemplate.Instantiate();
                instance.name = skill.name;
                listDocument.rootVisualElement.Add(instance);

                string costType = skill.costsHP ? "HP" : "SP";
                var skillButton = instance.Q<Button>("SkillButton");
                skillButton.SetEnabled((skill.costsHP && playerCharacter.Character.HealthManager.CurrentHP >= skill.cost) ||
                                       (!skill.costsHP && playerCharacter.Character.HealthManager.CurrentSP >= skill.cost));
                skillButton.text = $"{skill.skillName} ({skill.cost} {costType})";
                skillButton.clicked += () => playerController.SelectSkill(skill);
            }
        }


        public void Hide()
        {
            listDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
        public void Show()
        {
            listDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            // focus the first button
            var firstButton = listDocument.rootVisualElement.Q<Button>();
            PopulateList(currentPlayerCharacter);
            firstButton.Focus();
        }
    }

}