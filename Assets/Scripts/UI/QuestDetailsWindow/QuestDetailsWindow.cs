﻿using Rondo.Generic.Utility;
using Rondo.QuestSim.Heroes;
using Rondo.QuestSim.Inventory;
using Rondo.QuestSim.Quests;
using Rondo.QuestSim.Quests.Rewards;
using Rondo.QuestSim.Reputation;
using Rondo.QuestSim.UI.ActiveQuests;
using Rondo.QuestSim.UI.General;
using Rondo.QuestSim.UI.Reputation;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rondo.QuestSim.UI.PostedQuests {

    public class QuestDetailsWindow : MonoBehaviourSingleton<QuestDetailsWindow> {

        public TextMeshProUGUI questTitle;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI successText;
        public Button closeButton;
        public Button skipButton;
        public Button cancelButton;
        public Button acceptButton;
        public Button completeButton;
        public Button postButton;

        [Header("Mode objects")]
        [Header("Heroes")]
        public Button heroSelectionInstance;
        public RectTransform heroSelectedInstance;

        [Header("Item rewards")]
        public TMP_Dropdown heroRewardItemDropdown;
        public GameItemInstanceUI heroRewardItemInstance;
        public GameItemInstanceUI handlerRewardItemInstance;

        [Header("Gold rewards")]
        public TMP_InputField heroGoldRewardInput;
        public TextMeshProUGUI heroGoldRewardText;
        public TextMeshProUGUI handlerGoldReward;

        [Header("Additional rewards")]
        public TextMeshProUGUI handlerAdditionalReward;

        public Action OnWindowClose = delegate { };

        private QuestInstance m_CurrentQuest;
        private List<HeroInstance> m_AvailableHeroes = new List<HeroInstance>();
        private QuestMode m_WindowMode = QuestMode.ACTIVE_REVIEW;
        private List<GameObject> m_ItemsToDelete = new List<GameObject>();
        private HeroInstance m_SelectedHero = null;
        private int m_SelectedItemReward = 0;

        void Awake() {
            Instance = this;
        }

        void Start() {
            closeButton.onClick.AddListener(() => {
                CloseWindow();
            });

            skipButton.onClick.AddListener(() => {
                CloseWindow();
            });

            cancelButton.onClick.AddListener(() => {
                m_CurrentQuest.RefundQuestRewards(true, true);
                QuestManager.PostedQuests.Remove(m_CurrentQuest);
                QuestManager.Requests.Add(m_CurrentQuest);
                QuestsWindow.Instance.Reload();
                CloseWindow();
            });

            acceptButton.onClick.AddListener(() => {
                m_SelectedHero.HeroState = HeroStates.ON_QUEST;

                QuestManager.PostedQuests.Remove(m_CurrentQuest);
                QuestManager.ActiveQuests.Add(m_CurrentQuest, m_SelectedHero);
                QuestsWindow.Instance.Reload();
                CloseWindow();
            });

            completeButton.onClick.AddListener(() => {
                HeroManager.SetHeroToIdle(QuestManager.ActiveQuests[m_CurrentQuest]);
                QuestManager.ActiveQuests.Remove(m_CurrentQuest);
                QuestsWindow.Instance.Reload();
                CloseWindow();
            });

            postButton.onClick.AddListener(() => {
                QuestManager.Requests.Remove(m_CurrentQuest);
                QuestManager.PostedQuests.Add(m_CurrentQuest);
                QuestsWindow.Instance.Reload();

                InventoryManager.Gold -= m_CurrentQuest.GoldReward.GoldCount;
                if (m_SelectedItemReward != 0) {
                    GameItem item = InventoryManager.OwnedItems[m_SelectedItemReward - 1];
                    m_CurrentQuest.ItemReward = new QuestRewardItem(item);
                    InventoryManager.MoveItemToReserved(item);
                }

                CloseWindow();
            });

            heroGoldRewardInput.onValueChanged.AddListener((value) => {
                if (string.IsNullOrEmpty(value)) {
                    value = "0";
                }
                int goldValue = int.Parse(value);
                m_CurrentQuest.GoldReward.GoldCount = goldValue;

                CheckPostButtonStatus();
            });

            heroGoldRewardInput.onDeselect.AddListener((value) => {
                if (string.IsNullOrEmpty(value)) {
                    heroGoldRewardInput.text = "0";
                }
            });

            heroRewardItemDropdown.onValueChanged.AddListener((value) => {
                m_SelectedItemReward = value;

                if (m_SelectedItemReward != 0) {
                    heroRewardItemDropdown.GetComponent<GameItemPopupCaller>().associatedItem = InventoryManager.OwnedItems[m_SelectedItemReward - 1];
                } else {
                    heroRewardItemDropdown.GetComponent<GameItemPopupCaller>().associatedItem = null;
                }
            });

            heroSelectionInstance.onClick.AddListener(() => {
                ReputationUI.Instance.gameObject.SetActive(true);
            });

            Instance = this;
            gameObject.SetActive(false);
        }

        private void CloseWindow() {
            m_CurrentQuest = null;

            gameObject.SetActive(false);
            ReputationUI.Instance.ResetAvailableHeroes();
            OnWindowClose();
        }

        public void OpenWindow(QuestInstance quest, QuestMode mode) {
            m_WindowMode = mode;
            Reset();
            if (quest == m_CurrentQuest) {
                gameObject.SetActive(!gameObject.activeSelf);
            } else {
                gameObject.SetActive(true);
            }
            m_CurrentQuest = quest;

            switch (mode) {
                case QuestMode.SETUP:
                    closeButton.gameObject.SetActive(true);
                    skipButton.gameObject.SetActive(false);
                    cancelButton.gameObject.SetActive(false);
                    acceptButton.gameObject.SetActive(false);
                    completeButton.gameObject.SetActive(false);
                    postButton.gameObject.SetActive(true);

                    RefreshItemRewardDropdown();
                    SetNoHero();
                    break;
                case QuestMode.HERO_SELECT:
                    closeButton.gameObject.SetActive(false);
                    skipButton.gameObject.SetActive(true);
                    cancelButton.gameObject.SetActive(true);
                    acceptButton.gameObject.SetActive(true);
                    completeButton.gameObject.SetActive(false);
                    postButton.gameObject.SetActive(false);

                    FindPotentialHeroes();
                    break;
                case QuestMode.POSTED_REVIEW:
                    closeButton.gameObject.SetActive(true);
                    skipButton.gameObject.SetActive(false);
                    cancelButton.gameObject.SetActive(true);
                    acceptButton.gameObject.SetActive(false);
                    completeButton.gameObject.SetActive(false);
                    postButton.gameObject.SetActive(false);

                    SetNoHero();
                    break;
                case QuestMode.ACTIVE_REVIEW:
                    closeButton.gameObject.SetActive(true);
                    skipButton.gameObject.SetActive(false);
                    cancelButton.gameObject.SetActive(true);
                    acceptButton.gameObject.SetActive(false);
                    completeButton.gameObject.SetActive(false);
                    postButton.gameObject.SetActive(false);

                    FindActiveHero();
                    break;
                case QuestMode.COMPLETED:
                    closeButton.gameObject.SetActive(false);
                    skipButton.gameObject.SetActive(false);
                    cancelButton.gameObject.SetActive(false);
                    acceptButton.gameObject.SetActive(false);
                    completeButton.gameObject.SetActive(true);
                    postButton.gameObject.SetActive(false);

                    FindActiveHero();
                    break;
            }

            heroRewardItemDropdown.gameObject.SetActive(mode == QuestMode.SETUP);
            heroRewardItemInstance.gameObject.SetActive(mode != QuestMode.SETUP);

            heroGoldRewardInput.gameObject.SetActive(mode == QuestMode.SETUP);
            heroGoldRewardText.gameObject.SetActive(mode != QuestMode.SETUP);

            heroSelectionInstance.gameObject.SetActive(mode == QuestMode.HERO_SELECT);
            heroSelectedInstance.gameObject.SetActive(mode != QuestMode.HERO_SELECT);

            questTitle.text = "<b><u>" + m_CurrentQuest.QuestSource.RequestTitle + "</u>\n<size=18>" + m_CurrentQuest.QuestTypeDisplay + " - </b><i>" + m_CurrentQuest.DurationInDays + " Day" + (m_CurrentQuest.DurationInDays > 1 ? "s" : "") + " duration</i></size>";
            difficultyText.text = ""+ m_CurrentQuest.DifficultyLevel;
            heroGoldRewardText.text = ""+m_CurrentQuest.GoldReward.GoldCount;
            heroRewardItemInstance.SetItem(m_CurrentQuest.ItemReward);
            if(heroGoldRewardInput.gameObject.activeSelf) heroGoldRewardInput.text = "0";

            handlerGoldReward.text = m_CurrentQuest.HandlerGoldRewardEstimate;
            handlerRewardItemInstance.SetItem(m_CurrentQuest.HandlerItemReward != null ? m_CurrentQuest.HandlerItemReward.Item : (GameItem)null);
            handlerAdditionalReward.text = m_CurrentQuest.AdditionalReward != null ? m_CurrentQuest.AdditionalReward.DisplayValue : "-";

        }

        private void FindPotentialHeroes() {
            foreach (HeroInstance hero in HeroManager.GetAvailableHeroes()) {
                if (m_CurrentQuest.WouldHeroAccept(hero)) {
                    m_AvailableHeroes.Add(hero);
                }
            }

            ReputationUI.Instance.SetAvailableHeroes(m_AvailableHeroes, SetSelectedHero);
            successText.text = "-";
        }

        private void SetSelectedHero(HeroInstance hero) {
            m_SelectedHero = hero;
            heroSelectionInstance.GetComponentInChildren<ReputationHeroInstanceUI>(true).ApplyHero(m_SelectedHero);

            successText.text = GetSuccessRateForPercentage(m_CurrentQuest.GetHeroSuccessRate(hero));
        }

        private void FindActiveHero() {
            HeroInstance hero = QuestManager.ActiveQuests[m_CurrentQuest];
            heroSelectedInstance.GetComponentInChildren<ReputationHeroInstanceUI>(true).ApplyHero(hero);
            successText.text = GetSuccessRateForPercentage(m_CurrentQuest.GetHeroSuccessRate(hero));
        }

        private void SetNoHero() {
            heroSelectedInstance.GetComponentInChildren<ReputationHeroInstanceUI>(true).ApplyHero(null);
            successText.text = "-";
        }

        private string GetSuccessRateForPercentage(int percentage) {
            string s;
            if (percentage >= 95) s = "Extremely high";
            else if (percentage >= 85) s = "Very high";
            else if (percentage >= 70) s = "High";
            else if (percentage >= 50) s = "Average";
            else if (percentage >= 30) s = "Low";
            else if (percentage >= 15) s = "Very low";
            else s = "Extremely low";

            //If can see percentages
            if (true) s += " (" + percentage + "%)";
            return s;
        }

        private void RefreshItemRewardDropdown() {
            List<string> itemRewardNames = new List<string>() { "-" };
            foreach(GameItem item in InventoryManager.OwnedItems) {
                itemRewardNames.Add(item.DisplayName);
            }

            heroRewardItemDropdown.ClearOptions();
            heroRewardItemDropdown.AddOptions(itemRewardNames);
            heroRewardItemDropdown.value = 0;
            heroRewardItemDropdown.RefreshShownValue();

            heroRewardItemDropdown.GetComponent<GameItemPopupCaller>().associatedItem = null;
        }

        public void SetItemInstancesOnDropdown() {
            GameItemPopupCaller[] itemInstances = heroRewardItemDropdown.GetComponentsInChildren<GameItemPopupCaller>();
            for (int i = 0; i < itemInstances.Length; i++) {
                if (i == 0 || i == 1) continue;
                itemInstances[i].associatedItem = (InventoryManager.OwnedItems[i - 2]);
            }
        }

        private void CheckPostButtonStatus() {
            bool isPostable = true;
            if (InventoryManager.Gold < m_CurrentQuest.GoldReward.GoldCount && InventoryManager.Gold >= 0) isPostable = false;
            postButton.interactable = isPostable;
        }

        private void CheckAcceptButtonStatus() {
            bool isAcceptable = true;
            if (m_SelectedHero == null) isAcceptable = false;
            acceptButton.interactable = isAcceptable;
        }

        public void Reset() {
            m_AvailableHeroes.Clear();

            foreach(GameObject obj in m_ItemsToDelete) {
                Destroy(obj);
            }
            m_ItemsToDelete.Clear();

            m_SelectedHero = null;
            m_SelectedItemReward = 0;

            heroSelectionInstance.GetComponentInChildren<ReputationHeroInstanceUI>(true).ApplyHero(null);
        }

        public enum QuestMode {
            SETUP,
            HERO_SELECT,
            POSTED_REVIEW,
            ACTIVE_REVIEW,
            COMPLETED
        }

    }

}