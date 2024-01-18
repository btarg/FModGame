using ScriptableObjects.Characters;
using ScriptableObjects.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace Player.UI
{
    public class SkillListUI : MonoBehaviour
    {
        public VisualTreeAsset listElementTemplate;
        private bool isShowing;
        private UIDocument listDocument;
        private TemplateContainer listElementTemplateInstance;

        private PlayerController playerController;

        // Start is called before the first frame update
        private void Start()
        {
            listDocument = GetComponent<UIDocument>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerController = player.GetComponent<PlayerController>();
        }

        public void PopulateList(Character playerCharacter)
        {
            listDocument.rootVisualElement.Clear();

            foreach (BaseSkill skill in playerCharacter.AvailableSkills)
            {
                TemplateContainer instance = listElementTemplate.Instantiate();
                instance.name = skill.name;
                listDocument.rootVisualElement.Add(instance);

                string costType = skill.costsHP ? "HP" : "SP";
                Button skillButton = instance.Q<Button>("SkillButton");
                skillButton.SetEnabled((skill.costsHP && playerCharacter.HealthManager.CurrentHP >= skill.cost) ||
                                       (!skill.costsHP && playerCharacter.HealthManager.CurrentSP >= skill.cost));
                skillButton.text = $"{skill.skillName} ({skill.cost} {costType})";
                skillButton.clicked += () =>
                {
                    if (isShowing)
                        playerController.SelectSkill(skill);
                };
            }
        }


        public void Hide()
        {
            listDocument.rootVisualElement.style.display = DisplayStyle.None;
            isShowing = false;
        }

        public void Show()
        {
            listDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            // focus the first button
            Button firstButton = listDocument.rootVisualElement.Q<Button>();
            firstButton.Focus();
            isShowing = true;
        }
    }
}